using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text.RegularExpressions;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;

namespace ActionAttribute.Tests
{
    [AttributeUsage(AttributeTargets.Field, AllowMultiple = true)]
    internal sealed class ContextProbeConditionAttribute :
        ActionConditionAttribute
    {
        internal static ActionAttributeContext LastContext { get; private set; }

        public ContextProbeConditionAttribute() : base(ConditionMode.Show) { }

        public override bool Evaluate(ActionAttributeContext context)
        {
            LastContext = context;
            return true;
        }

        internal static void Reset()
        {
            LastContext = null;
        }
    }

    [AttributeUsage(AttributeTargets.Field)]
    internal sealed class ThrowingConditionAttribute :
        ActionConditionAttribute
    {
        public ThrowingConditionAttribute(
            ConditionMode mode = ConditionMode.Show) : base(mode) { }

        public override bool Evaluate(ActionAttributeContext context)
        {
            throw new InvalidOperationException("condition failure");
        }
    }

    [AttributeUsage(AttributeTargets.Field)]
    internal sealed class ThrowingValidationAttribute :
        ActionValidationAttribute
    {
        public override bool IsValid(ActionAttributeContext context)
        {
            throw new InvalidOperationException("validation failure");
        }
    }

    [AttributeUsage(AttributeTargets.Field)]
    internal sealed class ThrowingModifierAttribute :
        ActionValueModifierAttribute
    {
        public override object Modify(ActionAttributeContext context)
        {
            throw new InvalidOperationException("modifier failure");
        }
    }

    [AttributeUsage(AttributeTargets.Field)]
    internal sealed class ThrowingMessageAttribute : ActionMessageAttribute
    {
        public override ActionMessage GetMessage(ActionAttributeContext context)
        {
            throw new InvalidOperationException("message failure");
        }
    }

    [AttributeUsage(AttributeTargets.Field)]
    internal sealed class ThrowingLabelAttribute : ActionLabelAttribute
    {
        public override string GetLabel(ActionAttributeContext context)
        {
            throw new InvalidOperationException("label failure");
        }
    }

    [AttributeUsage(AttributeTargets.Field)]
    internal sealed class NegativeMessageAttribute : ActionMessageAttribute
    {
        public override ActionMessage GetMessage(ActionAttributeContext context)
        {
            return context.TryGetValue<int>(out int value) && value < 0
                ? new ActionMessage("值小于零。",
                    InspectorMessageType.Warning)
                : default;
        }
    }

    [AttributeUsage(AttributeTargets.Field)]
    internal sealed class ValueLabelAttribute : ActionLabelAttribute
    {
        public override string GetLabel(ActionAttributeContext context)
        {
            return $"当前值 {context.Value}";
        }

        public override string GetTooltip(ActionAttributeContext context)
        {
            return context.PropertyPath;
        }
    }

    [AttributeUsage(AttributeTargets.Field)]
    internal sealed class InvalidReturnModifierAttribute :
        ActionValueModifierAttribute
    {
        public override object Modify(ActionAttributeContext context)
        {
            return new object();
        }
    }

    internal static class ExtensionExecutionTrace
    {
        internal static readonly List<string> Entries = new List<string>();

        internal static void Reset()
        {
            Entries.Clear();
        }
    }

    [AttributeUsage(AttributeTargets.Field, AllowMultiple = true)]
    internal sealed class TraceConditionAttribute : ActionConditionAttribute
    {
        private readonly int marker;

        public TraceConditionAttribute(int marker) : base(ConditionMode.Show)
        {
            this.marker = marker;
        }

        public override bool Evaluate(ActionAttributeContext context)
        {
            ExtensionExecutionTrace.Entries.Add("C" + marker);
            return true;
        }
    }

    [AttributeUsage(AttributeTargets.Field, AllowMultiple = true)]
    internal sealed class TraceValidationAttribute :
        ActionValidationAttribute
    {
        private readonly int marker;

        public TraceValidationAttribute(int marker)
        {
            this.marker = marker;
        }

        public override bool IsValid(ActionAttributeContext context)
        {
            ExtensionExecutionTrace.Entries.Add("V" + marker);
            return true;
        }
    }

    [AttributeUsage(AttributeTargets.Field, AllowMultiple = true)]
    internal sealed class TraceModifierAttribute : ActionValueModifierAttribute
    {
        private readonly int marker;

        public TraceModifierAttribute(int marker)
        {
            this.marker = marker;
        }

        public override object Modify(ActionAttributeContext context)
        {
            ExtensionExecutionTrace.Entries.Add("M" + marker);
            return context.Value;
        }
    }

    [AttributeUsage(AttributeTargets.Field, AllowMultiple = true)]
    internal sealed class AppendDigitAttribute : ActionValueModifierAttribute
    {
        private readonly int digit;

        public AppendDigitAttribute(int digit)
        {
            this.digit = digit;
        }

