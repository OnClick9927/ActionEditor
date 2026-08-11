using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using ActionAttribute;
using ActionEditor.Nodes.BT;
using NUnit.Framework;

namespace ActionEditor.Nodes.BT.Tests
{
    public sealed class BehaviorTreeVariableTests
    {
        private const BindingFlags PrivateInstance =
            BindingFlags.Instance | BindingFlags.NonPublic;

        private sealed class VariableBlackboard : Blackboard
        {
            public bool BoolValue;
            public int IntValue;
            public float FloatValue;
            public string StringValue;

            public long LongValue;
            public double DoubleValue;
            public decimal DecimalValue;
            public char CharValue;
            public DayOfWeek EnumValue;
            public object ObjectValue;
        }

        private sealed class VariableTree : BTTree
        {
            private readonly VariableBlackboard defaults =
                new VariableBlackboard();

            public override Blackboard blackboard => defaults;
        }

        private sealed class VariableRuntime
        {
            public readonly VariableTree Tree;
            public readonly VariableBlackboard Value;

            public VariableRuntime(VariableTree tree, VariableBlackboard value)
            {
                Tree = tree;
                Value = value;
            }

            public BTNode.State Update(Blackboard blackboard) =>
                Tree.Update(blackboard);
        }

        [Test]
        public void GetVariableType_MapsOnlySupportedTypes()
        {
            Assert.That(BTVariableCondition.GetVariableType(typeof(bool)),
                Is.EqualTo(BTVariableCondition.VariableType.Bool));
            Assert.That(BTVariableCondition.GetVariableType(typeof(int)),
                Is.EqualTo(BTVariableCondition.VariableType.Int));
            Assert.That(BTVariableCondition.GetVariableType(typeof(float)),
                Is.EqualTo(BTVariableCondition.VariableType.Float));
            Assert.That(BTVariableCondition.GetVariableType(typeof(string)),
                Is.EqualTo(BTVariableCondition.VariableType.String));
            Assert.That(BTVariableCondition.GetVariableType(typeof(DayOfWeek)),
                Is.EqualTo(BTVariableCondition.VariableType.Enum));

            Type[] unsupportedTypes =
            {
                typeof(sbyte), typeof(byte), typeof(short), typeof(ushort),
                typeof(uint), typeof(long), typeof(ulong), typeof(double),
                typeof(decimal), typeof(char), typeof(object)
            };
            foreach (Type type in unsupportedTypes)
            {
                Assert.That(BTVariableCondition.GetVariableType(type),
                    Is.EqualTo(BTVariableCondition.VariableType.None),
                    $"{type} must not be exposed by variable nodes.");
            }
            Assert.That(BTVariableCondition.GetVariableType(null),
                Is.EqualTo(BTVariableCondition.VariableType.None));
            Assert.That((int)BTVariableCondition.VariableType.String,
                Is.EqualTo(15), "String's serialized enum value changed.");
            Assert.That((int)BTVariableCondition.VariableType.Enum,
                Is.EqualTo(16), "Enum's serialized enum value changed.");
        }

        [Test]
        public void SetVariable_OperationMatrixMatchesSupportedTypes()
        {
            Assert.That(GetSupportedOperations(
                    BTVariableCondition.VariableType.Bool),
                Is.EqualTo(new[]
                {
                    BTSetVariable.SetVariableType.Set,
                    BTSetVariable.SetVariableType.Not
                }));
            Assert.That(GetSupportedOperations(
                    BTVariableCondition.VariableType.String),
                Is.EqualTo(new[]
                {
                    BTSetVariable.SetVariableType.Set,
                    BTSetVariable.SetVariableType.Add
                }));
            Assert.That(GetSupportedOperations(
                    BTVariableCondition.VariableType.Enum),
                Is.EqualTo(new[] { BTSetVariable.SetVariableType.Set }));

            BTSetVariable.SetVariableType[] numeric =
                Enum.GetValues(typeof(BTSetVariable.SetVariableType))
                    .Cast<BTSetVariable.SetVariableType>()
                    .Where(value => value != BTSetVariable.SetVariableType.Not)
                    .ToArray();
            Assert.That(GetSupportedOperations(
                    BTVariableCondition.VariableType.Int),
                Is.EqualTo(numeric));
            Assert.That(GetSupportedOperations(
                    BTVariableCondition.VariableType.Float),
                Is.EqualTo(numeric));
            Assert.That(GetSupportedOperations(
                    BTVariableCondition.VariableType.None), Is.Empty);
        }

