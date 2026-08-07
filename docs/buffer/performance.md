# 格式、兼容与性能

## 格式选择

| 格式 | 适用 | 代价 |
| --- | --- | --- |
| Binary | 网络、存档、运行时高频 | 不便人工检查，协议改动必须严格版本化 |
| JSON | 调试、配置、跨语言 | 文本更大，解析和转义成本高 |
| YAML | 人工编辑的层级配置 | 空白与标量规则更复杂，通常最慢 |
| XML | 既有工具链、强结构集成 | 标记冗余，负载体积较大 |

四种格式共享 Converter 和对象模型语义，但文本表示不是 Binary 的逐字节镜像。协议测试应对所有实际启用的格式分别执行。

## 类型元数据

Binary Writer 的 `CollectMeta` 只消费 Scan 实际访问到的类型与字段，不再预先搜集所有可能子类，因此多态基类不会让无关类型膨胀元数据。关闭 `TypeInfo` 可以减小固定类型协议，但声明类型为接口/抽象类或实际类型变化时无法可靠恢复。

## 分配来源

- 返回 `byte[]` 或 `string` 必然创建归调用者所有的新对象。
- 首次访问类型会构建反射字段缓存和 Converter 缓存，预热后成本更稳定。
- Scan 缓存字段、对象、集合快照和 Converter 临时值；大对象图的峰值内存与实际访问节点数相关。
- 具体 Reader/Writer 通过各自公开的静态 `Get()` 和 `Back()` 管理池生命周期；`BufferScan`、列表和 CachedField 等内部对象由框架池管理。
- 超大缓存超过保留容量时会在归还时释放，避免对象池永久持有峰值内存。

## 高频使用建议

1. 复用 `BuffSettings` 和自定义 Converter 实例。
2. 启动阶段预热常用消息类型。
3. DTO 使用稳定字段，避免昂贵 getter；属性 getter 会在 Scan 被调用。
4. 不在 Converter 中使用 LINQ、闭包和临时反射。
5. 大字典只在协议需要时开启稳定排序。
6. 网络热路径使用 Binary，日志/调试按采样转 JSON。
7. 对峰值负载设置合理上限，防止合法但异常大的对象图推高内存。

手动调用 `WriteObject` 或 `ReadObject` 时必须归还实例：

```csharp
var writer = BufferWriter.Get();
try
{
    BuffSerializer.WriteObject(writer, value, settings);
    byte[] bytes = writer.GetValidBuffer();
}
finally
{
    BufferWriter.Back(writer);
}
```

## 基准和回归

仓库测试记录四格式 8 次往返的耗时与当前线程分配，并设 30 秒/512 MiB 的宽松失败阈值。另有 1000 轮混合格式压力测试，覆盖池复用、标记属性、共享/循环引用和三维/五维数组；重复写入测试验证同一输入不会因池中残留状态改变输出。

性能数字只在同一 Unity、运行时、构建配置和机器上有比较意义。提交优化时应报告预热轮数、对象规模、迭代数、GC 统计口径和格式，不要只报告单次 Stopwatch 数值。

## 协议兼容

- 使用 `[Buffer("稳定名")]` 固定协议成员名。
- 自定义 Converter 写明确版本。
- 类型、程序集或 delegate 方法重命名前提供迁移。
- 另存为 Graph 会重建资源、节点和 Group GUID，并重映射连接；普通保存不会。
- 不把 Editor InstanceID、临时路径或对象哈希作为跨会话 ID。