        public override object Modify(ActionAttributeContext context)
        {
            return context.TryGetValue<int>(out int value)
                ? value * 10 + digit
                : context.Value;
        }
    }

    [AttributeUsage(AttributeTargets.Field)]
    internal sealed class AppendTextAttribute : ActionValueModifierAttribute
    {
        private readonly string suffix;

        public AppendTextAttribute(string suffix)
        {
            this.suffix = suffix;
        }

        public override object Modify(ActionAttributeContext context)
        {
            return (context.Value as string ?? string.Empty) + suffix;
        }
    }

    [AttributeUsage(AttributeTargets.Field)]
    internal sealed class DynamicValidationAttribute :
        ActionValidationAttribute
    {
        public override bool IsValid(ActionAttributeContext context)
        {
            return context.TryGetValue<int>(out int value) && value % 2 == 0;
        }

        public override string GetMessage(ActionAttributeContext context)
        {
            return $"{context.MemberName}:{context.Value}@{context.PropertyPath}";
        }
    }

    public sealed class ActionAttributeExtensionTests
    {
        private const BindingFlags StaticFlags = BindingFlags.Static |
            BindingFlags.Public | BindingFlags.NonPublic;
        private const BindingFlags InstanceFlags = BindingFlags.Instance |
            BindingFlags.Public | BindingFlags.NonPublic;

        private enum SampleMode
        {
            Basic,
            Advanced
        }

        [Serializable]
        private sealed class NestedSettings
        {
            [ContextProbeCondition] public int value = 7;
        }

        private sealed class ComparisonOwner
        {
            public int minimum = 3;
            private int Maximum => 9;
            private int Expected() => 5;
        }

        private sealed class ExtensionTarget : ScriptableObject
        {
            [ThrowingCondition] public int throwingCondition = 1;
            [ThrowingCondition(ConditionMode.Hide)]
            public int throwingHide = 1;
            [ThrowingCondition(ConditionMode.Enable)]
            public int throwingEnable = 1;
            [ThrowingCondition(ConditionMode.Disable)]
            public int throwingDisable = 1;
            [ThrowingValidation] public int throwingValidation = 1;
            [ThrowingModifier] public int throwingModifier = 5;
            [ThrowingMessage] public int throwingMessage = 1;
            [ThrowingLabel] public int throwingLabel = 1;
            [InvalidReturnModifier] public int invalidReturnModifier = 9;
            [AppendDigit(1), AppendDigit(2)] public int stableOrder;
            [AppendDigit(2, Priority = 10),
             AppendDigit(1, Priority = -10)]
            public int priorityOrder;
            [DynamicValidation] public int invalid = 3;
            [Text(Pattern = "^[A-Z]{2}-[0-9]{2}$", AllowEmpty = false)]
            public string patterned = "invalid";
            [Text(Trim = true, Case = TextCase.UpperInvariant,
                CollapseWhitespace = true, MaxLength = 10)]
            public string normalized = "  mixed\t case  ";
            [Text(MaxLength = 5), AppendText("XYZ")]
            public string boundedText = "abcd";
            [Collection(1, 3, CollectionItemRule.Unique |
                CollectionItemRule.NotNull | CollectionItemRule.NotBlank)]
            public List<string> itemRules = new List<string> { "same", "same" };
            [Required] public int requiredId;
            public int minimum = 2;
            [CompareTo(nameof(minimum), ValueRelation.GreaterThanOrEqual)]
            public int compared = 1;
            [Finite] public float finite = float.NaN;
            [Text(MinLines = 2, MaxLines = 3)]
            public string lineCount = "one";
            [NegativeMessage] public int messaged = -1;
            [ValueLabel] public int labeled = 7;
            public NestedSettings nested = new NestedSettings();
            public List<NestedSettings> nestedList = new List<NestedSettings>
            {
                new NestedSettings()
            };
            public NestedSettings[] nestedArray = { new NestedSettings() };
            [TraceCondition(2, Priority = 10),
             TraceCondition(1, Priority = -10),
             TraceCondition(3, Priority = 10)]
            public int tracedCondition;
            [TraceValidation(2, Priority = 10),
             TraceValidation(1, Priority = -10),
             TraceValidation(3, Priority = 10)]
            public int tracedValidation;
            [TraceModifier(2, Priority = 10),
             TraceModifier(1, Priority = -10),
             TraceModifier(3, Priority = 10)]
            public int tracedModifier;
        }

        [Test]
        public void RuntimeAssemblyExposesOnlyDeclaredExtensionPoints()
        {
            var expected = new HashSet<Type>
            {
                typeof(ActionConditionAttribute),
                typeof(ActionLabelAttribute),
                typeof(ActionMessageAttribute),
                typeof(ActionValidationAttribute),
                typeof(ActionValueModifierAttribute)
            };
            Type[] types = typeof(ActionAttributeBase).Assembly.GetTypes();
            for (int i = 0; i < types.Length; i++)
            {
                Type type = types[i];
                if (type == typeof(ActionAttributeBase) || !type.IsAbstract ||
                    !typeof(ActionAttributeBase).IsAssignableFrom(type))
                    continue;
                Assert.That(expected.Remove(type), Is.True,
                    "发现未声明的可继承特性扩展点：" + type.FullName);
            }
            Assert.That(expected, Is.Empty);
        }

