using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

namespace ActionAttribute
{
    internal abstract class ActionPropertyDrawer : PropertyDrawer
    {
        private static readonly HashSet<PropertyKey> DrawingProperties = new();
        private static readonly HashSet<PropertyKey> MeasuringProperties = new();
        private static GUIStyle placeholderStyle;
        private bool initialized;
        private bool readOnly;
        private bool collectionField;
        private bool hideLabel;
        private bool delayed;
        private bool hideInEditorMode;
        private bool hideInPlayMode;
        private bool disableInEditorMode;
        private bool disableInPlayMode;
        private bool toggleLeft;
        private bool assetsOnly;
        private bool sceneObjectsOnly;
        private bool nonNegative;
        private bool positive;
        private bool expandable;
        private GUIContent nameLabel;
        private ShowIfAttribute[] showConditions = Array.Empty<ShowIfAttribute>();
        private HideIfAttribute[] hideConditions = Array.Empty<HideIfAttribute>();
        private EnableIfAttribute[] enableConditions = Array.Empty<EnableIfAttribute>();
        private DisableIfAttribute[] disableConditions = Array.Empty<DisableIfAttribute>();
        private HelpBoxAttribute[] helpBoxes = Array.Empty<HelpBoxAttribute>();
        private ValidateInputAttribute[] validators =
            Array.Empty<ValidateInputAttribute>();
        private OnValueChangedAttribute[] valueChangedCallbacks =
            Array.Empty<OnValueChangedAttribute>();
        private ClampAttribute clamp;
        private MinValueAttribute minValue;
        private MaxValueAttribute maxValue;
        private MultilineTextAttribute multiline;
        private ResizableTextAreaAttribute resizableTextArea;
        private RequiredAttribute required;
        private TitleAttribute title;
        private SuffixLabelAttribute suffix;
        private PrefixLabelAttribute prefix;
        private PropertySpaceAttribute propertySpace;
        private ProgressBarAttribute progressBar;
        private EnumFlagsAttribute enumFlags;
        private EnumSearchAttribute enumSearch;
        private EnumToggleButtonsAttribute enumToggleButtons;
        private ValueDropdownAttribute valueDropdown;
        private MinMaxSliderAttribute minMaxSlider;
        private HorizontalLineAttribute horizontalLine;
        private ShowAssetPreviewAttribute assetPreview;
        private TagAttribute tag;
        private LayerAttribute layer;
        private SortingLayerAttribute sortingLayer;
        private SceneNameAttribute sceneName;
        private InputAxisAttribute inputAxis;
        private AnimatorParamAttribute animatorParam;
        private PropertyTooltipAttribute propertyTooltip;
        private LabelWidthAttribute labelWidth;
        private IndentAttribute indent;
        private GUIColorAttribute guiColor;
        private FilePathAttribute filePath;
        private FolderPathAttribute folderPath;
        private RequiredListLengthAttribute requiredListLength;
        private UniqueListAttribute uniqueList;
        private ChildGameObjectsOnlyAttribute childGameObjectsOnly;
        private ParentGameObjectsOnlyAttribute parentGameObjectsOnly;
        private WrapAttribute wrap;
        private CurveRangeAttribute curveRange;
        private ReorderableListAttribute reorderableList;
        private PasswordFieldAttribute passwordField;
        private PlaceholderAttribute placeholder;
        private MaxLengthAttribute maxLength;
        private StepAttribute step;
        private SliderAttribute slider;
        private EulerAnglesAttribute eulerAngles;
        private AssetPathAttribute assetPath;
        private AssetGuidAttribute assetGuid;
        private ColorPaletteAttribute colorPalette;
        private Color[] paletteColors = Array.Empty<Color>();
        private FieldInfo explicitFieldInfo;

        private FieldInfo EffectiveFieldInfo => explicitFieldInfo ?? fieldInfo;

        internal static ActionPropertyDrawer Create(FieldInfo field)
        {
            return new CombinedActionPropertyDrawer
            {
                explicitFieldInfo = field
            };
        }
        private readonly Dictionary<string, UnityEditorInternal.ReorderableList>
            reorderableLists = new();
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
                return EditorGUI.GetPropertyHeight(property, label, true);
            if (!ShouldShow(property)) return 0;

            GUIContent fieldLabel = GetLabel(label);
            float width = Event.current == null
                ? 320
                : Mathf.Max(1, EditorGUIUtility.currentViewWidth - 40);
            float spacing = EditorGUIUtility.standardVerticalSpacing;
            float height = propertySpace?.before ?? 0;

            if (horizontalLine != null)
                height += horizontalLine.margin * 2 + horizontalLine.height;
            if (title != null) height += GetTitleHeight(width) + spacing;
            for (int i = 0; i < helpBoxes.Length; i++)
                height += GetHelpBoxHeight(helpBoxes[i].message, width) + spacing;

            height += GetFieldHeight(property, fieldLabel, width);
            if (ShouldDrawColorPalette(property))
                height += EditorGUIUtility.singleLineHeight + spacing;
            if (ShouldDrawProgressBar(property))
                height += EditorGUIUtility.singleLineHeight + spacing;
            if (ShouldDrawAssetPreview(property))
                height += assetPreview.height + spacing;

            if (IsRequiredValueMissing(property))
                height += GetHelpBoxHeight(GetRequiredMessage(property, fieldLabel),
                    width) + spacing;
            for (int i = 0; i < validators.Length; i++)
            {
                if (TryGetValidationError(property, validators[i], fieldLabel,
                    out string message))
                    height += GetHelpBoxHeight(message, width) + spacing;
            }
            if (TryGetListLengthError(property, out string listError))
                height += GetHelpBoxHeight(listError, width) + spacing;
            if (TryGetUniqueListError(property, out string uniqueError))
                height += GetHelpBoxHeight(uniqueError, width) + spacing;
            if (TryGetObjectScopeError(property, out string objectError))
                height += GetHelpBoxHeight(objectError, width) + spacing;
            height += propertySpace?.after ?? 0;
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
                OnGUICore(position, property, label);
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
                EditorGUI.PropertyField(position, property, label, true);
                return;
            }
            if (!ShouldShow(property)) return;

