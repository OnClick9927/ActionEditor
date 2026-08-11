using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace ActionAttribute
{
    internal abstract class ActionPropertyDrawer : PropertyDrawer
    {
        private static readonly HashSet<PropertyKey> DrawingProperties = new();
        private static readonly HashSet<PropertyKey> MeasuringProperties = new();
        private static readonly HashSet<string> ReportedExtensionFailures = new();
        private static GUIStyle placeholderStyle;
        private bool initialized;
        private bool readOnly;
        private bool collectionField;
        private bool toggleLeft;
        private ObjectsOnlyAttribute objectsOnly;
        private bool expandable;
        private GUIContent nameLabel;
        private ConditionAttribute[] conditions = Array.Empty<ConditionAttribute>();
        private ActionConditionAttribute[] extensionConditions =
            Array.Empty<ActionConditionAttribute>();
        private ActionLabelAttribute[] extensionLabels =
            Array.Empty<ActionLabelAttribute>();
        private ActionMessageAttribute[] extensionMessages =
            Array.Empty<ActionMessageAttribute>();
        private HelpBoxAttribute[] helpBoxes = Array.Empty<HelpBoxAttribute>();
        private OnValueChangedAttribute[] validators =
            Array.Empty<OnValueChangedAttribute>();
        private OnValueChangedAttribute[] valueChangedCallbacks =
            Array.Empty<OnValueChangedAttribute>();
        private ActionValidationAttribute[] extensionValidators =
            Array.Empty<ActionValidationAttribute>();
        private ActionValueModifierAttribute[] valueModifiers =
            Array.Empty<ActionValueModifierAttribute>();
        private SuffixLabelAttribute suffix;
        private ProgressBarAttribute progressBar;
        private EnumToggleButtonsAttribute enumToggleButtons;
        private ValueDropdownAttribute valueDropdown;
        private MinMaxSliderAttribute minMaxSlider;
        private HorizontalLineAttribute horizontalLine;
        private ShowAssetPreviewAttribute assetPreview;
        private PathAttribute path;
        private TextAttribute text;
        private SliderAttribute slider;
        private EulerAnglesAttribute eulerAngles;
        private FieldInfo explicitFieldInfo;
        private UnityFieldTypeDrawerBridge fieldTypeDrawer;
        private UnityFieldTypeDrawerBridge elementTypeDrawer;

        private FieldInfo EffectiveFieldInfo => explicitFieldInfo ?? fieldInfo;

        internal static ActionPropertyDrawer Create(FieldInfo field)
        {
            return new CombinedActionPropertyDrawer
            {
                explicitFieldInfo = field
            };
        }
        private InlineButtonAttribute[] inlineButtons =
            Array.Empty<InlineButtonAttribute>();

        public override float GetPropertyHeight(SerializedProperty property,
            GUIContent label)
        {
            PropertyKey key = new PropertyKey(property);
            if (!MeasuringProperties.Add(key))
                return EditorGUI.GetPropertyHeight(property, label, true);
            try
            {
                return GetPropertyHeightCore(property, label);
            }
            finally
            {
                MeasuringProperties.Remove(key);
            }
        }

        private float GetPropertyHeightCore(SerializedProperty property,
            GUIContent label)
        {
            if (IsCollectionElement(property))
                return elementTypeDrawer?.GetPropertyHeight(property, label) ??
                    EditorGUI.GetPropertyHeight(property, label, true);
            if (!ShouldShow(property)) return 0;

            GUIContent fieldLabel = GetLabel(property, label);
            float width = Event.current == null
                ? 320
                : Mathf.Max(1, EditorGUIUtility.currentViewWidth - 40);
            float spacing = EditorGUIUtility.standardVerticalSpacing;
            float height = 0;
            if (horizontalLine != null)
                height += horizontalLine.margin * 2 + horizontalLine.height;
            for (int i = 0; i < helpBoxes.Length; i++)
                height += GetHelpBoxHeight(helpBoxes[i].message, width) + spacing;
            for (int i = 0; i < extensionMessages.Length; i++)
                if (TryGetExtensionMessage(property, extensionMessages[i],
                        out ActionMessage message))
                    height += GetHelpBoxHeight(message.text, width) + spacing;

            height += GetFieldHeight(property, fieldLabel, width);
            if (ShouldDrawProgressBar(property))
                height += EditorGUIUtility.singleLineHeight + spacing;
            if (ShouldDrawAssetPreview(property))
                height += assetPreview.height + spacing;

            for (int i = 0; i < validators.Length; i++)
            {
                if (TryGetValidationError(property, validators[i], fieldLabel,
                    out string message))
                    height += GetHelpBoxHeight(message, width) + spacing;
            }
            for (int i = 0; i < extensionValidators.Length; i++)
            {
                if (TryGetValidationError(property, extensionValidators[i],
                    out string message))
                    height += GetHelpBoxHeight(message, width) + spacing;
            }
            if (TryGetObjectScopeError(property, out string objectError))
                height += GetHelpBoxHeight(objectError, width) + spacing;
            return height;
        }

        public override void OnGUI(Rect position, SerializedProperty property,
            GUIContent label)
        {
            PropertyKey key = new PropertyKey(property);
            if (!DrawingProperties.Add(key))
            {
                EditorGUI.PropertyField(position, property, label, true);
                return;
            }
            try
            {
                GUIContent propertyLabel = EditorGUI.BeginProperty(position,
                    label, property);
                try
                {
                    OnGUICore(position, property, propertyLabel);
                }
                finally
                {
                    EditorGUI.EndProperty();
                }
            }
            finally
            {
                DrawingProperties.Remove(key);
            }
        }

        private void OnGUICore(Rect position, SerializedProperty property,
            GUIContent label)
        {
            if (IsCollectionElement(property))
            {
                if (elementTypeDrawer != null)
                    elementTypeDrawer.OnGUI(position, property, label);
                else EditorGUI.PropertyField(position, property, label, true);
                return;
            }
            if (!ShouldShow(property)) return;

            GUIContent fieldLabel = GetLabel(property, label);
            float spacing = EditorGUIUtility.standardVerticalSpacing;
            if (horizontalLine != null)
            {
                position.y += horizontalLine.margin;
                EditorGUI.DrawRect(new Rect(position.x, position.y,
                    position.width, horizontalLine.height),
                    EditorGUIUtility.isProSkin
                        ? new Color(0.45f, 0.45f, 0.45f)
                        : new Color(0.55f, 0.55f, 0.55f));
                position.y += horizontalLine.height + horizontalLine.margin;
            }

            for (int i = 0; i < helpBoxes.Length; i++)
            {
                HelpBoxAttribute help = helpBoxes[i];
                float height = GetHelpBoxHeight(help.message, position.width);
                EditorGUI.HelpBox(new Rect(position.x, position.y, position.width,
                    height), help.message, ToMessageType(help.type));
                position.y += height + spacing;
            }
            for (int i = 0; i < extensionMessages.Length; i++)
            {
                if (!TryGetExtensionMessage(property, extensionMessages[i],
                        out ActionMessage message)) continue;
                float height = GetHelpBoxHeight(message.text, position.width);
                EditorGUI.HelpBox(new Rect(position.x, position.y, position.width,
                    height), message.text, ToMessageType(message.type));
                position.y += height + spacing;
            }

            float fieldHeight = GetFieldHeight(property, fieldLabel, position.width);
            Rect fieldRect = new Rect(position.x, position.y, position.width,
                fieldHeight);
            string invokedMethod = null;
            bool changed;
            using (var changeCheck = new EditorGUI.ChangeCheckScope())
            {
                using (new EditorGUI.DisabledScope(readOnly ||
                    !ShouldEnable(property)))
                    invokedMethod = DrawField(fieldRect, property, fieldLabel);
                changed = changeCheck.changed;
            }
            if (changed)
            {
                ApplyValueLimits(property);
                ApplyObjectScope(property);
            }
            position.y += fieldHeight + spacing;

            if (ShouldDrawProgressBar(property))
            {
                DrawProgressBar(new Rect(position.x, position.y, position.width,
                    EditorGUIUtility.singleLineHeight), property);
                position.y += EditorGUIUtility.singleLineHeight + spacing;
            }

            if (ShouldDrawAssetPreview(property))
            {
                DrawAssetPreview(new Rect(position.x, position.y,
                    position.width, assetPreview.height), property);
                position.y += assetPreview.height + spacing;
            }

            for (int i = 0; i < validators.Length; i++)
            {
                OnValueChangedAttribute validator = validators[i];
                if (!TryGetValidationError(property, validator, fieldLabel,
                    out string message)) continue;
                float height = GetHelpBoxHeight(message, position.width);
                EditorGUI.HelpBox(new Rect(position.x, position.y, position.width,
                    height), message, ToMessageType(validator.type));
                position.y += height + spacing;
            }
            for (int i = 0; i < extensionValidators.Length; i++)
            {
                ActionValidationAttribute validator = extensionValidators[i];
                if (!TryGetValidationError(property, validator,
                    out string message)) continue;
                float height = GetHelpBoxHeight(message, position.width);
                EditorGUI.HelpBox(new Rect(position.x, position.y, position.width,
                    height), message, ToMessageType(validator.type));
                position.y += height + spacing;
            }
            if (TryGetObjectScopeError(property, out string objectError))
            {
                float height = GetHelpBoxHeight(objectError, position.width);
                EditorGUI.HelpBox(new Rect(position.x, position.y, position.width,
                    height), objectError, MessageType.Error);
            }
            // Applying or updating a SerializedObject must happen after every
            // control for this property has finished drawing.
            if (changed)
                SerializedPropertyMemberUtility.InvokeCallbacks(property,
                    valueChangedCallbacks);
            if (!string.IsNullOrEmpty(invokedMethod))
                SerializedPropertyMemberUtility.InvokeMethod(property,
                    invokedMethod);
        }

        private string DrawField(Rect position, SerializedProperty property,
            GUIContent label)
        {
            string invokedMethod = null;
            Rect valueRect = position;
            Rect suffixRect = default;
            float buttonWidth = 0;
            for (int i = 0; i < inlineButtons.Length; i++)
            {
                string text = GetInlineButtonText(inlineButtons[i]);
                buttonWidth += Mathf.Max(24,
                    EditorStyles.miniButton.CalcSize(new GUIContent(text)).x + 8);
                if (i > 0) buttonWidth += 2;
            }
            if (buttonWidth > 0)
                valueRect.width = Mathf.Max(1, valueRect.width - buttonWidth - 4);
            if (suffix != null && !string.IsNullOrEmpty(suffix.label))
            {
                float suffixWidth = Mathf.Min(valueRect.width * 0.4f,
                    EditorStyles.miniLabel.CalcSize(new GUIContent(suffix.label)).x + 6);
                valueRect.width = Mathf.Max(1, valueRect.width - suffixWidth);
                suffixRect = new Rect(valueRect.xMax + 4, position.y,
                    suffixWidth - 4, EditorGUIUtility.singleLineHeight);
            }
            if (text?.mode == TextFieldMode.Password &&
                property.propertyType == SerializedPropertyType.String)
                property.stringValue = EditorGUI.PasswordField(valueRect, label,
                    property.stringValue);
            else if (slider?.mode == SliderMode.Slider &&
                (property.propertyType == SerializedPropertyType.Integer ||
                 property.propertyType == SerializedPropertyType.Float))
                DrawSlider(valueRect, property, label);
            else if (eulerAngles != null &&
                property.propertyType == SerializedPropertyType.Quaternion)
                DrawEulerAngles(valueRect, property, label);
            else if (path?.type == PathType.Asset &&
                property.propertyType == SerializedPropertyType.String)
                DrawAssetPath(valueRect, property, label, path.assetType);
            else if (minMaxSlider != null &&
                property.propertyType == SerializedPropertyType.Vector2)
                DrawMinMaxSlider(valueRect, property, label);
            else if (valueDropdown != null)
                DrawValueSelection(valueRect, property, label);
            else if (enumToggleButtons != null &&
                property.propertyType == SerializedPropertyType.Enum)
                DrawEnumToggleButtons(valueRect, property, label);
            else if (path != null && path.type != PathType.Asset &&
                property.propertyType == SerializedPropertyType.String)
                DrawPath(valueRect, property, label);
            else if (expandable &&
                property.propertyType == SerializedPropertyType.ObjectReference)
                DrawExpandable(valueRect, property, label);
            else if (toggleLeft &&
                property.propertyType == SerializedPropertyType.Boolean)
                property.boolValue = EditorGUI.ToggleLeft(valueRect,
                    label, property.boolValue);
            else if (objectsOnly != null &&
                property.propertyType == SerializedPropertyType.ObjectReference)
                property.objectReferenceValue = EditorGUI.ObjectField(valueRect,
                    label, property.objectReferenceValue,
                    EffectiveFieldInfo?.FieldType ?? typeof(UnityEngine.Object),
                    objectsOnly.source != ObjectSource.Assets);
            else if (fieldTypeDrawer != null)
                fieldTypeDrawer.OnGUI(valueRect, property, label);
            else EditorGUI.PropertyField(valueRect, property, label, true);

            if (text != null && property.propertyType ==
                    SerializedPropertyType.String && path == null &&
                valueDropdown == null)
                DrawPlaceholder(valueRect, property, label);

            if (suffixRect.width > 0)
                EditorGUI.LabelField(suffixRect, suffix.label, EditorStyles.miniLabel);

            float buttonX = position.xMax - buttonWidth;
            for (int i = 0; i < inlineButtons.Length; i++)
            {
                InlineButtonAttribute button = inlineButtons[i];
                string text = GetInlineButtonText(button);
                float width = Mathf.Max(24,
                    EditorStyles.miniButton.CalcSize(new GUIContent(text)).x + 8);
                if (GUI.Button(new Rect(buttonX, position.y, width,
                    EditorGUIUtility.singleLineHeight), text,
                    EditorStyles.miniButton))
                    invokedMethod = button.method;
                buttonX += width + 2;
            }
            return invokedMethod;
        }

        private static string GetInlineButtonText(InlineButtonAttribute button) =>
            string.IsNullOrEmpty(button.text)
                ? ObjectNames.NicifyVariableName(button.method)
                : button.text;

        private void DrawSlider(Rect position, SerializedProperty property,
            GUIContent label)
        {
            if (property.propertyType == SerializedPropertyType.Integer)
            {
                int min = slider.min <= int.MinValue
                    ? int.MinValue
                    : (int)Math.Ceiling(slider.min);
                int max = slider.max >= int.MaxValue
                    ? int.MaxValue
                    : (int)Math.Floor(slider.max);
                property.intValue = EditorGUI.IntSlider(position, label,
                    property.intValue, min, max);
                return;
            }

            property.doubleValue = EditorGUI.Slider(position, label,
                (float)property.doubleValue, (float)slider.min,
                (float)slider.max);
        }

        private static void DrawEulerAngles(Rect position,
            SerializedProperty property, GUIContent label)
        {
            Vector3 value = property.quaternionValue.eulerAngles;
            EditorGUI.BeginChangeCheck();
            value = EditorGUI.Vector3Field(position, label, value);
            if (EditorGUI.EndChangeCheck())
                property.quaternionValue = Quaternion.Euler(value);
        }

        private static void DrawAssetPath(Rect position,
            SerializedProperty property, GUIContent label, Type assetType)
        {
            if (assetType == null ||
                !typeof(UnityEngine.Object).IsAssignableFrom(assetType))
                assetType = typeof(UnityEngine.Object);
            string path = property.stringValue;
            UnityEngine.Object current = string.IsNullOrEmpty(path)
                ? null
                : AssetDatabase.LoadAssetAtPath(path, assetType);
            EditorGUI.BeginChangeCheck();
            UnityEngine.Object selected = EditorGUI.ObjectField(position, label,
                current, assetType, false);
            if (!EditorGUI.EndChangeCheck()) return;
            if (selected == null)
            {
                property.stringValue = string.Empty;
                return;
            }
            path = AssetDatabase.GetAssetPath(selected);
            property.stringValue = path;
        }

        private void DrawPlaceholder(Rect position, SerializedProperty property,
            GUIContent label)
        {
            if (!string.IsNullOrEmpty(property.stringValue) ||
                string.IsNullOrEmpty(text.Placeholder) ||
                Event.current.type != EventType.Repaint) return;

            Rect hintRect = EditorGUI.PrefixLabel(position, label);
            hintRect.xMin += 3;
            GUI.Label(hintRect, text.Placeholder, PlaceholderStyle);
        }

        private static GUIStyle PlaceholderStyle
        {
            get
            {
                if (placeholderStyle != null) return placeholderStyle;
                placeholderStyle = new GUIStyle(EditorStyles.label);
                Color color = EditorStyles.textField.normal.textColor;
                color.a *= 0.45f;
                placeholderStyle.normal.textColor = color;
                placeholderStyle.clipping = TextClipping.Clip;
                return placeholderStyle;
            }
        }

        private void DrawMinMaxSlider(Rect position, SerializedProperty property,
            GUIContent label)
        {
            Rect sliderRect = EditorGUI.PrefixLabel(position, label);
            Vector2 value = property.vector2Value;
            float min = Mathf.Clamp(value.x, minMaxSlider.min,
                minMaxSlider.max);
            float max = Mathf.Clamp(value.y, min, minMaxSlider.max);
            const float valueWidth = 48;
            Rect minRect = new Rect(sliderRect.x, sliderRect.y, valueWidth,
                sliderRect.height);
            Rect maxRect = new Rect(sliderRect.xMax - valueWidth, sliderRect.y,
                valueWidth, sliderRect.height);
            Rect rangeRect = new Rect(minRect.xMax + 4, sliderRect.y,
                Mathf.Max(1, maxRect.xMin - minRect.xMax - 8), sliderRect.height);
            min = EditorGUI.FloatField(minRect, min);
            max = EditorGUI.FloatField(maxRect, max);
            EditorGUI.MinMaxSlider(rangeRect, ref min, ref max,
                minMaxSlider.min, minMaxSlider.max);
            property.vector2Value = new Vector2(
                Mathf.Clamp(min, minMaxSlider.min, max),
                Mathf.Clamp(max, min, minMaxSlider.max));
        }

        private void DrawEnumSearch(Rect position, SerializedProperty property,
            GUIContent label)
        {
            Rect buttonRect = EditorGUI.PrefixLabel(position, label);
            string[] names = GetEnumNames();
            int index = Mathf.Clamp(property.enumValueIndex, 0,
                Mathf.Max(0, names.Length - 1));
            if (!EditorGUI.DropdownButton(buttonRect,
                new GUIContent(names.Length == 0 ? "NONE" : names[index]),
                FocusType.Keyboard)) return;
            string path = property.propertyPath;
            UnityEngine.Object[] targets = property.serializedObject.targetObjects;
            SearchPopupWindow.Show(buttonRect, names, index, selected =>
            {
                var serialized = new SerializedObject(targets);
                SerializedProperty selectedProperty = serialized.FindProperty(path);
                if (selectedProperty == null) return;
                selectedProperty.enumValueIndex = selected;
                serialized.ApplyModifiedProperties();
            });
        }

        private void DrawEnumToggleButtons(Rect position,
            SerializedProperty property, GUIContent label)
        {
            if (EffectiveFieldInfo?.FieldType.GetCustomAttribute<FlagsAttribute>() !=
                null)
            {
                DrawEnumFlags(position, property, label);
                return;
            }
            Rect toolbarRect = EditorGUI.PrefixLabel(position, label);
            string[] names = GetEnumNames();
            property.enumValueIndex = GUI.Toolbar(toolbarRect,
                property.enumValueIndex, names, EditorStyles.miniButton);
        }

        private string[] GetEnumNames()
        {
            return EffectiveFieldInfo?.FieldType.IsEnum == true
                ? EnumDisplayUtility.GetNames(EffectiveFieldInfo.FieldType)
                : Array.Empty<string>();
        }

        private void DrawValueSelection(Rect position,
            SerializedProperty property, GUIContent label)
        {
            switch (valueDropdown.source)
            {
                case ValueDropdownSource.Auto:
                    if (property.propertyType == SerializedPropertyType.Enum &&
                        string.IsNullOrEmpty(valueDropdown.valuesMember))
                        DrawEnumSearch(position, property, label);
                    else DrawMemberDropdown(position, property, label);
                    break;
                case ValueDropdownSource.Member:
                    DrawMemberDropdown(position, property, label);
                    break;
                case ValueDropdownSource.Search:
                    DrawSearchSource(position, property, label);
                    break;
                case ValueDropdownSource.Tag:
                    DrawTag(position, property, label);
                    break;
                case ValueDropdownSource.Layer:
                    DrawLayer(position, property, label);
                    break;
                case ValueDropdownSource.SortingLayer:
                    DrawSortingLayer(position, property, label);
                    break;
                case ValueDropdownSource.Scene:
                    DrawScene(position, property, label);
                    break;
                case ValueDropdownSource.InputAxis:
                    DrawStringSearch(position, property, label,
                        GetInputAxisNames());
                    break;
                case ValueDropdownSource.AnimatorParameter:
                    DrawAnimatorParameter(position, property, label);
                    break;
                default:
                    EditorGUI.PropertyField(position, property, label, true);
                    break;
            }
        }

        private void DrawMemberDropdown(Rect position,
            SerializedProperty property,
            GUIContent label)
        {
            if (!SerializedPropertyMemberUtility.TryGetDropdownValues(property,
                valueDropdown.valuesMember, out string[] labels,
                out object[] values))
            {
                EditorGUI.HelpBox(position,
                    $"找不到下拉数据 {valueDropdown.valuesMember}",
                    MessageType.Error);
                return;
            }
            DrawMappedSearchDropdown(position, property, label, labels, values);
        }

        private void DrawSearchSource(Rect position, SerializedProperty property,
            GUIContent label)
        {
            if (!SerializedPropertyMemberUtility.TryGetSearchValues(property,
                valueDropdown.valuesMember, string.Empty, out string[] labels,
                out object[] values))
            {
                EditorGUI.HelpBox(position,
                    $"找不到搜索源 {valueDropdown.valuesMember}",
                    MessageType.Error);
                return;
            }
            object current = SerializedPropertyMemberUtility.GetSerializedValue(
                property);
            int index = FindDropdownIndex(current, values);
            string text = index >= 0 && index < labels.Length
                ? labels[index]
                : current?.ToString() ?? "NONE";
            Rect buttonRect = EditorGUI.PrefixLabel(position, label);
            if (!EditorGUI.DropdownButton(buttonRect, new GUIContent(text),
                FocusType.Keyboard)) return;
            string path = property.propertyPath;
            UnityEngine.Object[] targets = property.serializedObject.targetObjects;
            object selectedValue = index >= 0 && index < values.Length
                ? values[index]
                : current;
            SearchPopupWindow.Show(buttonRect, query =>
            {
                var serialized = new SerializedObject(targets);
                SerializedProperty sourceProperty = serialized.FindProperty(path);
                if (sourceProperty == null ||
                    !SerializedPropertyMemberUtility.TryGetSearchValues(
                        sourceProperty, valueDropdown.valuesMember, query,
                        out string[] sourceLabels, out object[] sourceValues))
                    return Array.Empty<SearchPopupItem>();
                int count = Math.Min(sourceLabels.Length, sourceValues.Length);
                var items = new SearchPopupItem[count];
                for (int i = 0; i < count; i++)
                    items[i] = new SearchPopupItem(sourceLabels[i],
                        sourceValues[i]);
                return items;
            }, selectedValue, item =>
            {
                var serialized = new SerializedObject(targets);
                SerializedProperty selectedProperty = serialized.FindProperty(path);
                if (selectedProperty == null) return;
                SerializedPropertyMemberUtility.SetSerializedValue(
                    selectedProperty, item.Value);
                serialized.ApplyModifiedProperties();
            });
        }

        private static void DrawMappedSearchDropdown(Rect position,
            SerializedProperty property, GUIContent label, string[] labels,
            object[] values)
        {
            labels = labels ?? Array.Empty<string>();
            values = values ?? Array.Empty<object>();
            object current = SerializedPropertyMemberUtility.GetSerializedValue(
                property);
            int index = FindDropdownIndex(current, values);
            string text = index >= 0 && index < labels.Length
                ? labels[index]
                : current?.ToString() ?? "NONE";
            Rect buttonRect = EditorGUI.PrefixLabel(position, label);
            if (!EditorGUI.DropdownButton(buttonRect, new GUIContent(text),
                FocusType.Keyboard)) return;
            string path = property.propertyPath;
            UnityEngine.Object[] targets = property.serializedObject.targetObjects;
            SearchPopupWindow.Show(buttonRect, labels, index, selected =>
            {
                if (selected < 0 || selected >= values.Length) return;
                var serialized = new SerializedObject(targets);
                SerializedProperty selectedProperty = serialized.FindProperty(path);
                if (selectedProperty == null) return;
                SerializedPropertyMemberUtility.SetSerializedValue(
                    selectedProperty, values[selected]);
                serialized.ApplyModifiedProperties();
            });
        }

        private static int FindDropdownIndex(object current, object[] values)
        {
            for (int i = 0; i < values.Length; i++)
            {
                if (Equals(current, values[i])) return i;
                if (current == null || values[i] == null) continue;
                try
                {
                    object converted = Convert.ChangeType(values[i],
                        current.GetType());
                    if (Equals(current, converted)) return i;
                }
                catch { }
            }
            return -1;
        }

        private static void DrawTag(Rect position, SerializedProperty property,
            GUIContent label)
        {
            if (property.propertyType != SerializedPropertyType.String)
            {
                EditorGUI.PropertyField(position, property, label, true);
                return;
            }
            string[] names = UnityEditorInternal.InternalEditorUtility.tags;
            var values = new object[names.Length];
            for (int i = 0; i < names.Length; i++) values[i] = names[i];
            DrawMappedSearchDropdown(position, property, label, names, values);
        }

        private static void DrawLayer(Rect position, SerializedProperty property,
            GUIContent label)
        {
            var names = new List<string>();
            var indexes = new List<int>();
            for (int i = 0; i < 32; i++)
            {
                string name = LayerMask.LayerToName(i);
                if (string.IsNullOrEmpty(name)) continue;
                names.Add(name);
                indexes.Add(i);
            }
            if (property.propertyType != SerializedPropertyType.Integer &&
                property.propertyType != SerializedPropertyType.String)
            {
                EditorGUI.PropertyField(position, property, label, true);
                return;
            }
            var values = new object[names.Count];
            for (int i = 0; i < values.Length; i++)
                values[i] = property.propertyType == SerializedPropertyType.String
                    ? (object)names[i]
                    : indexes[i];
            DrawMappedSearchDropdown(position, property, label,
                names.ToArray(), values);
        }

        private static void DrawSortingLayer(Rect position,
            SerializedProperty property, GUIContent label)
        {
            if (property.propertyType != SerializedPropertyType.String &&
                property.propertyType != SerializedPropertyType.Integer)
            {
                EditorGUI.PropertyField(position, property, label, true);
                return;
            }
            SortingLayer[] layers = SortingLayer.layers;
            string[] names = Array.ConvertAll(layers, value => value.name);
            var values = new object[layers.Length];
            for (int i = 0; i < layers.Length; i++)
                values[i] = property.propertyType == SerializedPropertyType.String
                    ? (object)layers[i].name
                    : layers[i].id;
            DrawMappedSearchDropdown(position, property, label, names, values);
        }

        private void DrawScene(Rect position, SerializedProperty property,
            GUIContent label)
        {
            if (property.propertyType != SerializedPropertyType.String &&
                property.propertyType != SerializedPropertyType.Integer)
            {
                EditorGUI.PropertyField(position, property, label, true);
                return;
            }
            var names = new List<string>();
            var indexes = new List<int>();
            EditorBuildSettingsScene[] scenes = EditorBuildSettings.scenes;
            for (int i = 0; i < scenes.Length; i++)
            {
                if (!valueDropdown.includeDisabled && !scenes[i].enabled)
                    continue;
                names.Add(System.IO.Path.GetFileNameWithoutExtension(scenes[i].path));
                indexes.Add(i);
            }
            var values = new object[names.Count];
            for (int i = 0; i < values.Length; i++)
                values[i] = property.propertyType == SerializedPropertyType.String
                    ? (object)names[i]
                    : indexes[i];
            DrawMappedSearchDropdown(position, property, label,
                names.ToArray(), values);
        }

        private static void DrawStringSearch(Rect position,
            SerializedProperty property, GUIContent label, string[] options)
        {
            if (property.propertyType != SerializedPropertyType.String)
            {
                EditorGUI.PropertyField(position, property, label, true);
                return;
            }
            var values = new object[options.Length];
            for (int i = 0; i < options.Length; i++) values[i] = options[i];
            DrawMappedSearchDropdown(position, property, label, options, values);
        }

        private void DrawPath(Rect position, SerializedProperty property,
            GUIContent label)
        {
            Rect valueRect = EditorGUI.PrefixLabel(position, label);
            Rect buttonRect = new Rect(valueRect.xMax - 24, valueRect.y, 24,
                EditorGUIUtility.singleLineHeight);
            valueRect.width -= 28;
            property.stringValue = EditorGUI.TextField(valueRect,
                property.stringValue);
            if (!GUI.Button(buttonRect, EditorGUIUtility.IconContent("Folder Icon"),
                EditorStyles.miniButton)) return;
            bool selectFile = path.type == PathType.File;
            string selected = selectFile
                ? EditorUtility.OpenFilePanel("选择文件",
                    GetInitialDirectory(property.stringValue, true),
                    path.extension ?? string.Empty)
                : EditorUtility.OpenFolderPanel("选择文件夹",
                    GetInitialDirectory(property.stringValue, false),
                    string.Empty);
            if (string.IsNullOrEmpty(selected)) return;
            property.stringValue = NormalizeSelectedPath(selected,
                path.absolutePath);
            property.serializedObject.ApplyModifiedProperties();
            GUIUtility.ExitGUI();
        }

        private static string NormalizeSelectedPath(string path, bool absolute)
        {
            path = path.Replace('\\', '/');
            if (absolute) return path;
            string relative = FileUtil.GetProjectRelativePath(path);
            return string.IsNullOrEmpty(relative) ? path : relative;
        }

        private static string GetInitialDirectory(string path, bool file)
        {
            string projectDirectory = System.IO.Path.GetDirectoryName(
                Application.dataPath);
            if (string.IsNullOrWhiteSpace(path)) return projectDirectory;
            try
            {
                string fullPath = System.IO.Path.IsPathRooted(path)
                    ? System.IO.Path.GetFullPath(path)
                    : System.IO.Path.GetFullPath(System.IO.Path.Combine(
                        projectDirectory, path));
                if (file) fullPath = System.IO.Path.GetDirectoryName(fullPath);
                return !string.IsNullOrEmpty(fullPath) &&
                    System.IO.Directory.Exists(fullPath)
                    ? fullPath
                    : projectDirectory;
            }
            catch (ArgumentException)
            {
                return projectDirectory;
            }
            catch (System.IO.IOException)
            {
                return projectDirectory;
            }
            catch (NotSupportedException)
            {
                return projectDirectory;
            }
        }

        private static string[] GetInputAxisNames()
        {
            UnityEngine.Object[] assets = AssetDatabase.LoadAllAssetsAtPath(
                "ProjectSettings/InputManager.asset");
            if (assets.Length == 0) return Array.Empty<string>();
            var serialized = new SerializedObject(assets[0]);
            SerializedProperty axes = serialized.FindProperty("m_Axes");
            if (axes == null) return Array.Empty<string>();
            var names = new List<string>();
            for (int i = 0; i < axes.arraySize; i++)
            {
                string name = axes.GetArrayElementAtIndex(i)
                    .FindPropertyRelative("m_Name")?.stringValue;
                if (!string.IsNullOrEmpty(name) && !names.Contains(name))
                    names.Add(name);
            }
            return names.ToArray();
        }

        private void DrawAnimatorParameter(Rect position,
            SerializedProperty property, GUIContent label)
        {
            if (property.propertyType != SerializedPropertyType.String &&
                property.propertyType != SerializedPropertyType.Integer)
            {
                EditorGUI.PropertyField(position, property, label, true);
                return;
            }
            if (!SerializedPropertyMemberUtility.TryGetMemberValue(property,
                valueDropdown.valuesMember, out object value) ||
                !(value is Animator animator) || animator == null)
            {
                EditorGUI.HelpBox(position,
                    $"找不到 Animator：{valueDropdown.valuesMember}",
                    MessageType.Error);
                return;
            }
            UnityEngine.AnimatorControllerParameter[] parameters =
                animator.parameters;
            string[] names = Array.ConvertAll(parameters, item => item.name);
            var values = new object[parameters.Length];
            for (int i = 0; i < parameters.Length; i++)
                values[i] = property.propertyType == SerializedPropertyType.String
                    ? (object)parameters[i].name
                    : parameters[i].nameHash;
            DrawMappedSearchDropdown(position, property, label, names, values);
        }

        private static void DrawExpandable(Rect position,
            SerializedProperty property, GUIContent label)
        {
            Rect line = new Rect(position.x, position.y, position.width,
                EditorGUIUtility.singleLineHeight);
            if (property.objectReferenceValue != null)
            {
                Rect foldout = new Rect(line.x, line.y, 14, line.height);
                property.isExpanded = EditorGUI.Foldout(foldout,
                    property.isExpanded, GUIContent.none, false);
                line.xMin += 14;
            }
            EditorGUI.PropertyField(line, property, label, false);
            if (!property.isExpanded || property.objectReferenceValue == null)
                return;

            var nested = new SerializedObject(property.objectReferenceValue);
            nested.Update();
            SerializedProperty child = nested.GetIterator();
            bool enterChildren = true;
            float y = line.yMax + EditorGUIUtility.standardVerticalSpacing;
            int previousIndent = EditorGUI.indentLevel;
            EditorGUI.indentLevel++;
            while (child.NextVisible(enterChildren))
            {
                enterChildren = false;
                if (child.propertyPath == "m_Script") continue;
                float height = EditorGUI.GetPropertyHeight(child, true);
                EditorGUI.PropertyField(new Rect(position.x, y, position.width,
                    height), child, true);
                y += height + EditorGUIUtility.standardVerticalSpacing;
            }
            EditorGUI.indentLevel = previousIndent;
            nested.ApplyModifiedProperties();
        }

        private void DrawEnumFlags(Rect position, SerializedProperty property,
            GUIContent label)
        {
            Enum current = (Enum)Enum.ToObject(EffectiveFieldInfo.FieldType,
                property.intValue);
            Enum result = EditorGUI.EnumFlagsField(position, label, current);
            property.intValue = Convert.ToInt32(result);
        }

        private float GetFieldHeight(SerializedProperty property, GUIContent label,
            float width)
        {
            PropertyKey key = new PropertyKey(property);
            bool added = MeasuringProperties.Add(key);
            try
            {
                return GetFieldHeightCore(property, label, width);
            }
            finally
            {
                if (added) MeasuringProperties.Remove(key);
            }
        }

        private float GetFieldHeightCore(SerializedProperty property,
            GUIContent label, float width)
        {
            if (UsesSingleLineActionControl(property))
                return EditorGUIUtility.singleLineHeight;
            if (expandable &&
                property.propertyType == SerializedPropertyType.ObjectReference)
                return GetExpandableHeight(property);
            if (fieldTypeDrawer != null)
                return fieldTypeDrawer.GetPropertyHeight(property, label);
            return EditorGUI.GetPropertyHeight(property, label, true);
        }

        private bool UsesSingleLineActionControl(SerializedProperty property)
        {
            bool numeric = property.propertyType ==
                SerializedPropertyType.Integer || property.propertyType ==
                SerializedPropertyType.Float;
            return (text?.mode == TextFieldMode.Password && property.propertyType ==
                     SerializedPropertyType.String) ||
                (slider?.mode == SliderMode.Slider && numeric) ||
                (eulerAngles != null && property.propertyType ==
                    SerializedPropertyType.Quaternion) ||
                (path != null && property.propertyType ==
                    SerializedPropertyType.String) ||
                (minMaxSlider != null && property.propertyType ==
                    SerializedPropertyType.Vector2) ||
                valueDropdown != null ||
                (enumToggleButtons != null && property.propertyType ==
                    SerializedPropertyType.Enum) ||
                (toggleLeft && property.propertyType ==
                    SerializedPropertyType.Boolean) ||
                (objectsOnly != null && property.propertyType ==
                    SerializedPropertyType.ObjectReference);
        }

        private static float GetExpandableHeight(SerializedProperty property)
        {
            float height = EditorGUIUtility.singleLineHeight;
            if (!property.isExpanded || property.objectReferenceValue == null)
                return height;
            var nested = new SerializedObject(property.objectReferenceValue);
            SerializedProperty child = nested.GetIterator();
            bool enterChildren = true;
            while (child.NextVisible(enterChildren))
            {
                enterChildren = false;
                if (child.propertyPath == "m_Script") continue;
                height += EditorGUI.GetPropertyHeight(child, true) +
                    EditorGUIUtility.standardVerticalSpacing;
            }
            return height;
        }

        private void ApplyValueLimits(SerializedProperty property)
        {
            if (slider != null)
            {
                if (slider.Step > 0)
                {
                    if (property.propertyType == SerializedPropertyType.Integer)
                    {
                        double value = ValueConstraintUtility.Snap(
                            property.longValue, slider.Step, slider.Origin);
                        property.longValue = value <= long.MinValue
                            ? long.MinValue
                            : value >= long.MaxValue
                                ? long.MaxValue
                                : (long)value;
                    }
                    else if (property.propertyType ==
                        SerializedPropertyType.Float)
                        property.doubleValue = ValueConstraintUtility.Snap(
                            property.doubleValue, slider.Step, slider.Origin);
                }
                ApplySliderLimits(property);
            }

            ApplyValueModifiers(property);
            if (text != null &&
                property.propertyType == SerializedPropertyType.String)
                property.stringValue = text.Apply(property.stringValue);
        }

        private void ApplySliderLimits(SerializedProperty property)
        {
            if (slider.mode == SliderMode.Wrap)
            {
                double range = slider.max - slider.min;
                if (property.propertyType == SerializedPropertyType.Integer)
                {
                    long minimum = (long)Math.Ceiling(slider.min);
                    long maximum = (long)Math.Floor(slider.max);
                    long integerRange = maximum - minimum + 1;
                    if (integerRange > 0)
                    {
                        long value = property.longValue;
                        property.longValue = minimum +
                            ((value - minimum) % integerRange + integerRange) %
                            integerRange;
                    }
                }
                else if (property.propertyType == SerializedPropertyType.Float &&
                    range > 0)
                {
                    double value = property.doubleValue;
                    property.doubleValue = slider.min +
                        ((value - slider.min) % range + range) % range;
                }
                return;
            }

            bool hasMin = slider.mode == SliderMode.Slider ||
                slider.mode == SliderMode.Clamp ||
                slider.mode == SliderMode.Minimum ||
                slider.mode == SliderMode.NonNegative ||
                slider.mode == SliderMode.Positive;
            bool hasMax = slider.mode == SliderMode.Slider ||
                slider.mode == SliderMode.Clamp ||
                slider.mode == SliderMode.Maximum;
            double min = slider.min;
            double max = slider.max;
            if (slider.mode == SliderMode.NonNegative) min = 0;
            else if (slider.mode == SliderMode.Positive)
                min = property.propertyType == SerializedPropertyType.Integer
                    ? 1
                    : float.Epsilon;
            if ((hasMin || hasMax) &&
                property.propertyType == SerializedPropertyType.Integer)
            {
                long value = property.longValue;
                if (hasMin) value = Math.Max((long)Math.Ceiling(min), value);
                if (hasMax) value = Math.Min((long)Math.Floor(max), value);
                property.longValue = value;
            }
            else if ((hasMin || hasMax) &&
                property.propertyType == SerializedPropertyType.Float)
            {
                double value = property.doubleValue;
                if (hasMin) value = Math.Max(min, value);
                if (hasMax) value = Math.Min(max, value);
                property.doubleValue = value;
            }
        }

        private bool ShouldShow(SerializedProperty property)
        {
            Initialize();
            for (int i = 0; i < conditions.Length; i++)
            {
                ConditionAttribute condition = conditions[i];
                if (condition.mode != ConditionMode.Show &&
                    condition.mode != ConditionMode.Hide) continue;
                bool matched = IsConditionMatched(property, condition);
                if (condition.mode == ConditionMode.Show ? !matched : matched)
                    return false;
            }
            for (int i = 0; i < extensionConditions.Length; i++)
            {
                ActionConditionAttribute condition = extensionConditions[i];
                if (condition.mode != ConditionMode.Show &&
                    condition.mode != ConditionMode.Hide) continue;
                bool matched = EvaluateExtensionCondition(property, condition);
                if (condition.mode == ConditionMode.Show ? !matched : matched)
                    return false;
            }
            return true;
        }

        private bool ShouldEnable(SerializedProperty property)
        {
            Initialize();
            for (int i = 0; i < conditions.Length; i++)
            {
                ConditionAttribute condition = conditions[i];
                if (condition.mode != ConditionMode.Enable &&
                    condition.mode != ConditionMode.Disable) continue;
                bool matched = IsConditionMatched(property, condition);
                if (condition.mode == ConditionMode.Enable ? !matched : matched)
                    return false;
            }
            for (int i = 0; i < extensionConditions.Length; i++)
            {
                ActionConditionAttribute condition = extensionConditions[i];
                if (condition.mode != ConditionMode.Enable &&
                    condition.mode != ConditionMode.Disable) continue;
                bool matched = EvaluateExtensionCondition(property, condition);
                if (condition.mode == ConditionMode.Enable ? !matched : matched)
                    return false;
            }
            return true;
        }

        private static bool IsConditionMatched(SerializedProperty property,
            ConditionAttribute condition)
        {
            return condition.usesInspectorMode
                ? IsInspectorMode(condition.inspectorMode)
                : EvaluateConditions(property, condition.conditions,
                    condition.conditionOperator, condition.expected);
        }

        private bool EvaluateExtensionCondition(SerializedProperty property,
            ActionConditionAttribute condition)
        {
            try
            {
                return condition.Evaluate(CreateExtensionContext(property));
            }
            catch (Exception exception)
            {
                ReportExtensionFailure(condition, exception);
                return condition.mode == ConditionMode.Show ||
                    condition.mode == ConditionMode.Enable;
            }
        }

        private static bool IsInspectorMode(InspectorMode mode)
        {
            return mode == InspectorMode.Always ||
                (mode == InspectorMode.PlayMode && EditorApplication.isPlaying) ||
                (mode == InspectorMode.EditMode && !EditorApplication.isPlaying);
        }

        private static bool EvaluateConditions(SerializedProperty property,
            string[] conditions, ConditionOperator conditionOperator,
            object expected)
        {
            if (conditions == null || conditions.Length == 0) return true;
            bool isAnd = conditionOperator == ConditionOperator.And;
            for (int i = 0; i < conditions.Length; i++)
            {
                bool matched = SerializedPropertyMemberUtility.TryCompareCondition(
                    property, conditions[i], expected, out bool result) && result;
                if (isAnd && !matched) return false;
                if (!isAnd && matched) return true;
            }
            return isAnd;
        }

        private bool TryGetObjectScopeError(SerializedProperty property,
            out string message)
        {
            message = null;
            if (objectsOnly == null ||
                property.propertyType != SerializedPropertyType.ObjectReference ||
                property.objectReferenceValue == null) return false;
            bool persistent = EditorUtility.IsPersistent(
                property.objectReferenceValue);
            if (objectsOnly.source == ObjectSource.Assets && !persistent)
                message = "该字段只允许引用 Project 中的资源。";
            else if (objectsOnly.source == ObjectSource.Scene && persistent)
                message = "该字段只允许引用当前场景中的对象。";
            else if (IsHierarchySource(objectsOnly.source) &&
                (persistent || !IsAllowedHierarchyObject(property,
                    objectsOnly.source, objectsOnly.includeSelf)))
                message = objectsOnly.source == ObjectSource.Children
                    ? "该字段只允许引用当前对象的子层级。"
                    : "该字段只允许引用当前对象的父层级。";
            return message != null;
        }

        private void ApplyObjectScope(SerializedProperty property)
        {
            if (property.propertyType != SerializedPropertyType.ObjectReference ||
                property.objectReferenceValue == null) return;
            if (objectsOnly == null) return;
            bool persistent = EditorUtility.IsPersistent(
                property.objectReferenceValue);
            bool valid;
            switch (objectsOnly.source)
            {
                case ObjectSource.Assets:
                    valid = persistent;
                    break;
                case ObjectSource.Scene:
                    valid = !persistent;
                    break;
                case ObjectSource.Children:
                case ObjectSource.Parents:
                    valid = !persistent && IsAllowedHierarchyObject(property,
                        objectsOnly.source, objectsOnly.includeSelf);
                    break;
                default:
                    valid = false;
                    break;
            }
            if (!valid)
                property.objectReferenceValue = null;
        }

        private static bool IsHierarchySource(ObjectSource source)
        {
            return source == ObjectSource.Children ||
                source == ObjectSource.Parents;
        }

        private static bool IsAllowedHierarchyObject(SerializedProperty property,
            ObjectSource source, bool includeSelf)
        {
            Transform owner = GetTransform(property.serializedObject.targetObject);
            Transform selected = GetTransform(property.objectReferenceValue);
            if (owner == null || selected == null) return false;
            if (ReferenceEquals(owner, selected)) return includeSelf;
            return source == ObjectSource.Children
                ? selected.IsChildOf(owner)
                : owner.IsChildOf(selected);
        }

        private static Transform GetTransform(UnityEngine.Object value)
        {
            if (value is GameObject gameObject) return gameObject.transform;
            return value is Component component ? component.transform : null;
        }

        private bool ShouldDrawAssetPreview(SerializedProperty property) =>
            assetPreview != null &&
            property.propertyType == SerializedPropertyType.ObjectReference &&
            property.objectReferenceValue != null;

        private void DrawAssetPreview(Rect position,
            SerializedProperty property)
        {
            Texture texture = AssetPreview.GetAssetPreview(
                property.objectReferenceValue) ?? AssetPreview.GetMiniThumbnail(
                property.objectReferenceValue);
            if (texture == null) return;
            float width = Mathf.Min(assetPreview.width, position.width);
            float height = Mathf.Min(assetPreview.height, position.height);
            Rect previewRect = new Rect(position.x +
                (position.width - width) * 0.5f, position.y, width, height);
            GUI.DrawTexture(previewRect, texture, ScaleMode.ScaleToFit, true);
        }

        private bool TryGetValidationError(SerializedProperty property,
            OnValueChangedAttribute validator, GUIContent label,
            out string message)
        {
            if (SerializedPropertyMemberUtility.TryValidate(property,
                validator.callback, out bool valid))
            {
                message = valid
                    ? null
                    : string.IsNullOrEmpty(validator.message)
                        ? $"{label?.text ?? property.displayName}的值无效。"
                        : validator.message;
                return !valid;
            }
            message = $"找不到或无法调用校验方法 {validator.callback}。";
            return true;
        }

        private bool TryGetValidationError(SerializedProperty property,
            ActionValidationAttribute validator, out string message)
        {
            try
            {
                ActionAttributeContext context = CreateExtensionContext(property);
                if (validator.IsValid(context))
                {
                    message = null;
                    return false;
                }
                message = validator.GetMessage(context);
                if (string.IsNullOrEmpty(message))
                    message = $"{context.MemberName} 的值无效。";
                return true;
            }
            catch (Exception exception)
            {
                ReportExtensionFailure(validator, exception);
                message = null;
                return false;
            }
        }

        private bool TryGetExtensionMessage(SerializedProperty property,
            ActionMessageAttribute provider, out ActionMessage message)
        {
            try
            {
                message = provider.GetMessage(CreateExtensionContext(property));
                return !string.IsNullOrEmpty(message.text);
            }
            catch (Exception exception)
            {
                ReportExtensionFailure(provider, exception);
                message = default;
                return false;
            }
        }

        private void ApplyValueModifiers(SerializedProperty property)
        {
            for (int i = 0; i < valueModifiers.Length; i++)
            {
                ActionValueModifierAttribute modifier = valueModifiers[i];
                try
                {
                    ActionAttributeContext context = CreateExtensionContext(
                        property);
                    object modified = modifier.Modify(context);
                    if (Equals(context.Value, modified)) continue;
                    if (!SerializedPropertyMemberUtility.TrySetSerializedValue(
                        property, modified))
                        ReportExtensionFailure(modifier, new InvalidCastException(
                            $"无法将返回值写入 {context.ValueType}。"));
                }
                catch (Exception exception)
                {
                    ReportExtensionFailure(modifier, exception);
                }
            }
        }

        private ActionAttributeContext CreateExtensionContext(
            SerializedProperty property)
        {
            return SerializedPropertyMemberUtility.CreateActionAttributeContext(
                property, EffectiveFieldInfo);
        }

        private static void ReportExtensionFailure(ActionAttributeBase extension,
            Exception exception)
        {
            Exception cause = exception is TargetInvocationException invocation &&
                invocation.InnerException != null
                    ? invocation.InnerException
                    : exception;
            string key = extension.GetType().AssemblyQualifiedName + "|" +
                cause.GetType().FullName + "|" + cause.Message;
            if (!ReportedExtensionFailures.Add(key)) return;
            Debug.LogWarning($"ActionAttribute 扩展 {extension.GetType().FullName} " +
                $"执行失败：{cause.Message}");
        }

        private bool ShouldDrawProgressBar(SerializedProperty property)
        {
            return progressBar != null &&
                (property.propertyType == SerializedPropertyType.Integer ||
                 property.propertyType == SerializedPropertyType.Float);
        }

        private void DrawProgressBar(Rect position, SerializedProperty property)
        {
            double value = property.propertyType == SerializedPropertyType.Integer
                ? property.longValue
                : property.doubleValue;
            float normalized = (float)((value - progressBar.min) /
                (progressBar.max - progressBar.min));
            string text = string.IsNullOrEmpty(progressBar.label)
                ? $"{value:0.##} / {progressBar.max:0.##}"
                : progressBar.label;
            EditorGUI.ProgressBar(position, Mathf.Clamp01(normalized), text);
        }

        private GUIContent GetLabel(SerializedProperty property,
            GUIContent original)
        {
            Initialize();
            GUIContent fallback = original;
            if (fallback == null || string.IsNullOrEmpty(fallback.text))
                fallback = new GUIContent(property.displayName,
                    original?.image, string.IsNullOrEmpty(original?.tooltip)
                        ? property.tooltip
                        : original.tooltip);
            GUIContent result = nameLabel == null
                ? fallback
                : new GUIContent(nameLabel);
            if (nameLabel != null)
            {
                result.image = fallback?.image;
                if (string.IsNullOrEmpty(result.tooltip))
                    result.tooltip = fallback?.tooltip;
            }
            if (extensionLabels.Length == 0) return result;

            var dynamicLabel = result == null
                ? new GUIContent()
                : new GUIContent(result);
            ActionAttributeContext context = CreateExtensionContext(property);
            for (int i = 0; i < extensionLabels.Length; i++)
            {
                ActionLabelAttribute provider = extensionLabels[i];
                try
                {
                    string text = provider.GetLabel(context);
                    string tooltip = provider.GetTooltip(context);
                    if (text != null) dynamicLabel.text = text;
                    if (tooltip != null) dynamicLabel.tooltip = tooltip;
                }
                catch (Exception exception)
                {
                    ReportExtensionFailure(provider, exception);
                }
            }
            return dynamicLabel;
        }

        private bool IsCollectionElement(SerializedProperty property)
        {
            Initialize();
            return collectionField && !property.isArray;
        }

        private static float GetHelpBoxHeight(string message, float width)
        {
            float minimum = EditorGUIUtility.singleLineHeight * 2;
            if (string.IsNullOrEmpty(message)) return minimum;
            GUIStyle style;
            try
            {
                style = EditorStyles.helpBox;
            }
            catch (NullReferenceException)
            {
                style = null;
            }
            return style == null
                ? minimum
                : Mathf.Max(minimum, style.CalcHeight(new GUIContent(message),
                    Mathf.Max(1, width)));
        }

        private static MessageType ToMessageType(InspectorMessageType type)
        {
            switch (type)
            {
                case InspectorMessageType.Info: return MessageType.Info;
                case InspectorMessageType.Warning: return MessageType.Warning;
                case InspectorMessageType.Error: return MessageType.Error;
                default: return MessageType.None;
            }
        }

        private void Initialize()
        {
            if (initialized) return;
            var fieldConditions = new List<ConditionAttribute>();
            var customConditions = new List<ActionConditionAttribute>();
            var customLabels = new List<ActionLabelAttribute>();
            var customMessages = new List<ActionMessageAttribute>();
            var helps = new List<HelpBoxAttribute>();
            var validations = new List<OnValueChangedAttribute>();
            var customValidations = new List<ActionValidationAttribute>();
            var modifiers = new List<ActionValueModifierAttribute>();
            var callbacks = new List<OnValueChangedAttribute>();
            var buttons = new List<InlineButtonAttribute>();
            FieldInfo currentField = EffectiveFieldInfo;
            if (currentField != null)
            {
                fieldTypeDrawer = UnityFieldTypeDrawerBridge.Create(
                    currentField.FieldType, currentField);
                elementTypeDrawer = UnityFieldTypeDrawerBridge.Create(
                    GetCollectionElementType(currentField.FieldType),
                    currentField);
            }
            IEnumerable<ActionAttributeBase> attributes = currentField == null
                ? new[] { (object)attribute as ActionAttributeBase }
                : currentField.GetCustomAttributes<ActionAttributeBase>(true);

            foreach (ActionAttributeBase item in attributes)
            {
                switch (item)
                {
                    case null: break;
                    case NameAttribute value:
                        nameLabel = new GUIContent(value.name, value.comment);
                        break;
                    case ReadOnlyAttribute _: readOnly = true; break;
                    case ToggleLeftAttribute _: toggleLeft = true; break;
                    case ObjectsOnlyAttribute value: objectsOnly = value; break;
                    case ExpandableAttribute _: expandable = true; break;
                    case ConditionAttribute value:
                        fieldConditions.Add(value); break;
                    case ActionConditionAttribute value:
                        customConditions.Add(value); break;
                    case ActionLabelAttribute value:
                        customLabels.Add(value); break;
                    case ActionMessageAttribute value:
                        customMessages.Add(value); break;
                    case HelpBoxAttribute value: helps.Add(value); break;
                    case TextAttribute value:
                        text = value;
                        customValidations.Add(value);
                        break;
                    case OnValueChangedAttribute value:
                        if (value.mode == ValueChangedMode.Validate)
                            validations.Add(value);
                        else callbacks.Add(value);
                        break;
                    case ActionValidationAttribute value:
                        customValidations.Add(value); break;
                    case ActionValueModifierAttribute value:
                        modifiers.Add(value); break;
                    case SuffixLabelAttribute value: suffix = value; break;
                    case ProgressBarAttribute value: progressBar = value; break;
                    case EnumToggleButtonsAttribute value:
                        enumToggleButtons = value; break;
                    case ValueDropdownAttribute value: valueDropdown = value; break;
                    case MinMaxSliderAttribute value: minMaxSlider = value; break;
                    case HorizontalLineAttribute value: horizontalLine = value; break;
                    case ShowAssetPreviewAttribute value: assetPreview = value; break;
                    case PathAttribute value: path = value; break;
                    case SliderAttribute value: slider = value; break;
                    case EulerAnglesAttribute value: eulerAngles = value; break;
                    case InlineButtonAttribute value: buttons.Add(value); break;
                }
            }

            if (currentField != null)
                collectionField = typeof(IList).IsAssignableFrom(
                    currentField.FieldType);
            SortByPriority(customConditions);
            SortByPriority(customLabels);
            SortByPriority(customMessages);
            SortByPriority(customValidations);
            SortByPriority(modifiers);
            conditions = fieldConditions.ToArray();
            extensionConditions = customConditions.ToArray();
            extensionLabels = customLabels.ToArray();
            extensionMessages = customMessages.ToArray();
            helpBoxes = helps.ToArray();
            validators = validations.ToArray();
            extensionValidators = customValidations.ToArray();
            valueModifiers = modifiers.ToArray();
            valueChangedCallbacks = callbacks.ToArray();
            inlineButtons = buttons.ToArray();
            initialized = true;
        }

        private static Type GetCollectionElementType(Type type)
        {
            if (type == null) return null;
            if (type.IsArray) return type.GetElementType();
            if (type.IsGenericType && type.GetGenericTypeDefinition() ==
                typeof(List<>))
                return type.GetGenericArguments()[0];
            return null;
        }

        private static void SortByPriority<T>(List<T> values)
            where T : ActionAttributeBase
        {
            for (int i = 1; i < values.Count; i++)
            {
                T current = values[i];
                int index = i - 1;
                while (index >= 0 && values[index].Priority > current.Priority)
                {
                    values[index + 1] = values[index];
                    index--;
                }
                values[index + 1] = current;
            }
        }

        private readonly struct PropertyKey : IEquatable<PropertyKey>
        {
            private readonly int targetId;
            private readonly string path;

            internal PropertyKey(SerializedProperty property)
            {
                UnityEngine.Object target = property.serializedObject.targetObject;
                targetId = target == null ? 0 : target.GetInstanceID();
                path = property.propertyPath;
            }

            public bool Equals(PropertyKey other) => targetId == other.targetId &&
                string.Equals(path, other.path, StringComparison.Ordinal);

            public override bool Equals(object obj) => obj is PropertyKey other &&
                Equals(other);

            public override int GetHashCode()
            {
                unchecked
                {
                    return (targetId * 397) ^ (path == null ? 0 : path.GetHashCode());
                }
            }
        }

        private sealed class UnityFieldTypeDrawerBridge
        {
            private delegate float GetPropertyHeightSafeDelegate(
                PropertyDrawer drawer, SerializedProperty property,
                GUIContent label);
            private delegate void OnGUISafeDelegate(PropertyDrawer drawer,
                Rect position, SerializedProperty property, GUIContent label);

            private const BindingFlags InstanceFields = BindingFlags.Instance |
                BindingFlags.NonPublic;
            private const BindingFlags StaticMethods = BindingFlags.Static |
                BindingFlags.NonPublic | BindingFlags.Public;

            private static readonly MethodInfo GetDrawerTypeMethod =
                FindGetDrawerTypeMethod();
            private static readonly FieldInfo DrawerFieldInfoField =
                typeof(PropertyDrawer).GetField("m_FieldInfo", InstanceFields);
            private static readonly GetPropertyHeightSafeDelegate
                GetPropertyHeightSafe = CreateGetPropertyHeightSafeDelegate();
            private static readonly OnGUISafeDelegate OnGUISafe =
                CreateOnGUISafeDelegate();

            private readonly PropertyDrawer drawer;

            private UnityFieldTypeDrawerBridge(PropertyDrawer drawer)
            {
                this.drawer = drawer;
            }

            internal static UnityFieldTypeDrawerBridge Create(Type fieldType,
                FieldInfo field)
            {
                Type drawerType = GetDrawerType(fieldType);
                if (drawerType == null ||
                    !typeof(PropertyDrawer).IsAssignableFrom(drawerType) ||
                    typeof(ActionPropertyDrawer).IsAssignableFrom(drawerType))
                    return null;
                try
                {
                    var instance = Activator.CreateInstance(drawerType, true) as
                        PropertyDrawer;
                    if (instance == null) return null;
                    DrawerFieldInfoField?.SetValue(instance, field);
                    return new UnityFieldTypeDrawerBridge(instance);
                }
                catch (Exception)
                {
                    return null;
                }
            }

            internal float GetPropertyHeight(SerializedProperty property,
                GUIContent label)
            {
                return GetPropertyHeightSafe != null
                    ? GetPropertyHeightSafe(drawer, property, label)
                    : drawer.GetPropertyHeight(property, label);
            }

            internal void OnGUI(Rect position, SerializedProperty property,
                GUIContent label)
            {
                if (OnGUISafe != null)
                    OnGUISafe(drawer, position, property, label);
                else drawer.OnGUI(position, property, label);
            }

            private static Type GetDrawerType(Type targetType)
            {
                if (targetType == null || GetDrawerTypeMethod == null)
                    return null;
                try
                {
                    ParameterInfo[] parameters =
                        GetDrawerTypeMethod.GetParameters();
                    var arguments = new object[parameters.Length];
                    arguments[0] = targetType;
                    for (int i = 1; i < parameters.Length; i++)
                    {
                        Type parameterType = parameters[i].ParameterType;
                        if (parameterType == typeof(Type[]))
                            arguments[i] = Array.Empty<Type>();
                        else if (parameterType == typeof(bool))
                            arguments[i] = false;
                        else arguments[i] = parameters[i].HasDefaultValue
                            ? parameters[i].DefaultValue
                            : null;
                    }
                    return GetDrawerTypeMethod.Invoke(null, arguments) as Type;
                }
                catch (TargetInvocationException)
                {
                    return null;
                }
                catch (ArgumentException)
                {
                    return null;
                }
                catch (TargetParameterCountException)
                {
                    return null;
                }
            }

            private static MethodInfo FindGetDrawerTypeMethod()
            {
                Type utility = typeof(EditorGUI).Assembly.GetType(
                    "UnityEditor.ScriptAttributeUtility");
                if (utility == null) return null;
                MethodInfo[] methods = utility.GetMethods(StaticMethods);
                MethodInfo best = null;
                for (int i = 0; i < methods.Length; i++)
                {
                    MethodInfo method = methods[i];
                    if (method.Name != "GetDrawerTypeForType") continue;
                    ParameterInfo[] parameters = method.GetParameters();
                    if (parameters.Length == 0 ||
                        parameters[0].ParameterType != typeof(Type)) continue;
                    bool supported = true;
                    for (int j = 1; j < parameters.Length; j++)
                    {
                        Type parameterType = parameters[j].ParameterType;
                        if (parameterType != typeof(Type[]) &&
                            parameterType != typeof(bool) &&
                            !parameters[j].HasDefaultValue)
                        {
                            supported = false;
                            break;
                        }
                    }
                    if (supported && (best == null || parameters.Length <
                        best.GetParameters().Length))
                        best = method;
                }
                return best;
            }

            private static GetPropertyHeightSafeDelegate
                CreateGetPropertyHeightSafeDelegate()
            {
                MethodInfo method = typeof(PropertyDrawer).GetMethod(
                    "GetPropertyHeightSafe", BindingFlags.Instance |
                    BindingFlags.NonPublic, null,
                    new[] { typeof(SerializedProperty), typeof(GUIContent) },
                    null);
                if (method == null) return null;
                try
                {
                    return (GetPropertyHeightSafeDelegate)
                        Delegate.CreateDelegate(
                            typeof(GetPropertyHeightSafeDelegate), method);
                }
                catch (ArgumentException)
                {
                    return null;
                }
                catch (MemberAccessException)
                {
                    return null;
                }
            }

            private static OnGUISafeDelegate CreateOnGUISafeDelegate()
            {
                MethodInfo method = typeof(PropertyDrawer).GetMethod(
                    "OnGUISafe", BindingFlags.Instance |
                    BindingFlags.NonPublic, null,
                    new[]
                    {
                        typeof(Rect), typeof(SerializedProperty),
                        typeof(GUIContent)
                    }, null);
                if (method == null) return null;
                try
                {
                    return (OnGUISafeDelegate)Delegate.CreateDelegate(
                        typeof(OnGUISafeDelegate), method);
                }
                catch (ArgumentException)
                {
                    return null;
                }
                catch (MemberAccessException)
                {
                    return null;
                }
            }
        }

        private sealed class CombinedActionPropertyDrawer : ActionPropertyDrawer
        {
        }
    }

}
