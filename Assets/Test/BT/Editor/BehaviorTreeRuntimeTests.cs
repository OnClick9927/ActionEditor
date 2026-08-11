using System;
using System.Collections.Generic;
using System.Reflection;
using ActionEditor.Nodes.BT;
using NUnit.Framework;

namespace ActionEditor.Nodes.BT.Tests
{
    public sealed class BehaviorTreeRuntimeTests
    {
        private sealed class TestBlackboard : Blackboard
        {
            public int A;
            public int B;
            public int Executions;
        }

        private sealed class TestTree : BTTree
        {
            private readonly TestBlackboard defaults = new TestBlackboard();
            public override Blackboard blackboard => defaults;
            internal TestBlackboard Defaults => defaults;
        }

        private sealed class TestRuntime
        {
            public readonly TestTree Tree;
            public readonly TestBlackboard Value;

            public TestRuntime(TestTree tree, TestBlackboard value)
            {
                Tree = tree;
                Value = value;
            }

            public BTNode.State Update(Blackboard blackboard) =>
                Tree.Update(blackboard);
        }

        private sealed class NullBlackboardTree : BTTree
        {
            public override Blackboard blackboard => null;
        }

        private sealed class CountSuccess : BTAction
        {
            protected override State OnUpdate(Blackboard blackboard)
            {
                ((TestBlackboard)blackboard).Executions++;
                return State.Success;
            }
        }

        private sealed class ThrowOnStop : BTAction
        {
            protected override State OnUpdate(Blackboard blackboard) =>
                State.Success;

            protected override void OnStop(Blackboard blackboard) =>
                throw new InvalidOperationException("stop failed");
        }

        private sealed class ThrowOnAbort : BTAction
        {
            protected override State OnUpdate(Blackboard blackboard) =>
                State.Running;

            protected override void OnAbort(Blackboard blackboard) =>
                throw new InvalidOperationException("abort failed");
        }

        private sealed class NonZeroInitialValueAction : BTAction
        {
            protected override int RuntimeDataSize => 1;
            protected override int GetInitialRuntimeData(int index) => 7;
            protected override int GetMinRuntimeData(int index) => 0;
            protected override int GetMaxRuntimeData(int index) => 7;

            protected override State OnUpdate(Blackboard blackboard)
            {
                int value = GetRuntimeData(blackboard, 0);
                SetRuntimeData(blackboard, 0, 0);
                return value == 7 ? State.Success : State.Failure;
            }
        }

        [AssetFileExtension("custom.graph.data")]
        private sealed class CustomGraph : GraphAsset
        {
        }

        private sealed class DefaultGraph : GraphAsset
        {
        }

        [Test]
        public void VariableNodes_CopySwapAndCompareDeterministically()
        {
            var root = new BTRoot();
            var sequence = new BTSequence();
            var copy = new BTCopyVariable
            {
                sourceField = nameof(TestBlackboard.A),
                destinationField = nameof(TestBlackboard.B)
            };
            var swap = new BTSwapVariables
            {
                firstField = nameof(TestBlackboard.A),
                secondField = nameof(TestBlackboard.B)
            };
            var compare = new BTCompareVariables
            {
                firstField = nameof(TestBlackboard.A),
                secondField = nameof(TestBlackboard.B),
                comparison = BTCompareVariables.Comparison.Equal
            };
            TestRuntime tree = Build(root, sequence, copy, swap, compare);
            tree.Value.A = 7;
            tree.Value.B = 2;

            Assert.That(tree.Update(tree.Value), Is.EqualTo(BTNode.State.Success));
            Assert.That(tree.Value.A, Is.EqualTo(7));
            Assert.That(tree.Value.B, Is.EqualTo(7));
        }

