# Runtime 逻辑特性扩展

ActionAttribute 开放五种 Runtime 逻辑扩展：条件、校验、值修正、动态提示和动态标签。扩展类只引用 `ActionAttribute` Runtime 程序集，放在普通 Runtime 目录，不需要 `Editor` 文件夹、`UnityEditor` 引用或自定义 `PropertyDrawer`。

具体特性全部为 `sealed`，`ActionAttributeBase` 的构造函数也不对外开放。外部代码不能通过继承 `SliderAttribute`、`NameAttribute` 等具体类型创建别名，只能继承下表中的抽象父类并实现业务方法。

| 抽象父类 | 必须实现 | 调用时机 | 失败时回退 |
| --- | --- | --- | --- |
| `ActionConditionAttribute` | `Evaluate(context)` | Inspector 测量和绘制期间，可能一帧多次 | 字段保持显示且可编辑 |
| `ActionValidationAttribute` | `IsValid(context)` | 计算高度和绘制校验消息时 | 忽略本次校验消息 |
| `ActionValueModifierAttribute` | `Modify(context)` | 用户提交字段变化后、值变更回调前 | 忽略失败的当前修正 |
| `ActionMessageAttribute` | `GetMessage(context)` | 计算高度和绘制字段前提示时 | 不显示当前动态提示 |
| `ActionLabelAttribute` | `GetLabel(context)` | 计算高度和绘制主字段前 | 保留前一个标签和 tooltip |

内置 `Text`、`Collection`、`Required`、`CompareTo` 和 `Finite` 使用同一校验管线。`Text` 还在用户提交变化后执行字符串规范化，中央 Drawer 负责写回；这些类型的公开配置只包含字符串、数字、枚举和布尔值，不引用 Unity 资源类型。需要通用规则时优先直接配置这些具体特性，需要业务状态时再实现自定义派生类型。

## 内置资源无关规则

| 特性 | 支持值 | 空值行为 | 执行特点 |
| --- | --- | --- | --- |
| `Text` | string | null 和非 string 不通过；空字符串的正则行为由 `AllowEmpty` 决定 | 统一绘制、规范化、截断、正则和行数；正则对象按配置缓存 |
| `Collection` | Array、List | `min == 0` 时 null 通过，否则按数量错误处理 | 数量和元素规则一次遍历；唯一性直接比较，不分配 HashSet |
| `Required` | 引用、string、值类型 | null、空白字符串和类型默认值不通过 | 值类型默认值按字段类型缓存；同时识别 Unity 缺失引用的 null 语义 |
| `CompareTo` | 数字、字符串、枚举、IComparable | 目标成员缺失或不可比较时不通过 | 从当前 Owner 读取字段、属性或无参方法；字符串固定 Ordinal 比较 |
| `Finite` | 整数、decimal、float、double | null 和非数字不通过 | 整数和 decimal 恒为有限；float/double 拒绝 NaN 与 Infinity |

```csharp
[Text(Placeholder = "请输入协议标识", MaxLength = 24, Trim = true,
    CollapseWhitespace = true, Case = TextCase.UpperInvariant,
    Pattern = "^[A-Z0-9_]+$", AllowEmpty = false)]
public string protocolKey;

[Collection(1, 32, CollectionItemRule.Unique |
    CollectionItemRule.NotNull | CollectionItemRule.NotBlank)]
public List<string> commandKeys;

[Required]
public int configVersion = 1;

public int minimumDamage = 1;

[CompareTo(nameof(minimumDamage), ValueRelation.GreaterThanOrEqual)]
public int maximumDamage = 10;

[Finite]
public float simulationScale = 1;

[UnityEngine.TextArea(2, 6), Text(MinLines = 1, MaxLines = 6)]
public string releaseNotes;
```

`Text` 的正则只负责格式；希望空字符串也报错时设置 `AllowEmpty=false`，希望空白字符串和 null 都报错时组合 `Required`。文本写回顺序固定为 Trim、折叠空白、Invariant 大小写转换、`MaxLength` 截断，保证截断约束针对最终规范化结果。`Collection` 先检查数量，再按元素索引顺序检查 NotNull、NotBlank、Unique，因此同一帧只显示第一个确定错误。`CompareTo` 使用 `nameof` 指向同一 Owner 上的成员，嵌套对象会读取嵌套实例，而不是错误地读取最外层组件。

