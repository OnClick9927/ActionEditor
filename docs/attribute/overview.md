# Inspector 特性概览

ActionAttribute 用声明式特性组合 Unity Inspector。Runtime 程序集公开 Attribute 与行为扩展协议；所有 Drawer、反射、样式、资源预览缓存和 fallback Inspector 都在 Editor 程序集内部，外部程序集不依赖具体 Drawer 类型。外部派生特性由根组合 Drawer 自动发现。

## 最小示例

```csharp
using ActionAttribute;
using UnityEngine;

[TypeInfoBox("角色战斗参数，修改后立即参与预览。")]
public sealed class CombatConfig : MonoBehaviour
{
    [Name("生命值", "必须在 1 到 9999 之间。")]
    [Slider(1, 9999, SliderMode.Clamp)]
    [ProgressBar(1, 9999), SuffixLabel("HP")]
    [SerializeField] private int health = 100;

    [Name("高级选项")]
    [SerializeField] private bool advanced;

    [Condition(ConditionMode.Show, nameof(advanced))]
    [Path(PathType.Folder), Name("导出目录")]
    [SerializeField] private string output = "Assets";

    [InlineButton(nameof(ResetHealth), "重置")]
    [SerializeField] private int previewHealth = 100;

    private void ResetHealth() => previewHealth = health;
}
```

## 绘制流程

ActionPropertyDrawer 不是让每个特性各自包一层 Unity PropertyDrawer，而是一次收集字段上的全部 ActionAttribute，按条件、布局、值控件、附加控件和校验消息组合。`GetPropertyHeight` 与 `OnGUI` 使用同一布局信息，因此 Preview、ProgressBar、HelpBox、列表按钮不会覆盖下一字段。

如果类型使用 fallback Inspector，顶部始终绘制 Unity 原生风格的只读 Script 字段，可双击脚本或点击对象选择器快速定位。

## Name、tooltip 与枚举

`[Name("显示名", "说明")]` 的第二个参数进入 `GUIContent.tooltip`。Name 也可标记枚举成员，枚举下拉、搜索和按钮组使用同一显示名：

```csharp
public enum TargetMode
{
    [Name("最近目标", "选择距离最小的合法目标。")]
    Nearest,
    [Name("最低生命", "按当前生命值升序选择。")]
    LowestHealth
}
```

## 类型级信息

`TypeInfoBox` 可标记类并由 Inspector 顶部绘制。派生类自己声明 TypeInfoBox 时优先使用最近声明，不会再把基类同类提示重复绘制。Timeline、GraphAsset、行为树节点等自定义 Inspector 也统一调用 ActionAttribute 渲染器，保证类型提示和 Script 行存在。字段附近的说明使用 `HelpBox`。

## 逻辑扩展

具体 ActionAttribute 均为 `sealed`，不允许创建别名特性。外部扩展只能继承 `ActionConditionAttribute`、`ActionLabelAttribute`、`ActionMessageAttribute`、`ActionValidationAttribute` 或 `ActionValueModifierAttribute` 并覆写业务逻辑；它们只接收 Runtime 的 `ActionAttributeContext`，无需引用 `UnityEditor` 或注册 Drawer。

内置的 `Text`、`Collection`、`Required`、`CompareTo` 和 `Finite` 也复用这套 Runtime 校验管线。它们不引用 Unity 资源、场景、Tag 或 Layer，可用于普通字符串、数值、枚举、结构体、数组和 List；具体类型保持 `sealed`，不会为旧名称或参数预设提供别名。`Text` 统一字符串绘制、规范化与校验，`Collection` 统一数量和元素规则，`Required` 同时覆盖引用、空白字符串和值类型默认值。

组合 Drawer 会按 `Priority` 从小到大稳定执行同类扩展。条件和校验可能在 Layout 与 Repaint 阶段重复求值，因此必须无副作用；扩展异常会按“特性类型 + 异常”去重为一次警告，并采用不隐藏、不锁死字段的回退行为。

五个扩展点的完整代码、上下文字段、执行顺序、异常回退与迁移示例见 [Runtime 逻辑特性扩展](extensions.md)。

## 使用原则

- 条件、校验、下拉数据源和按钮方法用 `nameof`，避免重命名后静默失效。
- 回调必须快速、无副作用；Inspector 一帧可能多次请求高度和绘制。
- 复杂对象先用 `Group` 的折叠或标签页模式分层，避免在一个字段叠十几个视觉特性。
- 资源预览设置适当尺寸，大列表中不要每项使用 256px 预览。
- 运行时 Attribute 只是元数据，不会让 Unity 自动序列化属性或非序列化字段。
- `ShowInInspector` 是展示用途，持久化仍遵循 Unity 自己的序列化规则。
