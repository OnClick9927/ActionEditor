# 自定义节点与端口

## 节点数据

```csharp
[System.Serializable]
[Name("打印消息", "运行时输出一条配置文本。")]
[Node("行为")]
[Attachable(typeof(QuestGraph))]
[Icon("Console")]
public sealed class PrintNodeData : NodeData
{
    [NodePort(NodePortAttribute.Direction.Input)]
    public Flow input;

    [NodePort(NodePortAttribute.Direction.Output, single: false)]
    public Flow output;

    [Name("文本")]
    public string message;
}
```

`Node` 的 group 决定右键菜单分类，`Attachable` 限制可创建的 GraphAsset，`Name` 与 `Icon` 决定标题和图标。NodeData 构造函数创建 GUID 和默认位置。

## 自动端口

GraphNode 会缓存带 `NodePortAttribute` 的字段并自动生成端口。默认端口类型取字段类型，也可设置 attribute 的 `type`；`single=true` 对应 GraphView `Port.Capacity.Single`。连接兼容性由端口类型决定，端口名称进入持久协议，重命名会让旧 Connection 找不到端口。

## 自定义视图

```csharp
public sealed class PrintNodeView : GraphNode<PrintNodeData>
{
    public override void OnCreated(NodeGraphView graphView)
    {
        base.OnCreated(graphView);
        // 只有自动端口不够时才手工 GeneratePort。
    }

    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();
    }
}
```

`data` 是强类型 NodeData，`Data` 是基类视图，`view` 是当前 NodeGraphView，`asset` 是当前顶层 GraphAsset。节点视图不应通过静态窗口查找当前资源。标题最小宽度会根据名称测量，Inspector 可见时节点宽度还会适配面板。

## 运行时准备

调用 `GraphAsset.FromBytes(type, data)` 后框架先清理 null；执行图前调用 `PrepareForRuntime()` 重建端口和连接。业务执行器从 `nodes`、`connections` 或 PortData 遍历。编辑器中的选择、颜色、VisualElement 和 Edge 不存在于 Player。

## Group 与注释

GroupData 的 nodes 保存成员 GUID，只用于组织，不自动改变执行语义。StickyNote 注释应使用专门数据和 GraphView StickyNote 视图，不伪装成 GraphNode；这样运行时节点枚举不会混入说明文本。

## 性能建议

- 节点类型/字段/端口反射使用缓存，不在 Repaint 重扫程序集。
- 批量修改时合并历史记录和 Repaint。
- 连接查询频繁时建立 GUID/端口索引。
- 大图只刷新脏节点颜色和 Inspector，不全量重建 GraphView。
- 自定义 OnInspectorGUI 避免每帧 AssetDatabase 搜索。
