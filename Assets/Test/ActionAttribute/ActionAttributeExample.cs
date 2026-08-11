using System;
using System.Collections.Generic;
using ActionAttribute;
using UnityEngine;

[TypeInfoBox("该组件集中展示 ActionAttribute Inspector Attribute 的组合用法。")]
public sealed class ActionAttributeExample : MonoBehaviour
{
    private static readonly ValueDropdownItem<string>[] SearchCommands =
    {
        new ValueDropdownItem<string>("创建角色", "create_actor"),
        new ValueDropdownItem<string>("删除角色", "delete_actor"),
        new ValueDropdownItem<string>("重载配置", "reload_config")
    };

    public List<int> A;

    [Name("B")]
    public List<int> B;


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

    [Name("显示高级配置", "控制高级配置字段是否显示。")]
    [OnValueChanged(nameof(OnAdvancedChanged))]
    [SerializeField] private bool showAdvanced = true;

    [ValueDropdown]
    [Name("工作模式", "使用可搜索下拉框选择模式，枚举成员通过 Name 提供中文显示名。")]
    [SerializeField] private ExampleMode mode;

    [Condition(ConditionMode.Show, nameof(mode), ExampleMode.Advanced)]
    [Name("高级模式参数", "仅当工作模式等于高级模式时显示，演示枚举值条件判断。")]
    [SerializeField] private int advancedModeValue;

    [Condition(ConditionMode.Show, nameof(showAdvanced))]
    [Name("高级数值", "仅在开启高级配置时显示。")]
    [SerializeField] private int advancedValue = 10;

    [Condition(ConditionMode.Hide, nameof(showAdvanced))]
    [Name("简化提示", "仅在关闭高级配置时显示。")]
    [SerializeField] private string simpleHint = "当前使用简化配置";

    [Name("锁定配置", "开启后禁止修改受控字段。")]
    [SerializeField] private bool lockValues;

    [Condition(ConditionMode.Enable, nameof(showAdvanced))]
    [Condition(ConditionMode.Disable, nameof(lockValues))]
    [Condition(ConditionMode.Disable, InspectorMode.PlayMode)]
    [Name("条件编辑值", "显示高级配置且未锁定时可以编辑。")]
    [SerializeField] private int conditionalValue;

    [Slider(0, 100, SliderMode.Clamp), SuffixLabel("点")]
    [Name("生命值", "修改后自动限制在零到一百之间。")]
    [SerializeField] private int health = 75;

    [Slider(1, 20, SliderMode.Clamp)]
    [Name("队伍人数", "修改后限制在一到二十之间。")]
    [SerializeField] private int teamSize = 4;

    [Slider(SliderMode.NonNegative), SuffixLabel("次")]
    [Name("重试次数", "组合前缀、后缀与非负数约束。")]
    [SerializeField] private int retryCount = 3;

    [Slider(SliderMode.Positive)]
    [Name("批次数量", "只允许输入大于零的数值。")]
    [SerializeField] private int batchSize = 1;
    public int GG0;

    [ProgressBar(0, 100, "任务进度")]
    [Name("进度", "在数值字段下方显示进度条。")]
    [SerializeField] private int progress = 35;
    public int GG;
    [OnValueChanged(nameof(IsPositiveEven), ValueChangedMode.Validate,
        "数值必须是正偶数。")]
    [Name("正偶数", "通过指定方法动态校验输入值。")]
    [SerializeField] private int positiveEven = 2;

    [Delayed]
    [Name("延迟提交", "按回车或失去焦点后才提交修改。")]
    [SerializeField] private string delayedText;

    [Multiline(4)]
    [Name("固定文本框", "始终显示四行文本编辑区域。")]
    [SerializeField] private string fixedTextArea;

    [Text(TextFieldMode.Password, Placeholder = "请输入访问口令")]
    [Name("访问口令", "以掩码形式编辑字符串，序列化数据仍保存原始文本。")]
    [SerializeField] private string accessToken;

    [Text(Placeholder = "请输入简短标识", MaxLength = 12, Trim = true,
        Case = TextCase.UpperInvariant, CollapseWhitespace = true,
        Pattern = "^[A-Z0-9_]*$",
        PatternMessage = "短标识只能包含大写字母、数字和下划线。")]
    [Name("短标识", "空值时显示占位提示，输入内容最多保留十二个字符。")]
    [SerializeField] private string shortIdentifier;

    [Required]
    [Name("配置版本", "必须使用非零版本号。")]
    [SerializeField] private int configVersion = 1;

    [Name("最小伤害")]
    [SerializeField] private int minimumDamage = 1;

    [CompareTo(nameof(minimumDamage), ValueRelation.GreaterThanOrEqual)]
    [Name("最大伤害", "必须大于或等于最小伤害。")]
    [SerializeField] private int maximumDamage = 10;