        [Test]
        public void InspectorFieldDropdowns_ContainOnlySupportedFields()
        {
            string[] expected =
            {
                nameof(VariableBlackboard.BoolValue),
                nameof(VariableBlackboard.IntValue),
                nameof(VariableBlackboard.FloatValue),
                nameof(VariableBlackboard.StringValue),
                nameof(VariableBlackboard.EnumValue)
            };

            var condition = new BTVariableCondition();
            ((IBTInspectorContext)condition).SetInspectorBlackboard(
                typeof(VariableBlackboard));
            Assert.That(GetInspectorFieldNames(condition),
                Is.EquivalentTo(expected));

            var setVariable = new BTSetVariable();
            ((IBTInspectorContext)setVariable).SetInspectorBlackboard(
                typeof(VariableBlackboard));
            Assert.That(GetInspectorFieldNames(setVariable),
                Is.EquivalentTo(expected));
        }

        [TestCase(typeof(BTVariableCondition))]
        [TestCase(typeof(BTSetVariable))]
        public void BoolValue_IsSerializableFieldWithCorrectVisibilityCondition(
            Type nodeType)
        {
            FieldInfo boolValue = nodeType.GetField("boolValue",
                BindingFlags.Instance | BindingFlags.Public);

            Assert.That(boolValue, Is.Not.Null);
            Assert.That(boolValue.FieldType, Is.EqualTo(typeof(bool)));
            Assert.That(boolValue.IsNotSerialized, Is.False);
            Assert.That(nodeType.GetProperty("boolValue",
                BindingFlags.Instance | BindingFlags.Public), Is.Null,
                "boolValue must not be an int-backed compatibility property.");
            AssertVisibilityCondition(boolValue,
                "ShowBoolInspectorValue");
        }

        [TestCase(typeof(BTVariableCondition))]
        [TestCase(typeof(BTSetVariable))]
        public void OperandFields_UseTheirMatchingVisibilityConditions(
            Type nodeType)
        {
            AssertVisibilityCondition(nodeType.GetField("intValue"),
                "ShowIntInspectorValue");
            AssertVisibilityCondition(nodeType.GetField("floatValue"),
                "ShowFloatInspectorValue");
            AssertVisibilityCondition(nodeType.GetField("stringValue"),
                "ShowStringInspectorValue");
            AssertVisibilityCondition(nodeType.GetField("enumValue"),
                "ShowEnumInspectorValue");
        }

        [Test]
        public void VariableCondition_InspectorSelectionSynchronizesAndClearsType()
        {
            var node = new BTVariableCondition();
            AssertConditionSelection(node, nameof(VariableBlackboard.BoolValue),
                BTVariableCondition.VariableType.Bool,
                "ShowBoolInspectorValue");
            AssertConditionSelection(node, nameof(VariableBlackboard.IntValue),
                BTVariableCondition.VariableType.Int,
                "ShowIntInspectorValue");
            AssertConditionSelection(node, nameof(VariableBlackboard.FloatValue),
                BTVariableCondition.VariableType.Float,
                "ShowFloatInspectorValue");
            AssertConditionSelection(node, nameof(VariableBlackboard.StringValue),
                BTVariableCondition.VariableType.String,
                "ShowStringInspectorValue");
            AssertConditionSelection(node, nameof(VariableBlackboard.EnumValue),
                BTVariableCondition.VariableType.Enum,
                "ShowEnumInspectorValue");
            Assert.That(node.enumValue, Is.EqualTo(nameof(DayOfWeek.Sunday)));
            Assert.That(GetInspectorValues<string>(node,
                    "InspectorEnumValues"),
                Is.EqualTo(Enum.GetNames(typeof(DayOfWeek))));

            node.fieldName = nameof(VariableBlackboard.LongValue);
            ((IBTInspectorContext)node).SetInspectorBlackboard(
                typeof(VariableBlackboard));
            Assert.That(node.variableType,
                Is.EqualTo(BTVariableCondition.VariableType.None));
            AssertAllOperandsHidden(node);
        }

