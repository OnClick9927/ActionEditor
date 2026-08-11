using System;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace ActionAttribute.Tests
{
    [Serializable]
    internal sealed class NativeDrawerValue
    {
        public int value;
    }

    [CustomPropertyDrawer(typeof(NativeDrawerValue))]
    internal sealed class NativeDrawerValueDrawer : PropertyDrawer
    {
        internal const float Height = 37f;

        public override float GetPropertyHeight(SerializedProperty property,
            GUIContent label)
        {
            return Height;
        }

        public override void OnGUI(Rect position, SerializedProperty property,
            GUIContent label)
        {
            EditorGUI.PropertyField(position,
                property.FindPropertyRelative(nameof(NativeDrawerValue.value)),
                label);
        }
    }

    [AttributeUsage(AttributeTargets.Field)]
    internal sealed class NativeProbeAttribute : UnityEngine.PropertyAttribute { }

    [AttributeUsage(AttributeTargets.Field)]
    internal sealed class NonNullConditionAttribute : ActionConditionAttribute
    {
        public NonNullConditionAttribute() : base(ConditionMode.Show) { }

        public override bool Evaluate(ActionAttributeContext context)
        {
            return context.Value != null;
        }
    }

    [CustomPropertyDrawer(typeof(NativeProbeAttribute))]
    internal sealed class NativeProbeAttributeDrawer : PropertyDrawer
    {
        internal const float ExtraHeight = 6f;

        public override float GetPropertyHeight(SerializedProperty property,
            GUIContent label)
        {
            return EditorGUI.GetPropertyHeight(property, label, true) +
                ExtraHeight;
        }

        public override void OnGUI(Rect position, SerializedProperty property,
            GUIContent label)
        {
            EditorGUI.PropertyField(position, property, label, true);
        }
    }

    public sealed class ActionAttributeEditorTests
    {
        private const BindingFlags StaticFlags = BindingFlags.Static |
            BindingFlags.Public | BindingFlags.NonPublic;

        private enum TestMode
        {
            [Name("基础模式", "用于验证枚举成员显示名称。")] Basic,
            Advanced
        }

        private sealed class InlineButtonTarget : ScriptableObject
        {
            public string value = "需要清空";

            private void ClearValue()
            {
                value = string.Empty;
            }
        }

        private sealed class LayoutTarget : ScriptableObject
        {
            [ProgressBar(0, 100)] public int progress = 50;
            [ShowAssetPreview(64, 48)] public Texture2D preview;
            [SuffixLabel("个")]
            public int affixedValue = 1;
            public List<int> plainList = new List<int>();
            [Name("命名列表")] public List<int> namedList = new List<int>();
            [ReadOnly] public List<int> readOnlyList = new List<int>();
            public int[] plainArray = Array.Empty<int>();
            [Header("Native Header"), Space(7), TextArea(2, 5)]
            public string nativeTextArea = "line one\nline two";
            [Header("Native Header"), Space(7), TextArea(2, 5)]
            [Name("Mixed Text Area")]
            public string mixedTextArea = "line one\nline two";
            [TextArea(2, 5)]
            public string nativePlaceholderTextArea = "line one\nline two";
            [TextArea(2, 5), Text(Placeholder = "请输入内容")]
            public string mixedPlaceholderTextArea = "line one\nline two";
            [Text(TextFieldMode.Password, Placeholder = "请输入口令")]
            public string password;
            [UnityEngine.Range(0, 10)] public int nativeRange = 5;
            [UnityEngine.Range(0, 10), ReadOnly] public int mixedRange = 5;
            [UnityEngine.Min(1), Name("Mixed Min")] public int mixedMin = 1;
            [Multiline(3), Name("Mixed Multiline")]
            public string mixedMultiline;
            [Delayed, Name("Mixed Delayed")] public string mixedDelayed;
            [ColorUsage(true, true), Name("Mixed Color")]
            public Color mixedColor = Color.white;
            [GradientUsage(true, ColorSpace.Linear), Name("Mixed Gradient")]
            public Gradient mixedGradient = new Gradient();
            [Tooltip("Native tooltip"), Name("Mixed Tooltip")]
            public int mixedTooltip;
            [NativeProbe] public int nativeProbe;
            [NativeProbe, Name("Mixed Probe")] public int mixedProbe;
            public NativeDrawerValue nativeDrawerValue =
                new NativeDrawerValue();
            [NonNullCondition]
            public NativeDrawerValue mixedDrawerValue =
                new NativeDrawerValue();
            public List<NativeDrawerValue> nativeDrawerList =
                new List<NativeDrawerValue> { new NativeDrawerValue() };
            [NonNullCondition]
            public List<NativeDrawerValue> mixedDrawerList =
                new List<NativeDrawerValue> { new NativeDrawerValue() };
            public NativeDrawerValue[] nativeDrawerArray =
                { new NativeDrawerValue() };
            [NonNullCondition]
            public NativeDrawerValue[] mixedDrawerArray =
                { new NativeDrawerValue() };
            [Name("命名数组")] public int[] namedArray = Array.Empty<int>();
        }

        private sealed class ConstraintTarget : ScriptableObject
        {
            [Slider(SliderMode.NonNegative)] public int count = -1;
            [Slider(SliderMode.Positive)] public int size;
            [Slider(0, 10, SliderMode.Clamp)] public int clamped = 12;
            [Slider(0, 3, SliderMode.Wrap)] public int wrapped = 4;
            [Slider(0, 10)] public float ranged = 12;
        }

        private sealed class ScriptLookupProbe
        {
        }

        private sealed class DropdownSearchTarget : ScriptableObject
        {
            [ValueDropdown(ValueDropdownSource.Search, nameof(SearchValues))]
            public string selected;

            private IEnumerable<ValueDropdownItem<string>> SearchValues(
                string query)
            {
                var values = new[]
                {
                    new ValueDropdownItem<string>("Alpha", "a"),
                    new ValueDropdownItem<string>("Beta", "b")
                };
                for (int i = 0; i < values.Length; i++)
                    if (values[i].text.IndexOf(query,
                        StringComparison.OrdinalIgnoreCase) >= 0)
                        yield return values[i];
            }
        }

        [Test]
        public void ConsolidatedAttributesCoverFormerVariants()
        {
            var enumDropdown = new ValueDropdownAttribute();
            var memberDropdown = new ValueDropdownAttribute("Values");
            var folder = new PathAttribute(PathType.Folder,
                absolutePath: true);
            var parents = new ObjectsOnlyAttribute(ObjectSource.Parents, true);
            var sceneObjects = new ObjectsOnlyAttribute(ObjectSource.Scene);

            Assert.That(enumDropdown.valuesMember, Is.Null);
            Assert.That(memberDropdown.valuesMember, Is.EqualTo("Values"));
            Assert.That(folder.type, Is.EqualTo(PathType.Folder));
            Assert.That(folder.absolutePath, Is.True);
            Assert.That(parents.source, Is.EqualTo(ObjectSource.Parents));
            Assert.That(parents.includeSelf, Is.True);
            Assert.That(sceneObjects.source, Is.EqualTo(ObjectSource.Scene));
            Assert.That(sceneObjects.includeSelf, Is.False);
            Assert.That(new OnValueChangedAttribute("Validate",
                    ValueChangedMode.Validate).mode,
                Is.EqualTo(ValueChangedMode.Validate));
            var searchDropdown = new ValueDropdownAttribute(
                ValueDropdownSource.Search, "SearchValues");
            Assert.That(searchDropdown.source,
                Is.EqualTo(ValueDropdownSource.Search));
            Assert.That(searchDropdown.valuesMember, Is.EqualTo("SearchValues"));
            Assert.That(new ConditionAttribute(ConditionMode.Disable,
                    InspectorMode.PlayMode)
                .usesInspectorMode, Is.True);
            Assert.That(new ConditionAttribute(ConditionMode.Hide,
                    InspectorMode.EditMode)
                .inspectorMode, Is.EqualTo(InspectorMode.EditMode));
            Assert.That(new SliderAttribute(0, 10, SliderMode.Clamp).mode,
                Is.EqualTo(SliderMode.Clamp));
            Assert.That(new SliderAttribute(2, SliderMode.Minimum).min,
                Is.EqualTo(2));
            var stepped = new SliderAttribute(5, SliderMode.Step);
            Assert.That(stepped.Step, Is.EqualTo(5));
            FieldInfo rangedField = typeof(ConstraintTarget).GetField(
                nameof(ConstraintTarget.ranged), BindingFlags.Instance |
                BindingFlags.Public | BindingFlags.NonPublic);
            var ranged = rangedField.GetCustomAttribute<SliderAttribute>();
            Assert.That(ranged.mode, Is.EqualTo(SliderMode.Slider));
            Assert.That(ranged.min, Is.Zero);
            Assert.That(ranged.max, Is.EqualTo(10));
            var clampedStep = new SliderAttribute(0, 10, SliderMode.Clamp)
            {
                Step = 2,
                Origin = 1
            };
            Assert.That(clampedStep.Step, Is.EqualTo(2));
            Assert.That(clampedStep.Origin, Is.EqualTo(1));
            var text = new TextAttribute(TextFieldMode.Password)
            {
                Placeholder = "secret",
                MaxLength = 8
            };
            Assert.That(text.mode, Is.EqualTo(TextFieldMode.Password));
            Assert.That(text.Placeholder, Is.EqualTo("secret"));
            Assert.That(text.Apply("123456789"), Is.EqualTo("12345678"));
            var collection = new CollectionAttribute(1, 4,
                CollectionItemRule.Unique);
            Assert.That(collection.min, Is.EqualTo(1));
            Assert.That(collection.max, Is.EqualTo(4));
            Assert.That(collection.itemRules,
                Is.EqualTo(CollectionItemRule.Unique));
            var group = new GroupAttribute("Layout", GroupType.Tab)
            {
                Tab = "General"
            };
            Assert.That(group.type, Is.EqualTo(GroupType.Tab));
            Assert.That(group.Tab, Is.EqualTo("General"));
            var grid = new GroupAttribute("Grid", GroupType.Grid)
            {
                Columns = 3
            };
            var scroll = new GroupAttribute("Scroll", GroupType.Scroll)
            {
                Height = 96
            };
            Assert.That(grid.Columns, Is.EqualTo(3));
            Assert.That(scroll.Height, Is.EqualTo(96));
            Assert.That(Enum.IsDefined(typeof(GroupType), GroupType.FoldoutBox),
                Is.True);
            Assert.That(Enum.IsDefined(typeof(GroupType), GroupType.Indent),
                Is.True);

            Assembly assembly = typeof(ActionAttributeBase).Assembly;
            Assert.That(assembly.GetType("ActionAttribute.SearchableAttribute"),
                Is.Null);
            Assert.That(assembly.GetType("ActionAttribute.DropdownAttribute"),
                Is.Null);
            Assert.That(assembly.GetType("ActionAttribute.AllowNestingAttribute"),
                Is.Null);
            Assert.That(assembly.GetType("ActionAttribute.ClampAttribute"),
                Is.Null);
            Assert.That(assembly.GetType("ActionAttribute.BoxGroupAttribute"),
                Is.Null);
            Assert.That(assembly.GetType("ActionAttribute.EnumFlagsAttribute"),
                Is.Null);
            Assert.That(assembly.GetType("ActionAttribute.AssetsOnlyAttribute"),
                Is.Null);
            Assert.That(assembly.GetType(
                "ActionAttribute.SceneObjectsOnlyAttribute"), Is.Null);
            Assert.That(assembly.GetType(
                "ActionAttribute.HierarchyObjectsOnlyAttribute"), Is.Null);
            Assert.That(assembly.GetType(
                "ActionAttribute.ValidateInputAttribute"), Is.Null);
            Assert.That(assembly.GetType(
                "ActionAttribute.OnInspectorInitAttribute"), Is.Null);
            Assert.That(assembly.GetType(
                "ActionAttribute.OnInspectorGUIAttribute"), Is.Null);
            Assert.That(assembly.GetType(
                "ActionAttribute.AssetPathAttribute"), Is.Null);
            Assert.That(assembly.GetType(
                "ActionAttribute.AssetGuidAttribute"), Is.Null);
            Assert.That(assembly.GetType(
                "ActionAttribute.ColorPaletteAttribute"), Is.Null);
            Assert.That(assembly.GetType(
                "ActionAttribute.PrefixLabelAttribute"), Is.Null);
            Assert.That(assembly.GetType(
                "ActionAttribute.PropertySpaceAttribute"), Is.Null);
            Assert.That(assembly.GetType(
                "ActionAttribute.PropertyOrderAttribute"), Is.Null);
            Assert.That(assembly.GetType(
                "ActionAttribute.ReorderableListAttribute"), Is.Null);
            Assert.That(assembly.GetType(
                "ActionAttribute.ResizableTextAreaAttribute"), Is.Null);
            Assert.That(assembly.GetType(
                "ActionAttribute.SearchSourceAttribute"), Is.Null);
            Assert.That(assembly.GetType("ActionAttribute.TagAttribute"), Is.Null);
            Assert.That(assembly.GetType("ActionAttribute.LayerAttribute"), Is.Null);
            Assert.That(assembly.GetType(
                "ActionAttribute.SortingLayerAttribute"), Is.Null);
            Assert.That(assembly.GetType(
                "ActionAttribute.SceneNameAttribute"), Is.Null);
            Assert.That(assembly.GetType(
                "ActionAttribute.InputAxisAttribute"), Is.Null);
            Assert.That(assembly.GetType(
                "ActionAttribute.AnimatorParamAttribute"), Is.Null);
            Assert.That(new TypeInfoBoxAttribute("Type help").type,
                Is.EqualTo(InspectorMessageType.Info));
            Assert.That(assembly.GetType(
                "ActionAttribute.HideIfAttribute"), Is.Null);
            Assert.That(assembly.GetType(
                "ActionAttribute.EnableIfAttribute"), Is.Null);
            Assert.That(assembly.GetType(
                "ActionAttribute.DisableIfAttribute"), Is.Null);
            Assert.That(assembly.GetType(
                "ActionAttribute.StepAttribute"), Is.Null);
            Assert.That(assembly.GetType(
                "ActionAttribute.TitleAttribute"), Is.Null);
            Assert.That(assembly.GetType(
                "ActionAttribute.GUIColorAttribute"), Is.Null);
            Assert.That(assembly.GetType(
                "ActionAttribute.UniqueListAttribute"), Is.Null);
            Assert.That(assembly.GetType("ActionAttribute.ShowIfAttribute"),
                Is.Null);
        }

        [Test]
        public void RuntimeAttributesRejectAliasesAndExposeLogicExtensions()
        {
            Type[] types = typeof(ActionAttributeBase).Assembly.GetTypes();
            for (int i = 0; i < types.Length; i++)
            {
                Type type = types[i];
                if (type.IsAbstract ||
                    !typeof(Attribute).IsAssignableFrom(type))
                    continue;
                Assert.That(type.IsSealed, Is.True,
                    type.FullName + " 必须 sealed，禁止派生别名特性。");
            }

            ConstructorInfo baseConstructor = typeof(ActionAttributeBase)
                .GetConstructor(BindingFlags.Instance |
                    BindingFlags.NonPublic, null, Type.EmptyTypes, null);
            Assert.That(baseConstructor, Is.Not.Null);
            Assert.That(baseConstructor.IsAssembly, Is.True,
                "ActionAttributeBase 只能由 Runtime 程序集内的扩展点继承。");
            Assert.That(typeof(ActionConditionAttribute).IsAbstract, Is.True);
            Assert.That(typeof(ActionLabelAttribute).IsAbstract, Is.True);
            Assert.That(typeof(ActionMessageAttribute).IsAbstract, Is.True);
            Assert.That(typeof(ActionValidationAttribute).IsAbstract, Is.True);
            Assert.That(typeof(ActionValueModifierAttribute).IsAbstract,
                Is.True);

            var context = new ActionAttributeContext(new object(), new object(),
                5, typeof(int), "value", "value", false);
            Assert.That(context.TryGetValue<double>(out double converted),
                Is.True);
            Assert.That(converted, Is.EqualTo(5));
        }

        [Test]
        public void ExternalAttributesAreRoutedAndExecutedWithoutCustomDrawers()
        {
            Type targetType = GetExampleType("ExtensibleAttributeExample");
            Type conditionType = GetExampleType(
                "PositiveValueConditionAttribute");
            Type validationType = GetExampleType("EvenValueAttribute");
            Type modifierType = GetExampleType("ScaleValueAttribute");
            Type messageType = GetExampleType("LimitMessageAttribute");
            Type labelType = GetExampleType("CurrentValueLabelAttribute");

            Assert.That(conditionType.BaseType,
                Is.EqualTo(typeof(ActionConditionAttribute)));
            Assert.That(validationType.BaseType,
                Is.EqualTo(typeof(ActionValidationAttribute)));
            Assert.That(modifierType.BaseType,
                Is.EqualTo(typeof(ActionValueModifierAttribute)));
            Assert.That(messageType.BaseType,
                Is.EqualTo(typeof(ActionMessageAttribute)));
            Assert.That(labelType.BaseType,
                Is.EqualTo(typeof(ActionLabelAttribute)));
            AssertUnityRoutesToActionDrawer(conditionType);
            AssertUnityRoutesToActionDrawer(validationType);
            AssertUnityRoutesToActionDrawer(modifierType);
            AssertUnityRoutesToActionDrawer(messageType);
            AssertUnityRoutesToActionDrawer(labelType);

            var gameObject = new GameObject("ExtensibleAttributeTest");
            Component target = gameObject.AddComponent(targetType);
            try
            {
                var serializedObject = new SerializedObject(target);
                float hiddenHeight = GetCombinedHeight(serializedObject,
                    targetType, "positiveOnly");
                Assert.That(hiddenHeight, Is.Zero);

                float validationHeight = GetCombinedHeight(serializedObject,
                    targetType, "evenValue");
                Assert.That(validationHeight,
                    Is.GreaterThan(EditorGUIUtility.singleLineHeight));

                ApplyValueLimits(serializedObject, targetType, "percent");
                ApplyValueLimits(serializedObject, targetType, "orderedValue");
                serializedObject.ApplyModifiedPropertiesWithoutUndo();
                Assert.That(targetType.GetField("percent").GetValue(target),
                    Is.EqualTo(100));
                Assert.That(targetType.GetField("orderedValue").GetValue(target),
                    Is.EqualTo(11));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(gameObject);
            }
        }

        [Test]
        public void NameAttribute_ProvidesEnumDisplayNameAndTooltip()
        {
            FieldInfo field = typeof(TestMode).GetField(nameof(TestMode.Basic));
            var attribute = field.GetCustomAttribute<NameAttribute>();

            Assert.That(attribute.name, Is.EqualTo("基础模式"));
            Assert.That(attribute.comment, Is.EqualTo("用于验证枚举成员显示名称。"));

            Type utility = GetEditorType("ActionAttribute.EnumDisplayUtility");
            var names = (string[])utility.GetMethod("GetNames", StaticFlags)
                .Invoke(null, new object[] { typeof(TestMode) });
            Assert.That(names, Is.EqualTo(new[] { "基础模式", "Advanced" }));
        }

        [Test]
        public void ValueDropdown_SupportsQueryDrivenPrivateMethod()
        {
            DropdownSearchTarget target = ScriptableObject.CreateInstance<
                DropdownSearchTarget>();
            try
            {
                var serializedObject = new SerializedObject(target);
                SerializedProperty property = serializedObject.FindProperty(
                    nameof(DropdownSearchTarget.selected));
                Type utility = GetEditorType(
                    "ActionAttribute.SerializedPropertyMemberUtility");
                MethodInfo method = utility.GetMethod("TryGetSearchValues",
                    StaticFlags);
                object[] arguments =
                {
                    property, "SearchValues", "bet", null, null
                };

                Assert.That((bool)method.Invoke(null, arguments), Is.True);
                Assert.That((string[])arguments[3], Is.EqualTo(
                    new[] { "Beta" }));
                Assert.That((object[])arguments[4], Is.EqualTo(
                    new object[] { "b" }));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(target);
            }
        }

        [Test]
        public void LocateScript_FindsTypeWhenFileNameDiffers()
        {
            string path = EditorEX.LocateScript(typeof(ScriptLookupProbe));
            Assert.That(path.Replace('\\', '/'),
                Does.EndWith("ActionAttributeEditorTests.cs"));
        }

        [Test]
        public void ValueConstraints_SnapAndTruncateValues()
        {
            Type utility = GetEditorType("ActionAttribute.ValueConstraintUtility");
            MethodInfo snap = utility.GetMethod("Snap", StaticFlags);
            MethodInfo truncate = utility.GetMethod("Truncate", StaticFlags);

            Assert.That((double)snap.Invoke(null, new object[] { 12d, 5d, 0d }),
                Is.EqualTo(10d));
            Assert.That((double)snap.Invoke(null, new object[] { 13d, 5d, 0d }),
                Is.EqualTo(15d));
            Assert.That((string)truncate.Invoke(null,
                new object[] { "123456", 4 }), Is.EqualTo("1234"));
            Assert.That(new TextAttribute { MaxLength = -1 }.Apply("123456"),
                Is.Empty);
            var slider = new SliderAttribute(-2, 8);
            Assert.That(slider.min, Is.EqualTo(-2));
            Assert.That(slider.max, Is.EqualTo(8));
            var assetPath = new PathAttribute(typeof(Texture2D));
            Assert.That(assetPath.type, Is.EqualTo(PathType.Asset));
            Assert.That(assetPath.assetType, Is.EqualTo(typeof(Texture2D)));
        }

        [Test]
        public void InlineButton_InvokesPrivateMethodAndMarksTargetDirty()
        {
            InlineButtonTarget target = ScriptableObject.CreateInstance<
                InlineButtonTarget>();
            try
            {
                var serializedObject = new SerializedObject(target);
                SerializedProperty property = serializedObject.FindProperty("value");
                Type utility = GetEditorType(
                    "ActionAttribute.SerializedPropertyMemberUtility");
                utility.GetMethod("InvokeMethod", StaticFlags).Invoke(null,
                    new object[] { property, "ClearValue" });

                Assert.That(target.value, Is.Empty);
                Assert.That(EditorUtility.IsDirty(target), Is.True);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(target);
            }
        }

        [Test]
        public void AdditionalControls_ReserveTheirFullInspectorHeight()
        {
            LayoutTarget target = ScriptableObject.CreateInstance<LayoutTarget>();
            var texture = new Texture2D(2, 2);
            target.preview = texture;
            try
            {
                var serializedObject = new SerializedObject(target);
                float spacing = EditorGUIUtility.standardVerticalSpacing;
                float line = EditorGUIUtility.singleLineHeight;

                float progressHeight = GetCombinedHeight(serializedObject,
                    nameof(LayoutTarget.progress));
                Assert.That(progressHeight, Is.EqualTo(line * 2 + spacing));
                float progressFieldHeight = GetCombinedFieldHeight(
                    serializedObject, nameof(LayoutTarget.progress));
                Assert.That(progressFieldHeight, Is.EqualTo(line));

                float previewHeight = GetCombinedHeight(serializedObject,
                    nameof(LayoutTarget.preview));
                Assert.That(previewHeight, Is.EqualTo(line + 48 + spacing));

                float affixedHeight = GetCombinedHeight(serializedObject,
                    nameof(LayoutTarget.affixedValue));
                Assert.That(affixedHeight, Is.EqualTo(line));

            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(texture);
                UnityEngine.Object.DestroyImmediate(target);
            }
        }

        [Test]
        public void DecoratedCollectionsUseUnitysCompletePropertyHeight()
        {
            LayoutTarget target = ScriptableObject.CreateInstance<LayoutTarget>();
            try
            {
                var serializedObject = new SerializedObject(target);
                AssertMatchingCollectionHeight(serializedObject,
                    nameof(LayoutTarget.plainList), nameof(LayoutTarget.namedList));
                AssertMatchingCollectionHeight(serializedObject,
                    nameof(LayoutTarget.plainList),
                    nameof(LayoutTarget.readOnlyList));
                AssertMatchingCollectionHeight(serializedObject,
                    nameof(LayoutTarget.plainArray),
                    nameof(LayoutTarget.namedArray));

                target.plainList.AddRange(new[] { 1, 2 });
                target.namedList.AddRange(new[] { 1, 2 });
                target.readOnlyList.AddRange(new[] { 1, 2 });
                target.plainArray = new[] { 1, 2 };
                target.namedArray = new[] { 1, 2 };
                serializedObject.Update();

                AssertMatchingCollectionHeight(serializedObject,
                    nameof(LayoutTarget.plainList), nameof(LayoutTarget.namedList));
                AssertMatchingCollectionHeight(serializedObject,
                    nameof(LayoutTarget.plainList),
                    nameof(LayoutTarget.readOnlyList));
                AssertMatchingCollectionHeight(serializedObject,
                    nameof(LayoutTarget.plainArray),
                    nameof(LayoutTarget.namedArray));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(target);
            }
        }

        [Test]
        public void UnityPropertyAttributesCanBeMixedWithActionAttributes()
        {
            Assert.That(new NameAttribute("Mixed").order,
                Is.LessThan(new UnityEngine.RangeAttribute(0, 1).order));
            LayoutTarget target = ScriptableObject.CreateInstance<LayoutTarget>();
            try
            {
                var serializedObject = new SerializedObject(target);
                AssertNativeDecorators(nameof(LayoutTarget.mixedTextArea),
                    "HeaderDrawer", "SpaceDrawer");
                AssertMatchingHeight(serializedObject,
                    nameof(LayoutTarget.nativeRange),
                    nameof(LayoutTarget.mixedRange));
                Assert.That(UsesSingleLineActionControl(serializedObject,
                    nameof(LayoutTarget.mixedPlaceholderTextArea)), Is.False);
                Assert.That(UsesSingleLineActionControl(serializedObject,
                    nameof(LayoutTarget.password)), Is.True);

                GUIContent label = GetCombinedLabel(serializedObject,
                    nameof(LayoutTarget.mixedTooltip));
                Assert.That(label.text, Is.EqualTo("Mixed Tooltip"));
                Assert.That(label.tooltip, Is.EqualTo("Native tooltip"));

                AssertNativeDrawer(nameof(LayoutTarget.mixedRange),
                    "RangeDrawer");
                AssertNativeDrawer(nameof(LayoutTarget.mixedMin), "MinDrawer");
                AssertNativeDrawer(nameof(LayoutTarget.mixedTextArea),
                    "TextAreaDrawer");
                AssertNativeDrawer(nameof(LayoutTarget.mixedPlaceholderTextArea),
                    "TextAreaDrawer");
                AssertNativeDrawer(nameof(LayoutTarget.mixedMultiline),
                    "MultilineDrawer");
                AssertNativeDrawer(nameof(LayoutTarget.mixedDelayed),
                    "DelayedDrawer");
                AssertNativeDrawer(nameof(LayoutTarget.mixedColor),
                    "ColorUsageDrawer");
                AssertNativeDrawer(nameof(LayoutTarget.mixedGradient),
                    "GradientUsageDrawer");
                AssertNativeDrawer(nameof(LayoutTarget.mixedProbe),
                    nameof(NativeProbeAttributeDrawer));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(target);
            }
        }

        [Test]
        public void UnityCustomDrawersSurviveActionAttributesAndCollections()
        {
            AssertUnityRoutesToActionDrawer(typeof(NonNullConditionAttribute));
            LayoutTarget target = ScriptableObject.CreateInstance<LayoutTarget>();
            try
            {
                var serializedObject = new SerializedObject(target);
                AssertUnityMatchingHeight(serializedObject,
                    nameof(LayoutTarget.nativeProbe),
                    nameof(LayoutTarget.mixedProbe));
                AssertUnityMatchingHeight(serializedObject,
                    nameof(LayoutTarget.nativeDrawerValue),
                    nameof(LayoutTarget.mixedDrawerValue));
                AssertNativeDrawer(nameof(LayoutTarget.mixedDrawerValue),
                    nameof(NativeDrawerValueDrawer));

                SerializedProperty nativeList = serializedObject.FindProperty(
                    nameof(LayoutTarget.nativeDrawerList));
                SerializedProperty mixedList = serializedObject.FindProperty(
                    nameof(LayoutTarget.mixedDrawerList));
                nativeList.isExpanded = true;
                mixedList.isExpanded = true;
                float nativeListHeight = EditorGUI.GetPropertyHeight(nativeList,
                    new GUIContent(nativeList.displayName), true);
                float mixedListHeight = EditorGUI.GetPropertyHeight(mixedList,
                    new GUIContent(mixedList.displayName), true);
                Assert.That(mixedListHeight,
                    Is.EqualTo(nativeListHeight).Within(0.01f));

                SerializedProperty nativeElement =
                    nativeList.GetArrayElementAtIndex(0);
                SerializedProperty mixedElement =
                    mixedList.GetArrayElementAtIndex(0);
                float nativeElementHeight = EditorGUI.GetPropertyHeight(
                    nativeElement, new GUIContent(nativeElement.displayName),
                    true);
                float mixedElementHeight = EditorGUI.GetPropertyHeight(
                    mixedElement, new GUIContent(mixedElement.displayName), true);
                Assert.That(nativeElementHeight,
                    Is.EqualTo(NativeDrawerValueDrawer.Height).Within(0.01f));
                Assert.That(mixedElementHeight,
                    Is.EqualTo(nativeElementHeight).Within(0.01f));

                SerializedProperty nativeArray = serializedObject.FindProperty(
                    nameof(LayoutTarget.nativeDrawerArray));
                SerializedProperty mixedArray = serializedObject.FindProperty(
                    nameof(LayoutTarget.mixedDrawerArray));
                nativeArray.isExpanded = true;
                mixedArray.isExpanded = true;
                float nativeArrayHeight = EditorGUI.GetPropertyHeight(
                    nativeArray, new GUIContent(nativeArray.displayName), true);
                float mixedArrayHeight = EditorGUI.GetPropertyHeight(
                    mixedArray, new GUIContent(mixedArray.displayName), true);
                Assert.That(mixedArrayHeight,
                    Is.EqualTo(nativeArrayHeight).Within(0.01f));

                SerializedProperty nativeArrayElement =
                    nativeArray.GetArrayElementAtIndex(0);
                SerializedProperty mixedArrayElement =
                    mixedArray.GetArrayElementAtIndex(0);
                float nativeArrayElementHeight = EditorGUI.GetPropertyHeight(
                    nativeArrayElement,
                    new GUIContent(nativeArrayElement.displayName), true);
                float mixedArrayElementHeight = EditorGUI.GetPropertyHeight(
                    mixedArrayElement,
                    new GUIContent(mixedArrayElement.displayName), true);
                Assert.That(nativeArrayElementHeight,
                    Is.EqualTo(NativeDrawerValueDrawer.Height).Within(0.01f));
                Assert.That(mixedArrayElementHeight,
                    Is.EqualTo(nativeArrayElementHeight).Within(0.01f));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(target);
            }
        }

        [Test]
        public void NumericSignConstraints_ClampIntegerValues()
        {
            ConstraintTarget target = ScriptableObject.CreateInstance<
                ConstraintTarget>();
            try
            {
                var serializedObject = new SerializedObject(target);
                ApplyValueLimits(serializedObject, nameof(ConstraintTarget.count));
                ApplyValueLimits(serializedObject, nameof(ConstraintTarget.size));
                ApplyValueLimits(serializedObject,
                    nameof(ConstraintTarget.clamped));
                ApplyValueLimits(serializedObject,
                    nameof(ConstraintTarget.wrapped));
                ApplyValueLimits(serializedObject,
                    nameof(ConstraintTarget.ranged));
                serializedObject.ApplyModifiedPropertiesWithoutUndo();
                Assert.That(target.count, Is.Zero);
                Assert.That(target.size, Is.EqualTo(1));
                Assert.That(target.clamped, Is.EqualTo(10));
                Assert.That(target.wrapped, Is.Zero);
                Assert.That(target.ranged, Is.EqualTo(10));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(target);
            }
        }

        private static float GetCombinedHeight(SerializedObject serializedObject,
            string fieldName)
        {
            return GetCombinedHeight(serializedObject, typeof(LayoutTarget),
                fieldName);
        }

        private static float GetCombinedHeight(SerializedObject serializedObject,
            Type targetType, string fieldName)
        {
            Type drawerType = GetEditorType("ActionAttribute.ActionPropertyDrawer");
            FieldInfo field = targetType.GetField(fieldName,
                BindingFlags.Instance | BindingFlags.Public |
                BindingFlags.NonPublic);
            object drawer = drawerType.GetMethod("Create", StaticFlags)
                .Invoke(null, new object[] { field });
            SerializedProperty property = serializedObject.FindProperty(fieldName);
            var label = new GUIContent(property.displayName, property.tooltip);
            MethodInfo method = drawerType.GetMethod("GetPropertyHeight",
                BindingFlags.Instance | BindingFlags.Public);
            return (float)method.Invoke(drawer,
                new object[] { property, label });
        }

        private static float GetCombinedFieldHeight(
            SerializedObject serializedObject, string fieldName)
        {
            Type drawerType = GetEditorType("ActionAttribute.ActionPropertyDrawer");
            FieldInfo field = typeof(LayoutTarget).GetField(fieldName,
                BindingFlags.Instance | BindingFlags.Public |
                BindingFlags.NonPublic);
            object drawer = drawerType.GetMethod("Create", StaticFlags)
                .Invoke(null, new object[] { field });
            SerializedProperty property = serializedObject.FindProperty(fieldName);
            var label = new GUIContent(property.displayName, property.tooltip);
            MethodInfo method = drawerType.GetMethod("GetFieldHeight",
                BindingFlags.Instance | BindingFlags.NonPublic);
            return (float)method.Invoke(drawer,
                new object[] { property, label, 320f });
        }

        private static bool UsesSingleLineActionControl(
            SerializedObject serializedObject, string fieldName)
        {
            Type drawerType = GetEditorType("ActionAttribute.ActionPropertyDrawer");
            FieldInfo field = typeof(LayoutTarget).GetField(fieldName,
                BindingFlags.Instance | BindingFlags.Public |
                BindingFlags.NonPublic);
            object drawer = drawerType.GetMethod("Create", StaticFlags)
                .Invoke(null, new object[] { field });
            drawerType.GetMethod("Initialize",
                    BindingFlags.Instance | BindingFlags.NonPublic)
                .Invoke(drawer, null);
            SerializedProperty property = serializedObject.FindProperty(fieldName);
            return (bool)drawerType.GetMethod("UsesSingleLineActionControl",
                    BindingFlags.Instance | BindingFlags.NonPublic)
                .Invoke(drawer, new object[] { property });
        }

        private static void AssertMatchingCollectionHeight(
            SerializedObject serializedObject, string plainField,
            string decoratedField)
        {
            SerializedProperty plain = serializedObject.FindProperty(plainField);
            SerializedProperty decorated = serializedObject.FindProperty(
                decoratedField);
            plain.isExpanded = true;
            decorated.isExpanded = true;
            float expected = EditorGUI.GetPropertyHeight(plain,
                new GUIContent(plain.displayName, plain.tooltip), true);
            float actual = GetCombinedHeight(serializedObject, decoratedField);
            Assert.That(actual, Is.EqualTo(expected).Within(0.01f),
                decoratedField + " did not reserve Unity's complete collection height.");
        }

        private static void AssertMatchingHeight(SerializedObject serializedObject,
            string nativeField, string mixedField)
        {
            SerializedProperty native = serializedObject.FindProperty(nativeField);
            float expected = EditorGUI.GetPropertyHeight(native,
                new GUIContent(native.displayName, native.tooltip), true);
            float actual = GetCombinedHeight(serializedObject, mixedField);
            Assert.That(actual, Is.EqualTo(expected).Within(0.01f),
                mixedField + " did not preserve Unity's native drawer layout.");
        }

        private static void AssertUnityMatchingHeight(
            SerializedObject serializedObject, string nativeField,
            string mixedField)
        {
            SerializedProperty native = serializedObject.FindProperty(nativeField);
            SerializedProperty mixed = serializedObject.FindProperty(mixedField);
            float expected = EditorGUI.GetPropertyHeight(native,
                new GUIContent(native.displayName, native.tooltip), true);
            float actual = EditorGUI.GetPropertyHeight(mixed,
                new GUIContent(mixed.displayName, mixed.tooltip), true);
            Assert.That(actual, Is.EqualTo(expected).Within(0.01f),
                mixedField + " did not preserve Unity's handler chain.");
        }

        private static GUIContent GetCombinedLabel(
            SerializedObject serializedObject, string fieldName)
        {
            Type drawerType = GetEditorType("ActionAttribute.ActionPropertyDrawer");
            FieldInfo field = typeof(LayoutTarget).GetField(fieldName,
                BindingFlags.Instance | BindingFlags.Public |
                BindingFlags.NonPublic);
            object drawer = drawerType.GetMethod("Create", StaticFlags)
                .Invoke(null, new object[] { field });
            SerializedProperty property = serializedObject.FindProperty(fieldName);
            var original = new GUIContent(property.displayName, property.tooltip);
            return (GUIContent)drawerType.GetMethod("GetLabel",
                    BindingFlags.Instance | BindingFlags.NonPublic)
                .Invoke(drawer, new object[] { property, original });
        }

        private static void AssertNativeDrawer(string fieldName,
            string expectedTypeName)
        {
            FieldInfo field = typeof(LayoutTarget).GetField(fieldName,
                BindingFlags.Instance | BindingFlags.Public |
                BindingFlags.NonPublic);
            object[] values = field.GetCustomAttributes(
                typeof(UnityEngine.PropertyAttribute), true);
            for (int i = 0; i < values.Length; i++)
            {
                if (values[i] is ActionAttributeBase) continue;
                Type drawer = ResolveUnityDrawerType(values[i].GetType());
                if (drawer != null && drawer.Name == expectedTypeName) return;
            }
            Type fieldDrawer = ResolveUnityDrawerType(field.FieldType);
            Assert.That(fieldDrawer?.Name, Is.EqualTo(expectedTypeName));
        }

        private static void AssertNativeDecorators(string fieldName,
            params string[] expectedTypeNames)
        {
            FieldInfo field = typeof(LayoutTarget).GetField(fieldName,
                BindingFlags.Instance | BindingFlags.Public |
                BindingFlags.NonPublic);
            object[] values = field.GetCustomAttributes(
                typeof(UnityEngine.PropertyAttribute), true);
            var decorators = new List<string>();
            for (int i = 0; i < values.Length; i++)
            {
                if (values[i] is ActionAttributeBase) continue;
                Type drawer = ResolveUnityDrawerType(values[i].GetType());
                if (drawer != null &&
                    typeof(DecoratorDrawer).IsAssignableFrom(drawer))
                    decorators.Add(drawer.Name);
            }
            string[] actualTypeNames = decorators.ToArray();
            Assert.That(actualTypeNames, Is.EqualTo(expectedTypeNames));
        }

        private static void ApplyValueLimits(SerializedObject serializedObject,
            string fieldName)
        {
            ApplyValueLimits(serializedObject, typeof(ConstraintTarget),
                fieldName);
        }

        private static void ApplyValueLimits(SerializedObject serializedObject,
            Type targetType, string fieldName)
        {
            Type drawerType = GetEditorType("ActionAttribute.ActionPropertyDrawer");
            FieldInfo field = targetType.GetField(fieldName,
                BindingFlags.Instance | BindingFlags.Public |
                BindingFlags.NonPublic);
            object drawer = drawerType.GetMethod("Create", StaticFlags)
                .Invoke(null, new object[] { field });
            SerializedProperty property = serializedObject.FindProperty(fieldName);
            drawerType.GetMethod("Initialize", BindingFlags.Instance |
                BindingFlags.NonPublic).Invoke(drawer, null);
            drawerType.GetMethod("ApplyValueLimits", BindingFlags.Instance |
                BindingFlags.NonPublic).Invoke(drawer, new object[] { property });
        }

        private static Type GetExampleType(string name)
        {
            Type type = Type.GetType("ActionAttribute.Examples." + name +
                ", Assembly-CSharp");
            Assert.That(type, Is.Not.Null, "找不到外部扩展示例类型：" + name);
            return type;
        }

        private static void AssertUnityRoutesToActionDrawer(Type attributeType)
        {
            Type drawerType = ResolveUnityDrawerType(attributeType);
            Assert.That(drawerType, Is.Not.Null,
                attributeType.FullName + " 未被 Unity 路由到组合 Drawer。");
            Assert.That(GetEditorType("ActionAttribute.ActionPropertyDrawer")
                .IsAssignableFrom(drawerType), Is.True);
        }

        private static Type ResolveUnityDrawerType(Type targetType)
        {
            Type utility = typeof(EditorGUI).Assembly.GetType(
                "UnityEditor.ScriptAttributeUtility");
            MethodInfo resolver = utility?.GetMethod("GetDrawerTypeForType",
                StaticFlags, null, new[] { typeof(Type) }, null);
            Assert.That(resolver, Is.Not.Null);
            return resolver.Invoke(null, new object[] { targetType }) as Type;
        }

        private static Type GetEditorType(string name)
        {
            Type type = Type.GetType(name + ", ActionAttribute.Editor");
            Assert.That(type, Is.Not.Null, "找不到编辑器类型：" + name);
            return type;
        }
    }
}
