using System;
using System.Collections.Generic;
using ActionAttribute;
using UnityEngine;

[TypeInfoBox("该组件集中展示 ActionAttribute Inspector Attribute 的组合用法。")]
public sealed class ActionAttributeExample : MonoBehaviour
{
    private enum ExampleMode
    {
        [Name("基础模式", "使用基础配置。")] Basic,
        [Name("高级模式", "显示并使用高级配置。")] Advanced,
        [Name("调试模式", "启用调试用途的配置。")] Debug
    }

    [Flags]
    private enum ExampleOptions
    {
        None = 0,
        Movement = 1 << 0,
        Combat = 1 << 1,
        Interaction = 1 << 2
    }

    [Title("条件绘制", "条件可以引用同级字段、属性或无参数方法。")]
    [Name("显示高级配置", "控制高级配置字段是否显示。")]
    [OnValueChanged(nameof(OnAdvancedChanged))]
    [SerializeField] private bool showAdvanced = true;

    [EnumSearch]
    [Name("工作模式", "使用可搜索下拉框选择模式，枚举成员通过 Name 提供中文显示名。")]
    [SerializeField] private ExampleMode mode;

    [ShowIf(nameof(mode), ExampleMode.Advanced)]
    [Name("高级模式参数", "仅当工作模式等于高级模式时显示，演示枚举值条件判断。")]
    [SerializeField] private int advancedModeValue;

    [ShowIf(nameof(showAdvanced))]
    [Name("高级数值", "仅在开启高级配置时显示。")]
    [SerializeField] private int advancedValue = 10;

    [HideIf(nameof(showAdvanced))]
    [Name("简化提示", "仅在关闭高级配置时显示。")]
    [SerializeField] private string simpleHint = "当前使用简化配置";

    [Name("锁定配置", "开启后禁止修改受控字段。")]
    [SerializeField] private bool lockValues;

    [EnableIf(nameof(showAdvanced)), DisableIf(nameof(lockValues))]
    [Name("条件编辑值", "显示高级配置且未锁定时可以编辑。")]
    [SerializeField] private int conditionalValue;

    [Title("数值约束")]
    [Clamp(0, 100), SuffixLabel("点")]
    [Name("生命值", "修改后自动限制在零到一百之间。")]
    [SerializeField] private int health = 75;

    [MinValue(1), MaxValue(20)]
    [Name("队伍人数", "修改后限制在一到二十之间。")]
    [SerializeField] private int teamSize = 4;
    public int GG0;

    [ProgressBar(0, 100, "任务进度")]
    [Name("进度", "在数值字段下方显示进度条。")]
    [SerializeField] private int progress = 35;
    public int GG;
    [ValidateInput(nameof(IsPositiveEven), "数值必须是正偶数。")]
    [Name("正偶数", "通过指定方法动态校验输入值。")]
    [SerializeField] private int positiveEven = 2;

    [DelayedInput]
    [Name("延迟提交", "按回车或失去焦点后才提交修改。")]
    [SerializeField] private string delayedText;

    [Title("文本与引用")]
    [MultilineText(4)]
    [Name("固定文本框", "始终显示四行文本编辑区域。")]
    [SerializeField] private string fixedTextArea;

    [ResizableTextArea(3, 10)]
    [Name("自适应文本框", "文本框高度会根据内容自动变化。")]
    [SerializeField] private string resizableTextArea;

    [PasswordField]
    [Name("访问口令", "以掩码形式编辑字符串，序列化数据仍保存原始文本。")]
    [SerializeField] private string accessToken;

    [Placeholder("请输入简短标识")]
    [MaxLength(12)]
    [Name("短标识", "空值时显示占位提示，输入内容最多保留十二个字符。")]
    [SerializeField] private string shortIdentifier;

    [Step(5)]
    [Name("网格数值", "修改后自动吸附到最接近的五的倍数。")]
    [SerializeField] private int gridValue = 10;

    [Slider(0, 10)]
    [Name("滑杆数值", "使用滑杆编辑并限制在指定的数值范围内。")]
    [SerializeField] private float sliderValue = 4.5f;

    [EulerAngles]
    [Name("欧拉旋转", "以三个欧拉角分量编辑底层 Quaternion 字段。")]
    [SerializeField] private Quaternion eulerRotation = Quaternion.identity;

    [ColorPalette("#E85D5D", "#F2C14E", "#55B88A", "#4B8FD8", "#A779D8")]
    [Name("颜色板", "保留完整颜色编辑器，并可从下方预设颜色中快速选择。")]
    [SerializeField] private Color paletteColor = Color.white;

    [AssetPath(typeof(Texture2D))]
    [Name("纹理路径", "通过项目资源选择器保存纹理的 Assets 相对路径。")]
    [SerializeField] private string texturePath;

    [AssetGuid(typeof(Texture2D))]
    [Name("纹理 GUID", "通过项目资源选择器保存纹理的稳定 GUID，移动资源后仍可解析。")]
    [SerializeField] private string textureGuid;

    [HelpBox("该引用用于演示必填校验。", InspectorMessageType.Info)]
    [Required("必须指定目标对象。")]
    [Name("目标对象", "不能为空的 Unity 对象引用。")]
    [SerializeField] private Transform requiredTarget;

