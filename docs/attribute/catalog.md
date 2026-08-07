# 特性分类手册

当前 Runtime 目录包含 104 个 `*Attribute` 类型。下表按主要用途归类；同一特性可能同时影响布局和交互。

## 命名与说明

| 特性 | 用途 |
| --- | --- |
| `Name` / `Label` / `LabelText` | 替换字段或枚举项名称，可附 tooltip |
| `PropertyTooltip` | 只追加 tooltip，不改变名称 |
| `PrefixLabel` / `SuffixLabel` | 在控件前后绘制短标签或单位 |
| `HideLabel` | 隐藏 Unity 默认前缀标签 |
| `LabelWidth` | 调整当前字段标签宽度 |
| `Title` | 字段前绘制标题与可选副标题 |
| `TypeInfoBox` | 在类型 Inspector 顶部展示说明 |
| `InfoBox` / `HelpBox` | 绘制字段相关帮助消息 |
| `Icon` | 类型/节点图标；支持名称、路径、类型推导和 Base64 |

## 条件与编辑状态

| 特性 | 用途 |
| --- | --- |
| `ShowIf` / `HideIf` | 根据成员值显示或隐藏字段 |
| `EnableIf` / `DisableIf` | 根据成员值启用或禁用字段 |
| `ShowIfGroup` / `HideIfGroup` | 条件控制整个逻辑分组 |
| `ReadOnly` | 始终只读 |
| `DisableInPlayMode` / `DisableInEditorMode` | 按运行状态禁用 |
| `HideInPlayMode` / `HideInEditorMode` | 按运行状态隐藏 |

条件构造支持布尔成员、`(condition, expected)` 精确比较，以及 `ConditionOperator` 组合多个条件。枚举可传枚举常量作为 expected；字符串不要替代强类型枚举值。

```csharp
[ShowIf(nameof(mode), Mode.Advanced)]
public int advancedValue;

[EnableIf(ConditionOperator.And, nameof(enabled), nameof(hasPermission))]
public string command;
```

## 分组与布局

| 特性 | 用途 |
| --- | --- |
| `Group` / `VerticalGroup` | 垂直逻辑分组 |
| `BoxGroup` | 带边框和可选标题的分组 |
| `Foldout` / `FoldoutGroup` | 可折叠区域 |
| `HorizontalGroup` | 横向排列，支持宽度和比例 |
| `TabGroup` | 同组字段分到标签页 |
| `TitleGroup` | 带标题/副标题的区域 |
| `ToggleGroup` | 由布尔成员控制的区域 |
| `ButtonGroup` / `ResponsiveButtonGroup` | 普通/响应式按钮排列 |
| `Indent` | 增加缩进 |
| `PropertySpace` | 字段前后留白 |
| `HorizontalLine` | 分隔线 |
| `PropertyOrder` | 调整绘制顺序 |
| `AllowNesting` | 允许在嵌套序列化对象中继续应用组合绘制 |

Group 名称是逻辑路径。相同名称、不同不兼容分组特性会产生不可预测布局，应在一个 Inspector 内保持唯一含义。

## 数值与颜色

| 特性 | 用途 |
| --- | --- |
| `Clamp` | 将值限制在闭区间 |
| `MinValue` / `MaxValue` | 单侧限制 |
| `NonNegative` / `Positive` | 0 以上或严格大于 0 |
| `Slider` | 单值滑条 |
| `MinMaxSlider` | Vector2 范围滑条 |
| `Step` | 将值吸附到固定步长 |
| `Wrap` | 超出区间时循环回绕 |
| `ProgressBar` | 用进度条显示/编辑数字 |
| `DelayedInput` | Enter 或失焦后提交数字/文本 |
| `ColorPalette` | 从固定颜色列表选择 |
| `GUIColor` | 临时改变字段 GUI 颜色 |
| `CurveRange` | 指定 AnimationCurve 编辑范围 |
| `EulerAngles` | 用欧拉角编辑 Quaternion |

Clamp/Step/Wrap 可能同时改变值。组合时推荐顺序语义为输入控件 -> Step -> Clamp；避免同时使用 Wrap 和 Clamp 表达冲突规则。