## 工程放置

建议把扩展放在业务 Runtime 程序集中：

```text
Assets/Game/Runtime/Inspector/
  PositiveConditionAttribute.cs
  NonBlankAttribute.cs
  RoundToStepAttribute.cs
  LimitMessageAttribute.cs
  CurrentValueLabelAttribute.cs
```

该程序集只需引用 `ActionAttribute`。不要引用 `ActionAttribute.Editor`，也不要注册同名 Drawer。根组合 Drawer 会通过 `ActionAttributeBase` 自动发现五个抽象父类的派生类型。

## 自定义条件

条件扩展通过构造函数选择 `ConditionMode`，通过 `Evaluate` 返回业务判断结果：

```csharp
using System;
using ActionAttribute;

[AttributeUsage(AttributeTargets.Field, AllowMultiple = true)]
public sealed class PositiveConditionAttribute : ActionConditionAttribute
{
    public PositiveConditionAttribute(
        ConditionMode mode = ConditionMode.Show) : base(mode) { }

    public override bool Evaluate(ActionAttributeContext context)
    {
        return context.TryGetValue<double>(out double value) && value > 0;
    }
}
```

```csharp
[PositiveCondition(ConditionMode.Show)]
public int positiveOnly;

[PositiveCondition(ConditionMode.Enable)]
public float editableWhenPositive;
```

`Show` 在结果为 `false` 时隐藏字段；`Hide` 在结果为 `true` 时隐藏字段。`Enable` 在结果为 `false` 时禁用字段；`Disable` 在结果为 `true` 时禁用字段。规则按顺序求值，遇到首个隐藏或禁用结果就停止；不要让条件依赖求值次数或反射发现顺序产生副作用。

## 自定义校验

校验扩展返回当前值是否合法。固定消息传给父构造；需要包含当前值或路径时覆盖 `GetMessage`：

```csharp
using System;
using ActionAttribute;

[AttributeUsage(AttributeTargets.Field, AllowMultiple = true)]
public sealed class NonBlankAttribute : ActionValidationAttribute
{
    public NonBlankAttribute(
        InspectorMessageType type = InspectorMessageType.Error)
        : base(null, type) { }

    public override bool IsValid(ActionAttributeContext context)
    {
        return context.Value is string value &&
            !string.IsNullOrWhiteSpace(value);
    }

    public override string GetMessage(ActionAttributeContext context)
    {
        return $"{context.MemberName} 不能为空。";
    }
}
```

```csharp
[UnityEngine.TextArea(3, 8)]
[NonBlank(InspectorMessageType.Warning)]
public string description;
```

校验可能在 Layout、Repaint 和其他 Inspector 事件中重复执行。它只能读取状态，不应写字段、创建资源、记录重复日志或调用耗时查询。

## 自定义值修正

值修正在用户提交变化后接收当前值，并返回写回值。返回对象必须能写入字段的 `ValueType`：

```csharp
using System;
using ActionAttribute;

[AttributeUsage(AttributeTargets.Field, AllowMultiple = true)]
public sealed class RoundToStepAttribute : ActionValueModifierAttribute
{
    private readonly double step;

    public RoundToStepAttribute(double step)
    {
        this.step = step;
    }

    public override object Modify(ActionAttributeContext context)
    {
        if (step <= 0 ||
            !context.TryGetValue<double>(out double value))
            return context.Value;

        double rounded = Math.Round(value / step) * step;
        return Convert.ChangeType(rounded, context.ValueType);
    }
}
```

```csharp
[Slider(0, 10)]
[RoundToStep(0.25, Priority = -100)]
public float interval = 1;
```

无法修正时返回 `context.Value`。该值是当前规则开始时的权威值：它包含用户刚提交的编辑，以及已经成功的前序修正；此时从 `Owner` 或 `Target` 重新读取字段，可能仍得到 `ApplyModifiedProperties` 前的对象值。不要直接修改 `Target` 或 `Owner`；组合 Drawer 负责写回、Undo、序列化应用和后续回调。

## 自定义动态提示

