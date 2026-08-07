# 故障排查

## `.action.bytes` 双击没有打开 Timeline

确认具体类型继承 `ActionEditor.Asset`，后缀由 `AssetFileExtensionUtility.Get(type)` 返回且文件名完整匹配。多段后缀不能用 `Path.GetExtension`。等待脚本编译完成后重新导入资源；若类型无法从文件内容解析，先从窗口的 Open 搜索选择具体 Asset 类型。

## 搜索框出现其他 `.bytes`

检查资源选择器是否按候选具体类型调用 `AssetFileExtensionUtility.Matches`，而不是只过滤最后一个扩展名。BT 应只显示 `.bt.bytes`，Timeline 默认只显示 `.action.bytes`。

## Inspector 字段重叠

先检查 Unity Console 是否有 `BeginProperty/EndProperty` 栈异常。动态条件在 Layout 和 Repaint 必须返回一致结果；Preview、ProgressBar、HelpBox、列表警告的高度由中央 Drawer 统一计算。自定义 GUI 不要在 Rect PropertyDrawer 内使用 EditorGUILayout。

## Inspector 没有 Script 行

fallback Inspector 默认绘制只读 Script ObjectField，可双击定位。确认类型没有 `[HideMonoScript]`，CustomEditor 是否调用了 ActionAttribute 渲染/base，脚本对象能由 MonoScript/AssetDatabase 找到。Timeline/BT 自定义 Inspector 应先画 Script，再画标题和 TypeInfoBox。

## TypeInfoBox 不显示或重复

派生类声明会覆盖继承查找；没有声明时才从基类继承。完全自定义 OnInspectorGUI 可能绕过类型头。不要在自定义 Inspector 手工再画同一 TypeInfoBox。

## FolderPath 报错或没有生效

字段必须是 string。工程相对模式选择项目外目录时不能转换为 Assets 路径；需要外部目录使用 `FolderPath(absolutePath: true)`。路径选择后不要把返回值再次传给只接受绝对路径的 API。

## InlineButton 看不到

方法名用 `nameof`，方法应位于当前字段 owning object，参数签名必须受支持。Inspector 太窄时按钮会保留最小宽度并压缩字段；确认没有 HideIf 和禁用 Group 把整行隐藏。

## 黑板区域高度不能调整

BT 左侧面板由 Graph 区、垂直 splitter 和 Blackboard 区组成。拖动“黑板”标题上方边界；高度保存在 EditorPrefs。若布局卡住，检查控制 ID 是否在 MouseDown/Drag/Up 正确释放 hotControl。

## Ctrl+Z 后要再点一下才能操作

历史恢复必须同步 Graph 数据、重建视图、恢复选择/焦点并 Repaint。不要恢复到 loaded 中间状态，也不要销毁重建 MiniMap。打开新文件必须清空旧历史。

## 序列化循环引用错误

如果业务要求对象身份，写读两端使用同一 `BuffSettings { SupportReferences = true }` 语义。关闭引用时循环应报错，不能通过提高 MaxDepth 解决。意外循环通常来自 parent/back-reference、事件 target 或集合包含自身。

## Unity Object 在 Player 找不到

自动 ID 只支持同一 resolver 生命周期。跨重启对象必须 `Register` 业务稳定 ID，或 `RegisterResource`。反序列化前先注册场景对象；Editor 的 AssetDatabase resolver 不可用于 Player。

## BT 快照读取失败

检查树资源版本、节点顺序、节点配置和 Semaphore 列表是否完全相同。截断、多余值、非法索引都会被拒绝。不要手工编辑 List<int>，也不要把一个子树的快照读到另一布局。
