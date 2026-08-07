# ActionEditor.Nodes

[在线完整文档](https://onclick9927.github.io/ActionEditor/#/graph/editor) · [自定义节点与端口](../../docs/graph/extensions.md) · [故障排查](../../docs/advanced/troubleshooting.md)

ActionEditor.Nodes 基于 Unity GraphView 提供通用节点图编辑器，包括节点、端口、连线、分组、注释、MiniMap、树形辅助视图和独立撤销历史。

## 定义图资源

```csharp
using ActionAttribute;
using ActionEditor;
using ActionEditor.Nodes;

[Name("任务图")]
[AssetFileExtension("quest.graph.bytes")]
public sealed class QuestGraph : GraphAsset
{
}
```

具体 `GraphAsset` 使用 `AssetFileExtensionAttribute` 决定文件后缀。未声明时使用 `bytes`，可按资源类型覆盖。所有资源选择器会按候选类型的扩展名过滤。

## 定义节点与视图

```csharp
[System.Serializable]
[Name("打印消息"), Node("行为"), Attachable(typeof(QuestGraph))]
public sealed class PrintNodeData : NodeData
{
    public string message;
}

public sealed class PrintNodeView : GraphNode<PrintNodeData>
{
}
```

`GraphNode<T>.data` 是当前节点数据；继承得到的 `asset` 指向当前正在编辑的顶层 `GraphAsset`，`view` 指向当前图视图。端口可通过 `NodePortAttribute` 自动生成，也可在 `OnCreated` 中显式生成。

## 注释与分组

右键空白区域创建注释。注释继承 GraphView 的 StickyNote，不是 `GraphNode`，数据只在右侧 Inspector 编辑；节点尺寸根据未换行文本宽度和实际行数重新计算。Group 用于组织节点，不改变运行时执行关系。

## 撤销与另存为

撤销历史保存图数据、选择、Inspector 滚动和视图变换。另存为前会同步当前 GraphView，然后为资源、全部节点和分组创建新 GUID，并重映射连线端点与分组成员，复制文件不会再共享节点身份。

## 编辑器设置

设置通过节点图工具栏的设置按钮编辑，存储到：

`ProjectSettings/ActionNodeEditorSettings.asset`

该设置不注册到 Unity Project Settings 页面。节点及 Port 颜色只在图编辑器的设置弹窗中展示，并按当前打开的 `GraphAsset` 具体类型分别保存。旧 `Assets/Editor/NodeGraph.txt` 仅用于一次迁移，不再写入。

## 注意事项

- GUID 是资源协议的一部分，不要手工复用或按节点名称生成。
- 修改端口名称或类型会影响旧连线恢复，应提供迁移逻辑。
- GraphView 属于 Editor API，运行时程序集只应依赖 `GraphAsset`/`NodeData` 数据。
- 大量节点时避免在 `Update` 中反复调用 LINQ 和反射；缓存类型元数据并按脏标记刷新视图。
