# ActionAttribute

[在线完整文档](https://onclick9927.github.io/ActionEditor/#/attribute/overview) · [特性分类手册](../../docs/attribute/catalog.md) · [组合与布局](../../docs/attribute/composition.md)

ActionAttribute 是基于 Unity Inspector 的组合式绘制扩展。运行时程序集只包含 Attribute 和轻量数据类型，全部 Drawer、缓存与反射逻辑位于 Editor 程序集。

## 快速开始

```csharp
using ActionAttribute;
using UnityEngine;

public sealed class CharacterConfig : MonoBehaviour
{
    [Name("生命值", "角色当前生命值。")]
    [Clamp(0, 100), ProgressBar(0, 100), SuffixLabel("点")]
    [SerializeField] private int health = 100;

    [ShowIf(nameof(showAdvanced)), FolderPath]
    [Name("输出目录", "高级配置启用后显示。")]
    [SerializeField] private string output = "Assets";

    [SerializeField] private bool showAdvanced;
}
```

带有 ActionAttribute 的字段可同时使用多个特性。中央组合 Drawer 统一计算控件、预览、进度条、校验消息和间距的总高度，避免多个 PropertyDrawer 相互覆盖。

## 特性分类

- 条件：`ShowIf`、`HideIf`、`EnableIf`、`DisableIf`，支持布尔、枚举和期望值比较。
- 分组：`BoxGroup`、`FoldoutGroup`、`HorizontalGroup`、`TabGroup`、`ToggleGroup` 等。
- 数值：`Clamp`、`MinValue`、`MaxValue`、`NonNegative`、`Positive`、`Slider`、`MinMaxSlider`、`Step`、`Wrap`、`ProgressBar`。
- 文本：`MultilineText`、`ResizableTextArea`、`PasswordField`、`Placeholder`、`MaxLength`、`PrefixLabel`、`SuffixLabel`。
- 引用：`AssetsOnly`、`SceneObjectsOnly`、`ChildGameObjectsOnly`、`ParentGameObjectsOnly`、`ShowAssetPreview`、`Expandable`。
- 路径与标识：`FilePath`、`FolderPath`、`AssetPath`、`AssetGuid`、`Scene`、`Tag`、`Layer`、`SortingLayer`。
- 集合：`ReorderableList`、`RequiredListLength`、`UniqueList`。
- 校验与回调：`Required`、`ValidateInput`、`OnValueChanged`、`InlineButton`、`Button`。
- 展示：`Name`、`TypeInfoBox`、`HelpBox`、`Title`、`ReadOnly`、`HideLabel`、`GUIColor`、`Icon`。

运行时公开特性超过 100 个。完整组合示例位于 `Assets/Test/ActionAttribute/ActionAttributeExample.cs`，编辑器布局测试位于 `Assets/Test/ActionAttribute/Editor`。

## Name 与图标

`NameAttribute` 的第二个参数作为 `GUIContent.tooltip`。它也可标记枚举成员，为枚举下拉框提供本地化显示名。

`IconAttribute` 接受 Resources 名称、资源路径或 Base64 图片。Base64 会在 Editor 侧解码并缓存，不应在每帧动态创建字符串。

## 注意事项

- Drawer 类型均为 Editor 内部实现，不作为外部扩展 API。
- 不要在 PropertyDrawer 中使用 `EditorGUILayout`；组合 Drawer 使用 `Rect` 与 `GetPropertyHeight` 保证布局稳定。
- 条件方法应无副作用。Inspector 重绘频繁，条件和下拉数据源可能在一秒内被调用多次。
- 大型资源预览会增加 Inspector 重绘成本；列表中建议使用较小预览尺寸。
- `ShowNativeProperty` 只展示值，不会让 Unity 自动序列化属性。

## 兼容性

包声明最低 Unity 2019.4。部分较新的 Unity 控件会按当前编辑器 API 降级为普通字段。运行时代码不引用 `UnityEditor`。