        [Test]
        public void CooldownTicks_RoundTripsRemainingTicks()
        {
            TestRuntime original = BuildCooldown(out BTCooldownTicks cooldown);
            Assert.That(original.Update(original.Value), Is.EqualTo(BTNode.State.Success));
            Assert.That(original.Value.Executions, Is.EqualTo(1));
            IReadOnlyList<int> status = original.Value.RuntimeValues;

            TestRuntime restored = BuildCooldown(out _);
            restored.Value.Initialize(restored.Tree, status);
            Assert.That(restored.Update(restored.Value), Is.EqualTo(BTNode.State.Failure));
            Assert.That(restored.Update(restored.Value), Is.EqualTo(BTNode.State.Failure));
            Assert.That(restored.Value.Executions, Is.Zero);
            Assert.That(restored.Update(restored.Value), Is.EqualTo(BTNode.State.Success));
            Assert.That(restored.Value.Executions, Is.EqualTo(1));
            Assert.That(cooldown, Is.Not.Null);
        }

        [Test]
        public void Once_RoundTripsCompletedResultWithoutRunningChildAgain()
        {
            TestRuntime original = BuildOnce();
            Assert.That(original.Update(original.Value), Is.EqualTo(BTNode.State.Success));
            Assert.That(original.Value.Executions, Is.EqualTo(1));
            IReadOnlyList<int> status = original.Value.RuntimeValues;

            TestRuntime restored = BuildOnce();
            restored.Value.Initialize(restored.Tree, status);
            Assert.That(restored.Update(restored.Value), Is.EqualTo(BTNode.State.Success));
            Assert.That(restored.Value.Executions, Is.Zero);
        }

        [Test]
        public void RuntimeStatusRejectsTruncatedExtraAndInvalidValues()
        {
            TestRuntime tree = BuildCooldown(out _);
            tree.Update(tree.Value);
            var status = new List<int>(tree.Value.RuntimeValues);

            var truncated = new List<int>(status);
            truncated.RemoveAt(truncated.Count - 1);
            Assert.Throws<ArgumentException>(() =>
                tree.Value.Initialize(tree.Tree, truncated));

            var extra = new List<int>(status) { 123 };
            Assert.Throws<ArgumentException>(() =>
                tree.Value.Initialize(tree.Tree, extra));

            var invalid = new List<int>(status) { [0] = int.MaxValue };
            Assert.Throws<ArgumentException>(() =>
                tree.Value.Initialize(tree.Tree, invalid));
        }

        [Test]
        public void OneThousandRuntimeSnapshotsRestoreDeterministically()
        {
            for (int iteration = 0; iteration < 1000; iteration++)
            {
                var root = new BTRoot();
                var sequence = new BTSequence();
                var wait = new BTWaitTicks { tickCount = 2 };
                var action = new CountSuccess();
                TestRuntime original = Build(root, sequence, wait, action);

                Assert.That(original.Update(original.Value), Is.EqualTo(BTNode.State.Running),
                    $"Original tree failed at iteration {iteration}.");
                IReadOnlyList<int> status = original.Value.RuntimeValues;

                TestRuntime restored = Build(new BTRoot(), new BTSequence(),
                    new BTWaitTicks { tickCount = 2 }, new CountSuccess());
                restored.Value.Initialize(restored.Tree, status);
                Assert.That(restored.Update(restored.Value), Is.EqualTo(BTNode.State.Running),
                    $"Restored tree lost its wait state at iteration {iteration}.");
                Assert.That(restored.Update(restored.Value), Is.EqualTo(BTNode.State.Success),
                    $"Restored tree diverged at iteration {iteration}.");
                Assert.That(restored.Value.Executions, Is.EqualTo(1));
            }
        }

        [Test]
        public void SharedTreeUsesIndependentRuntimeStateAndBlackboards()
        {
            var root = new BTRoot();
            var sequence = new BTSequence();
            var wait = new BTWaitTicks { tickCount = 1 };
            var action = new CountSuccess();
            TestRuntime tree = Build(root, sequence, wait, action);
            var firstBlackboard = new TestBlackboard();
            var secondBlackboard = new TestBlackboard();
            firstBlackboard.Initialize(tree.Tree);
            secondBlackboard.Initialize(tree.Tree);

            Assert.That(tree.Update(firstBlackboard), Is.EqualTo(BTNode.State.Running));
            Assert.That(firstBlackboard.GetState(wait), Is.EqualTo(BTNode.State.Running));
            Assert.That(secondBlackboard.GetState(wait), Is.EqualTo(BTNode.State.Inactive));

            Assert.That(tree.Update(firstBlackboard), Is.EqualTo(BTNode.State.Success));
            Assert.That(firstBlackboard.Executions, Is.EqualTo(1));
            Assert.That(secondBlackboard.Executions, Is.Zero);

            Assert.That(tree.Update(secondBlackboard), Is.EqualTo(BTNode.State.Running));
            Assert.That(tree.Update(secondBlackboard), Is.EqualTo(BTNode.State.Success));
            Assert.That(secondBlackboard.Executions, Is.EqualTo(1));
        }

