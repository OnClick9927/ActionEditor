# ActionAttribute

[在线完整文档](https://onclick9927.github.io/ActionEditor/#/attribute/overview) · [特性分类手册](../../docs/attribute/catalog.md) · [组合与布局](../../docs/attribute/composition.md) · [Runtime 逻辑扩展](../../docs/attribute/extensions.md)

ActionAttribute 是基于 Unity Inspector 的组合式绘制扩展。运行时程序集只包含 Attribute 和轻量数据类型，全部 Drawer、缓存与反射逻辑位于 Editor 程序集。

## 快速开始

```csharp
using ActionAttribute;
using UnityEngine;

public sealed class CharacterConfig : MonoBehaviour
{
    [Name("生命值", "角色当前生命值。")]
    [Slider(0, 100, SliderMode.Clamp)]
    [ProgressBar(0, 100), SuffixLabel("点")]
    [SerializeField] private int health = 100;

    [Condition(ConditionMode.Show, nameof(showAdvanced)), Path(PathType.Folder)]
    [Name("输出目录", "高级配置启用后显示。")]
    [SerializeField] private string output = "Assets";

    [SerializeField] private bool showAdvanced;
}
```

带有 ActionAttribute 的字段可同时使用多个特性。中央组合 Drawer 统一计算控件、预览、进度条、校验消息和间距的总高度，避免多个 PropertyDrawer 相互覆盖。

## 特性分类

- 条件：`Condition` 通过 `ConditionMode` 控制显示、隐藏、启用或禁用，支持成员比较和 `InspectorMode`。
- 分组：`Group` 通过 `GroupType` 选择纵向、横向、Box、折叠、带框折叠、标签页、标题、开关、按钮、网格、缩进或滚动布局。
- 数值：`Slider` 通过 `SliderMode` 选择滑杆、Clamp、Wrap、单边、非负、正数或步长吸附；另支持 `MinMaxSlider`、`ProgressBar`、`Finite`。
- 文本：`Text` 统一密码显示、占位符、最大长度、空白规范化、大小写、正则和行数校验；`SuffixLabel` 负责字段后的短单位。
- 引用：`ObjectsOnly` 通过 `ObjectSource` 限制 Project、Scene、Children、Parents 范围，并可配置是否包含自身；另支持 `ShowAssetPreview`、`Expandable`。
- 路径与标识：`Path` 通过 `PathType` 选择文件、文件夹或项目资源。
- 搜索选择：`ValueDropdown` 通过 `ValueDropdownSource` 统一支持枚举、成员、动态搜索、Tag、Layer、Sorting Layer、Scene、Input Axis 和 Animator 参数。
- 集合：`Collection` 同时配置数量范围和 `CollectionItemRule` 元素规则。
- 校验与回调：`Required` 统一引用、字符串和值类型默认值校验，另支持 `Finite`、`CompareTo`、`OnValueChanged`、`InlineButton`、`Button`。
- 展示：`Name`、`TypeInfoBox`、`HelpBox`、`ReadOnly`、`Icon`。

完整组合示例位于 `Assets/Test/ActionAttribute/ActionAttributeExample.cs`，编辑器布局测试位于 `Assets/Test/ActionAttribute/Editor`。

## 无 Editor 逻辑扩展

具体特性均为 `sealed`，不允许通过继承创建别名。需要少量业务逻辑时，只能继承以下 Runtime 抽象父特性并覆盖对应方法：

- `ActionConditionAttribute.Evaluate`：显示、隐藏、启用或禁用。
- `ActionLabelAttribute.GetLabel`：动态字段名和 tooltip。
- `ActionMessageAttribute.GetMessage`：动态 Info、Warning 或 Error 提示。
- `ActionValidationAttribute.IsValid`：校验并由组合 Drawer 展示消息。
- `ActionValueModifierAttribute.Modify`：字段提交后修正值。

逻辑通过 `ActionAttributeContext` 读取 `Target`、`Owner`、`Value`、`ValueType`、`PropertyPath`、`MemberName` 和 `IsPlaying`，并可用 `TryGetMemberValue<T>` 读取当前 Owner 的相关成员。多个扩展规则可用 `Priority` 指定顺序，数值越小越先执行。直接使用 `ActionAttributeBase` 或具体特性作为父类在 API 层被禁止。完整示例见 `Assets/Test/ActionAttribute/ExtensibleAttributeExample.cs`，完整契约、生命周期和故障处理见 [Runtime 逻辑扩展](../../docs/attribute/extensions.md)。

## Name 与图标

`NameAttribute` 的第二个参数作为 `GUIContent.tooltip`。它也可标记枚举成员，为枚举下拉框提供本地化显示名。

`IconAttribute` 接受 Resources 名称、资源路径或 Base64 图片。Base64 会在 Editor 侧解码并缓存，不应在每帧动态创建字符串。

## 注意事项

- Drawer 类型均为 Editor 内部实现，不作为外部扩展 API。
- 行为扩展应快速且无副作用；框架会隔离异常并只记录一次警告。
- 不要在 PropertyDrawer 中使用 `EditorGUILayout`；组合 Drawer 使用 `Rect` 与 `GetPropertyHeight` 保证布局稳定。
- 条件方法应无副作用。Inspector 重绘频繁，条件和下拉数据源可能在一秒内被调用多次。
- 大型资源预览会增加 Inspector 重绘成本；列表中建议使用较小预览尺寸。
- Unity 已有的 `Delayed`、`Multiline`、`TextArea`、`Tooltip`、`Range`、`Min`、`Header`、`Space`、`ColorUsage`、`GradientUsage` 等特性直接使用 Unity 版本，并可与 ActionAttribute 混用；标准 `[Flags]` 枚举继续使用 Unity 自己的掩码字段。
- 第三方 `PropertyAttribute`、`DecoratorDrawer` 和字段类型 `CustomPropertyDrawer` 仍通过 Unity 的 PropertyHandler 调用，包含数组、List 元素和会继续委托 `EditorGUI.PropertyField` 的 Drawer。
- `ShowInInspector` 只展示值，不会让 Unity 自动序列化属性。

## 兼容性

包声明最低 Unity 2019.4。部分较新的 Unity 控件会按当前编辑器 API 降级为普通字段。运行时代码不引用 `UnityEditor`。
