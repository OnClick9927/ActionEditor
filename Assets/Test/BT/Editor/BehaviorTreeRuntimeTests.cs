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
            private readonly TestBlackboard value = new TestBlackboard();
            protected override Blackboard blackboard => value;
            internal TestBlackboard Value => value;
        }

        private sealed class CountSuccess : BTAction
        {
            protected override State OnUpdate()
            {
                ((TestBlackboard)blackboard).Executions++;
                return State.Success;
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
            TestTree tree = Build(root, sequence, copy, swap, compare);
            tree.Value.A = 7;
            tree.Value.B = 2;

            Assert.That(tree.Update(), Is.EqualTo(BTNode.State.Success));
            Assert.That(tree.Value.A, Is.EqualTo(7));
            Assert.That(tree.Value.B, Is.EqualTo(7));
        }

        [Test]
        public void CooldownTicks_RoundTripsRemainingTicks()
        {
            TestTree original = BuildCooldown(out BTCooldownTicks cooldown);
            Assert.That(original.Update(), Is.EqualTo(BTNode.State.Success));
            Assert.That(original.Value.Executions, Is.EqualTo(1));
            List<int> status = original.CollectStatus();

            TestTree restored = BuildCooldown(out _);
            restored.ReadStatus(status);
            Assert.That(restored.Update(), Is.EqualTo(BTNode.State.Failure));
            Assert.That(restored.Update(), Is.EqualTo(BTNode.State.Failure));
            Assert.That(restored.Value.Executions, Is.Zero);
            Assert.That(restored.Update(), Is.EqualTo(BTNode.State.Success));
            Assert.That(restored.Value.Executions, Is.EqualTo(1));
            Assert.That(cooldown, Is.Not.Null);
        }

        [Test]
        public void Once_RoundTripsCompletedResultWithoutRunningChildAgain()
        {
            TestTree original = BuildOnce();
            Assert.That(original.Update(), Is.EqualTo(BTNode.State.Success));
            Assert.That(original.Value.Executions, Is.EqualTo(1));
            List<int> status = original.CollectStatus();

            TestTree restored = BuildOnce();
            restored.ReadStatus(status);
            Assert.That(restored.Update(), Is.EqualTo(BTNode.State.Success));
            Assert.That(restored.Value.Executions, Is.Zero);
        }

        [Test]
        public void RuntimeStatusRejectsTruncatedExtraAndInvalidValues()
        {
            TestTree tree = BuildCooldown(out _);
            tree.Update();
            List<int> status = tree.CollectStatus();

            var truncated = new List<int>(status);
            truncated.RemoveAt(truncated.Count - 1);
            Assert.Throws<ArgumentException>(() => tree.ReadStatus(truncated));

            var extra = new List<int>(status) { 123 };
            Assert.Throws<ArgumentException>(() => tree.ReadStatus(extra));

            var invalid = new List<int>(status) { [0] = int.MaxValue };
            Assert.Throws<ArgumentException>(() => tree.ReadStatus(invalid));
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
                TestTree original = Build(root, sequence, wait, action);

                Assert.That(original.Update(), Is.EqualTo(BTNode.State.Running),
                    $"Original tree failed at iteration {iteration}.");
                List<int> status = original.CollectStatus();

                TestTree restored = Build(new BTRoot(), new BTSequence(),
                    new BTWaitTicks { tickCount = 2 }, new CountSuccess());
                restored.ReadStatus(status);
                Assert.That(restored.Update(), Is.EqualTo(BTNode.State.Running),
                    $"Restored tree lost its wait state at iteration {iteration}.");
                Assert.That(restored.Update(), Is.EqualTo(BTNode.State.Success),
                    $"Restored tree diverged at iteration {iteration}.");
                Assert.That(restored.Value.Executions, Is.EqualTo(1));
            }
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

        private static TestTree BuildCooldown(out BTCooldownTicks cooldown)
        {
            var root = new BTRoot();
            cooldown = new BTCooldownTicks { tickCount = 2 };
            return Build(root, cooldown, new CountSuccess());
        }

        private static TestTree BuildOnce()
        {
            var root = new BTRoot();
            return Build(root, new BTOnce(), new CountSuccess());
        }

        private static TestTree Build(params BTNode[] nodes)
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
            tree.PrepareForRuntime(null);
            return tree;
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
