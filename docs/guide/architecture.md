# 模块与程序集

## 依赖方向

```text
ActionAttribute
      ↑
ActionBuffer ← ActionEditor ← ActionEditor.Nodes ← ActionEditor.Nodes.BT
```

编辑器程序集只依赖同包 Runtime 和必要的下层包。业务运行时不应引用任何 `*.Editor` asmdef。

## Editor 与 Runtime 隔离

ActionAttribute 的运行时目录只定义 Attribute 和轻量配置；PropertyDrawer、反射缓存、脚本定位和 fallback Inspector 在 Editor 中。ActionBuffer 核心 asmdef 使用引擎无关实现，Unity 类型支持放在 `Assets/ActionBuffer/Unity` 并由 Unity 编译符号隔离。Timeline 与节点图的资源模型可序列化，窗口、GraphView 和工具栏只存在于编辑器。

## 数据流

Timeline 的层级是 `Asset -> Group -> Track -> Clip`。Graph 的层级是 `GraphAsset -> NodeData/GroupData/ConnectionData`。行为树在 Graph 数据基础上调用 `PrepareForRuntime`，重建端口与连接，再将节点连接转换为运行时父子关系。

ActionBuffer 写入数据时先解析根对象 Converter，创建 `BufferScan` 并遍历实际对象图。Scan 记录字段值、集合快照、引用 ID、类型元数据和 Converter 临时值；只有扫描完整通过后才初始化 Writer 并按同样顺序消费缓存。读取端直接由 Reader 和 Converter 重建对象图。

## 编辑器状态

- Timeline 设置：`ProjectSettings/ActionEditorSettings.asset`
- Node/BT 设置：`ProjectSettings/ActionNodeEditorSettings.asset`
- 左侧 TreeView 是否展示和宽度：EditorPrefs
- 撤销/重做：窗口自己的历史栈，不进入 Unity Undo
- 当前文件有未保存修改：标题后追加 `*`

这两个设置文件是内部 `ScriptableSingleton` 数据，不注册 Project Settings Provider，因此不会出现在 Unity Project Settings 左侧列表。颜色配置按当前资源具体类型分区，切换 Graph 或 Timeline 类型不会互相覆盖。

## 文件后缀

`AssetFileExtensionAttribute` 可继承，允许多段扩展名。Timeline 基类默认 `action.bytes`，行为树默认 `bt.bytes`，普通 `GraphAsset` 未声明时回退到 `bytes`。创建、打开、搜索和另存为必须统一通过 `AssetFileExtensionUtility`，不要自行使用 `Path.GetExtension` 判断多段后缀。