        [Test]
        public void ConcreteRuntimeAttributesMatchCanonicalWhitelist()
        {
            var expected = new HashSet<Type>
            {
                typeof(ButtonAttribute),
                typeof(CollectionAttribute),
                typeof(CompareToAttribute),
                typeof(ConditionAttribute),
                typeof(EnumToggleButtonsAttribute),
                typeof(EulerAnglesAttribute),
                typeof(ExpandableAttribute),
                typeof(FiniteAttribute),
                typeof(GroupAttribute),
                typeof(HelpBoxAttribute),
                typeof(HorizontalLineAttribute),
                typeof(InlineButtonAttribute),
                typeof(MinMaxSliderAttribute),
                typeof(NameAttribute),
                typeof(ObjectsOnlyAttribute),
                typeof(OnValueChangedAttribute),
                typeof(PathAttribute),
                typeof(ProgressBarAttribute),
                typeof(ReadOnlyAttribute),
                typeof(RequiredAttribute),
                typeof(ShowAssetPreviewAttribute),
                typeof(ShowInInspectorAttribute),
                typeof(SliderAttribute),
                typeof(SuffixLabelAttribute),
                typeof(TextAttribute),
                typeof(ToggleLeftAttribute),
                typeof(TypeInfoBoxAttribute),
                typeof(ValueDropdownAttribute)
            };
            Type[] types = typeof(ActionAttributeBase).Assembly.GetTypes();
            for (int i = 0; i < types.Length; i++)
            {
                Type type = types[i];
                if (type.IsAbstract || !typeof(ActionAttributeBase)
                    .IsAssignableFrom(type)) continue;
                Assert.That(expected.Remove(type), Is.True,
                    "发现未登记的具体特性，可能是别名：" + type.FullName);
                Assert.That(type.IsSealed, Is.True,
                    type.FullName + " 必须 sealed。");
                Assert.That(type.BaseType?.IsAbstract, Is.True,
                    type.FullName + " 只能继承抽象特性协议，不能形成别名。");
            }
            Assert.That(expected, Is.Empty, "规范具体特性缺失。");
        }

        [Test]
        public void ExtensionPointsRequireFieldLogicAndAllowComposition()
        {
            AssertExtensionContract(typeof(ActionConditionAttribute),
                nameof(ActionConditionAttribute.Evaluate));
            AssertExtensionContract(typeof(ActionLabelAttribute),
                nameof(ActionLabelAttribute.GetLabel));
            AssertExtensionContract(typeof(ActionMessageAttribute),
                nameof(ActionMessageAttribute.GetMessage));
            AssertExtensionContract(typeof(ActionValidationAttribute),
                nameof(ActionValidationAttribute.IsValid));
            AssertExtensionContract(typeof(ActionValueModifierAttribute),
                nameof(ActionValueModifierAttribute.Modify));
        }

        [Test]
        public void ContextPreservesMetadataAndConvertsSupportedValues()
        {
            var target = new object();
            var owner = new object();
            var context = new ActionAttributeContext(target, owner, "Advanced",
                typeof(string), "nested.mode", "mode", false);

            Assert.That(context.Target, Is.SameAs(target));
            Assert.That(context.Owner, Is.SameAs(owner));
            Assert.That(context.PropertyPath, Is.EqualTo("nested.mode"));
            Assert.That(context.MemberName, Is.EqualTo("mode"));
            Assert.That(context.IsPlaying, Is.False);
            Assert.That(context.TryGetValue<SampleMode>(out SampleMode mode),
                Is.True);
            Assert.That(mode, Is.EqualTo(SampleMode.Advanced));

            var number = new ActionAttributeContext(target, owner, 5,
                typeof(int), "value", "value", true);
            Assert.That(number.TryGetValue<double>(out double converted),
                Is.True);
            Assert.That(converted, Is.EqualTo(5));

            var invalid = new ActionAttributeContext(target, owner, "invalid",
                typeof(string), null, null, false);
            Assert.That(invalid.TryGetValue<int>(out _), Is.False);

            var nullable = new ActionAttributeContext(target, owner, null,
                typeof(int?), "nullable", "nullable", false);
            Assert.That(nullable.TryGetValue<int?>(out int? empty), Is.True);
            Assert.That(empty, Is.Null);
            Assert.That(nullable.TryGetValue<int>(out _), Is.False);
        }

