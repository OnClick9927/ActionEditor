# 安装与升级

## 环境要求

各包的 `package.json` 声明最低 Unity 2019.4。本仓库持续在 Unity 2021.3 LTS 下验证。GraphView、Inspector 和资源窗口属于 Editor 功能；ActionBuffer 核心与行为树 Runtime 可以在不加载 UnityEngine 的 .NET 程序集中使用。

## 依赖顺序

按下列顺序安装可以避免 asmdef 引用暂时丢失：

1. `ActionAttribute`
2. `ActionBuffer`
3. `ActionEditor`
4. `ActionEditor.Nodes`
5. `ActionEditor.Nodes.BT`

只使用序列化时安装 ActionBuffer 即可；需要 Unity 类型序列化时保留包内 `Unity` 目录。只使用 Inspector 特性时不需要其余四个包。

## Package Manager 安装

打开 `Window > Package Manager`，选择 `Add package from git URL`。仓库保留了独立 UPM 分支：

```text
https://github.com/OnClick9927/ActionEditor.git#upm_buffer
https://github.com/OnClick9927/ActionEditor.git#upm
https://github.com/OnClick9927/ActionEditor.git#upm_node
https://github.com/OnClick9927/ActionEditor.git#upm_bt
```

ActionAttribute 当前可从仓库子路径安装：

```text
https://github.com/OnClick9927/ActionEditor.git?path=/Assets/ActionAttribute#main
```

在团队项目中建议把 URL 写入 `Packages/manifest.json` 并固定 tag，而不是长期跟随分支。例如 ActionBuffer 可固定为 `upm_buffer_1.0.12`。不同包的 tag 前缀不同，提交前应确认五个包版本彼此兼容。

## 直接放入 Assets

也可以把需要的 `Assets/Action*` 目录放进项目。必须同时保留 `.asmdef`、`.meta`、Editor/Runtime 目录结构和 Resources 图标。不要把 Editor 脚本移动到 Runtime 目录，否则 Player 构建会引用 `UnityEditor`。

## 升级检查清单

- 备份所有 `.action.bytes`、`.bt.bytes` 和自定义 graph 数据。
- 检查具体资源类型的 `AssetFileExtensionAttribute` 是否变化。
- 检查 `[Buffer("协议名")]`、字段类型和自定义 Converter 的写入顺序。
- 打开一份旧资源并另存为副本，确认节点 GUID、连线与 Group 成员完整。
- 进入 Play Mode，验证行为树 `PrepareForRuntime`、事件、中断和状态快照。
- 运行 EditMode 测试；序列化协议至少对 Binary、JSON、YAML、XML 各做一次往返。

## 卸载

先删除依赖上层包，再删除底层包。卸载节点图前先把业务代码中继承 `GraphAsset`、`GraphNode<T>` 的类型迁走；卸载 ActionBuffer 前先转换二进制资源，否则项目将无法读取现有 `.bytes` 文件。`ProjectSettings/ActionEditorSettings.asset` 与 `ActionNodeEditorSettings.asset` 可以在彻底卸载后手工删除。