    [Finite]
    [Name("模拟倍率", "拒绝 NaN 和正负无穷。")]
    [SerializeField] private float simulationScale = 1;

    [TextArea(2, 4), Text(MinLines = 1, MaxLines = 4)]
    [Name("版本说明", "使用 Unity TextArea 编辑，并校验一到四行文本。")]
    [SerializeField] private string releaseNotes = "初始版本";

    [Slider(5, SliderMode.Step)]
    [Name("网格数值", "修改后自动吸附到最接近的五的倍数。")]
    [SerializeField] private int gridValue = 10;

    [Slider(0, 10)]
    [Name("滑杆数值", "使用滑杆编辑并限制在指定的数值范围内。")]
    [SerializeField] private float sliderValue = 4.5f;

    [EulerAngles]
    [Name("欧拉旋转", "以三个欧拉角分量编辑底层 Quaternion 字段。")]
    [SerializeField] private Quaternion eulerRotation = Quaternion.identity;

    [Path(typeof(Texture2D))]
    [Name("纹理路径", "通过项目资源选择器保存纹理的 Assets 相对路径。")]
    [SerializeField] private string texturePath;

    [HelpBox("该引用用于演示必填校验。", InspectorMessageType.Info)]
    [Required("必须指定目标对象。")]
    [Name("目标对象", "不能为空的 Unity 对象引用。")]
    [SerializeField] private Transform requiredTarget;

    [Name("功能选项", "可以同时选择多个枚举标记。")]
    [SerializeField] private ExampleOptions options;

    [ReadOnly]
    [Name("最近回调值", "由 OnValueChanged 回调更新，Inspector 中只读。")]
    [SerializeField] private string lastCallbackValue;

    [Group("角色参数", GroupType.Box)]
    [Name("移动速度", "GroupType.Box 会把同名字段绘制在同一块区域。")]
    [SerializeField] private int moveSpeed = 5;

    [Group("角色参数", GroupType.Box)]
    [Name("攻击力", "字段仍然可以与数值约束、条件显示等其他 Attribute 组合。")]
    [SerializeField] private int attack = 10;

    [Group("高级折叠组", GroupType.Foldout)]
    [Name("折叠值 A", "GroupType.Foldout 的展开状态在当前编辑器会话内缓存。")]
    [SerializeField] private int foldoutA;

    [Group("高级折叠组", GroupType.Foldout)]
    [Name("折叠值 B", "同名折叠组中的字段会一起展开或收起。")]
    [SerializeField] private int foldoutB;

    [Group("横向布局", GroupType.Horizontal)]
    [Name("左侧", "GroupType.Horizontal 把同组字段放在同一行。")]
    [SerializeField] private int horizontalLeft;

    [Group("横向布局", GroupType.Horizontal)]
    [Name("右侧", "可以用 Group.Width 为每个横向字段指定固定宽度。")]
    [SerializeField] private int horizontalRight;

    [Group("分类设置", GroupType.Tab, Tab = "基础")]
    [Name("基础选项", "GroupType.Tab 只显示当前标签页中的字段。")]
    [SerializeField] private string basicOption;

    [Group("分类设置", GroupType.Tab, Tab = "高级")]
    [Name("高级选项", "切换标签不会修改未显示字段的数据。")]
    [SerializeField] private string expertOption;

    [Group("可选功能", GroupType.Toggle,
        ToggleMember = nameof(enableOptional))]
    [Name("启用可选功能", "该布尔字段同时作为 GroupType.Toggle 的开关。")]
    [SerializeField] private bool enableOptional;

    [Group("可选功能", GroupType.Toggle,
        ToggleMember = nameof(enableOptional))]
    [Name("可选参数", "关闭组开关后字段仍然可见，但不能编辑。")]
    [SerializeField] private int optionalValue;

    [Group("带框折叠", GroupType.FoldoutBox)]
    [Name("折叠面板 A", "FoldoutBox 同时提供边框和可缓存的折叠状态。")]
    [SerializeField] private int foldoutBoxA;

    [Group("带框折叠", GroupType.FoldoutBox)]
    [Name("折叠面板 B", "同名 FoldoutBox 字段在一个面板内显示。")]
    [SerializeField] private int foldoutBoxB;

    [Group("网格布局", GroupType.Grid, Columns = 2)]
    [Name("网格 A", "Grid 按 Columns 指定的列数排列字段。")]
    [SerializeField] private int gridA;

    [Group("网格布局", GroupType.Grid, Columns = 2)]
    [Name("网格 B", "不足一整行时仍保持前面各列宽度稳定。")]
    [SerializeField] private int gridB;

    [Group("缩进布局", GroupType.Indent)]
    [Name("缩进内容", "Indent 在可选标题下统一缩进组内字段。")]
    [SerializeField] private string indentedValue;

