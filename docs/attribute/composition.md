# 组合、布局与扩展

## 推荐组合顺序

特性的源码书写顺序不应成为业务正确性的唯一依据。按职责思考更稳定：

1. 可见/可编辑条件：Condition、ConditionMode。
2. 分组：Group、GroupType。
3. 名称和说明：Name、ActionLabelAttribute、Tooltip、TypeInfoBox、HelpBox、ActionMessageAttribute。
4. 主值控件：Slider、ValueDropdown、Path。
5. 附加控件：InlineButton、ShowAssetPreview、ProgressBar。
6. 值修正和校验：SliderMode、Slider.Step、Text、Required、Finite、CompareTo、Collection、OnValueChanged Validate。
7. 变更通知：OnValueChanged。

```csharp
[Group("导出", GroupType.Box)]
[Condition(ConditionMode.Show, nameof(enableExport))]
[Name("输出目录", "必须位于项目 Assets 内。")]
[Path(PathType.Folder)]
[Required("请选择有效输出目录。")]
[OnValueChanged(nameof(OnOutputChanged))]
[SerializeField] private string output = "Assets";
```

## 高度与重叠

所有在主字段上方/下方绘制内容的特性都必须贡献高度。框架中央渲染器会统一计算 HelpBox、Preview、ProgressBar 和集合警告，不允许子 Drawer 自己调用不配对的 `BeginProperty/EndProperty`。条件、校验和值修正应使用 Runtime 扩展父类，由中央渲染器自动纳入高度和生命周期；只有全新的 IMGUI 控件才需要手写 PropertyDrawer，并自行保证 `GetPropertyHeight` 与 `OnGUI` 完全一致。

常见重叠原因：

- 回调在高度阶段和绘制阶段返回不同条件。
- 动态列表数量在同一 Event 中变化。
- 自定义 GUI 使用 EditorGUILayout，而父级按 Rect 布局。
- 预览尺寸或文本行数在绘制后才更新，却没有请求 Repaint/Layout。

## 条件成员解析

条件成员从当前序列化对象解析字段、属性或无参方法。嵌套对象使用实际 owning object，不应假设 `serializedObject.targetObject` 就是字段直接拥有者。布尔条件直接读取；带 expected 的条件使用兼容类型比较；枚举保持枚举类型和值，不走本地化显示文本。

## 下拉数据源

```csharp
private ValueDropdownList<int> DamageTypes => new ValueDropdownList<int>
{
    { "物理", 1 },
    { "火焰", 2 },
    { "冰霜", 3 }
};

[ValueDropdown(nameof(DamageTypes))]
public int damageType;
```

候选集合很大时缓存列表，避免每次 Repaint 分配。需要动态内容时在业务数据改变后清理缓存，不要依赖每帧重新扫描 AssetDatabase。

需要按搜索词动态生成候选时使用 `ValueDropdownSource.Search`：

```csharp
[ValueDropdown(ValueDropdownSource.Search, nameof(SearchItems))]
public string item;

private IEnumerable<ValueDropdownItem<string>> SearchItems(string query)
{
    // 返回与 query 匹配的显示名和值。
}
```

搜索源只在弹窗打开和搜索词变化时调用。方法可以是私有或静态方法，必须接收一个 string 并返回 IEnumerable。

## Base64 图标

`IconAttribute(value, isBase64: true)` 在 Editor 解码 PNG/JPG 字节并缓存 Texture。Base64 适合小型内嵌图标；大图应放 Resources 或资产路径，避免程序集元数据和首次解码成本。无效 Base64 会回退为空图标并记录问题，不应在 OnGUI 重复抛异常。

## 自定义 Inspector 共存

完全重写 `OnInspectorGUI` 会绕过 ActionAttribute 类型提示、Script 行和组合绘制。优先让 `ActionFallbackInspector` 处理序列化字段，只用特性表达额外行为。确实需要 CustomEditor 时，应调用共享渲染入口或 `base.OnInspectorGUI()`，再追加少量专用控件。

Timeline 与 BT Inspector 已遵循这一规则：Script 定位在顶部、类型名称与 TypeInfoBox 随后、业务字段最后，避免提示重复和大段空白。

## Unity Drawer 共存

fallback Inspector 和普通 Unity Inspector 都通过 `EditorGUI.PropertyField` 进入 Unity 的 PropertyHandler。`Header`、`Space` 等 Decorator 只绘制一次；`TextArea`、`Range` 等原生 Drawer 以及第三方 `PropertyAttribute` Drawer 保留自己的高度和嵌套调用语义。字段类型上注册的 `CustomPropertyDrawer` 同样适用于带 ActionAttribute 的普通字段、数组元素和 List 元素。

```csharp
[Header("说明")]
[TextArea(3, 8), Name("描述")]
[SerializeField] private string description;
```

Action 主控件仅在序列化类型兼容时接管值区域。例如 `Slider` 只接管数值字段；误用于字符串时不会把原生 `TextArea` 压成单行。Decorator、tooltip、prefab override 和右键菜单仍由 Unity 的属性生命周期处理。

`Text(Placeholder = "...")` 只在原生控件绘制后叠加提示，不改变 `TextArea`、`Multiline` 或第三方字符串 Drawer 的高度。只有 `TextFieldMode.Password` 明确要求单行掩码控件时才接管主值区域。文本规范化只在真实值变化后写回，校验阶段不会修改序列化数据。

## Runtime-only 特性扩展

具体特性均为 `sealed`；不要为已有特性创建别名或仅转发父构造的参数预设。动态规则继承抽象行为父特性。以下校验特性只覆盖一个方法，仍由框架负责高度、消息类型和绘制：

```csharp
[AttributeUsage(AttributeTargets.Field, AllowMultiple = true)]
public sealed class EvenValueAttribute : ActionValidationAttribute
{
    public EvenValueAttribute() : base("数值必须是偶数。") { }

    public override bool IsValid(ActionAttributeContext context)
    {
        return context.TryGetValue<long>(out long value) && value % 2 == 0;
    }
}
```

`ActionConditionAttribute` 的构造函数接收 `ConditionMode`，必须覆盖 `Evaluate`；`ActionLabelAttribute` 覆盖 `GetLabel`，可选覆盖 `GetTooltip`；`ActionMessageAttribute` 覆盖 `GetMessage`；`ActionValidationAttribute` 覆盖 `IsValid`；`ActionValueModifierAttribute` 覆盖 `Modify` 并返回修正值。多个规则使用 `Priority`，较小值先执行，同优先级保持声明发现顺序。规则应快速、确定且无副作用，不应修改 `Target` 或 `Owner`。

完整实现与 API 约束见 [Runtime 逻辑特性扩展](extensions.md)。

## 性能

- 类型、字段、特性、回调 MethodInfo 和 GUIContent 会缓存。
- Drawer 为 internal，不构成外部二进制兼容承诺。
- Inspector 中避免 LINQ、AssetDatabase 全量查询和每帧 Texture2D 创建。
- `ValueChangedMode.Callback` 只在真实值变化并 Apply 后执行；`ValueChangedMode.Validate` 会在 Inspector 测量和绘制时调用，因此校验方法必须快速且无副作用。
- 大型 Inspector 使用 `GroupType.Foldout`、`GroupType.FoldoutBox` 或 `GroupType.Tab` 延迟绘制不可见内容；规则密集的字段可用 `Grid`，大量低频字段可放入固定高度的 `Scroll`。