        [Test]
        public void VariableCondition_BoolSelectionRestrictsComparisonDropdown()
        {
            var node = new BTVariableCondition
            {
                fieldName = nameof(VariableBlackboard.BoolValue),
                compareType = BTVariableCondition.CompareType.GreaterThan
            };
            ((IBTInspectorContext)node).SetInspectorBlackboard(
                typeof(VariableBlackboard));

            Assert.That(node.compareType,
                Is.EqualTo(BTVariableCondition.CompareType.Equals));
            Assert.That(GetInspectorValues<BTVariableCondition.CompareType>(
                    node, "InspectorComparisons"),
                Is.EqualTo(new[]
                {
                    BTVariableCondition.CompareType.Equals,
                    BTVariableCondition.CompareType.NotEquals
                }));
        }

        [Test]
        public void VariableCondition_EnumSelectionRestrictsComparisonDropdown()
        {
            var node = new BTVariableCondition
            {
                fieldName = nameof(VariableBlackboard.EnumValue),
                compareType = BTVariableCondition.CompareType.LessThan,
                enumValue = nameof(DayOfWeek.Friday)
            };
            ((IBTInspectorContext)node).SetInspectorBlackboard(
                typeof(VariableBlackboard));

            Assert.That(node.compareType,
                Is.EqualTo(BTVariableCondition.CompareType.Equals));
            Assert.That(GetInspectorValues<BTVariableCondition.CompareType>(
                    node, "InspectorComparisons"),
                Is.EqualTo(new[]
                {
                    BTVariableCondition.CompareType.Equals,
                    BTVariableCondition.CompareType.NotEquals
                }));
            Assert.That(node.enumValue, Is.EqualTo(nameof(DayOfWeek.Friday)));
        }

        [Test]
        public void SetVariable_InspectorSelectionSynchronizesAndClearsType()
        {
            var node = new BTSetVariable
            {
                setType = BTSetVariable.SetVariableType.Set
            };
            AssertSetSelection(node, nameof(VariableBlackboard.BoolValue),
                BTVariableCondition.VariableType.Bool,
                "ShowBoolInspectorValue");
            AssertSetSelection(node, nameof(VariableBlackboard.IntValue),
                BTVariableCondition.VariableType.Int,
                "ShowIntInspectorValue");
            AssertSetSelection(node, nameof(VariableBlackboard.FloatValue),
                BTVariableCondition.VariableType.Float,
                "ShowFloatInspectorValue");
            AssertSetSelection(node, nameof(VariableBlackboard.StringValue),
                BTVariableCondition.VariableType.String,
                "ShowStringInspectorValue");
            AssertSetSelection(node, nameof(VariableBlackboard.EnumValue),
                BTVariableCondition.VariableType.Enum,
                "ShowEnumInspectorValue");
            Assert.That(GetInspectorValues<string>(node,
                    "InspectorEnumValues"),
                Is.EqualTo(Enum.GetNames(typeof(DayOfWeek))));

            node.fieldName = nameof(VariableBlackboard.LongValue);
            ((IBTInspectorContext)node).SetInspectorBlackboard(
                typeof(VariableBlackboard));
            Assert.That(node.variableType,
                Is.EqualTo(BTVariableCondition.VariableType.None));
            AssertAllOperandsHidden(node);
        }

        [Test]
        public void SetVariable_NotHidesBoolOperand()
        {
            var node = new BTSetVariable
            {
                fieldName = nameof(VariableBlackboard.BoolValue),
                setType = BTSetVariable.SetVariableType.Not
            };
            ((IBTInspectorContext)node).SetInspectorBlackboard(
                typeof(VariableBlackboard));

            Assert.That(node.variableType,
                Is.EqualTo(BTVariableCondition.VariableType.Bool));
            AssertAllOperandsHidden(node);
        }

        [Test]
        public void LegacyBoolOperands_MigrateToSerializableBoolFields()
        {
            var condition = new BTVariableCondition
            {
                fieldName = nameof(VariableBlackboard.BoolValue),
                variableType = BTVariableCondition.VariableType.Bool,
                intValue = 1,
                boolValue = false
            };
            SetPrivateField(condition, "boolValueInitialized", false);
            ((IBTInspectorContext)condition).SetInspectorBlackboard(
                typeof(VariableBlackboard));
            Assert.That(condition.boolValue, Is.True);

            var setVariable = new BTSetVariable
            {
                fieldName = nameof(VariableBlackboard.BoolValue),
                variableType = BTVariableCondition.VariableType.Bool,
                intValue = 0,
                floatValue = 1f,
                boolValue = false
            };
            SetPrivateField(setVariable, "boolValueInitialized", false);
            ((IBTInspectorContext)setVariable).SetInspectorBlackboard(
                typeof(VariableBlackboard));
            Assert.That(setVariable.boolValue, Is.True);
        }