        [Test]
        public void BuiltInRuntimeRulesAreResourceIndependentAndDeterministic()
        {
            Type[] ruleTypes =
            {
                typeof(TextAttribute), typeof(CollectionAttribute),
                typeof(RequiredAttribute), typeof(CompareToAttribute),
                typeof(FiniteAttribute)
            };
            for (int i = 0; i < ruleTypes.Length; i++)
                AssertResourceIndependentPublicApi(ruleTypes[i]);

            var pattern = new TextAttribute
            {
                Pattern = "^[A-Z]{2}-[0-9]{2}$",
                AllowEmpty = false
            };
            Assert.That(pattern.IsValid(Context("AB-12", typeof(string))),
                Is.True);
            Assert.That(pattern.IsValid(Context("ab-12", typeof(string))),
                Is.False);
            pattern.IgnoreCase = true;
            Assert.That(pattern.IsValid(Context("ab-12", typeof(string))),
                Is.True);
            var invalidPattern = new TextAttribute { Pattern = "[" };
            Assert.That(invalidPattern.IsValid(Context("value", typeof(string))),
                Is.False);

            var normalize = new TextAttribute
            {
                Trim = true,
                CollapseWhitespace = true,
                Case = TextCase.UpperInvariant,
                MaxLength = 10
            };
            Assert.That(normalize.Apply("  mixed\t case suffix  "),
                Is.EqualTo("MIXED CASE"));
            Assert.That(new TextAttribute { MaxLength = -1 }.Apply("value"),
                Is.Empty);

            var collection = new CollectionAttribute(1, 2,
                CollectionItemRule.Unique | CollectionItemRule.NotNull |
                CollectionItemRule.NotBlank);
            Assert.That(collection.IsValid(Context(new List<string>
                { "first", "second" }, typeof(List<string>))), Is.True);
            Assert.That(collection.IsValid(Context(new List<string>
                { "same", "same" }, typeof(List<string>))), Is.False);
            Assert.That(collection.IsValid(Context(new List<string>
                { "value", " " }, typeof(List<string>))), Is.False);
            Assert.That(collection.IsValid(Context(new List<string>(),
                typeof(List<string>))), Is.False);
            Assert.That(collection.IsValid(Context(
                new[] { "first", "second" }, typeof(string[]))), Is.True);

            var required = new RequiredAttribute();
            Assert.That(required.IsValid(Context(0, typeof(int))), Is.False);
            Assert.That(required.IsValid(Context(7, typeof(int))), Is.True);
            Assert.That(required.IsValid(Context(" ", typeof(string))), Is.False);
            Assert.That(required.IsValid(Context(SampleMode.Basic,
                typeof(SampleMode))), Is.False);
            Assert.That(required.IsValid(Context(SampleMode.Advanced,
                typeof(SampleMode))), Is.True);
            ExtensionTarget destroyed = ScriptableObject.CreateInstance<
                ExtensionTarget>();
            UnityEngine.Object.DestroyImmediate(destroyed);
            Assert.That(required.IsValid(Context(destroyed,
                typeof(ExtensionTarget))), Is.False);

            var owner = new ComparisonOwner();
            var comparison = new CompareToAttribute(nameof(owner.minimum),
                ValueRelation.GreaterThanOrEqual);
            Assert.That(comparison.IsValid(Context(3, typeof(int), owner)),
                Is.True);
            Assert.That(comparison.IsValid(Context(2, typeof(int), owner)),
                Is.False);
            ActionAttributeContext memberContext = Context(0, typeof(int), owner);
            Assert.That(memberContext.TryGetMemberValue("Maximum",
                out int maximum), Is.True);
            Assert.That(maximum, Is.EqualTo(9));
            Assert.That(memberContext.TryGetMemberValue("Expected",
                out int expected), Is.True);
            Assert.That(expected, Is.EqualTo(5));

            var finite = new FiniteAttribute();
            Assert.That(finite.IsValid(Context(1.5f, typeof(float))), Is.True);
            Assert.That(finite.IsValid(Context(float.NaN, typeof(float))),
                Is.False);
            Assert.That(finite.IsValid(Context(double.PositiveInfinity,
                typeof(double))), Is.False);

            var lines = new TextAttribute { MinLines = 2, MaxLines = 3 };
            Assert.That(lines.IsValid(Context("first\nsecond", typeof(string))),
                Is.True);
            Assert.That(lines.IsValid(Context("single", typeof(string))),
                Is.False);
            Assert.That(lines.IsValid(Context("1\r\n2\r3\n4", typeof(string))),
                Is.False);
        }

