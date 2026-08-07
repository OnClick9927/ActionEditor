# ActionEditor

[在线完整文档](https://onclick9927.github.io/ActionEditor/#/timeline/editor) · [Timeline 扩展](../../docs/timeline/extensions.md) · [迁移指南](../../docs/advanced/migration.md)

ActionEditor 是可扩展的时间轴编辑器框架。运行时只保存 Asset、Group、Track、Clip 等数据和播放协议，所有窗口、Inspector、撤销历史和资源选择逻辑位于 Editor 程序集。

## 定义时间轴资源

```csharp
using ActionAttribute;
using ActionEditor;

[Name("战斗时间轴")]
[AssetFileExtension("combat.timeline.bytes")]
public sealed class CombatTimeline : Asset
{
}
```

`AssetFileExtensionAttribute` 由具体资源类型声明并可继承。编辑器的新建、打开、资源筛选和另存为都通过 `AssetFileExtensionUtility` 查询，不再强制全局 `Asset.FileEx`。旧常量仅为源码兼容保留。

## 编辑与播放

从 `Tools` 菜单打开 ActionEditor，创建资源后添加 Group、Track 与 Clip。实现自定义类型时使用 `AttachableAttribute` 声明允许的父类型，使用 `Name`、`Icon` 和其他 ActionAttribute 控制 Inspector 展示。

编辑器维护独立撤销历史，不使用 Unity Undo。`Ctrl+Z`、`Ctrl+Shift+Z` 和历史列表跳转都会恢复时间轴数据、选择和播放位置。打开其他文件时历史自动清空；未保存资源名显示 `*`。

## 自定义编辑器视图

继承 `ActonEditorView` 或 `ClipEditorView<T>` 并使用 `CustomActionViewAttribute` 绑定数据类型。每个 View 的 `target` 是当前检查对象，`asset` 始终指向当前编辑的顶层 `Asset`，因此 Clip/Track 扩展不需要通过全局窗口反查所属资源。

## 编辑器设置

设置通过时间轴工具栏的设置按钮编辑，保存到：

`ProjectSettings/ActionEditorSettings.asset`

该设置不注册到 Unity Project Settings 页面。旧版 `Assets/Editor/ActionEditor.txt` 只在第一次加载时迁移，之后不再写 JSON。Track/Clip 颜色只在时间轴设置弹窗中展示，并按当前打开的 `Asset` 具体类型分别保存。

## 本地化

内置简体中文和 English。语言系统使用动态键值表，不再反射实例化语言类；切换语言后工具栏缓存会自动刷新。注册 API 和语言接口为 `internal`，仅白名单框架编辑器程序集可添加语言，业务程序集不能修改框架词条。

## 保存注意点

- 修改已有二进制协议前先保留兼容 Converter 或提供迁移工具。
- 自动保存只保存当前已打开资源，不改变另存为目标。
- 文件扩展名可以包含多个点，但不要包含目录分隔符。
- 时间轴中的运行时对象引用应使用稳定 ID，不要保存 Editor 实例 ID。