        [Test]
        public void SelfAbortOnlyStopsRunningBranchAfterConditionFails()
        {
            var root = new BTRoot();
            var sequence = new BTSequence
            {
                abortType = BTComposite.AbortType.Self
            };
            var condition = new BTVariableCondition
            {
                fieldName = nameof(TestBlackboard.A),
                variableType = BTVariableCondition.VariableType.Int,
                compareType = BTVariableCondition.CompareType.GreaterThan,
                intValue = 0
            };
            var wait = new BTWaitTicks { tickCount = 2 };
            TestRuntime runtime = Build(root, sequence, condition, wait);
            runtime.Value.A = 1;

            Assert.That(runtime.Update(runtime.Value),
                Is.EqualTo(BTNode.State.Running));
            Assert.That(runtime.Update(runtime.Value),
                Is.EqualTo(BTNode.State.Running));
            Assert.That(runtime.Update(runtime.Value),
                Is.EqualTo(BTNode.State.Success));

            Assert.That(runtime.Update(runtime.Value),
                Is.EqualTo(BTNode.State.Running));
            runtime.Value.A = 0;
            Assert.That(runtime.Update(runtime.Value),
                Is.EqualTo(BTNode.State.Failure));
            Assert.That(runtime.Value.GetState(wait),
                Is.EqualTo(BTNode.State.Inactive));
        }

        [Test]
        public void LowerPriorityAbortStopsRunningLowerBranchWhenConditionRecovers()
        {
            var root = new BTRoot();
            var selector = new BTSelector();
            var highPriority = new BTSequence
            {
                abortType = BTComposite.AbortType.LowerPriority
            };
            var condition = new BTVariableCondition
            {
                fieldName = nameof(TestBlackboard.A),
                variableType = BTVariableCondition.VariableType.Int,
                compareType = BTVariableCondition.CompareType.GreaterThan,
                intValue = 0
            };
            var action = new CountSuccess();
            var lowPriority = new BTWaitTicks { tickCount = 10 };
            TestRuntime runtime = BuildGraph(
                new List<BTNode>
                {
                    root, selector, highPriority, condition, action,
                    lowPriority
                },
                Connect(root, selector),
                Connect(selector, highPriority),
                Connect(selector, lowPriority),
                Connect(highPriority, condition),
                Connect(highPriority, action));

            runtime.Value.A = 0;
            Assert.That(runtime.Update(runtime.Value),
                Is.EqualTo(BTNode.State.Running));
            Assert.That(runtime.Value.GetState(lowPriority),
                Is.EqualTo(BTNode.State.Running));

            runtime.Value.A = 1;
            Assert.That(runtime.Update(runtime.Value),
                Is.EqualTo(BTNode.State.Success));
            Assert.That(runtime.Value.GetState(lowPriority),
                Is.EqualTo(BTNode.State.Inactive));
            Assert.That(runtime.Value.Executions, Is.EqualTo(1));
        }