        [Test]
        public void BuiltInRuntimeRulesUseTheCombinedDrawerPipeline()
        {
            ExtensionTarget target = ScriptableObject.CreateInstance<
                ExtensionTarget>();
            try
            {
                var serializedObject = new SerializedObject(target);
                float line = EditorGUIUtility.singleLineHeight;
                Assert.That(GetCombinedHeight(serializedObject,
                    nameof(ExtensionTarget.patterned)), Is.GreaterThan(line));
                Assert.That(GetCombinedHeight(serializedObject,
                    nameof(ExtensionTarget.itemRules)), Is.GreaterThan(line));
                Assert.That(GetCombinedHeight(serializedObject,
                    nameof(ExtensionTarget.requiredId)), Is.GreaterThan(line));
                Assert.That(GetCombinedHeight(serializedObject,
                    nameof(ExtensionTarget.compared)), Is.GreaterThan(line));
                Assert.That(GetCombinedHeight(serializedObject,
                    nameof(ExtensionTarget.finite)), Is.GreaterThan(line));
                Assert.That(GetCombinedHeight(serializedObject,
                    nameof(ExtensionTarget.lineCount)), Is.GreaterThan(line));
                Assert.That(GetCombinedHeight(serializedObject,
                    nameof(ExtensionTarget.messaged)), Is.GreaterThan(line));

                GUIContent label = GetCombinedLabel(serializedObject,
                    nameof(ExtensionTarget.labeled));
                Assert.That(label.text, Is.EqualTo("当前值 7"));
                Assert.That(label.tooltip,
                    Is.EqualTo(nameof(ExtensionTarget.labeled)));

                ApplyValueLimits(serializedObject,
                    nameof(ExtensionTarget.normalized));
                ApplyValueLimits(serializedObject,
                    nameof(ExtensionTarget.boundedText));
                serializedObject.ApplyModifiedPropertiesWithoutUndo();
                Assert.That(target.normalized, Is.EqualTo("MIXED CASE"));
                Assert.That(target.boundedText, Is.EqualTo("abcdX"));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(target);
            }
        }

        [Test]
        public void NestedConditionReceivesTargetOwnerAndPropertyPath()
        {
            ExtensionTarget target = ScriptableObject.CreateInstance<
                ExtensionTarget>();
            ContextProbeConditionAttribute.Reset();
            try
            {
                var serializedObject = new SerializedObject(target);
                FieldInfo field = typeof(NestedSettings).GetField(
                    nameof(NestedSettings.value), InstanceFlags);
                float height = GetCombinedHeight(serializedObject, field,
                    "nested.value");

                Assert.That(height,
                    Is.EqualTo(EditorGUIUtility.singleLineHeight));
                ActionAttributeContext context =
                    ContextProbeConditionAttribute.LastContext;
                Assert.That(context, Is.Not.Null);
                Assert.That(context.Target, Is.SameAs(target));
                Assert.That(context.Owner, Is.SameAs(target.nested));
                Assert.That(context.Value, Is.EqualTo(7));
                Assert.That(context.ValueType, Is.EqualTo(typeof(int)));
                Assert.That(context.PropertyPath, Is.EqualTo("nested.value"));
                Assert.That(context.MemberName, Is.EqualTo("value"));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(target);
                ContextProbeConditionAttribute.Reset();
            }
        }

        [Test]
        public void CollectionElementsReceiveTheirConcreteOwnerAndPath()
        {
            ExtensionTarget target = ScriptableObject.CreateInstance<
                ExtensionTarget>();
            ContextProbeConditionAttribute.Reset();
            try
            {
                var serializedObject = new SerializedObject(target);
                FieldInfo field = typeof(NestedSettings).GetField(
                    nameof(NestedSettings.value), InstanceFlags);

                GetCombinedHeight(serializedObject, field,
                    "nestedList.Array.data[0].value");
                AssertContextOwner(target, target.nestedList[0],
                    "nestedList.Array.data[0].value");

                GetCombinedHeight(serializedObject, field,
                    "nestedArray.Array.data[0].value");
                AssertContextOwner(target, target.nestedArray[0],
                    "nestedArray.Array.data[0].value");
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(target);
                ContextProbeConditionAttribute.Reset();
            }
        }

        [Test]
        public void ModifierPriorityIsStableForEqualAndDifferentPriorities()
        {
            ExtensionTarget target = ScriptableObject.CreateInstance<
                ExtensionTarget>();
            try
            {
                var serializedObject = new SerializedObject(target);
                ApplyValueLimits(serializedObject,
                    nameof(ExtensionTarget.stableOrder));
                ApplyValueLimits(serializedObject,
                    nameof(ExtensionTarget.priorityOrder));
                serializedObject.ApplyModifiedPropertiesWithoutUndo();

                Assert.That(target.stableOrder, Is.EqualTo(12),
                    "同 Priority 必须保持声明发现顺序。");
                Assert.That(target.priorityOrder, Is.EqualTo(12),
                    "较小 Priority 必须先执行。");
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(target);
            }
        }

