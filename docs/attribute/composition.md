# 组合、布局与扩展

## 推荐组合顺序

特性的源码书写顺序不应成为业务正确性的唯一依据。按职责思考更稳定：

1. 可见/可编辑条件：ShowIf、EnableIf。
2. 分组与顺序：BoxGroup、TabGroup、PropertyOrder。
3. 名称和说明：Name、Tooltip、Title。
4. 主值控件：Slider、Dropdown、FolderPath。
5. 附加控件：InlineButton、Preview、ProgressBar。
6. 值修正和校验：Clamp、Required、ValidateInput。
7. 变更通知：OnValueChanged。

```csharp
[BoxGroup("导出")]
[ShowIf(nameof(enableExport))]
[Name("输出目录", "必须位于项目 Assets 内。")]
[FolderPath]
[Required("请选择有效输出目录。")]
[OnValueChanged(nameof(OnOutputChanged))]
[SerializeField] private string output = "Assets";
```

## 高度与重叠

所有在主字段上方/下方绘制内容的特性都必须贡献高度。框架中央渲染器会统一计算 HelpBox、Preview、ProgressBar、集合警告和自适应文本区，不允许子 Drawer 自己调用不配对的 `BeginProperty/EndProperty`。外部扩展若绕过渲染器手写 PropertyDrawer，需自行保证 `GetPropertyHeight` 与 `OnGUI` 完全一致。

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

[ValueDropdown(nameof(DamageTypes)), Searchable]
public int damageType;
```

候选集合很大时缓存列表，避免每次 Repaint 分配。需要动态内容时在业务数据改变后清理缓存，不要依赖每帧重新扫描 AssetDatabase。

## Base64 图标

`IconAttribute(value, isBase64: true)` 在 Editor 解码 PNG/JPG 字节并缓存 Texture。Base64 适合小型内嵌图标；大图应放 Resources 或资产路径，避免程序集元数据和首次解码成本。无效 Base64 会回退为空图标并记录问题，不应在 OnGUI 重复抛异常。

## 自定义 Inspector 共存

完全重写 `OnInspectorGUI` 会绕过 ActionAttribute 类型提示、Script 行和组合绘制。优先让 `ActionFallbackInspector` 处理序列化字段，只用特性表达额外行为。确实需要 CustomEditor 时，应调用共享渲染入口或 `base.OnInspectorGUI()`，再追加少量专用控件。

Timeline 与 BT Inspector 已遵循这一规则：Script 定位在顶部、类型名称与 TypeInfoBox 随后、业务字段最后，避免提示重复和大段空白。

## 性能

- 类型、字段、特性、回调 MethodInfo 和 GUIContent 会缓存。
- Drawer 为 internal，不构成外部二进制兼容承诺。
- Inspector 中避免 LINQ、AssetDatabase 全量查询和每帧 Texture2D 创建。
- OnValueChanged 只在真实值变化并 Apply 后执行。
- 大型 Inspector 使用 Foldout/Tab 延迟绘制不可见内容。