        [Test]
        public void ReactiveSelectorOnlyWritesAndAbortsWhenBranchChanges()
        {
            var root = new BTRoot();
            var selector = new BTReactiveSelector();
            var condition = new BTVariableCondition
            {
                fieldName = nameof(TestBlackboard.A),
                variableType = BTVariableCondition.VariableType.Int,
                compareType = BTVariableCondition.CompareType.GreaterThan,
                intValue = 0
            };
            var wait = new BTWaitTicks { tickCount = 10 };
            TestRuntime runtime = BuildGraph(
                new List<BTNode> { root, selector, condition, wait },
                Connect(root, selector),
                Connect(selector, condition),
                Connect(selector, wait));

            runtime.Value.A = 0;
            Assert.That(runtime.Update(runtime.Value),
                Is.EqualTo(BTNode.State.Running));
            Assert.That(runtime.Update(runtime.Value),
                Is.EqualTo(BTNode.State.Running));
            Assert.That(runtime.Value.GetState(wait),
                Is.EqualTo(BTNode.State.Running));

            runtime.Value.A = 1;
            Assert.That(runtime.Update(runtime.Value),
                Is.EqualTo(BTNode.State.Success));
            Assert.That(runtime.Value.GetState(wait),
                Is.EqualTo(BTNode.State.Inactive));
        }

        [Test]
        public void LifecycleExceptionsStillResetNodeState()
        {
            var stop = new ThrowOnStop();
            TestRuntime stopRuntime = Build(new BTRoot(), stop);
            Assert.Throws<InvalidOperationException>(() =>
                stopRuntime.Update(stopRuntime.Value));
            Assert.That(stopRuntime.Value.GetState(stop),
                Is.EqualTo(BTNode.State.Inactive));

            var abort = new ThrowOnAbort();
            TestRuntime abortRuntime = Build(new BTRoot(), abort);
            Assert.That(abortRuntime.Update(abortRuntime.Value),
                Is.EqualTo(BTNode.State.Running));
            Assert.Throws<InvalidOperationException>(() =>
                abortRuntime.Tree.Abort(abortRuntime.Value));
            Assert.That(abortRuntime.Value.GetState(abort),
                Is.EqualTo(BTNode.State.Inactive));
        }

        [Test]
        public void GetStateReturnsNullWhenStateIsUnavailable()
        {
            var wait = new BTWaitTicks { tickCount = 1 };
            TestRuntime runtime = Build(new BTRoot(), wait);

            Assert.That(new TestBlackboard().GetState(wait), Is.Null);
            Assert.That(runtime.Value.GetState(new BTWaitTicks()), Is.Null);
            Assert.That(runtime.Value.GetState(null), Is.Null);
        }

        [Test]
        public void BlackboardPublicRuntimeApiRequiresTreeContext()
        {
            const BindingFlags publicInstance = BindingFlags.Instance |
                BindingFlags.Public | BindingFlags.DeclaredOnly;
            const BindingFlags nonPublicInstance = BindingFlags.Instance |
                BindingFlags.NonPublic | BindingFlags.DeclaredOnly;

            Assert.That(typeof(Blackboard).GetMethod("Update", publicInstance),
                Is.Null);
            Assert.That(typeof(Blackboard).GetMethod("Abort", publicInstance),
                Is.Null);
            Assert.That(typeof(Blackboard).GetMethod("PushEvent", publicInstance),
                Is.Null);
            Assert.That(typeof(Blackboard).GetMethod("TryGetState", publicInstance),
                Is.Null);
            Assert.That(typeof(Blackboard).GetMethod("GetState", publicInstance)
                ?.ReturnType, Is.EqualTo(typeof(BTNode.State?)));

            int abortMethodCount = 0;
            MethodInfo[] methods = typeof(Blackboard).GetMethods(
                nonPublicInstance);
            for (int i = 0; i < methods.Length; i++)
            {
                if (methods[i].Name == "Abort") abortMethodCount++;
                Assert.That(methods[i].Name.EndsWith("Core",
                    StringComparison.Ordinal), Is.False);
                if (methods[i].Name == "Update" ||
                    methods[i].Name == "Abort" ||
                    methods[i].Name == "PushEvent")
                    Assert.That(Array.Exists(methods[i].GetParameters(),
                        parameter => parameter.ParameterType == typeof(BTTree)),
                        Is.False);
            }
            Assert.That(abortMethodCount, Is.EqualTo(2));
        }