        [Test]
        public void EveryExtensionCategoryUsesStablePriorityOrdering()
        {
            ExtensionTarget target = ScriptableObject.CreateInstance<
                ExtensionTarget>();
            try
            {
                var serializedObject = new SerializedObject(target);
                AssertExecutionOrder(serializedObject,
                    nameof(ExtensionTarget.tracedCondition), "C1", "C2", "C3");
                AssertExecutionOrder(serializedObject,
                    nameof(ExtensionTarget.tracedValidation), "V1", "V2", "V3");

                ExtensionExecutionTrace.Reset();
                ApplyValueLimits(serializedObject,
                    nameof(ExtensionTarget.tracedModifier));
                Assert.That(ExtensionExecutionTrace.Entries,
                    Is.EqualTo(new[] { "M1", "M2", "M3" }));
            }
            finally
            {
                ExtensionExecutionTrace.Reset();
                UnityEngine.Object.DestroyImmediate(target);
            }
        }

        [Test]
        public void ValidationCanBuildMessageFromCurrentContext()
        {
            ExtensionTarget target = ScriptableObject.CreateInstance<
                ExtensionTarget>();
            try
            {
                var serializedObject = new SerializedObject(target);
                FieldInfo field = typeof(ExtensionTarget).GetField(
                    nameof(ExtensionTarget.invalid), InstanceFlags);
                object drawer = CreateDrawer(field);
                SerializedProperty property = serializedObject.FindProperty(
                    nameof(ExtensionTarget.invalid));
                var validation = field.GetCustomAttribute<
                    DynamicValidationAttribute>();
                MethodInfo method = FindExtensionValidationMethod();
                object[] arguments = { property, validation, null };

                Assert.That((bool)method.Invoke(drawer, arguments), Is.True);
                Assert.That(arguments[2], Is.EqualTo("invalid:3@invalid"));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(target);
            }
        }

        [TestCase(nameof(ExtensionTarget.throwingCondition), "ShouldShow")]
        [TestCase(nameof(ExtensionTarget.throwingHide), "ShouldShow")]
        [TestCase(nameof(ExtensionTarget.throwingEnable), "ShouldEnable")]
        [TestCase(nameof(ExtensionTarget.throwingDisable), "ShouldEnable")]
        public void EveryThrowingConditionModeFailsOpen(string fieldName,
            string ruleMethod)
        {
            ExtensionTarget target = ScriptableObject.CreateInstance<
                ExtensionTarget>();
            ClearFailureReports(typeof(ThrowingConditionAttribute));
            try
            {
                var serializedObject = new SerializedObject(target);
                FieldInfo field = typeof(ExtensionTarget).GetField(fieldName,
                    InstanceFlags);
                object drawer = CreateDrawer(field);
                SerializedProperty property = serializedObject.FindProperty(
                    fieldName);
                MethodInfo method = GetEditorType(
                    "ActionAttribute.ActionPropertyDrawer").GetMethod(
                    ruleMethod, InstanceFlags);

                LogAssert.Expect(LogType.Warning, new Regex(
                    "ThrowingConditionAttribute.*condition failure"));
                Assert.That((bool)method.Invoke(drawer,
                    new object[] { property }), Is.True);
                Assert.That((bool)method.Invoke(drawer,
                    new object[] { property }), Is.True);
                LogAssert.NoUnexpectedReceived();
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(target);
            }
        }

        [Test]
        public void IncompatibleModifierResultIsIgnoredAndWarnedOnce()
        {
            ExtensionTarget target = ScriptableObject.CreateInstance<
                ExtensionTarget>();
            ClearFailureReports(typeof(InvalidReturnModifierAttribute));
            try
            {
                var serializedObject = new SerializedObject(target);
                LogAssert.Expect(LogType.Warning, new Regex(
                    "InvalidReturnModifierAttribute.*无法将返回值写入"));
                ApplyValueLimits(serializedObject,
                    nameof(ExtensionTarget.invalidReturnModifier));
                serializedObject.ApplyModifiedPropertiesWithoutUndo();
                Assert.That(target.invalidReturnModifier, Is.EqualTo(9));

                ApplyValueLimits(serializedObject,
                    nameof(ExtensionTarget.invalidReturnModifier));
                LogAssert.NoUnexpectedReceived();
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(target);
            }
        }

