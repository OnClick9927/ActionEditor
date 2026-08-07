# ActionEditor

ActionEditor 是一组可组合的 Unity 工具包：Inspector 特性、全平台序列化、Timeline 风格编辑器、GraphView 节点图和引擎无关行为树。Runtime 与 Editor 按程序集隔离，ActionBuffer 核心和行为树运行时可以脱离 UnityEngine 使用。

## 文档

- [在线完整文档](https://onclick9927.github.io/ActionEditor/)
- [仓库内文档首页](docs/README.md)
- [安装与升级](docs/guide/installation.md)
- [ActionBuffer 序列化](docs/buffer/serialization.md)
- [104 个 Inspector 特性手册](docs/attribute/catalog.md)
- [Timeline 编辑器](docs/timeline/editor.md)
- [节点图编辑器](docs/graph/editor.md)
- [行为树运行时与内置节点](docs/bt/runtime.md)
- [测试、迁移和故障排查](docs/advanced/testing.md)

文档站参考 [WooAsset 文档](https://github.com/OnClick9927/WooAsset/tree/main/docs) 的 Docsify 组织方式，所有章节按本仓库当前代码重新编写。

## 包

| 包 | 当前版本 | 内容 |
| --- | --- | --- |
| `com.woo.actionattribute` | 1.0.1 | 104 个可组合 Inspector Attribute 和统一 Editor Drawer |
| `com.woo.actionbuffer` | 1.0.12 | Binary/JSON/YAML/XML、引用、delegate/event、Unity 类型扩展 |
| `com.woo.actioneditor` | 1.1.20 | Timeline Asset/Group/Track/Clip 与编辑器 |
| `com.woo.actionnodes` | 1.0.37 | GraphAsset、GraphView、注释、MiniMap、历史记录 |
| `com.woo.actionbt` | 1.0.16 | 引擎无关行为树、整数 Tick 节点、状态快照 |

安装顺序是 ActionAttribute -> ActionBuffer -> ActionEditor -> ActionEditor.Nodes -> ActionEditor.Nodes.BT。独立 UPM 分支仍为 `upm_buffer`、`upm`、`upm_node`、`upm_bt`，详见[安装文档](docs/guide/installation.md)。

## 关键能力

- ActionBuffer 严格执行完整 `Scan` 后再初始化 Writer 和 `Write`，只采集实际对象图元数据。
- 支持公开字段、显式 `[Buffer]` 私有字段和属性、一至五维数组、共享/循环引用、delegate 与 event。
- Unity 扩展支持所有 `UnityEngine.Object` 子类、UnityEvent 和大量常用 Unity 值类型，Player 使用稳定 ID resolver。
- Timeline 和 Graph 文件后缀由具体 Asset 类型声明；BT 默认 `.bt.bytes`，Timeline 默认 `.action.bytes`。
- Graph/Timeline 使用独立可跳转历史，不依赖 Unity Undo；另存为 Graph 会重建全部 GUID 并重映射连接。
- 行为树 Runtime 不依赖 UnityEngine，运行状态递归收集为 `List<int>`，支持快速回滚恢复。
- Editor GUI 内建简体中文/English 本地化，设置保存在 ProjectSettings 资源但不占用 Project Settings 页面。

## 验证

仓库包含 ActionBuffer、Unity 类型、ActionAttribute 和行为树 EditMode 测试。本次核心独立验证展开执行 132 个测试实例，0 失败；其中序列化混合格式和行为树状态恢复分别连续测试 1000 轮。

## 截图

![ActionEditor](https://github.com/user-attachments/assets/c77113b9-ae30-4e16-9941-5bf65133c15a)

<img width="1412" height="593" alt="ActionEditor Node Graph" src="https://github.com/user-attachments/assets/231ff457-6573-4a93-8c55-55c24b9ae277" />

## 来源

本项目基于 [NoBugCn/ActionEditor](https://github.com/NoBugCn/ActionEditor) 持续扩展。