        [Test]
        public void CopyFieldsFromCopiesFieldsAndRuntimeValues()
        {
            var wait = new BTWaitTicks { tickCount = 2 };
            TestRuntime runtime = Build(new BTRoot(), wait);
            runtime.Value.A = 11;
            runtime.Value.B = 23;
            runtime.Value.Executions = 5;
            Assert.That(runtime.Update(runtime.Value),
                Is.EqualTo(BTNode.State.Running));

            var copied = new TestBlackboard();
            copied.Initialize(runtime.Tree);
            copied.CopyFieldsFrom(runtime.Value);

            Assert.That(copied.A, Is.EqualTo(11));
            Assert.That(copied.B, Is.EqualTo(23));
            Assert.That(copied.Executions, Is.EqualTo(5));
            Assert.That(copied.RuntimeValues,
                Is.Not.SameAs(runtime.Value.RuntimeValues));
            Assert.That(copied.RuntimeValues,
                Is.EqualTo(runtime.Value.RuntimeValues));
            Assert.That(copied.GetState(wait), Is.EqualTo(BTNode.State.Running));
            Assert.That(runtime.Value.GetState(wait),
                Is.EqualTo(BTNode.State.Running));

            Assert.That(runtime.Update(copied), Is.EqualTo(BTNode.State.Running));
            Assert.That(copied.RuntimeValues,
                Is.Not.EqualTo(runtime.Value.RuntimeValues));
        }

        [Test]
        public void CopyFieldsFromRequiresInitializedDestinationForRuntimeValues()
        {
            TestRuntime runtime = Build(new BTRoot(),
                new BTWaitTicks { tickCount = 1 });
            var copied = new TestBlackboard();

            Assert.Throws<InvalidOperationException>(() =>
                copied.CopyFieldsFrom(runtime.Value));
        }

        [Test]
        public void SharedBlackboardsReuseTreeRuntimeLayout()
        {
            TestRuntime runtime = Build(new BTRoot(),
                new BTWaitTicks { tickCount = 1 });
            var second = new TestBlackboard();
            second.Initialize(runtime.Tree);

            const BindingFlags flags = BindingFlags.Instance |
                BindingFlags.NonPublic;
            FieldInfo treeLayoutField = typeof(BTTree).GetField(
                "_runtimeLayout", flags);
            FieldInfo blackboardLayoutField = typeof(Blackboard).GetField(
                "runtimeLayout", flags);
            object treeLayout = treeLayoutField.GetValue(runtime.Tree);
            Assert.That(blackboardLayoutField.GetValue(runtime.Value),
                Is.SameAs(treeLayout));
            Assert.That(blackboardLayoutField.GetValue(second),
                Is.SameAs(treeLayout));

            int runtimeArrayCount = 0;
            FieldInfo[] fields = typeof(Blackboard).GetFields(
                flags | BindingFlags.DeclaredOnly);
            for (int i = 0; i < fields.Length; i++)
            {
                if (fields[i].FieldType != typeof(int[])) continue;
                runtimeArrayCount++;
                Assert.That(fields[i].Name, Is.EqualTo("runtimeValues"));
            }
            Assert.That(runtimeArrayCount, Is.EqualTo(1));
        }

        [Test]
        public void BlackboardInitializationUsesTreeDefaultsWhenSourceIsMissing()
        {
            TestRuntime runtime = Build(new BTRoot(),
                new NonZeroInitialValueAction());
            Assert.That(runtime.Update(runtime.Value),
                Is.EqualTo(BTNode.State.Success));

            runtime.Value.Initialize(runtime.Tree);
            Assert.That(runtime.Update(runtime.Value),
                Is.EqualTo(BTNode.State.Success));
        }

        [Test]
        public void BlackboardInitializationCanRestoreAnotherRuntimeValues()
        {
            var root = new BTRoot();
            var wait = new BTWaitTicks { tickCount = 2 };
            TestRuntime runtime = Build(root, wait);
            Assert.That(runtime.Update(runtime.Value),
                Is.EqualTo(BTNode.State.Running));

            var restored = new TestBlackboard();
            restored.Initialize(runtime.Tree, runtime.Value.RuntimeValues);

            Assert.That(restored.GetState(wait), Is.EqualTo(BTNode.State.Running));
            Assert.That(runtime.Update(restored), Is.EqualTo(BTNode.State.Running));
            Assert.That(runtime.Update(restored), Is.EqualTo(BTNode.State.Success));
        }