        [Test]
        public void ExtensionFailuresFailOpenAndLogOnlyOnce()
        {
            ExtensionTarget target = ScriptableObject.CreateInstance<
                ExtensionTarget>();
            ClearFailureReports(typeof(ThrowingConditionAttribute),
                typeof(ThrowingValidationAttribute),
                typeof(ThrowingModifierAttribute),
                typeof(ThrowingMessageAttribute),
                typeof(ThrowingLabelAttribute));
            try
            {
                var serializedObject = new SerializedObject(target);
                LogAssert.Expect(LogType.Warning, new Regex(
                    "ThrowingConditionAttribute.*condition failure"));
                float conditionHeight = GetCombinedHeight(serializedObject,
                    typeof(ExtensionTarget).GetField(
                        nameof(ExtensionTarget.throwingCondition),
                        InstanceFlags),
                    nameof(ExtensionTarget.throwingCondition));
                Assert.That(conditionHeight,
                    Is.EqualTo(EditorGUIUtility.singleLineHeight));

                LogAssert.Expect(LogType.Warning, new Regex(
                    "ThrowingValidationAttribute.*validation failure"));
                float validationHeight = GetCombinedHeight(serializedObject,
                    typeof(ExtensionTarget).GetField(
                        nameof(ExtensionTarget.throwingValidation),
                        InstanceFlags),
                    nameof(ExtensionTarget.throwingValidation));
                Assert.That(validationHeight,
                    Is.EqualTo(EditorGUIUtility.singleLineHeight));

                LogAssert.Expect(LogType.Warning, new Regex(
                    "ThrowingModifierAttribute.*modifier failure"));
                ApplyValueLimits(serializedObject,
                    nameof(ExtensionTarget.throwingModifier));
                serializedObject.ApplyModifiedPropertiesWithoutUndo();
                Assert.That(target.throwingModifier, Is.EqualTo(5));

                LogAssert.Expect(LogType.Warning, new Regex(
                    "ThrowingMessageAttribute.*message failure"));
                float messageHeight = GetCombinedHeight(serializedObject,
                    nameof(ExtensionTarget.throwingMessage));
                Assert.That(messageHeight,
                    Is.EqualTo(EditorGUIUtility.singleLineHeight));

                LogAssert.Expect(LogType.Warning, new Regex(
                    "ThrowingLabelAttribute.*label failure"));
                float labelHeight = GetCombinedHeight(serializedObject,
                    nameof(ExtensionTarget.throwingLabel));
                Assert.That(labelHeight,
                    Is.EqualTo(EditorGUIUtility.singleLineHeight));

                GetCombinedHeight(serializedObject,
                    typeof(ExtensionTarget).GetField(
                        nameof(ExtensionTarget.throwingCondition),
                        InstanceFlags),
                    nameof(ExtensionTarget.throwingCondition));
                GetCombinedHeight(serializedObject,
                    typeof(ExtensionTarget).GetField(
                        nameof(ExtensionTarget.throwingValidation),
                        InstanceFlags),
                    nameof(ExtensionTarget.throwingValidation));
                ApplyValueLimits(serializedObject,
                    nameof(ExtensionTarget.throwingModifier));
                GetCombinedHeight(serializedObject,
                    nameof(ExtensionTarget.throwingMessage));
                GetCombinedHeight(serializedObject,
                    nameof(ExtensionTarget.throwingLabel));
                LogAssert.NoUnexpectedReceived();
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(target);
            }
        }

        private static void AssertExtensionContract(Type type,
            string methodName)
        {
            Assert.That(type.IsAbstract, Is.True);
            AttributeUsageAttribute usage = type.GetCustomAttribute<
                AttributeUsageAttribute>();
            Assert.That(usage, Is.Not.Null);
            Assert.That(usage.ValidOn, Is.EqualTo(AttributeTargets.Field));
            Assert.That(usage.AllowMultiple, Is.True);
            Assert.That(usage.Inherited, Is.True);
            MethodInfo method = type.GetMethod(methodName, InstanceFlags);
            Assert.That(method, Is.Not.Null);
            Assert.That(method.IsAbstract, Is.True);
        }

        private static void AssertResourceIndependentPublicApi(Type type)
        {
            const BindingFlags declaredPublic = BindingFlags.Public |
                BindingFlags.Instance | BindingFlags.DeclaredOnly;
            FieldInfo[] fields = type.GetFields(declaredPublic);
            for (int i = 0; i < fields.Length; i++)
                Assert.That(typeof(UnityEngine.Object).IsAssignableFrom(
                    fields[i].FieldType), Is.False,
                    type.Name + "." + fields[i].Name);
            PropertyInfo[] properties = type.GetProperties(declaredPublic);
            for (int i = 0; i < properties.Length; i++)
                Assert.That(typeof(UnityEngine.Object).IsAssignableFrom(
                    properties[i].PropertyType), Is.False,
                    type.Name + "." + properties[i].Name);
            ConstructorInfo[] constructors = type.GetConstructors();
            for (int i = 0; i < constructors.Length; i++)
            {
                ParameterInfo[] parameters = constructors[i].GetParameters();
                for (int j = 0; j < parameters.Length; j++)
                    Assert.That(typeof(UnityEngine.Object).IsAssignableFrom(
                        parameters[j].ParameterType), Is.False,
                        type.Name + " constructor " + parameters[j].Name);
            }
        }