动态提示返回 `ActionMessage`。返回默认值或空文本时不占高度；返回非空文本时由组合 Drawer 在主字段上方绘制 HelpBox，并自动加入 `GetPropertyHeight`：

```csharp
[AttributeUsage(AttributeTargets.Field, AllowMultiple = true)]
public sealed class LimitMessageAttribute : ActionMessageAttribute
{
    private readonly double maximum;

    public LimitMessageAttribute(double maximum)
    {
        this.maximum = maximum;
    }

    public override ActionMessage GetMessage(ActionAttributeContext context)
    {
        return context.TryGetValue<double>(out double value) &&
            value > maximum
                ? new ActionMessage($"当前值超过建议上限 {maximum}。",
                    InspectorMessageType.Warning)
                : default;
    }
}
```

```csharp
[LimitMessage(100)]
public int cacheCapacity = 128;
```

动态提示不是校验：它适合建议、状态和上下文说明，不会把字段标记为有效或无效。需要约束数据时使用 `ActionValidationAttribute`。

## 自定义动态标签

动态标签返回当前字段显示名，并可覆盖 tooltip。返回 null 表示保留前一个结果。`Name` 先建立静态标签，随后 `ActionLabelAttribute` 按 Priority 从小到大应用，因此较高 Priority 可以覆盖较低 Priority：

```csharp
[AttributeUsage(AttributeTargets.Field)]
public sealed class CurrentValueLabelAttribute : ActionLabelAttribute
{
    private readonly string prefix;

    public CurrentValueLabelAttribute(string prefix)
    {
        this.prefix = prefix;
    }

    public override string GetLabel(ActionAttributeContext context)
    {
        return $"{prefix} ({context.Value})";
    }

    public override string GetTooltip(ActionAttributeContext context)
    {
        return context.PropertyPath;
    }
}
```

标签提供器可能在 Layout 和 Repaint 各执行一次。不要在其中做本地化表全量扫描；应在业务层缓存翻译表，只执行 O(1) 查询。

## ActionAttributeContext

| 成员 | 含义 |
| --- | --- |
| `Target` | 当前 `SerializedObject` 的目标 Unity 对象 |
| `Owner` | 直接拥有字段的对象；嵌套字段时通常不同于 `Target` |
| `Value` | 当前字段值，引用和值都可能为 `null` |
| `ValueType` | 字段的目标类型，用于校验修正结果 |
| `PropertyPath` | 完整 Unity 序列化路径，例如 `settings.damage` |
| `MemberName` | 当前字段名，例如 `damage` |
| `IsPlaying` | 当前是否处于 Play Mode |
| `TryGetValue<T>` | 读取精确类型，或执行数字、字符串和枚举等安全转换 |
| `TryGetMemberValue<T>` | 从 Owner（没有 Owner 时从 Target）读取字段、属性或无参方法，并安全转换 |

成员查询按“Owner 类型 + 成员名”缓存 `MemberInfo`，支持私有成员、继承成员和静态成员。属性 getter 或方法抛异常、成员不存在、参数不为零、返回值无法转换时返回 false；框架不会让这些错误破坏 Inspector。

嵌套数据示例：

```csharp
[Serializable]
public sealed class DamageSettings
{
    [NonBlank]
    public string damageType;
}

public sealed class WeaponConfig : MonoBehaviour
{
    public DamageSettings settings = new DamageSettings();
}
```

绘制 `settings.damageType` 时，`Target` 是 `WeaponConfig`，`Owner` 是对应的 `DamageSettings`，`PropertyPath` 是 `settings.damageType`。

多对象编辑时，上下文当前取自 `SerializedObject.targetObject`，即首个目标对象。扩展逻辑不应假设一次调用会遍历所有选中对象。

## 顺序与组合

同类逻辑扩展按 `Priority` 从小到大执行；数值相同时保持特性声明发现顺序。条件、校验、值修正、动态提示和动态标签分别排序，不能用一个类别的 Priority 强制另一个类别提前执行。

```csharp
[ScaleValue(2, Priority = -20)]
[OffsetValue(3, Priority = 20)]
public int value = 4;
```

上例中的两个逻辑特性分别实现乘法和加法，完整代码见文末项目样例。它会先乘 2，再加 3，结果为 11。Priority 应表达稳定的业务依赖，不要使用极端数值为视觉特性排序。

