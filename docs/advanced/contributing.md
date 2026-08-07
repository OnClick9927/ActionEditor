# 参与开发

## 目录责任

- `Assets/ActionAttribute/Runtime`：公开特性，不放 UnityEditor。
- `Assets/ActionAttribute/Editor`：internal Drawer、缓存和 fallback Inspector。
- `Assets/ActionBuffer/Runtime`：全平台核心，不引用 UnityEngine。
- `Assets/ActionBuffer/Unity`：所有 UnityEngine/Object/Event 扩展。
- `Assets/ActionEditor*/Runtime`：可保存数据和运行逻辑。
- `Assets/ActionEditor*/Editor`：窗口、GraphView、Inspector、设置和本地化。
- `Assets/Test`：回归、边界、性能与示例。

## 修改原则

- 不把 Editor 类型泄露到 Player asmdef。
- 不在 hot path 每帧做程序集扫描、反射或 LINQ。
- 序列化严格先 Scan 完成，再创建/初始化 Writer 并 Write。
- Reader/Writer/Scan 和高频 class 使用 ClassPool，finally 清理归还。
- GraphWindow 等稳定边界没有必要时不改，功能放到拥有它的视图组件。
- 行为树运行数据 private，外部只用树级 API。
- 帧同步通用节点不读取时间和随机数；整数规则必须明确。
- 新节点使用不重复、符合语义的 Resources 图标。
- 新 UI 文本通过框架 internal 本地化表，业务程序集不能注入框架词条。

## 代码和资源

保持现有 C# 格式和 UTF-8 中文注释。每个 Attribute 与对应 Drawer 单独文件。Unity 资源移动/重命名必须保留 meta；新增 png/uss/uxml 需确认导入设置和暗色主题表现。

## 提交要求

1. 说明用户可见行为和协议影响。
2. 新增或更新对应测试。
3. 运行 EditMode 测试和相关 1000 轮压力入口。
4. 检查 Unity Console、Editor.log 和 `git diff --check`。
5. 更新包 README 与 `docs/` 对应章节。
6. 不提交 Temp、Library、Logs、用户布局和本地缓存。

提交信息使用简短动词描述结果，例如 `完善序列化引用测试与文档`。一个提交可以包含同一目标所需的 Runtime、Editor、测试、资源和文档，但不要混入无关格式化。