    [Group("滚动区域", GroupType.Scroll, Height = 72)]
    [Name("滚动内容 A", "Scroll 使用固定高度区域容纳较多字段。")]
    [SerializeField] private string scrollValueA;

    [Group("滚动区域", GroupType.Scroll, Height = 72)]
    [Name("滚动内容 B", "滚动位置由当前 Inspector Renderer 缓存。")]
    [SerializeField] private string scrollValueB;

    [ValueDropdown(nameof(DifficultyValues))]
    [Name("难度", "选项由字段、属性或无参数方法返回的 IEnumerable 提供，并支持搜索。")]
    [SerializeField] private string difficulty = "normal";

    [ValueDropdown(ValueDropdownSource.Search, nameof(SearchCommandValues))]
    [Name("搜索命令", "搜索词变化时调用自定义搜索源，并保存选中项对应的值。")]
    [SerializeField] private string searchCommand;

    [Collection(1, 8, CollectionItemRule.Unique |
        CollectionItemRule.NotNull | CollectionItemRule.NotBlank)]
    [Name("检查点", "列表支持数量范围校验。")]
    [SerializeField] private List<string> checkpoints = new List<string>();

    [ObjectsOnly(ObjectSource.Children, true)]
    [Name("子层级目标", "只接受当前对象自身或子层级中的对象。")]
    [SerializeField] private Transform childTarget;

    [ObjectsOnly(ObjectSource.Parents, true)]
    [Name("父层级目标", "只接受当前对象自身或父层级中的对象。")]
    [SerializeField] private Transform parentTarget;

    [MinMaxSlider(0, 100)]
    [Name("随机范围", "Vector2 的 X/Y 分别保存最小值和最大值。")]
    [SerializeField] private Vector2 integerRange = new Vector2(20, 80);

    [Name("响应曲线", "使用 Unity 原生 AnimationCurve 编辑器修改曲线。")]
    [SerializeField]
    private AnimationCurve responseCurve =
        AnimationCurve.Linear(0, 0, 10, 1);

    [ShowAssetPreview(128, 72), ObjectsOnly(ObjectSource.Assets)]
    [Name("资源预览", "只允许 Project 资源，并在字段下方显示缩略图。")]
    [SerializeField] private Texture2D previewTexture;

    [ValueDropdown(ValueDropdownSource.Tag)]
    [Name("标签", "从项目 Tag 列表中搜索并选择。")]
    [SerializeField] private string targetTag = "Untagged";

    [ValueDropdown(ValueDropdownSource.Layer)]
    [Name("层级", "从项目 Layer 列表中搜索并选择。")]
    [SerializeField] private int targetLayer;

    [ValueDropdown(ValueDropdownSource.SortingLayer)]
    [Name("排序层", "从项目 Sorting Layer 列表中搜索并选择。")]
    [SerializeField] private string targetSortingLayer = "Default";

    [ValueDropdown(ValueDropdownSource.Scene)]
    [Name("场景", "从 Build Settings 场景列表中搜索并保存场景名。")]
    [SerializeField] private string sceneName;

    [ValueDropdown(ValueDropdownSource.InputAxis)]
    [Name("输入轴", "从 Input Manager 轴名称中搜索并选择。")]
    [SerializeField] private string inputAxis;

    [Path(PathType.Folder)]
    [Name("输出目录", "可通过文件夹按钮选择路径，默认优先保存为项目相对路径。")]
    [SerializeField] private string outputFolder = "Assets";

    [Name("参数动画器", "为动画参数选择器提供 Animator 参数列表。")]
    [SerializeField] private Animator parameterAnimator;

    [ValueDropdown(ValueDropdownSource.AnimatorParameter,
        nameof(parameterAnimator))]
    [Name("动画参数", "可搜索 Animator Controller 中定义的参数。")]
    [SerializeField] private string animatorParameter;

    [InlineButton(nameof(ClearMessage), "清空")]
    [Name("行内操作", "InlineButton 在字段右侧调用无参数方法。")]
    [SerializeField] private string inlineMessage;

    [ShowInInspector, Name("运行时摘要", "未序列化属性通过 ShowInInspector 以只读方式展示。")]
    private string RuntimeSummary => mode + " / " + difficulty;

    private bool IsPositiveEven(int value) => value > 0 && (value & 1) == 0;

    private IEnumerable<ValueDropdownItem<string>> SearchCommandValues(
        string query)
    {
        for (int i = 0; i < SearchCommands.Length; i++)
        {
            ValueDropdownItem<string> item = SearchCommands[i];
            if (string.IsNullOrEmpty(query) ||
                item.text.IndexOf(query,
                    StringComparison.OrdinalIgnoreCase) >= 0)
                yield return item;
        }
    }

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

    [Button("重置演示数据", InspectorMode.EditMode)]
    private void ResetExample()
    {
        health = 75;
        progress = 35;
        difficulty = "normal";
    }
}
