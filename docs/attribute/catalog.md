# 特性分类手册

Runtime 仅保留具有独立行为的特性。下表按主要用途归类；同一特性可能同时影响布局和交互。

## 命名与说明

| 特性 | 用途 |
| --- | --- |
| `Name` | 替换字段或枚举项名称，可附 tooltip |
| `SuffixLabel` | 在控件后绘制短标签或单位 |
| `TypeInfoBox` | 在 Inspector 顶部展示类型说明 |
| `HelpBox` | 在字段附近展示说明 |
| `Icon` | 类型/节点图标；支持名称、路径、类型推导和 Base64 |

## 条件与编辑状态

| 特性 | 用途 |
| --- | --- |
| `Condition` | 通过 `ConditionMode` 根据成员值显示、隐藏、启用或禁用字段 |
| `ReadOnly` | 始终只读 |
| `InspectorMode` | 供条件特性和按钮统一指定编辑/运行模式 |

条件构造支持布尔成员、`(condition, expected)` 精确比较、`ConditionOperator` 组合多个条件，以及直接传入 `InspectorMode`。枚举可传枚举常量作为 expected；字符串不要替代强类型枚举值。

```csharp
[Condition(ConditionMode.Show, nameof(mode), Mode.Advanced)]
public int advancedValue;

[Condition(ConditionMode.Enable, ConditionOperator.And,
    nameof(enabled), nameof(hasPermission))]
public string command;
```

## 分组与布局

| 特性 | 用途 |
| --- | --- |
| `Group` | 通过 `GroupType` 选择纵向、横向、Box、折叠、带框折叠、标签页、标题、开关、按钮、网格、缩进或滚动布局 |
| `HorizontalLine` | 分隔线 |

相同名称和 `GroupType` 的成员属于同一组。Tab 使用 `Tab`，Toggle 使用 `ToggleMember`，横向布局使用 `Width`，Grid 使用 `Columns`，Scroll 使用 `Height`。条件控制直接在组内字段上组合 `Condition`。

## 数值与颜色

| 特性 | 用途 |
| --- | --- |
| `Slider` | 通过 `SliderMode` 选择滑杆、闭区间限制、环绕、单侧限制、非负或严格正数 |
| `MinMaxSlider` | Vector2 范围滑条 |
| `ProgressBar` | 用进度条显示/编辑数字 |
| `EulerAngles` | 用欧拉角编辑 Quaternion |
| `Finite` | 拒绝 float/double 的 NaN 和正负无穷 |

范围滑杆使用 `[Slider(0, 10)]`；单边限制使用
`[Slider(SliderMode.NonNegative)]` 或
`[Slider(2, SliderMode.Minimum)]`；步长吸附使用
`[Slider(5, SliderMode.Step)]`。其他模式可通过 `Step`、`Origin`
命名参数组合步长，处理时先吸附，再应用边界规则。

## 文本、枚举与选择器

| 特性 | 用途 |
| --- | --- |
| `Text` | 统一字符串绘制、规范化、长度限制、正则和行数校验 |
| `ValueDropdown` | 通过 `ValueDropdownSource` 选择枚举、成员、动态搜索或 Unity 项目数据 |
| `EnumToggleButtons` | 枚举按钮组 |

```csharp
[Text(Placeholder = "请输入协议标识", MaxLength = 24, Trim = true,
    CollapseWhitespace = true, Case = TextCase.UpperInvariant,
    Pattern = "^[A-Z0-9_]+$", AllowEmpty = false)]
public string protocolKey;

[Text(TextFieldMode.Password, Placeholder = "请输入访问口令")]
public string accessToken;

[UnityEngine.TextArea(3, 8), Text(MinLines = 1, MaxLines = 8)]
public string releaseNotes;
```

`Text` 的处理顺序固定为 Trim、折叠空白、Invariant 大小写转换、最大长度截断；截断在外部值修正之后执行，因此最终写回值一定满足 `MaxLength`。只有用户提交变化时才写回；Layout 和 Repaint 阶段只执行正则与行数校验。`Pattern` 为 null 时不启用正则；启用后空字符串默认通过，可用 `AllowEmpty=false` 拒绝。正则使用 CultureInvariant、实例缓存和 100ms 匹配超时，超时后不再在每次重绘中重复执行。行数同时识别 LF、CRLF 和 CR，空字符串按 0 行处理。

`Placeholder` 只是显示配置，不会写入字段。它作为叠加层绘制，因此可以与 Unity `TextArea`、`Multiline` 和 `Delayed` 保持原生高度。`TextFieldMode.Password` 会接管为单行掩码输入，但序列化值仍是原始字符串。

`ValueDropdown(valuesMember)` 支持返回普通集合、键值项或 `ValueDropdownList<T>`。数据源可以是字段、属性或无参方法；返回顺序就是展示顺序。枚举字段直接使用无参数 `[ValueDropdown]`。

Tag、Layer、Sorting Layer、Scene、Input Axis、Animator 参数以及枚举选择器都使用可搜索弹窗。`ValueDropdown(ValueDropdownSource.Search, sourceMember)` 支持接收当前搜索词的单字符串参数方法，适合候选很多或需要按查询动态生成结果的场景；返回项同样支持 `ValueDropdownItem<T>`。

## 路径、资产和对象限制