        [Test]
        public void RuntimeDebugInstanceTracksTheExplicitBlackboard()
        {
            var root = new BTRoot();
            var wait = new BTWaitTicks { tickCount = 1 };
            TestRuntime tree = Build(root, wait);
            var first = new TestBlackboard();
            var second = new TestBlackboard();
            first.Initialize(tree.Tree);
            second.Initialize(tree.Tree);
            Assert.That(tree.Update(first), Is.EqualTo(BTNode.State.Running));

            BTTree.ClearInstance();
            int changeCount = 0;
            BTTree changedTree = null;
            Blackboard changedBlackboard = null;
            Action<BTTree, Blackboard> handler = (changed, runtime) =>
            {
                changeCount++;
                changedTree = changed;
                changedBlackboard = runtime;
            };
            BTTree.onInstanceChanged += handler;
            try
            {
                BTTree.SetAsInstance(tree.Tree, first);
                Assert.That(BTTree.instance, Is.SameAs(tree.Tree));
                Assert.That(BTTree.instanceBlackboard, Is.SameAs(first));
                Assert.That(changedTree, Is.SameAs(tree.Tree));
                Assert.That(changedBlackboard, Is.SameAs(first));
                Assert.That(first.GetState(wait),
                    Is.EqualTo(BTNode.State.Running));

                BTTree.SetAsInstance(tree.Tree, second);
                Assert.That(BTTree.instance, Is.SameAs(tree.Tree));
                Assert.That(BTTree.instanceBlackboard, Is.SameAs(second));
                Assert.That(changedTree, Is.SameAs(tree.Tree));
                Assert.That(changedBlackboard, Is.SameAs(second));
                Assert.That(second.GetState(wait),
                    Is.EqualTo(BTNode.State.Inactive));
                Assert.That(changeCount, Is.EqualTo(2));
            }
            finally
            {
                BTTree.onInstanceChanged -= handler;
                BTTree.ClearInstance();
            }
        }

        [Test]
        public void RuntimeInitializationDoesNotUseEditorBlackboardHelper()
        {
            var root = new BTRoot();
            var wait = new BTWaitTicks { tickCount = 1 };
            var tree = new NullBlackboardTree();
            SetGraphData(tree, new List<NodeData> { root, wait },
                new List<GroupData>(),
                new List<ConnectionData> { Connect(root, wait) });

            Assert.DoesNotThrow(() => tree.PrepareForRuntime());
            Assert.That(root.child, Is.SameAs(wait));
            var runtimeBlackboard = new TestBlackboard();
            Assert.DoesNotThrow(() => runtimeBlackboard.Initialize(tree));
            Assert.That(runtimeBlackboard.IsInitializedFor(tree), Is.True);
            Assert.That(tree.Update(runtimeBlackboard),
                Is.EqualTo(BTNode.State.Running));
        }

        [Test]
        public void PrepareForRuntimeUsesStaticSubTreeLoader()
        {
            const string subTreePath = "subtree.bt.bytes";
            var childRoot = new BTRoot();
            var wait = new BTWaitTicks { tickCount = 1 };
            var childTree = new TestTree { IsSubTree = true };
            SetGraphData(childTree, new List<NodeData> { childRoot, wait },
                new List<GroupData>(),
                new List<ConnectionData> { Connect(childRoot, wait) });

            var root = new BTRoot();
            var subTree = new BTSubTree { path = subTreePath };
            var tree = new TestTree();
            SetGraphData(tree, new List<NodeData> { root, subTree },
                new List<GroupData>(),
                new List<ConnectionData> { Connect(root, subTree) });

            Func<string, BTTree> previousLoader = BTTree.loader;
            int loadCount = 0;
            try
            {
                BTTree.loader = path =>
                {
                    loadCount++;
                    return path == subTreePath ? childTree : null;
                };

                tree.PrepareForRuntime();

                Assert.That(loadCount, Is.EqualTo(1));
                Assert.That(subTree.tree, Is.SameAs(childTree));
                Assert.That(subTree.runtimeNode, Is.SameAs(wait));
                Assert.That(tree.root.child, Is.SameAs(wait));
                Assert.That(childTree.parent, Is.SameAs(tree));
            }
            finally
            {
                BTTree.loader = previousLoader;
            }
        }