        [Test]
        public void VariableCondition_ExecutesAllSupportedTypes()
        {
            VariableRuntime boolTree = Build(new BTVariableCondition
            {
                fieldName = nameof(VariableBlackboard.BoolValue),
                variableType = BTVariableCondition.VariableType.Bool,
                compareType = BTVariableCondition.CompareType.Equals,
                boolValue = true
            });
            boolTree.Value.BoolValue = true;
            Assert.That(boolTree.Update(boolTree.Value), Is.EqualTo(BTNode.State.Success));

            VariableRuntime intTree = Build(new BTVariableCondition
            {
                fieldName = nameof(VariableBlackboard.IntValue),
                variableType = BTVariableCondition.VariableType.Int,
                compareType = BTVariableCondition.CompareType.GreaterThan,
                intValue = 10
            });
            intTree.Value.IntValue = 11;
            Assert.That(intTree.Update(intTree.Value), Is.EqualTo(BTNode.State.Success));

            VariableRuntime floatTree = Build(new BTVariableCondition
            {
                fieldName = nameof(VariableBlackboard.FloatValue),
                variableType = BTVariableCondition.VariableType.Float,
                compareType = BTVariableCondition.CompareType.LessOrEquals,
                floatValue = 1.5f
            });
            floatTree.Value.FloatValue = 1.25f;
            Assert.That(floatTree.Update(floatTree.Value), Is.EqualTo(BTNode.State.Success));

            VariableRuntime stringTree = Build(new BTVariableCondition
            {
                fieldName = nameof(VariableBlackboard.StringValue),
                variableType = BTVariableCondition.VariableType.String,
                compareType = BTVariableCondition.CompareType.GreaterThan,
                stringValue = "alpha"
            });
            stringTree.Value.StringValue = "beta";
            Assert.That(stringTree.Update(stringTree.Value), Is.EqualTo(BTNode.State.Success));

            VariableRuntime enumTree = Build(new BTVariableCondition
            {
                fieldName = nameof(VariableBlackboard.EnumValue),
                variableType = BTVariableCondition.VariableType.Enum,
                compareType = BTVariableCondition.CompareType.Equals,
                enumValue = nameof(DayOfWeek.Friday)
            });
            enumTree.Value.EnumValue = DayOfWeek.Friday;
            Assert.That(enumTree.Update(enumTree.Value),
                Is.EqualTo(BTNode.State.Success));
        }