        private static void AssertContextOwner(ExtensionTarget target,
            NestedSettings owner, string propertyPath)
        {
            ActionAttributeContext context =
                ContextProbeConditionAttribute.LastContext;
            Assert.That(context, Is.Not.Null);
            Assert.That(context.Target, Is.SameAs(target));
            Assert.That(context.Owner, Is.SameAs(owner));
            Assert.That(context.Value, Is.EqualTo(7));
            Assert.That(context.PropertyPath, Is.EqualTo(propertyPath));
            Assert.That(context.MemberName, Is.EqualTo("value"));
        }

        private static void AssertExecutionOrder(
            SerializedObject serializedObject, string fieldName,
            params string[] expected)
        {
            ExtensionExecutionTrace.Reset();
            FieldInfo field = typeof(ExtensionTarget).GetField(fieldName,
                InstanceFlags);
            GetCombinedHeight(serializedObject, field, fieldName);
            Assert.That(ExtensionExecutionTrace.Entries, Is.EqualTo(expected));
        }

        private static object CreateDrawer(FieldInfo field)
        {
            Type drawerType = GetEditorType(
                "ActionAttribute.ActionPropertyDrawer");
            object drawer = drawerType.GetMethod("Create", StaticFlags)
                .Invoke(null, new object[] { field });
            drawerType.GetMethod("Initialize", InstanceFlags)
                .Invoke(drawer, null);
            return drawer;
        }

        private static ActionAttributeContext Context(object value,
            Type valueType, object owner = null)
        {
            return new ActionAttributeContext(owner, owner, value, valueType,
                "value", "value", false);
        }

        private static float GetCombinedHeight(
            SerializedObject serializedObject, string fieldName)
        {
            return GetCombinedHeight(serializedObject,
                typeof(ExtensionTarget).GetField(fieldName, InstanceFlags),
                fieldName);
        }

        private static GUIContent GetCombinedLabel(
            SerializedObject serializedObject, string fieldName)
        {
            Type drawerType = GetEditorType(
                "ActionAttribute.ActionPropertyDrawer");
            FieldInfo field = typeof(ExtensionTarget).GetField(fieldName,
                InstanceFlags);
            object drawer = CreateDrawer(field);
            SerializedProperty property = serializedObject.FindProperty(
                fieldName);
            return (GUIContent)drawerType.GetMethod("GetLabel", InstanceFlags)
                .Invoke(drawer, new object[]
                {
                    property,
                    new GUIContent(property.displayName, property.tooltip)
                });
        }

        private static float GetCombinedHeight(
            SerializedObject serializedObject, FieldInfo field,
            string propertyPath)
        {
            Type drawerType = GetEditorType(
                "ActionAttribute.ActionPropertyDrawer");
            object drawer = CreateDrawer(field);
            SerializedProperty property = serializedObject.FindProperty(
                propertyPath);
            var label = new GUIContent(property.displayName, property.tooltip);
            return (float)drawerType.GetMethod("GetPropertyHeight",
                    InstanceFlags)
                .Invoke(drawer, new object[] { property, label });
        }

        private static void ApplyValueLimits(SerializedObject serializedObject,
            string fieldName)
        {
            Type drawerType = GetEditorType(
                "ActionAttribute.ActionPropertyDrawer");
            FieldInfo field = typeof(ExtensionTarget).GetField(fieldName,
                InstanceFlags);
            object drawer = CreateDrawer(field);
            SerializedProperty property = serializedObject.FindProperty(
                fieldName);
            drawerType.GetMethod("ApplyValueLimits", InstanceFlags)
                .Invoke(drawer, new object[] { property });
        }

        private static MethodInfo FindExtensionValidationMethod()
        {
            Type drawerType = GetEditorType(
                "ActionAttribute.ActionPropertyDrawer");
            MethodInfo[] methods = drawerType.GetMethods(InstanceFlags);
            for (int i = 0; i < methods.Length; i++)
            {
                MethodInfo method = methods[i];
                if (method.Name != "TryGetValidationError") continue;
                ParameterInfo[] parameters = method.GetParameters();
                if (parameters.Length == 3 && parameters[1].ParameterType ==
                    typeof(ActionValidationAttribute)) return method;
            }
            Assert.Fail("找不到逻辑扩展校验入口。");
            return null;
        }

        private static void ClearFailureReports(params Type[] types)
        {
            Type drawerType = GetEditorType(
                "ActionAttribute.ActionPropertyDrawer");
            var reports = (HashSet<string>)drawerType.GetField(
                    "ReportedExtensionFailures", StaticFlags)
                .GetValue(null);
            reports.RemoveWhere(key => ContainsTypeName(key, types));
        }

        private static bool ContainsTypeName(string key, Type[] types)
        {
            for (int i = 0; i < types.Length; i++)
                if (key.IndexOf(types[i].FullName,
                    StringComparison.Ordinal) >= 0) return true;
            return false;
        }

        private static Type GetEditorType(string name)
        {
            Type type = Type.GetType(name + ", ActionAttribute.Editor");
            Assert.That(type, Is.Not.Null, "找不到编辑器类型：" + name);
            return type;
        }
    }
}
