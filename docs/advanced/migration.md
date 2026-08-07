# 迁移与协议演进

## ActionBuffer 字段变更

兼容改名优先保留协议名：

```csharp
// C# 从 displayName 改为 title，数据字段名仍保持 display_name。
[Buffer("display_name")]
public string title;
```

删除字段时旧 Reader 会忽略未知成员还是拒绝，取决于格式和对象读取规则；不要只凭 JSON 手工测试推断 Binary。字段类型变化、集合元素类型变化和 Converter 顺序变化都应提升协议版本并提供旧数据读取分支。

## 类型和程序集改名

开启 TypeInfo 后会保存类型与程序集信息。移动 namespace、修改 asmdef 名称或删除派生类会影响多态与 delegate 解析。迁移步骤：

1. 旧版本读取旧数据。
2. 转换成独立 DTO 或新类型。
3. 用新版本写回。
4. 保留一段时间的旧版本离线迁移工具。

不要依赖已移除的 `BufferFormerNameAttribute`；稳定协议名由 `[Buffer("name")]` 管理。

## 文件后缀迁移

Timeline、Graph 和 BT 后缀来自 `AssetFileExtensionAttribute`。行为树标准后缀是 `.bt.bytes`，Timeline 标准后缀是 `.action.bytes`。修改具体类型后缀时：

- 批量重命名文件并保留 `.meta` GUID。
- 更新搜索、构建收集和 Addressables/WooAsset 标签规则。
- 验证双击路由到正确窗口。
- 验证搜索弹窗不会出现其他后缀。

`Path.GetExtension` 对多段后缀只返回 `.bytes`，必须使用 `AssetFileExtensionUtility.Matches`。

## Graph GUID

另存为会为 Asset、Node 和 Group 生成新 GUID，并重映射 Connection/Group。复制文件系统文件不是等价替代：它可能保留业务 GUID。需要模板克隆时调用框架另存为流程或 `RegenerateGuids()`，再完整保存。

## 设置迁移

旧 JSON 设置迁移到：

- `ProjectSettings/ActionEditorSettings.asset`
- `ProjectSettings/ActionNodeEditorSettings.asset`

它们不出现在 Project Settings UI。颜色由全局类型键改为 Graph/Timeline 具体类型分区后，第一次打开各类型应检查默认颜色；Port 颜色也按当前图类型独立保存。

## 行为树状态版本

状态快照没有自描述类型表。新增/删除/重排节点，或改变 OnCollectStatus 数量，都会改变布局。外层回放/回滚数据必须保存树资源版本或内容哈希。版本不一致时丢弃旧快照并从权威状态重建，不能尝试按长度猜测。
