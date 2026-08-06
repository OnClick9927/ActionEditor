using System;
using System.Reflection;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace ActionAttribute.Tests
{
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
            [ColorPalette("#ff0000", "00ff00", "#0000ff")]
            public Color palette = Color.white;
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
            Assert.That(new MaxLengthAttribute(-1).length, Is.Zero);
            var slider = new SliderAttribute(-2, 8);
            Assert.That(slider.min, Is.EqualTo(-2));
            Assert.That(slider.max, Is.EqualTo(8));
            Assert.That(new AssetPathAttribute(typeof(Texture2D)).assetType,
                Is.EqualTo(typeof(Texture2D)));
            Assert.That(new AssetGuidAttribute(typeof(Material)).assetType,
                Is.EqualTo(typeof(Material)));
            Assert.That(new ColorPaletteAttribute("#fff", "#000").colors,
                Has.Length.EqualTo(2));
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

                float previewHeight = GetCombinedHeight(serializedObject,
                    nameof(LayoutTarget.preview));
                Assert.That(previewHeight, Is.EqualTo(line + 48 + spacing));

                float paletteHeight = GetCombinedHeight(serializedObject,
                    nameof(LayoutTarget.palette));
                Assert.That(paletteHeight, Is.EqualTo(line * 2 + spacing));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(texture);
                UnityEngine.Object.DestroyImmediate(target);
            }
        }

        private static float GetCombinedHeight(SerializedObject serializedObject,
            string fieldName)
        {
            Type drawerType = GetEditorType("ActionAttribute.ActionPropertyDrawer");
            FieldInfo field = typeof(LayoutTarget).GetField(fieldName,
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

        private static Type GetEditorType(string name)
        {
            Type type = Type.GetType(name + ", ActionAttribute.Editor");
            Assert.That(type, Is.Not.Null, "找不到编辑器类型：" + name);
            return type;
        }
    }
}
