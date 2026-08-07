# ActionEditor

ActionEditor 是一组可以独立组合的 Unity 工具包，覆盖 Inspector 增强、通用序列化、Timeline 风格编辑器、GraphView 节点图和引擎无关行为树。编辑器负责创建与检查数据，运行时程序集只保留可执行的数据结构和协议。

## 包组成

| 包 | 用途 | 运行时是否依赖 UnityEngine |
| --- | --- | --- |
| ActionAttribute | 100+ 个可组合 Inspector 特性与统一绘制器 | 特性程序集不依赖 UnityEditor，部分字段类型依赖 UnityEngine |
| ActionBuffer | Binary、JSON、YAML、XML 统一序列化 | 核心不依赖；`Unity/` 扩展按编译符号隔离 |
| ActionEditor | Timeline 风格的资源、轨道、片段编辑框架 | 数据层可独立使用，Editor UI 仅在 Unity 中存在 |
| ActionEditor.Nodes | GraphView 节点、端口、连线、分组和注释 | 数据层与 GraphView 编辑器隔离 |
| ActionEditor.Nodes.BT | 通用行为树运行时及其图编辑器 | 行为树 Runtime 不依赖 UnityEngine |

## 设计重点

- ActionBuffer 写入严格分为 `Scan` 和 `Write` 两阶段，扫描完成后才初始化 Writer。
- 序列化支持一至五维数组、共享引用、循环引用、显式委托和事件。
- `GraphAsset` 与 Timeline `Asset` 由具体类型声明文件后缀，资源选择器只展示匹配类型。
- 图编辑器和 Timeline 使用自己的历史记录，不依赖 Unity Undo；文件未保存时标题显示 `*`。
- 行为树以整数 Tick 驱动，状态快照只由 `List<int>` 组成，适合回滚和帧同步。
- 编辑器设置保存在 `ProjectSettings/*.asset`，但不占用 Unity 的 Project Settings 页面。

## 从哪里开始

首次使用请先阅读[安装与升级](guide/installation.md)，再按[五分钟上手](guide/quick-start.md)创建一份可保存、可打开、可执行的示例资源。已有项目升级时先看[迁移与协议演进](advanced/migration.md)，尤其不要直接修改已发布二进制协议的字段名、类型或 Converter 布局。
