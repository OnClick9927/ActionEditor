# ActionEditor

ActionEditor 是一组可组合的 Unity 工具包，包含 Inspector 特性、全平台序列化、Timeline 编辑器、GraphView 节点图和引擎无关行为树。

## UPM 安装

在 Unity 中打开 `Window > Package Manager`，选择 `Add package from git URL`，复制所需包的安装地址：

| 包 | UPM 安装 URL |
| --- | --- |
| ActionAttribute | `https://github.com/OnClick9927/ActionEditor.git#upm_attribute` |
| ActionBuffer | `https://github.com/OnClick9927/ActionEditor.git#upm_buffer` |
| ActionEditor | `https://github.com/OnClick9927/ActionEditor.git#upm` |
| ActionEditor.Nodes | `https://github.com/OnClick9927/ActionEditor.git#upm_node` |
| ActionEditor.Nodes.BT | `https://github.com/OnClick9927/ActionEditor.git#upm_bt` |

各包可按需安装；只使用序列化时安装 ActionBuffer，只使用 Inspector 特性时安装 ActionAttribute。使用完整编辑器套件时按上述顺序添加。

## 文档

- [在线完整文档](https://onclick9927.github.io/ActionEditor/)
- [文档首页](docs/README.md)
- [安装与升级](docs/guide/installation.md)
- [ActionBuffer 序列化](docs/buffer/serialization.md)
- [104 个 Inspector 特性手册](docs/attribute/catalog.md)
- [Timeline 编辑器](docs/timeline/editor.md)
- [节点图编辑器](docs/graph/editor.md)
- [行为树与内置节点](docs/bt/runtime.md)

## 包

| 包 | 内容 |
| --- | --- |
| `com.woo.actionattribute` | Inspector Attribute 和统一 Editor Drawer |
| `com.woo.actionbuffer` | Binary、JSON、YAML、XML 序列化 |
| `com.woo.actioneditor` | Timeline Asset、Group、Track、Clip 与编辑器 |
| `com.woo.actionnodes` | GraphAsset、GraphView、注释、MiniMap、历史记录 |
| `com.woo.actionbt` | 引擎无关行为树、整数 Tick 节点、状态快照 |

完整安装顺序是 ActionAttribute -> ActionBuffer -> ActionEditor -> ActionEditor.Nodes -> ActionEditor.Nodes.BT，详细说明见[安装文档](docs/guide/installation.md)。

## 关键能力

- 支持 Inspector 字段分组、条件显示、字段约束、预览、按钮、路径选择等特性。
- 支持 Binary、JSON、YAML、XML 格式，以及字段、`[Buffer]` 属性、多维数组、引用、delegate、event 和自定义 Converter。
- 支持 `UnityEngine.Object`、UnityEvent 和常用 Unity 值类型序列化。
- 支持 Timeline、GraphView、注释、MiniMap、操作历史和自定义资源后缀。
- 支持引擎无关行为树、整数 Tick 节点、事件、中断、信号量和运行状态快照。
- 支持简体中文和 English 编辑器界面。

## 截图

![ActionEditor](https://github.com/user-attachments/assets/c77113b9-ae30-4e16-9941-5bf65133c15a)

<img width="1412" height="593" alt="ActionEditor Node Graph" src="https://github.com/user-attachments/assets/231ff457-6573-4a93-8c55-55c24b9ae277" />