        [Test]
        public void PrepareForRuntimeHasOnlyTheParameterlessPublicEntry()
        {
            Assert.That(typeof(BTTree).GetField(nameof(BTTree.loader),
                BindingFlags.Public | BindingFlags.Static), Is.Not.Null);
            Assert.That(typeof(BTTree).GetMethod(nameof(BTTree.PrepareForRuntime),
                new[] { typeof(Func<string, BTTree>) }), Is.Null);
            Assert.That(typeof(BTTree).GetMethod(nameof(BTTree.PrepareForRuntime),
                Type.EmptyTypes), Is.Not.Null);
        }

        [Test]
        public void SemaphoreOccupancyIsIndependentForEachBlackboard()
        {
            var root = new BTRoot();
            var semaphore = new BTSemaphore { semaphore = 0, wait = false };
            var wait = new BTWaitTicks { tickCount = 2 };
            var tree = new TestTree();
            tree.semaphores.Add(new BTTree.Semaphore { max = 1 });
            SetGraphData(tree, new List<NodeData> { root, semaphore, wait },
                new List<GroupData>(), new List<ConnectionData>
                {
                    Connect(root, semaphore),
                    Connect(semaphore, wait)
                });
            tree.PrepareForRuntime();

            var first = new TestBlackboard();
            var second = new TestBlackboard();
            first.Initialize(tree);
            second.Initialize(tree);

            Assert.That(tree.Update(first), Is.EqualTo(BTNode.State.Running));
            Assert.That(tree.Update(second), Is.EqualTo(BTNode.State.Running));
            tree.Abort(first);
            Assert.That(tree.Update(second), Is.EqualTo(BTNode.State.Running));
            Assert.That(tree.Update(second), Is.EqualTo(BTNode.State.Success));
        }

        [Test]
        public void RuntimeValuesCopyFlatStorageBetweenSharedInstances()
        {
            var root = new BTRoot();
            var wait = new BTWaitTicks { tickCount = 2 };
            TestRuntime tree = Build(root, wait);
            var source = new TestBlackboard();
            var restored = new TestBlackboard();
            source.Initialize(tree.Tree);
            restored.Initialize(tree.Tree);

            Assert.That(tree.Update(source), Is.EqualTo(BTNode.State.Running));
            restored.Initialize(tree.Tree, source.RuntimeValues);
            Assert.That(tree.Update(restored), Is.EqualTo(BTNode.State.Running));
            Assert.That(tree.Update(restored), Is.EqualTo(BTNode.State.Success));
        }

        [Test]
        public void NodeTriggeredEventTargetsTheActiveSharedRuntime()
        {
            const string eventName = "ready";
            var root = new BTRoot();
            var sequence = new BTSequence();
            var push = new BTPushEvent { eventName = eventName };
            var wait = new BTWaitEvent { eventName = eventName };
            TestRuntime tree = Build(root, sequence, push, wait);
            var first = new TestBlackboard();
            var second = new TestBlackboard();
            first.Initialize(tree.Tree);
            second.Initialize(tree.Tree);

            Assert.That(tree.Update(first), Is.EqualTo(BTNode.State.Success));
            Assert.That(second.GetState(wait), Is.EqualTo(BTNode.State.Inactive));
            Assert.That(tree.Update(second), Is.EqualTo(BTNode.State.Success));
        }

        [Test]
        public void RegenerateGuids_RemapsConnectionsAndGroupMembers()
        {
            var graph = new DefaultGraph();
            var first = new NodeData();
            var second = new NodeData();
            var group = new GroupData();
            group._nodes.Add(first.guid);
            var connection = Connect(first, second);
            SetGraphData(graph, new List<NodeData> { first, second },
                new List<GroupData> { group },
                new List<ConnectionData> { connection });
            string assetGuid = graph.guid;
            string firstGuid = first.guid;

            graph.RegenerateGuids();

            Assert.That(graph.guid, Is.Not.EqualTo(assetGuid));
            Assert.That(first.guid, Is.Not.EqualTo(firstGuid));
            Assert.That(connection.outNodeGuid, Is.EqualTo(first.guid));
            Assert.That(connection.InNodeGuid, Is.EqualTo(second.guid));
            Assert.That(group.nodes[0], Is.EqualTo(first.guid));
        }