    [PropertySpace(12, 4), EnumFlags]
    [Name("功能选项", "可以同时选择多个枚举标记。")]
    [SerializeField] private ExampleOptions options;

    [HideLabel]
    [SerializeField] private string labelFreeText = "这是一个隐藏标签的字段";

    [ReadOnly]
    [Name("最近回调值", "由 OnValueChanged 回调更新，Inspector 中只读。")]
    [SerializeField] private string lastCallbackValue;

    [BoxGroup("角色参数")]
    [Name("移动速度", "BoxGroup 会把使用相同组名的字段绘制在同一块区域。")]
    [SerializeField] private int moveSpeed = 5;

    [BoxGroup("角色参数")]
    [Name("攻击力", "字段仍然可以与数值约束、条件显示等其他 Attribute 组合。")]
    [SerializeField] private int attack = 10;

    [FoldoutGroup("高级折叠组")]
    [Name("折叠值 A", "FoldoutGroup 的展开状态在当前编辑器会话内缓存。")]
    [SerializeField] private int foldoutA;

    [FoldoutGroup("高级折叠组")]
    [Name("折叠值 B", "同名折叠组中的字段会一起展开或收起。")]
    [SerializeField] private int foldoutB;

    [HorizontalGroup("横向布局")]
    [Name("左侧", "HorizontalGroup 把同组字段放在同一行。")]
    [SerializeField] private int horizontalLeft;

    [HorizontalGroup("横向布局")]
    [Name("右侧", "可以为每个 HorizontalGroup 字段指定固定宽度。")]
    [SerializeField] private int horizontalRight;

    [TabGroup("分类设置", "基础")]
    [Name("基础选项", "TabGroup 只显示当前标签页中的字段。")]
    [SerializeField] private string basicOption;

    [TabGroup("分类设置", "高级")]
    [Name("高级选项", "切换标签不会修改未显示字段的数据。")]
    [SerializeField] private string expertOption;

    [ToggleGroup(nameof(enableOptional), "可选功能")]
    [Name("启用可选功能", "该布尔字段同时作为 ToggleGroup 的开关。")]
    [SerializeField] private bool enableOptional;

    [ToggleGroup(nameof(enableOptional), "可选功能")]
    [Name("可选参数", "关闭组开关后字段仍然可见，但不能编辑。")]
    [SerializeField] private int optionalValue;

    [ValueDropdown(nameof(DifficultyValues))]
    [Name("难度", "选项由字段、属性或无参数方法返回的 IEnumerable 提供，并支持搜索。")]
    [SerializeField] private string difficulty = "normal";

    [ReorderableList]
    [RequiredListLength(1, 8)]
    [Name("检查点", "列表支持拖拽排序和元素数量校验。")]
    [SerializeField] private List<string> checkpoints = new List<string>();

    [MinMaxSlider(0, 100)]
    [Name("随机范围", "Vector2 的 X/Y 分别保存最小值和最大值。")]
    [SerializeField] private Vector2 integerRange = new Vector2(20, 80);

    [CurveRange(0, 0, 10, 1)]
    [Name("响应曲线", "曲线编辑器限制在指定的横纵坐标范围内。")]
    [SerializeField] private AnimationCurve responseCurve =
        AnimationCurve.Linear(0, 0, 10, 1);

    [ShowAssetPreview(128, 72), AssetsOnly]
    [Name("资源预览", "只允许 Project 资源，并在字段下方显示缩略图。")]
    [SerializeField] private Texture2D previewTexture;

    [Tag]
    [Name("标签", "使用项目 Tag 列表绘制下拉选择。")]
    [SerializeField] private string targetTag = "Untagged";

    [Layer]
    [Name("层级", "使用项目 Layer 列表绘制下拉选择。")]
    [SerializeField] private int targetLayer;

    [Scene]
    [Name("场景", "从 Build Settings 场景列表中选择并保存场景名。")]
    [SerializeField] private string sceneName;

    [FolderPath]
    [Name("输出目录", "可通过文件夹按钮选择路径，默认优先保存为项目相对路径。")]
    [SerializeField] private string outputFolder = "Assets";

    [InlineButton(nameof(ClearMessage), "清空")]
    [Name("行内操作", "InlineButton 在字段右侧调用无参数方法。")]
    [SerializeField] private string inlineMessage;

    [ShowNativeProperty, Name("运行时摘要", "未序列化属性通过 ShowNativeProperty 以只读方式展示。")]
    private string RuntimeSummary => mode + " / " + difficulty;

    private bool IsPositiveEven(int value) => value > 0 && (value & 1) == 0;

    private void OnAdvancedChanged(bool value)
    {
        lastCallbackValue = value ? "已开启高级配置" : "已关闭高级配置";
    }

    private IEnumerable<ValueDropdownItem<string>> DifficultyValues()
    {
        yield return new ValueDropdownItem<string>("简单", "easy");
        yield return new ValueDropdownItem<string>("普通", "normal");
        yield return new ValueDropdownItem<string>("困难", "hard");
    }

    private void ClearMessage() => inlineMessage = string.Empty;

    [Button("重置演示数据")]
    [PropertyOrder(100)]
    private void ResetExample()
    {
        health = 75;
        progress = 35;
        difficulty = "normal";
    }
}