| 特性 | 用途 |
| --- | --- |
| `Path` | 通过 `PathType` 选择文件、目录或指定类型的项目资源，可返回绝对或工程相对路径 |
| `ObjectsOnly` | 通过 `ObjectSource` 限制 Object 来自 Project、当前场景、子层级或父层级，可包含自身 |
| `ShowAssetPreview` | 在字段下展示资源预览 |
| `Expandable` | 内联绘制引用对象 Inspector |

Path 选择后会规范化分隔符；返回工程相对路径时目标必须位于当前项目下。路径控件和预览会增加额外高度，组合 Drawer 会把它们放在主字段下方，不应手工使用负间距修补。

## 集合

| 特性 | 用途 |
| --- | --- |
| `Collection` | 同时校验最小/最大元素数与元素唯一、非 null、非空白规则 |

```csharp
[Collection(1, 32, CollectionItemRule.Unique |
    CollectionItemRule.NotNull | CollectionItemRule.NotBlank)]
public List<string> commandKeys;
```

`min`、`max` 在构造时校验，非法范围直接抛出参数异常。`Unique` 使用直接索引比较，不创建 HashSet 快照，避免 Inspector 每次校验分配临时集合；大型列表应根据规模决定是否改用业务自定义校验。null 集合在 `min == 0` 时通过，在要求至少一个元素时按数量错误处理。`NotBlank` 只接受非空白字符串元素，不能用于混合类型列表。

大型列表应折叠复杂元素或分页。元素自身的 ActionAttribute 会递归参与高度计算，资源预览和 Expandable 尤其容易让单行变高。

## 校验、按钮和生命周期

| 特性 | 用途 |
| --- | --- |
| `Required` | 引用不能为 null、字符串不能为空白、值类型不能保持默认值 |
| `CompareTo` | 校验当前值与同一 Owner 上另一个成员的关系 |
| `OnValueChanged` | 通过 `ValueChangedMode` 选择值应用后的回调，或调用布尔方法校验并展示消息 |
| `InlineButton` | 在字段同一行末尾放方法按钮 |
| `Button` | 将无参/兼容方法绘制成按钮 |
| `ShowInInspector` | 展示 Unity 默认不会画出的成员 |
| `ToggleLeft` | bool 使用左侧复选框样式 |

回调异常会影响 Inspector 绘制，应在业务方法内处理可预期错误。`OnInspectorGUI` 适合少量无法声明式表达的内容，不应重新实现整个 Inspector。

`Required` 对值类型使用字段的真实 `ValueType` 创建并缓存默认值：整数 0、枚举第一个值、零初始化结构体都会失败。集合是否允许为空集合由 `Collection` 的数量范围决定，`Required` 只负责集合引用本身。

`CompareTo` 的成员名必须使用 `nameof`。支持字段、可读属性和无参方法；数值跨类型比较会统一为 decimal 或 double，字符串固定使用 Ordinal，其他类型要求实现兼容的 `IComparable`。

```csharp
public int minimum = 1;

[CompareTo(nameof(minimum), ValueRelation.GreaterThanOrEqual)]
public int maximum = 10;
```

## 外部扩展父特性

| 父特性 | 外部需要实现 | 用途 |
| --- | --- | --- |
| `ActionConditionAttribute` | `Evaluate(context)` | 自定义显示、隐藏、启用或禁用规则 |
| `ActionLabelAttribute` | `GetLabel(context)` | 动态字段名，可覆盖 tooltip |
| `ActionMessageAttribute` | `GetMessage(context)` | 动态 Info、Warning 或 Error 提示 |
| `ActionValidationAttribute` | `IsValid(context)` | 自定义校验，可覆盖动态消息 |
| `ActionValueModifierAttribute` | `Modify(context)` | 提交后转换、归一化或修正字段值 |

只有以上五个抽象类型允许外部继承，并且都要求实现业务逻辑。具体特性均为 `sealed`，不提供别名或参数预设派生。根组合 Drawer 会自动路由逻辑扩展，不需要外部 Editor 脚本。`ActionAttributeContext` 不包含 `SerializedProperty`；扩展可以安全放在 Runtime 程序集中。`Priority` 只决定同类行为扩展的执行顺序，不改变视觉特性的书写顺序。完整代码、生命周期与故障处理见 [Runtime 逻辑特性扩展](extensions.md)。

## Unity 原生特性

`Delayed`、`Multiline`、`TextArea`、`Range`、`Min`、`Header`、`Space`、`ColorUsage`、`GradientUsage`，以及当前 Unity 版本提供的 `SearchContext`，直接使用 Unity 的 Drawer。`Tooltip`、`InspectorName`、`ContextMenuItem`、`NonReorderable` 和标准 `[Flags]` 枚举继续遵循 Unity 自己的 PropertyHandler 与序列化规则。

组合 Drawer 保留原生高度、Decorator 顺序、`BeginProperty/EndProperty` 生命周期和字段类型 `CustomPropertyDrawer`，也支持数组与 List 元素。第三方 `PropertyAttribute` Drawer 可以继续委托 `EditorGUI.PropertyField`，不需要为 ActionAttribute 编写适配器。类型兼容的 Action 主控件（例如数值字段上的 `Slider`）优先接管主字段；类型不兼容时会回退到 Unity 原生 Drawer。

## 框架元数据

`Attachable` 声明 Timeline/Graph 类型可以挂到哪些父对象；它不是普通字段绘制器，但和其他 Runtime Attribute 一起发布。节点系统还使用 `Name`、`Icon`、`Node` 等元数据生成右键菜单和节点标题。