## 文本、枚举与选择器

| 特性 | 用途 |
| --- | --- |
| `MultilineText` | 固定行数多行文本 |
| `ResizableTextArea` | 在最小/最大行数间自适应文本区 |
| `PasswordField` | 掩码输入 |
| `Placeholder` | 空文本占位提示 |
| `MaxLength` | 限制字符串长度 |
| `Dropdown` / `ValueDropdown` | 从成员提供的数据源选择 |
| `Searchable` | 大量候选项使用搜索弹窗 |
| `EnumSearch` | 枚举搜索，支持最小项数阈值 |
| `EnumFlags` | Flags 枚举掩码 |
| `EnumToggleButtons` | 枚举按钮组 |
| `Tag` / `Layer` / `SortingLayer` | Unity 工程配置选择 |
| `InputAxis` / `AnimatorParam` | Input/Animator 参数选择 |
| `Scene` / `SceneName` | Build Settings 场景选择 |

`ValueDropdown(valuesMember)` 支持返回普通集合、键值项或 `ValueDropdownList<T>`。数据源可以是字段、属性或无参方法；返回顺序就是展示顺序。

## 路径、资产和对象限制

| 特性 | 用途 |
| --- | --- |
| `FilePath` | 文件选择，可限制扩展名和绝对/工程相对路径 |
| `FolderPath` | 目录选择，可返回绝对或工程相对路径 |
| `AssetPath` / `AssetGuid` | 用路径或 GUID 引用指定资产类型 |
| `AssetsOnly` / `SceneObjectsOnly` | 限制 Object 来源 |
| `ChildGameObjectsOnly` / `ParentGameObjectsOnly` | 限制层级范围，可包含自身 |
| `ShowAssetPreview` / `Preview` / `PreviewField` | 在字段下展示资源预览 |
| `Expandable` / `InlineEditor` | 内联绘制引用对象 Inspector |

FolderPath 选择后会规范化分隔符；返回工程相对路径时目标必须位于当前项目下。路径控件和预览会增加额外高度，组合 Drawer 会把它们放在主字段下方，不应手工使用负间距修补。

## 集合

| 特性 | 用途 |
| --- | --- |
| `ReorderableList` | 可拖动、有增删按钮的列表 |
| `ListViewSettings` | 配置列表拖动、增删和分页/视图行为 |
| `RequiredListLength` | 校验最小/最大元素数 |
| `UniqueList` | 校验重复元素 |

大型列表应折叠复杂元素或分页。元素自身的 ActionAttribute 会递归参与高度计算，资源预览和 InlineEditor 尤其容易让单行变高。

## 校验、按钮和生命周期

| 特性 | 用途 |
| --- | --- |
| `Required` | null、空字符串或缺失引用提示 |
| `ValidateInput` | 调用业务方法校验值并展示消息 |
| `OnValueChanged` | 值应用后调用回调 |
| `InlineButton` | 在字段同一行末尾放方法按钮 |
| `Button` | 将无参/兼容方法绘制成按钮 |
| `OnInspectorInit` / `OnInspectorDispose` | Inspector 生命周期回调 |
| `OnInspectorGUI` | 在类型 Inspector 插入业务 GUI 回调 |
| `ShowInInspector` | 展示 Unity 默认不会画出的成员 |
| `ShowNativeProperty` | 只读展示原生 C# 属性 |
| `ShowNonSerializedField` | 展示非序列化字段 |
| `HideMonoScript` | 隐藏顶部 Script 定位行 |
| `ToggleLeft` | bool 使用左侧复选框样式 |

回调异常会影响 Inspector 绘制，应在业务方法内处理可预期错误。`OnInspectorGUI` 适合少量无法声明式表达的内容，不应重新实现整个 Inspector。

## 框架元数据

`Attachable` 声明 Timeline/Graph 类型可以挂到哪些父对象；它不是普通字段绘制器，但和其他 Runtime Attribute 一起发布。节点系统还使用 `Name`、`Icon`、`Node` 等元数据生成右键菜单和节点标题。