内置特性和逻辑扩展可以直接组合：

```csharp
[Header("发布")]
[TextArea(3, 8)]
[Name("更新说明", "发布前必须填写。")]
[NonBlank]
public string releaseNotes;
```

Unity 的 `Header`、`TextArea`、tooltip、字段类型 Drawer 和第三方 `PropertyAttribute` 仍由 Unity PropertyHandler 处理。逻辑扩展不应尝试接管 Rect 或绘制 GUI。

## 异常与返回类型

框架捕获扩展异常，并按“扩展类型 + 异常类型 + 消息”只记录一次警告：

- 条件异常采用开放回退，字段不会被隐藏或锁死。
- 校验异常不显示错误消息，避免 Inspector 高度持续抖动。
- 值修正异常或返回类型不兼容时忽略当前规则；已经成功写入的前序修正不会回滚。
- 动态提示异常时不显示该条提示，也不保留空白高度。
- 动态标签异常时保留 `Name`、Unity 标签或前一个成功提供器的结果。

异常隔离用于保证 Inspector 可操作，不代表业务错误可以忽略。扩展应在自身测试中覆盖 null、类型不匹配、嵌套对象和边界值。

## 不允许的写法

以下写法不受支持，并由 `sealed` 或构造函数可见性在编译期阻止：

```csharp
// 错误：具体特性不能作为别名父类。
public sealed class PercentAttribute : SliderAttribute { }

// 错误：不能直接继承无行为协议的根类型。
public sealed class UnknownAttribute : ActionAttributeBase { }
```

直接使用具体特性表达配置：

```csharp
[Slider(0, 100)]
public int percent;

[Condition(ConditionMode.Show, nameof(advanced))]
public int advancedValue;
```

旧的显示/隐藏/启用/禁用缩写应迁移为统一条件：

| 旧写法 | 当前写法 |
| --- | --- |
| `ShowIf(member)` | `Condition(ConditionMode.Show, member)` |
| `HideIf(member)` | `Condition(ConditionMode.Hide, member)` |
| `EnableIf(member)` | `Condition(ConditionMode.Enable, member)` |
| `DisableIf(member)` | `Condition(ConditionMode.Disable, member)` |

数值缩写统一迁移到 `SliderMode`，选择器缩写统一迁移到 `ValueDropdownSource`，分组缩写统一迁移到 `GroupType`，对象来源缩写统一迁移到 `ObjectSource`。

文本、集合和必填规则也只保留统一入口，不提供旧名称包装：

| 旧写法 | 当前写法 |
| --- | --- |
| `PasswordField` | `Text(TextFieldMode.Password)` |
| `Placeholder(text)` | `Text(Placeholder = text)` |
| `MaxLength(length)` | `Text(MaxLength = length)` |
| `NormalizeText(case)` | `Text(Case = case, Trim = true, ...)` |
| `Pattern(pattern)` | `Text(Pattern = pattern)` |
| `TextLines(min, max)` | `Text(MinLines = min, MaxLines = max)` |
| `RequiredListLength(min, max)` | `Collection(min, max)` |
| `CollectionItems(rules)` | `Collection(rules)` |
| `NonDefault` | `Required` |

同一字段原来叠加多个文本特性时，应合成一个 `Text`。同一字段原来同时使用集合长度和元素规则时，应合成一个 `Collection(min, max, rules)`。这是源码迁移，不存在兼容别名；迁移后编译器会直接指出遗漏位置。

## 测试建议

每个外部逻辑特性至少覆盖：

- 正常值、null、错误类型和边界值。
- 嵌套字段时 `Target`、`Owner` 和 `PropertyPath`。
- 多个实例的 Priority 顺序及相同 Priority 的稳定顺序。
- `TryGetMemberValue` 对私有字段、属性、无参方法和缺失成员的行为。
- 与 Unity 原生 Drawer、数组和 List 元素的组合高度。
- 抛出异常或返回错误类型时 Inspector 仍可测量和绘制。

项目内回归样例位于 `Assets/Test/ActionAttribute/ExtensibleAttributeExample.cs`，契约测试位于 `Assets/Test/ActionAttribute/Editor/ActionAttributeExtensionTests.cs`。