            GUIContent fieldLabel = GetLabel(label);
            float spacing = EditorGUIUtility.standardVerticalSpacing;
            position.y += propertySpace?.before ?? 0;

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

            if (title != null)
            {
                float titleHeight = GetTitleHeight(position.width);
                DrawTitle(new Rect(position.x, position.y, position.width,
                    titleHeight));
                position.y += titleHeight + spacing;
            }

            for (int i = 0; i < helpBoxes.Length; i++)
            {
                HelpBoxAttribute help = helpBoxes[i];
                float height = GetHelpBoxHeight(help.message, position.width);
                EditorGUI.HelpBox(new Rect(position.x, position.y, position.width,
                    height), help.message, ToMessageType(help.type));
                position.y += height + spacing;
            }

            float fieldHeight = GetFieldHeight(property, fieldLabel, position.width);
            Rect fieldRect = new Rect(position.x, position.y, position.width,
                fieldHeight);
            string invokedMethod = null;
            bool changed;
            using (var changeCheck = new EditorGUI.ChangeCheckScope())
            {
                float previousLabelWidth = EditorGUIUtility.labelWidth;
                int previousIndent = EditorGUI.indentLevel;
                Color previousColor = GUI.color;
                try
                {
                    if (labelWidth != null)
                        EditorGUIUtility.labelWidth = labelWidth.width;
                    if (indent != null) EditorGUI.indentLevel += indent.level;
                    if (guiColor != null) GUI.color = new Color(guiColor.red,
                        guiColor.green, guiColor.blue, guiColor.alpha);
                    using (new EditorGUI.DisabledScope(readOnly ||
                        !ShouldEnable(property)))
                        invokedMethod = DrawField(fieldRect, property, fieldLabel);
                }
                finally
                {
                    GUI.color = previousColor;
                    EditorGUI.indentLevel = previousIndent;
                    EditorGUIUtility.labelWidth = previousLabelWidth;
                }
                changed = changeCheck.changed;
            }
            if (changed)
            {
                ApplyValueLimits(property);
                ApplyObjectScope(property);
            }
            position.y += fieldHeight + spacing;

            if (ShouldDrawColorPalette(property))
            {
                DrawColorPalette(new Rect(position.x, position.y,
                    position.width, EditorGUIUtility.singleLineHeight), property);
                position.y += EditorGUIUtility.singleLineHeight + spacing;
            }

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

            if (IsRequiredValueMissing(property))
            {
                string message = GetRequiredMessage(property, fieldLabel);
                float height = GetHelpBoxHeight(message, position.width);
                EditorGUI.HelpBox(new Rect(position.x, position.y, position.width,
                    height), message, MessageType.Error);
                position.y += height + spacing;
            }

