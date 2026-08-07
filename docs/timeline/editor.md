# Timeline 资源与编辑器

## 数据层级

ActionEditor 的数据结构是：

```text
Asset
└─ Group
   └─ Track
      └─ Clip / ClipSignal
```

`IAction` 提供 Length、StartTime、EndTime。`ISegment` 增加 Root、Parent、Children、IsActive、IsLocked。Asset 验证时会清理 null、重建父级引用并按有效 Clip 的最大结束时间计算总长度。

Group 统一控制一组 Track 的折叠、锁定和启用；Track 的有效/锁定状态叠加父 Group；Clip 保存开始时间和长度。`ClipSignal` 长度固定为 0，只表示一个触发时刻。

## 文件与打开

Timeline `Asset` 默认 `[AssetFileExtension("action.bytes")]`。具体类型可以覆盖为多段后缀：

```csharp
[AssetFileExtension("skill.action.bytes")]
public sealed class SkillTimeline : Asset { }
```

双击匹配资源会由公开的资源打开处理器路由到 Timeline 编辑器。搜索和资源选择按候选具体类型过滤，不混入其他 `.bytes` 文件。另存为保持按钮位置和图标语义，只创建新的资源文件。

## 常用操作

- 工具栏新建、打开、保存、另存为和定位 Project 资源。
- 时间标尺拖动播放头；帧模式按 FrameRate 显示和吸附。
- Group/Track 左侧控制启用、锁定与折叠。
- 拖动 Clip 改变开始时间，拖边调整长度；相邻可混合 Clip 显示 Blend。
- Inspector 顶部保留 Script 定位行，随后绘制类型名、TypeInfoBox 和业务字段。
- 设置按钮只显示当前 Asset 具体类型的 Track/Clip 颜色。

## 历史记录

Timeline 使用独立历史栈，不调用 Unity Undo。`Ctrl+Z` 撤销，`Ctrl+Shift+Z` 重做；历史弹窗按行展示操作并支持点击跳转。切换文件会清空旧文件历史，初始历史点在资源加载和布局初始化完成之后，不能回到会改变节点/片段位置的 Loaded 中间态。

恢复历史后窗口立即重建数据和选中状态，不要求用户额外点击 Graph/Timeline 才继续操作。文件数据改变后标题加 `*`，保存成功后清除。

## 设置

Timeline 设置保存在 `ProjectSettings/ActionEditorSettings.asset`，不出现在 Unity Project Settings 页面。颜色按当前 Asset 具体类型分区，打开另一种 Timeline 时只展示并修改该类型设置。旧 JSON 设置只做一次迁移。

## 保存与运行时

`Asset.ToBytes()` 使用 ActionBuffer，`Asset.FromBytes(type, bytes)` 读取后执行 Validate。运行时播放协议由业务扩展；框架不强制 MonoBehaviour Player。需要跨版本保存时给自定义字段使用稳定 `[Buffer("name")]`，并避免在 Clip 中持有 Editor-only 对象。
