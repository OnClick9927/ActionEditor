# 节点图编辑器

ActionEditor.Nodes 基于 `UnityEditor.Experimental.GraphView`，提供 GraphAsset 数据模型、GraphView 窗口、节点/端口/连线、Group、StickyNote 注释、MiniMap、TreeView 和独立历史。

## Graph 数据

`GraphAsset` 保存资源 GUID、视图 position/scale、NodeData、GroupData 和 ConnectionData。Connection 使用节点 GUID、端口类型和端口名称保存端点；`PrepareForRuntime()` 重建 PortData 和运行时连接引用。

`FindNode(guid)` 使用字典缓存。图结构改变或 Read/RegenerateGuids 后缓存会清理。运行前必须确保节点 GUID 唯一、连接端点存在，不能把编辑器 GraphElement 当运行时数据。

## 编辑器体验

- 右键空白处按 Node group 创建节点、Group 或注释。
- 注释使用 GraphView `StickyNote`，不继承 GraphNode；文本只在 Inspector 修改，尺寸根据最长未换行文本和实际行数自适应。
- MiniMap 使用图标按钮切换。
- 左侧行为树 TreeView 使用 Unity IMGUI TreeView，可折叠父节点、显示运行绿点/连线和子树深度字符。
- 左侧栏浮在 Graph 上，不把画布整体向右挤；宽度和显示状态通过 EditorPrefs 保留。
- resize 热区平时不可见，鼠标进入时显示白色反馈并使用左右缩放光标。

## 撤销与重做

节点图使用自己的快照历史。复制、移动、连接、删除、Inspector 修改和布局操作在完成边界记录，避免拖动过程中每帧产生一条。历史跳转恢复 Graph 数据、选择、视图变换和相关窗口状态；不把 `loaded` 中间态暴露给用户。

Ctrl+Z 后 GraphView 立即获得可操作状态，MiniMap 不销毁重建而闪烁。打开另一个资源会清空历史，防止跨文件应用快照。

## 另存为和 GUID

另存为会先把当前 GraphView 同步到 GraphAsset，然后调用 `RegenerateGuids()`：

- 资源 GUID 更新。
- 全部节点和 Group GUID 更新。
- Connection 的输入/输出 GUID 重映射。
- Group 成员 GUID 重映射。

普通保存不更改 GUID。不要用文件名或节点标题替代 GUID；它们可以重复或重命名。

## 设置和颜色

设置保存在 `ProjectSettings/ActionNodeEditorSettings.asset`，不注册 Project Settings 页面。节点类型颜色和 Port 类型颜色按当前 GraphAsset 具体类型保存，设置弹窗只展示当前图的分区。行为树是 GraphAsset 的一种，因此拥有独立颜色集。