        [Test]
        public void SetVariable_ExecutesSupportedSetAndUnaryOperations()
        {
            VariableRuntime intTree = Build(new BTSetVariable
            {
                fieldName = nameof(VariableBlackboard.IntValue),
                variableType = BTVariableCondition.VariableType.Int,
                setType = BTSetVariable.SetVariableType.Set,
                intValue = 17
            });
            intTree.Value.IntValue = 99;
            Assert.That(intTree.Update(intTree.Value), Is.EqualTo(BTNode.State.Success));
            Assert.That(intTree.Value.IntValue, Is.EqualTo(17));

            VariableRuntime floatTree = Build(new BTSetVariable
            {
                fieldName = nameof(VariableBlackboard.FloatValue),
                variableType = BTVariableCondition.VariableType.Float,
                setType = BTSetVariable.SetVariableType.Set,
                floatValue = 2.5f
            });
            floatTree.Value.FloatValue = 99f;
            Assert.That(floatTree.Update(floatTree.Value), Is.EqualTo(BTNode.State.Success));
            Assert.That(floatTree.Value.FloatValue, Is.EqualTo(2.5f));

            VariableRuntime stringTree = Build(new BTSetVariable
            {
                fieldName = nameof(VariableBlackboard.StringValue),
                variableType = BTVariableCondition.VariableType.String,
                setType = BTSetVariable.SetVariableType.Add,
                stringValue = "-right"
            });
            stringTree.Value.StringValue = "left";
            Assert.That(stringTree.Update(stringTree.Value), Is.EqualTo(BTNode.State.Success));
            Assert.That(stringTree.Value.StringValue, Is.EqualTo("left-right"));

            VariableRuntime boolTree = Build(new BTSetVariable
            {
                fieldName = nameof(VariableBlackboard.BoolValue),
                variableType = BTVariableCondition.VariableType.Bool,
                setType = BTSetVariable.SetVariableType.Not
            });
            boolTree.Value.BoolValue = true;
            Assert.That(boolTree.Update(boolTree.Value), Is.EqualTo(BTNode.State.Success));
            Assert.That(boolTree.Value.BoolValue, Is.False);

            VariableRuntime boolSetTree = Build(new BTSetVariable
            {
                fieldName = nameof(VariableBlackboard.BoolValue),
                variableType = BTVariableCondition.VariableType.Bool,
                setType = BTSetVariable.SetVariableType.Set,
                boolValue = true
            });
            Assert.That(boolSetTree.Update(boolSetTree.Value), Is.EqualTo(BTNode.State.Success));
            Assert.That(boolSetTree.Value.BoolValue, Is.True);

            VariableRuntime enumTree = Build(new BTSetVariable
            {
                fieldName = nameof(VariableBlackboard.EnumValue),
                variableType = BTVariableCondition.VariableType.Enum,
                setType = BTSetVariable.SetVariableType.Set,
                enumValue = nameof(DayOfWeek.Wednesday)
            });
            enumTree.Value.EnumValue = DayOfWeek.Sunday;
            Assert.That(enumTree.Update(enumTree.Value),
                Is.EqualTo(BTNode.State.Success));
            Assert.That(enumTree.Value.EnumValue,
                Is.EqualTo(DayOfWeek.Wednesday));
        }

        [Test]
        public void VariableNodes_RejectUnknownEnumValue()
        {
            var condition = new BTVariableCondition
            {
                fieldName = nameof(VariableBlackboard.EnumValue),
                variableType = BTVariableCondition.VariableType.Enum,
                compareType = BTVariableCondition.CompareType.Equals,
                enumValue = "Missing"
            };
            Assert.Throws<InvalidOperationException>(() => Build(condition));

            var setVariable = new BTSetVariable
            {
                fieldName = nameof(VariableBlackboard.EnumValue),
                variableType = BTVariableCondition.VariableType.Enum,
                setType = BTSetVariable.SetVariableType.Set,
                enumValue = "Missing"
            };
            Assert.Throws<InvalidOperationException>(() => Build(setVariable));
        }

        [Test]
        public void BlackboardSetValueAcceptsEnumNamesAndNumbers()
        {
            var blackboard = new VariableBlackboard();

            blackboard.SetValue(nameof(VariableBlackboard.EnumValue),
                nameof(DayOfWeek.Wednesday));
            Assert.That(blackboard.EnumValue,
                Is.EqualTo(DayOfWeek.Wednesday));

            blackboard.SetValue(nameof(VariableBlackboard.EnumValue),
                (int)DayOfWeek.Friday);
            Assert.That(blackboard.EnumValue, Is.EqualTo(DayOfWeek.Friday));

            Assert.Throws<InvalidOperationException>(() =>
                blackboard.SetValue(nameof(VariableBlackboard.EnumValue),
                    "Missing"));
        }

        [Test]
        public void VariableNodes_RejectUnsupportedLongField()
        {
            var condition = new BTVariableCondition
            {
                fieldName = nameof(VariableBlackboard.LongValue),
                variableType = BTVariableCondition.VariableType.None,
                compareType = BTVariableCondition.CompareType.Equals
            };
            Assert.Throws<InvalidOperationException>(() => Build(condition));

            var setVariable = new BTSetVariable
            {
                fieldName = nameof(VariableBlackboard.LongValue),
                variableType = BTVariableCondition.VariableType.None,
                setType = BTSetVariable.SetVariableType.Set
            };
            Assert.Throws<InvalidOperationException>(() => Build(setVariable));
        }