        [Test]
        public void AssetFileExtension_IsInheritedAndCanBeOverridden()
        {
            Assert.That(AssetFileExtensionUtility.Get(typeof(TestTree)),
                Is.EqualTo("bt.bytes"));
            Assert.That(AssetFileExtensionUtility.Get(typeof(DefaultGraph)),
                Is.EqualTo("bytes"));
            Assert.That(AssetFileExtensionUtility.Get(typeof(CustomGraph)),
                Is.EqualTo("custom.graph.data"));
            Assert.That(AssetFileExtensionUtility.Matches("tree.bt.bytes",
                typeof(TestTree)), Is.True);
            Assert.That(AssetFileExtensionUtility.Matches("tree.graph.bytes",
                typeof(TestTree)), Is.False);
            Assert.That(AssetFileExtensionUtility.WithExtension("test.bytes",
                typeof(CustomGraph)), Is.EqualTo("test.custom.graph.data"));
        }

        private static TestRuntime BuildCooldown(out BTCooldownTicks cooldown)
        {
            var root = new BTRoot();
            cooldown = new BTCooldownTicks { tickCount = 2 };
            return Build(root, cooldown, new CountSuccess());
        }

        private static TestRuntime BuildOnce()
        {
            var root = new BTRoot();
            return Build(root, new BTOnce(), new CountSuccess());
        }

        private static TestRuntime Build(params BTNode[] nodes)
        {
            var connections = new List<ConnectionData>();
            for (int i = 0; i < nodes.Length - 1; i++)
            {
                if (nodes[i] is BTSequence && i + 1 < nodes.Length)
                {
                    for (int child = i + 1; child < nodes.Length; child++)
                        connections.Add(Connect(nodes[i], nodes[child]));
                    break;
                }
                connections.Add(Connect(nodes[i], nodes[i + 1]));
            }
            var tree = new TestTree();
            SetGraphData(tree, new List<NodeData>(nodes),
                new List<GroupData>(), connections);
            tree.PrepareForRuntime();
            var runtimeBlackboard = new TestBlackboard();
            runtimeBlackboard.Initialize(tree);
            Assert.That(tree.Defaults.IsInitializedFor(tree), Is.False);
            return new TestRuntime(tree, runtimeBlackboard);
        }

        private static TestRuntime BuildGraph(List<BTNode> nodes,
            params ConnectionData[] connections)
        {
            var tree = new TestTree();
            SetGraphData(tree, new List<NodeData>(nodes),
                new List<GroupData>(), new List<ConnectionData>(connections));
            tree.PrepareForRuntime();
            var runtimeBlackboard = new TestBlackboard();
            runtimeBlackboard.Initialize(tree);
            return new TestRuntime(tree, runtimeBlackboard);
        }

        private static ConnectionData Connect(NodeData output, NodeData input)
        {
            return new ConnectionData
            {
                outNodeGuid = output.guid,
                InNodeGuid = input.guid,
                outPortType = typeof(BTNode).AssemblyQualifiedName,
                inPortType = typeof(BTNode).AssemblyQualifiedName,
                outputPortName = "Out",
                InPortName = "In"
            };
        }

        private static void SetGraphData(GraphAsset graph,
            List<NodeData> nodes, List<GroupData> groups,
            List<ConnectionData> connections)
        {
            const BindingFlags flags = BindingFlags.Instance |
                BindingFlags.NonPublic;
            typeof(GraphAsset).GetField("_nodes", flags).SetValue(graph, nodes);
            typeof(GraphAsset).GetField("_groups", flags).SetValue(graph, groups);
            typeof(GraphAsset).GetField("_connections", flags).SetValue(graph,
                connections);
        }
    }
}