            for (int i = 0; i < validators.Length; i++)
            {
                ValidateInputAttribute validator = validators[i];
                if (!TryGetValidationError(property, validator, fieldLabel,
                    out string message)) continue;
                float height = GetHelpBoxHeight(message, position.width);
                EditorGUI.HelpBox(new Rect(position.x, position.y, position.width,
                    height), message, ToMessageType(validator.type));
                position.y += height + spacing;
            }
            if (TryGetListLengthError(property, out string listError))
            {
                float height = GetHelpBoxHeight(listError, position.width);
                EditorGUI.HelpBox(new Rect(position.x, position.y, position.width,
                    height), listError, MessageType.Error);
                position.y += height + spacing;
            }
            if (TryGetUniqueListError(property, out string uniqueError))
            {
                float height = GetHelpBoxHeight(uniqueError, position.width);
                EditorGUI.HelpBox(new Rect(position.x, position.y, position.width,
                    height), uniqueError, MessageType.Error);
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
            if (prefix != null && !string.IsNullOrEmpty(prefix.label))
            {
                Rect contentRect = EditorGUI.PrefixLabel(valueRect, label);
                float prefixWidth = Mathf.Min(contentRect.width * 0.4f,
                    EditorStyles.miniLabel.CalcSize(
                        new GUIContent(prefix.label)).x + 6);
                EditorGUI.LabelField(new Rect(contentRect.x, contentRect.y,
                    prefixWidth, EditorGUIUtility.singleLineHeight),
                    prefix.label, EditorStyles.miniLabel);
                valueRect = new Rect(contentRect.x + prefixWidth,
                    contentRect.y, Mathf.Max(1, contentRect.width - prefixWidth),
                    contentRect.height);
                label = GUIContent.none;
            }

            if (resizableTextArea != null &&
                property.propertyType == SerializedPropertyType.String)
                DrawTextArea(valueRect, property, label);
            else if (multiline != null &&
                property.propertyType == SerializedPropertyType.String)
                DrawTextArea(valueRect, property, label);
            else if (passwordField != null &&
                property.propertyType == SerializedPropertyType.String)
                property.stringValue = EditorGUI.PasswordField(valueRect, label,
                    property.stringValue);
            else if (slider != null &&
                (property.propertyType == SerializedPropertyType.Integer ||
                 property.propertyType == SerializedPropertyType.Float))
                DrawSlider(valueRect, property, label);
            else if (eulerAngles != null &&
                property.propertyType == SerializedPropertyType.Quaternion)
                DrawEulerAngles(valueRect, property, label);
            else if (assetPath != null &&
                property.propertyType == SerializedPropertyType.String)
                DrawAssetIdentifier(valueRect, property, label,
                    assetPath.assetType, false);
            else if (assetGuid != null &&
                property.propertyType == SerializedPropertyType.String)
                DrawAssetIdentifier(valueRect, property, label,
                    assetGuid.assetType, true);
            else if (colorPalette != null &&
                property.propertyType == SerializedPropertyType.Color)
                property.colorValue = EditorGUI.ColorField(valueRect, label,
                    property.colorValue);
            else if (placeholder != null &&
                property.propertyType == SerializedPropertyType.String)
                DrawPlaceholder(valueRect, property, label);
            else if (enumFlags != null &&
                property.propertyType == SerializedPropertyType.Enum &&
                EffectiveFieldInfo?.FieldType.IsEnum == true)
                DrawEnumFlags(valueRect, property, label);
            else if (minMaxSlider != null &&
                property.propertyType == SerializedPropertyType.Vector2)
                DrawMinMaxSlider(valueRect, property, label);
            else if (valueDropdown != null)
                DrawValueDropdown(valueRect, property, label);
            else if (enumToggleButtons != null &&
                property.propertyType == SerializedPropertyType.Enum)
                DrawEnumToggleButtons(valueRect, property, label);
            else if (enumSearch != null &&
                property.propertyType == SerializedPropertyType.Enum)
                DrawEnumSearch(valueRect, property, label);
            else if (tag != null &&
                property.propertyType == SerializedPropertyType.String)
                property.stringValue = EditorGUI.TagField(valueRect, label,
                    property.stringValue);
            else if (layer != null)
                DrawLayer(valueRect, property, label);
            else if (sortingLayer != null)
                DrawSortingLayer(valueRect, property, label);
            else if (sceneName != null)
                DrawScene(valueRect, property, label);
            else if (inputAxis != null &&
                property.propertyType == SerializedPropertyType.String)
                DrawStringPopup(valueRect, property, label,
                    GetInputAxisNames());
            else if (animatorParam != null)
                DrawAnimatorParameter(valueRect, property, label);
            else if (filePath != null &&
                property.propertyType == SerializedPropertyType.String)
                DrawFilePath(valueRect, property, label);
            else if (folderPath != null &&
                property.propertyType == SerializedPropertyType.String)
                DrawFolderPath(valueRect, property, label);
            else if (curveRange != null &&
                property.propertyType == SerializedPropertyType.AnimationCurve)
                property.animationCurveValue = EditorGUI.CurveField(valueRect,
                    label, property.animationCurveValue,
                    new Color(curveRange.red, curveRange.green, curveRange.blue),
                    new Rect(curveRange.minX, curveRange.minY,
                        curveRange.maxX - curveRange.minX,
                        curveRange.maxY - curveRange.minY));
            else if (reorderableList != null && property.isArray &&
                property.propertyType != SerializedPropertyType.String)
                GetReorderableList(property, label).DoList(valueRect);
            else if (expandable &&
                property.propertyType == SerializedPropertyType.ObjectReference)
                DrawExpandable(valueRect, property, label);
            else if (toggleLeft &&
                property.propertyType == SerializedPropertyType.Boolean)
                property.boolValue = EditorGUI.ToggleLeft(valueRect,
                    label, property.boolValue);
            else if (delayed && DrawDelayed(valueRect, property, label))
            {
            }
            else if ((assetsOnly || sceneObjectsOnly ||
                childGameObjectsOnly != null || parentGameObjectsOnly != null) &&
                property.propertyType == SerializedPropertyType.ObjectReference)
                property.objectReferenceValue = EditorGUI.ObjectField(valueRect,
                    label, property.objectReferenceValue,
                    EffectiveFieldInfo?.FieldType ?? typeof(UnityEngine.Object),
                    !assetsOnly);
            else
                EditorGUI.PropertyField(valueRect, property, label, true);

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

        private static void DrawAssetIdentifier(Rect position,
            SerializedProperty property, GUIContent label, Type assetType,
            bool storeGuid)
        {
            if (assetType == null ||
                !typeof(UnityEngine.Object).IsAssignableFrom(assetType))
                assetType = typeof(UnityEngine.Object);
            string path = storeGuid
                ? AssetDatabase.GUIDToAssetPath(property.stringValue)
                : property.stringValue;
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
            property.stringValue = storeGuid
                ? AssetDatabase.AssetPathToGUID(path)
                : path;
        }

        private void DrawColorPalette(Rect position,
            SerializedProperty property)
        {
            float startX = position.x + Mathf.Min(EditorGUIUtility.labelWidth,
                position.width * 0.45f);
            float available = Mathf.Max(1, position.xMax - startX);
            float width = Mathf.Min(28, (available -
                (paletteColors.Length - 1) * 2) / paletteColors.Length);
            for (int i = 0; i < paletteColors.Length; i++)
            {
                Rect swatch = new Rect(startX + i * (width + 2), position.y,
                    width, position.height);
                EditorGUI.DrawRect(swatch, new Color(0.15f, 0.15f, 0.15f));
                Rect inner = new Rect(swatch.x + 1, swatch.y + 1,
                    Mathf.Max(1, swatch.width - 2),
                    Mathf.Max(1, swatch.height - 2));
                EditorGUI.DrawRect(inner, paletteColors[i]);
                string tooltip = "#" +
                    ColorUtility.ToHtmlStringRGBA(paletteColors[i]);
                if (GUI.Button(swatch, new GUIContent(string.Empty, tooltip),
                        GUIStyle.none))
                    property.colorValue = paletteColors[i];
            }
        }

        private void DrawPlaceholder(Rect position, SerializedProperty property,
            GUIContent label)
        {
            Rect valueRect = EditorGUI.PrefixLabel(position, label);
            property.stringValue = EditorGUI.TextField(valueRect,
                property.stringValue);
            if (!string.IsNullOrEmpty(property.stringValue) ||
                string.IsNullOrEmpty(placeholder.text) ||
                Event.current.type != EventType.Repaint) return;

            Rect hintRect = valueRect;
            hintRect.xMin += 3;
            GUI.Label(hintRect, placeholder.text, PlaceholderStyle);
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

        private void DrawValueDropdown(Rect position, SerializedProperty property,
            GUIContent label)
        {
            Rect buttonRect = EditorGUI.PrefixLabel(position, label);
            if (!SerializedPropertyMemberUtility.TryGetDropdownValues(property,
                valueDropdown.valuesMember, out string[] labels,
                out object[] values))
            {
                EditorGUI.HelpBox(buttonRect,
                    $"找不到下拉数据 {valueDropdown.valuesMember}",
                    MessageType.Error);
                return;
            }
            object current = SerializedPropertyMemberUtility.GetSerializedValue(
                property);
            int index = FindDropdownIndex(current, values);
            string text = index >= 0 && index < labels.Length
                ? labels[index]
                : "NONE";
            if (!EditorGUI.DropdownButton(buttonRect, new GUIContent(text),
                FocusType.Keyboard)) return;
            string path = property.propertyPath;
            UnityEngine.Object[] targets = property.serializedObject.targetObjects;
            SearchPopupWindow.Show(buttonRect, labels, index, selected =>
            {
                var serialized = new SerializedObject(targets);
                SerializedProperty selectedProperty = serialized.FindProperty(path);
                if (selectedProperty == null || selected < 0 ||
                    selected >= values.Length) return;
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

        private static void DrawLayer(Rect position, SerializedProperty property,
            GUIContent label)
        {
            if (property.propertyType == SerializedPropertyType.Integer)
                property.intValue = EditorGUI.LayerField(position, label,
                    property.intValue);
            else if (property.propertyType == SerializedPropertyType.String)
            {
                int selected = LayerMask.NameToLayer(property.stringValue);
                selected = EditorGUI.LayerField(position, label,
                    Math.Max(0, selected));
                property.stringValue = LayerMask.LayerToName(selected);
            }
            else EditorGUI.PropertyField(position, property, label, true);
        }

        private static void DrawSortingLayer(Rect position,
            SerializedProperty property, GUIContent label)
        {
            SortingLayer[] layers = SortingLayer.layers;
            string[] names = Array.ConvertAll(layers, value => value.name);
            int selected = 0;
            for (int i = 0; i < layers.Length; i++)
            {
                if ((property.propertyType == SerializedPropertyType.String &&
                     layers[i].name == property.stringValue) ||
                    (property.propertyType == SerializedPropertyType.Integer &&
                     layers[i].id == property.intValue))
                {
                    selected = i;
                    break;
                }
            }
            Rect popupRect = EditorGUI.PrefixLabel(position, label);
            selected = EditorGUI.Popup(popupRect, selected, names);
            if (layers.Length == 0) return;
            if (property.propertyType == SerializedPropertyType.String)
                property.stringValue = layers[selected].name;
            else if (property.propertyType == SerializedPropertyType.Integer)
                property.intValue = layers[selected].id;
        }

        private void DrawScene(Rect position, SerializedProperty property,
            GUIContent label)
        {
            var names = new List<string>();
            var indexes = new List<int>();
            EditorBuildSettingsScene[] scenes = EditorBuildSettings.scenes;
            for (int i = 0; i < scenes.Length; i++)
            {
                if (!sceneName.includeDisabled && !scenes[i].enabled) continue;
                names.Add(System.IO.Path.GetFileNameWithoutExtension(scenes[i].path));
                indexes.Add(i);
            }
            int selected = 0;
            for (int i = 0; i < names.Count; i++)
            {
                if ((property.propertyType == SerializedPropertyType.String &&
                     property.stringValue == names[i]) ||
                    (property.propertyType == SerializedPropertyType.Integer &&
                     property.intValue == indexes[i])) selected = i;
            }
            Rect popupRect = EditorGUI.PrefixLabel(position, label);
            selected = EditorGUI.Popup(popupRect, selected, names.ToArray());
            if (names.Count == 0) return;
            if (property.propertyType == SerializedPropertyType.String)
                property.stringValue = names[selected];
            else if (property.propertyType == SerializedPropertyType.Integer)
                property.intValue = indexes[selected];
        }

        private static void DrawStringPopup(Rect position,
            SerializedProperty property, GUIContent label, string[] options)
        {
            int index = Math.Max(0, Array.IndexOf(options, property.stringValue));
            Rect popupRect = EditorGUI.PrefixLabel(position, label);
            index = EditorGUI.Popup(popupRect, index, options);
            if (options.Length > 0) property.stringValue = options[index];
        }

        private void DrawFilePath(Rect position, SerializedProperty property,
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
            string selected = EditorUtility.OpenFilePanel("选择文件",
                GetInitialDirectory(property.stringValue, true),
                filePath.extension ?? string.Empty);
            if (string.IsNullOrEmpty(selected)) return;
            property.stringValue = NormalizeSelectedPath(selected,
                filePath.absolutePath);
            property.serializedObject.ApplyModifiedProperties();
            GUIUtility.ExitGUI();
        }

        private void DrawFolderPath(Rect position, SerializedProperty property,
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
            string selected = EditorUtility.OpenFolderPanel("选择文件夹",
                GetInitialDirectory(property.stringValue, false), string.Empty);
            if (string.IsNullOrEmpty(selected)) return;
            property.stringValue = NormalizeSelectedPath(selected,
                folderPath.absolutePath);
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
            if (!SerializedPropertyMemberUtility.TryGetMemberValue(property,
                animatorParam.animatorMember, out object value) ||
                !(value is Animator animator) || animator == null)
            {
                EditorGUI.HelpBox(position,
                    $"找不到 Animator：{animatorParam.animatorMember}",
                    MessageType.Error);
                return;
            }
            UnityEngine.AnimatorControllerParameter[] parameters =
                animator.parameters;
            string[] names = Array.ConvertAll(parameters, item => item.name);
            int selected = 0;
            for (int i = 0; i < parameters.Length; i++)
            {
                if ((property.propertyType == SerializedPropertyType.String &&
                     property.stringValue == parameters[i].name) ||
                    (property.propertyType == SerializedPropertyType.Integer &&
                     property.intValue == parameters[i].nameHash))
                    selected = i;
            }
            Rect popupRect = EditorGUI.PrefixLabel(position, label);
            selected = EditorGUI.Popup(popupRect, selected, names);
            if (parameters.Length == 0) return;
            if (property.propertyType == SerializedPropertyType.String)
                property.stringValue = parameters[selected].name;
            else if (property.propertyType == SerializedPropertyType.Integer)
                property.intValue = parameters[selected].nameHash;
        }

        private UnityEditorInternal.ReorderableList GetReorderableList(
            SerializedProperty property, GUIContent label)
        {
            string key = property.serializedObject.targetObject.GetInstanceID() +
                ":" + property.propertyPath;
            if (reorderableLists.TryGetValue(key, out var list)) return list;
            list = new UnityEditorInternal.ReorderableList(
                property.serializedObject, property, reorderableList.draggable,
                true, reorderableList.add, reorderableList.remove);
            list.drawHeaderCallback = rect => EditorGUI.LabelField(rect, label);
            list.elementHeightCallback = index =>
            {
                if (index < 0 || index >= property.arraySize)
                    return EditorGUIUtility.singleLineHeight;
                return EditorGUI.GetPropertyHeight(
                    property.GetArrayElementAtIndex(index), true) +
                    EditorGUIUtility.standardVerticalSpacing;
            };
            list.drawElementCallback = (rect, index, active, focused) =>
            {
                if (index < 0 || index >= property.arraySize) return;
                SerializedProperty element = property.GetArrayElementAtIndex(index);
                rect.height = EditorGUI.GetPropertyHeight(element, true);
                EditorGUI.PropertyField(rect, element, GUIContent.none, true);
            };
            reorderableLists.Add(key, list);
            return list;
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

        private static void DrawTextArea(Rect position, SerializedProperty property,
            GUIContent label)
        {
            Rect valueRect = label == GUIContent.none
                ? position
                : EditorGUI.PrefixLabel(position, label);
            property.stringValue = EditorGUI.TextArea(valueRect,
                property.stringValue ?? string.Empty);
        }

        private void DrawEnumFlags(Rect position, SerializedProperty property,
            GUIContent label)
        {
            Enum current = (Enum)Enum.ToObject(EffectiveFieldInfo.FieldType,
                property.intValue);
            Enum result = EditorGUI.EnumFlagsField(position, label, current);
            property.intValue = Convert.ToInt32(result);
        }

        private bool DrawDelayed(Rect position, SerializedProperty property,
            GUIContent label)
        {
            switch (property.propertyType)
            {
                case SerializedPropertyType.Integer when
                    EffectiveFieldInfo?.FieldType == typeof(int):
                    property.intValue = EditorGUI.DelayedIntField(position, label,
                        property.intValue);
                    return true;
                case SerializedPropertyType.Float when
                    EffectiveFieldInfo?.FieldType == typeof(double):
                    property.doubleValue = EditorGUI.DelayedDoubleField(position,
                        label, property.doubleValue);
                    return true;
                case SerializedPropertyType.Float:
                    property.floatValue = EditorGUI.DelayedFloatField(position,
                        label, property.floatValue);
                    return true;
                case SerializedPropertyType.String:
                    property.stringValue = EditorGUI.DelayedTextField(position,
                        label, property.stringValue);
                    return true;
                default:
                    return false;
            }
        }

        private float GetFieldHeight(SerializedProperty property, GUIContent label,
            float width)
        {
            if (minMaxSlider != null || enumToggleButtons != null ||
                valueDropdown != null || enumSearch != null ||
                tag != null || layer != null || sortingLayer != null ||
                sceneName != null || inputAxis != null || filePath != null ||
                folderPath != null || toggleLeft || animatorParam != null ||
                slider != null || eulerAngles != null || assetPath != null ||
                assetGuid != null || colorPalette != null)
                return EditorGUIUtility.singleLineHeight;
            if (reorderableList != null && property.isArray &&
                property.propertyType != SerializedPropertyType.String)
                return GetReorderableList(property, label).GetHeight();
            if (expandable &&
                property.propertyType == SerializedPropertyType.ObjectReference)
                return GetExpandableHeight(property);
            if (property.propertyType == SerializedPropertyType.String)
            {
                if (resizableTextArea != null)
                {
                    float min = EditorGUIUtility.singleLineHeight *
                        resizableTextArea.minLines;
                    float max = EditorGUIUtility.singleLineHeight *
                        resizableTextArea.maxLines;
                    float valueWidth = label == GUIContent.none
                        ? width
                        : Mathf.Max(1, width - EditorGUIUtility.labelWidth);
                    float calculated = EditorStyles.textArea.CalcHeight(
                        new GUIContent(property.stringValue ?? string.Empty),
                        valueWidth);
                    return Mathf.Clamp(calculated, min, max);
                }
                if (multiline != null)
                    return EditorGUIUtility.singleLineHeight * multiline.lines;
            }
            if (explicitFieldInfo != null)
                return GetUndecoratedPropertyHeight(property);
            return EditorGUI.GetPropertyHeight(property, label, true);
        }

        private static float GetUndecoratedPropertyHeight(
            SerializedProperty property)
        {
            float height = EditorGUIUtility.singleLineHeight;
            if (!property.isExpanded || !HasExpandableChildren(property))
                return height;

            SerializedProperty child = property.Copy();
            SerializedProperty end = child.GetEndProperty();
            int childDepth = property.depth + 1;
            bool enterChildren = true;
            while (child.NextVisible(enterChildren) &&
                   !SerializedProperty.EqualContents(child, end))
            {
                enterChildren = false;
                if (child.depth != childDepth) continue;
                height += EditorGUIUtility.standardVerticalSpacing +
                    EditorGUI.GetPropertyHeight(child, true);
            }
            return height;
        }

        private static bool HasExpandableChildren(SerializedProperty property)
        {
            if (property.isArray &&
                property.propertyType != SerializedPropertyType.String)
                return true;
            return property.propertyType == SerializedPropertyType.Generic ||
                   property.propertyType ==
                   SerializedPropertyType.ManagedReference;
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
            if (maxLength != null &&
                property.propertyType == SerializedPropertyType.String)
                property.stringValue = ValueConstraintUtility.Truncate(
                    property.stringValue, maxLength.length);

            if (step != null && step.step > 0)
            {
                if (property.propertyType == SerializedPropertyType.Integer)
                {
                    double value = ValueConstraintUtility.Snap(property.longValue,
                        step.step, step.origin);
                    property.longValue = value <= long.MinValue
                        ? long.MinValue
                        : value >= long.MaxValue
                            ? long.MaxValue
                            : (long)value;
                }
                else if (property.propertyType == SerializedPropertyType.Float)
                    property.doubleValue = ValueConstraintUtility.Snap(
                        property.doubleValue, step.step, step.origin);
            }

            bool hasMin = false;
            bool hasMax = false;
            double min = double.MinValue;
            double max = double.MaxValue;
            if (clamp != null)
            {
                min = clamp.min;
                max = clamp.max;
                hasMin = hasMax = true;
            }
            if (minValue != null)
            {
                min = Math.Max(min, minValue.value);
                hasMin = true;
            }
            if (maxValue != null)
            {
                max = Math.Min(max, maxValue.value);
                hasMax = true;
            }
            if (nonNegative)
            {
                min = Math.Max(min, 0);
                hasMin = true;
            }
            if (positive)
            {
                double positiveMinimum = property.propertyType ==
                    SerializedPropertyType.Integer ? 1 : float.Epsilon;
                min = Math.Max(min, positiveMinimum);
                hasMin = true;
            }
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

            if (wrap != null)
            {
                double range = wrap.max - wrap.min;
                if (property.propertyType == SerializedPropertyType.Integer)
                {
                    long minValue = (long)Math.Ceiling(wrap.min);
                    long maxValue = (long)Math.Floor(wrap.max);
                    long integerRange = maxValue - minValue + 1;
                    if (integerRange > 0)
                    {
                        long value = property.longValue;
                        property.longValue = minValue +
                            ((value - minValue) % integerRange + integerRange) %
                            integerRange;
                    }
                }
                else if (property.propertyType == SerializedPropertyType.Float &&
                    range > 0)
                {
                    double value = property.doubleValue;
                    property.doubleValue = wrap.min +
                        ((value - wrap.min) % range + range) % range;
                }
            }
        }

        private bool ShouldShow(SerializedProperty property)
        {
            Initialize();
            if (hideInEditorMode && !EditorApplication.isPlaying) return false;
            if (hideInPlayMode && EditorApplication.isPlaying) return false;
            for (int i = 0; i < showConditions.Length; i++)
                if (!EvaluateConditions(property, showConditions[i].conditions,
                    showConditions[i].conditionOperator,
                    showConditions[i].expected)) return false;
            for (int i = 0; i < hideConditions.Length; i++)
                if (EvaluateConditions(property, hideConditions[i].conditions,
                    hideConditions[i].conditionOperator,
                    hideConditions[i].expected)) return false;
            return true;
        }

        private bool ShouldEnable(SerializedProperty property)
        {
            Initialize();
            if (disableInEditorMode && !EditorApplication.isPlaying) return false;
            if (disableInPlayMode && EditorApplication.isPlaying) return false;
            for (int i = 0; i < enableConditions.Length; i++)
                if (!EvaluateConditions(property, enableConditions[i].conditions,
                    enableConditions[i].conditionOperator,
                    enableConditions[i].expected)) return false;
            for (int i = 0; i < disableConditions.Length; i++)
                if (EvaluateConditions(property, disableConditions[i].conditions,
                    disableConditions[i].conditionOperator,
                    disableConditions[i].expected)) return false;
            return true;
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

        private bool IsRequiredValueMissing(SerializedProperty property)
        {
            Initialize();
            if (required == null) return false;
            switch (property.propertyType)
            {
                case SerializedPropertyType.ObjectReference:
                    return property.objectReferenceValue == null;
                case SerializedPropertyType.String:
                    return string.IsNullOrWhiteSpace(property.stringValue);
                case SerializedPropertyType.ManagedReference:
                    return property.managedReferenceValue == null;
                default:
                    return false;
            }
        }

        private bool TryGetListLengthError(SerializedProperty property,
            out string message)
        {
            message = null;
            if (requiredListLength == null || !property.isArray ||
                property.propertyType == SerializedPropertyType.String)
                return false;
            int count = property.arraySize;
            if (count >= requiredListLength.min && count <= requiredListLength.max)
                return false;
            message = requiredListLength.max == int.MaxValue
                ? $"集合至少需要 {requiredListLength.min} 个元素，当前为 {count} 个。"
                : $"集合元素数量必须在 {requiredListLength.min} 到 " +
                  $"{requiredListLength.max} 之间，当前为 {count} 个。";
            return true;
        }

        private bool TryGetObjectScopeError(SerializedProperty property,
            out string message)
        {
            message = null;
            if ((!assetsOnly && !sceneObjectsOnly &&
                childGameObjectsOnly == null && parentGameObjectsOnly == null) ||
                property.propertyType != SerializedPropertyType.ObjectReference ||
                property.objectReferenceValue == null) return false;
            bool persistent = EditorUtility.IsPersistent(
                property.objectReferenceValue);
            if (assetsOnly && !persistent)
                message = "该字段只允许引用 Project 中的资源。";
            else if (sceneObjectsOnly && persistent)
                message = "该字段只允许引用当前场景中的对象。";
            else if (!persistent && childGameObjectsOnly != null &&
                !IsAllowedHierarchyObject(property, true,
                    childGameObjectsOnly.includeSelf))
                message = "该字段只允许引用当前对象的子层级。";
            else if (!persistent && parentGameObjectsOnly != null &&
                !IsAllowedHierarchyObject(property, false,
                    parentGameObjectsOnly.includeSelf))
                message = "该字段只允许引用当前对象的父层级。";
            return message != null;
        }

        private bool TryGetUniqueListError(SerializedProperty property,
            out string message)
        {
            message = null;
            if (uniqueList == null || !property.isArray ||
                property.propertyType == SerializedPropertyType.String)
                return false;
            if (!(SerializedPropertyMemberUtility.GetSerializedValue(property)
                is IList values)) return false;
            for (int i = 0; i < values.Count; i++)
            {
                for (int j = i + 1; j < values.Count; j++)
                {
                    if (!Equals(values[i], values[j])) continue;
                    message = string.IsNullOrEmpty(uniqueList.message)
                        ? $"集合中第 {i + 1} 与第 {j + 1} 个元素重复。"
                        : uniqueList.message;
                    return true;
                }
            }
            return false;
        }

        private void ApplyObjectScope(SerializedProperty property)
        {
            if (property.propertyType != SerializedPropertyType.ObjectReference ||
                property.objectReferenceValue == null) return;
            if (childGameObjectsOnly != null &&
                !IsAllowedHierarchyObject(property, true,
                    childGameObjectsOnly.includeSelf))
                property.objectReferenceValue = null;
            else if (parentGameObjectsOnly != null &&
                !IsAllowedHierarchyObject(property, false,
                    parentGameObjectsOnly.includeSelf))
                property.objectReferenceValue = null;
        }

        private static bool IsAllowedHierarchyObject(SerializedProperty property,
            bool child, bool includeSelf)
        {
            Transform owner = GetTransform(property.serializedObject.targetObject);
            Transform selected = GetTransform(property.objectReferenceValue);
            if (owner == null || selected == null) return false;
            if (ReferenceEquals(owner, selected)) return includeSelf;
            return child ? selected.IsChildOf(owner) : owner.IsChildOf(selected);
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
            ValidateInputAttribute validator, GUIContent label, out string message)
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

        private string GetRequiredMessage(SerializedProperty property,
            GUIContent label)
        {
            return string.IsNullOrEmpty(required?.message)
                ? $"{label?.text ?? property.displayName}不能为空。"
                : required.message;
        }

        private bool ShouldDrawProgressBar(SerializedProperty property)
        {
            return progressBar != null &&
                (property.propertyType == SerializedPropertyType.Integer ||
                 property.propertyType == SerializedPropertyType.Float);
        }

        private bool ShouldDrawColorPalette(SerializedProperty property) =>
            colorPalette != null && paletteColors.Length > 0 &&
            property.propertyType == SerializedPropertyType.Color;

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

        private float GetTitleHeight(float width)
        {
            float height = EditorGUIUtility.singleLineHeight;
            if (!string.IsNullOrEmpty(title.subtitle))
                height += EditorStyles.wordWrappedMiniLabel.CalcHeight(
                    new GUIContent(title.subtitle), width) +
                    EditorGUIUtility.standardVerticalSpacing;
            return height;
        }

        private void DrawTitle(Rect position)
        {
            Rect titleRect = new Rect(position.x, position.y, position.width,
                EditorGUIUtility.singleLineHeight);
            EditorGUI.LabelField(titleRect, title.title, EditorStyles.boldLabel);
            if (string.IsNullOrEmpty(title.subtitle)) return;
            Rect subtitleRect = new Rect(position.x,
                titleRect.yMax + EditorGUIUtility.standardVerticalSpacing,
                position.width, position.yMax - titleRect.yMax);
            EditorGUI.LabelField(subtitleRect, title.subtitle,
                EditorStyles.wordWrappedMiniLabel);
        }

        private GUIContent GetLabel(GUIContent original)
        {
            Initialize();
            if (hideLabel) return GUIContent.none;
            if (nameLabel == null) return original;
            nameLabel.image = original?.image;
            if (string.IsNullOrEmpty(nameLabel.tooltip))
                nameLabel.tooltip = original?.tooltip;
            return nameLabel;
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
            return Mathf.Max(minimum, EditorStyles.helpBox.CalcHeight(
                new GUIContent(message), Mathf.Max(1, width)));
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
            var shows = new List<ShowIfAttribute>();
            var hides = new List<HideIfAttribute>();
            var enables = new List<EnableIfAttribute>();
            var disables = new List<DisableIfAttribute>();
            var helps = new List<HelpBoxAttribute>();
            var validations = new List<ValidateInputAttribute>();
            var callbacks = new List<OnValueChangedAttribute>();
            var buttons = new List<InlineButtonAttribute>();
            FieldInfo currentField = EffectiveFieldInfo;
            IEnumerable<ActionAttributeBase> attributes = currentField == null
                ? new[] { attribute as ActionAttributeBase }
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
                    case HideLabelAttribute _: hideLabel = true; break;
                    case DelayedInputAttribute _: delayed = true; break;
                    case HideInEditorModeAttribute _: hideInEditorMode = true; break;
                    case HideInPlayModeAttribute _: hideInPlayMode = true; break;
                    case DisableInEditorModeAttribute _:
                        disableInEditorMode = true; break;
                    case DisableInPlayModeAttribute _:
                        disableInPlayMode = true; break;
                    case ToggleLeftAttribute _: toggleLeft = true; break;
                    case AssetsOnlyAttribute _: assetsOnly = true; break;
                    case SceneObjectsOnlyAttribute _: sceneObjectsOnly = true; break;
                    case NonNegativeAttribute _: nonNegative = true; break;
                    case PositiveAttribute _: positive = true; break;
                    case ExpandableAttribute _: expandable = true; break;
                    case ShowIfAttribute value: shows.Add(value); break;
                    case HideIfAttribute value: hides.Add(value); break;
                    case EnableIfAttribute value: enables.Add(value); break;
                    case DisableIfAttribute value: disables.Add(value); break;
                    case HelpBoxAttribute value: helps.Add(value); break;
                    case ValidateInputAttribute value: validations.Add(value); break;
                    case OnValueChangedAttribute value: callbacks.Add(value); break;
                    case ClampAttribute value: clamp = value; break;
                    case MinValueAttribute value: minValue = value; break;
                    case MaxValueAttribute value: maxValue = value; break;
                    case MultilineTextAttribute value: multiline = value; break;
                    case ResizableTextAreaAttribute value:
                        resizableTextArea = value;
                        break;
                    case RequiredAttribute value: required = value; break;
                    case TitleAttribute value: title = value; break;
                    case SuffixLabelAttribute value: suffix = value; break;
                    case PrefixLabelAttribute value: prefix = value; break;
                    case PropertySpaceAttribute value: propertySpace = value; break;
                    case ProgressBarAttribute value: progressBar = value; break;
                    case EnumFlagsAttribute value: enumFlags = value; break;
                    case EnumSearchAttribute value: enumSearch = value; break;
                    case EnumToggleButtonsAttribute value:
                        enumToggleButtons = value; break;
                    case ValueDropdownAttribute value: valueDropdown = value; break;
                    case MinMaxSliderAttribute value: minMaxSlider = value; break;
                    case HorizontalLineAttribute value: horizontalLine = value; break;
                    case ShowAssetPreviewAttribute value: assetPreview = value; break;
                    case TagAttribute value: tag = value; break;
                    case LayerAttribute value: layer = value; break;
                    case SortingLayerAttribute value: sortingLayer = value; break;
                    case SceneNameAttribute value: sceneName = value; break;
                    case InputAxisAttribute value: inputAxis = value; break;
                    case AnimatorParamAttribute value: animatorParam = value; break;
                    case PropertyTooltipAttribute value:
                        propertyTooltip = value; break;
                    case LabelWidthAttribute value: labelWidth = value; break;
                    case IndentAttribute value: indent = value; break;
                    case GUIColorAttribute value: guiColor = value; break;
                    case FilePathAttribute value: filePath = value; break;
                    case FolderPathAttribute value: folderPath = value; break;
                    case RequiredListLengthAttribute value:
                        requiredListLength = value; break;
                    case UniqueListAttribute value: uniqueList = value; break;
                    case ChildGameObjectsOnlyAttribute value:
                        childGameObjectsOnly = value; break;
                    case ParentGameObjectsOnlyAttribute value:
                        parentGameObjectsOnly = value; break;
                    case WrapAttribute value: wrap = value; break;
                    case CurveRangeAttribute value: curveRange = value; break;
                    case ReorderableListAttribute value:
                        reorderableList = value; break;
                    case PasswordFieldAttribute value: passwordField = value; break;
                    case PlaceholderAttribute value: placeholder = value; break;
                    case MaxLengthAttribute value: maxLength = value; break;
                    case StepAttribute value: step = value; break;
                    case SliderAttribute value: slider = value; break;
                    case EulerAnglesAttribute value: eulerAngles = value; break;
                    case AssetPathAttribute value: assetPath = value; break;
                    case AssetGuidAttribute value: assetGuid = value; break;
                    case ColorPaletteAttribute value: colorPalette = value; break;
                    case InlineButtonAttribute value: buttons.Add(value); break;
                }
            }

            if (currentField != null)
                collectionField = typeof(IList).IsAssignableFrom(
                    currentField.FieldType);
            showConditions = shows.ToArray();
            hideConditions = hides.ToArray();
            enableConditions = enables.ToArray();
            disableConditions = disables.ToArray();
            helpBoxes = helps.ToArray();
            validators = validations.ToArray();
            valueChangedCallbacks = callbacks.ToArray();
            inlineButtons = buttons.ToArray();
            paletteColors = ParsePaletteColors(colorPalette);
            if (propertyTooltip != null)
            {
                if (nameLabel == null)
                    nameLabel = new GUIContent(currentField == null
                        ? string.Empty
                        : ObjectNames.NicifyVariableName(currentField.Name),
                        propertyTooltip.tooltip);
                else nameLabel.tooltip = propertyTooltip.tooltip;
            }
            initialized = true;
        }

        private static Color[] ParsePaletteColors(ColorPaletteAttribute palette)
        {
            if (palette?.colors == null || palette.colors.Length == 0)
                return Array.Empty<Color>();
            var colors = new List<Color>(palette.colors.Length);
            for (int i = 0; i < palette.colors.Length; i++)
            {
                string value = palette.colors[i];
                if (string.IsNullOrWhiteSpace(value)) continue;
                if (value[0] != '#') value = "#" + value;
                if (ColorUtility.TryParseHtmlString(value, out Color color))
                    colors.Add(color);
            }
            return colors.ToArray();
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

        private sealed class CombinedActionPropertyDrawer : ActionPropertyDrawer
        {
        }
    }
}