        private static void AssertConditionSelection(BTVariableCondition node,
            string fieldName, BTVariableCondition.VariableType expectedType,
            string visibleProperty)
        {
            node.fieldName = fieldName;
            ((IBTInspectorContext)node).SetInspectorBlackboard(
                typeof(VariableBlackboard));
            Assert.That(node.variableType, Is.EqualTo(expectedType), fieldName);
            AssertOnlyOperandVisible(node, visibleProperty);
        }

        private static void AssertSetSelection(BTSetVariable node,
            string fieldName, BTVariableCondition.VariableType expectedType,
            string visibleProperty)
        {
            node.fieldName = fieldName;
            ((IBTInspectorContext)node).SetInspectorBlackboard(
                typeof(VariableBlackboard));
            Assert.That(node.variableType, Is.EqualTo(expectedType), fieldName);
            AssertOnlyOperandVisible(node, visibleProperty);
        }

        private static void AssertVisibilityCondition(FieldInfo field,
            string conditionMember)
        {
            Assert.That(field, Is.Not.Null);
            ConditionAttribute attribute = field
                .GetCustomAttributes(typeof(ConditionAttribute), false)
                .Cast<ConditionAttribute>()
                .SingleOrDefault(value => value.mode == ConditionMode.Show);
            Assert.That(attribute, Is.Not.Null,
                $"{field.DeclaringType}.{field.Name} needs a Show condition.");
            Assert.That(attribute.conditions,
                Is.EqualTo(new[] { conditionMember }));
            Assert.That(attribute.expected, Is.EqualTo(true));
        }

        private static string[] GetInspectorFieldNames(object node)
        {
            return GetInspectorValues<string>(node, "InspectorFields");
        }

        private static T[] GetInspectorValues<T>(object node,
            string propertyName)
        {
            PropertyInfo property = node.GetType().GetProperty(
                propertyName, PrivateInstance);
            Assert.That(property, Is.Not.Null);
            var values = (ValueDropdownList<T>)property.GetValue(node);
            return values.Select(item => item.value).ToArray();
        }

        private static BTSetVariable.SetVariableType[] GetSupportedOperations(
            BTVariableCondition.VariableType type)
        {
            return Enum.GetValues(typeof(BTSetVariable.SetVariableType))
                .Cast<BTSetVariable.SetVariableType>()
                .Where(operation => BTSetVariable.SupportsOperation(
                    type, operation))
                .ToArray();
        }

        private static void AssertOnlyOperandVisible(object node,
            string visibleProperty)
        {
            string[] properties =
            {
                "ShowBoolInspectorValue",
                "ShowIntInspectorValue",
                "ShowFloatInspectorValue",
                "ShowStringInspectorValue",
                "ShowEnumInspectorValue"
            };
            foreach (string property in properties)
            {
                Assert.That(GetPrivateBool(node, property),
                    Is.EqualTo(property == visibleProperty), property);
            }
        }

        private static void AssertAllOperandsHidden(object node)
        {
            AssertOnlyOperandVisible(node, null);
        }

        private static bool GetPrivateBool(object node, string propertyName)
        {
            PropertyInfo property = node.GetType().GetProperty(propertyName,
                PrivateInstance);
            Assert.That(property, Is.Not.Null,
                $"Missing Inspector condition member {propertyName}.");
            return (bool)property.GetValue(node);
        }

        private static void SetPrivateField(object node, string fieldName,
            object value)
        {
            FieldInfo field = node.GetType().GetField(fieldName,
                PrivateInstance);
            Assert.That(field, Is.Not.Null);
            field.SetValue(node, value);
        }

        private static VariableRuntime Build(BTNode node)
        {
            var root = new BTRoot();
            var tree = new VariableTree();
            SetGraphData(tree, new List<NodeData> { root, node },
                new List<ConnectionData> { Connect(root, node) });
            tree.PrepareForRuntime();
            var runtimeBlackboard = new VariableBlackboard();
            runtimeBlackboard.Initialize(tree);
            return new VariableRuntime(tree, runtimeBlackboard);
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
            List<NodeData> nodes, List<ConnectionData> connections)
        {
            typeof(GraphAsset).GetField("_nodes", PrivateInstance)
                .SetValue(graph, nodes);
            typeof(GraphAsset).GetField("_groups", PrivateInstance)
                .SetValue(graph, new List<GroupData>());
            typeof(GraphAsset).GetField("_connections", PrivateInstance)
                .SetValue(graph, connections);
        }
    }
}
