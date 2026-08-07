# 序列化基础

## 统一入口

`BuffSerializer` 提供四组高层 API：

| 格式 | 写入 | 读取 | 返回类型 |
| --- | --- | --- | --- |
| Binary | `ToBytes` | `FromBytes<T>` / `FromBytes(bytes, type)` | `byte[]` |
| JSON | `ToJson` | `FromJson<T>` / `FromJson(text, type)` | `string` |
| YAML | `ToYaml` | `FromYaml<T>` / `FromYaml(text, type)` | `string` |
| XML | `ToXml` | `FromXml<T>` / `FromXml(text, type)` | `string` |

`DeepCopyByBuffer<T>` 是 Binary 往返的便捷扩展。根对象不能为 null，因为写入端需要运行时类型；对象内部字段、属性和集合元素可以为 null。

## 字段和属性规则

- 公开实例字段默认写入。
- 非公开字段只有标记 `[Buffer]` 才写入，或开启 `FullField`。
- 属性无论访问级别如何，都必须标记 `[Buffer]`。
- 属性必须有无参数 getter 和 setter；索引器跳过。
- 委托/事件默认只有显式标记 `[Buffer]` 才作为成员采集。
- `[NonSerialized]` 遵循类型字段缓存规则，不应和 `[Buffer]` 混用表达矛盾意图。
- `[Buffer("wireName")]` 固定协议成员名，重命名 C# 成员时可保持数据兼容。

```csharp
public sealed class Account
{
    public int id;
    [Buffer] private string token;
    [Buffer("display_name")] public string DisplayName { get; private set; }
    public int RuntimeOnly { get; set; } // 未标记属性，不写入
}
```

## 支持的基础类型

内置 Converter 覆盖布尔、全部整数、`char`、`float`、`double`、`decimal`、`string`、枚举、`Guid`、`DateTime`、`TimeSpan`、可空值类型、普通结构体和普通类。抽象类、接口、基类字段可以在 `TypeInfo` 开启时保存实际具体类型。

集合包括数组、一至五维多维数组、`List<T>`、常用泛型集合接口、`Dictionary`、`ConcurrentDictionary`、`HashSet`、`Queue`、`Stack`、`ArraySegment`、`KeyValuePair`、`ArrayList`、`Hashtable`，以及拥有可构造具体类型的常见集合子类。

自定义 comparer 可能改变相等性和排序语义，框架会拒绝无法可靠重建的 comparer，而不是静默替换成默认 comparer。需要这类集合时应编写 Converter，显式保存 comparer 身份或业务排序键。

## Scan 与 Write

写入一次对象的实际流程是：

1. 根据根运行时类型和 `BuffSettings` 解析 Converter。
2. 从池中取得 `BufferScan`。
3. Converter 递归扫描实际访问对象，缓存字段值、集合内容、引用关系和元数据。
4. 扫描完整结束后调用 `writer.Init(scan)`。
5. Converter 按扫描顺序写入，Writer 从 scan 消费缓存。
6. 校验 Converter 临时值已全部消费，清理并归还对象池。

因此枚举器只在 Scan 阶段访问一次，Write 阶段不会再次读取可能变化的业务对象。自定义 Converter 的 `OnScan` 和 `OnWrite` 必须保持完全一致的值顺序。

## 回调

实现 `IBufferObject` 可以接收：

```csharp
public interface IBufferObject
{
    void BeforeWriteBuffer();
    void AfterReadBuffer();
}
```

`BeforeWriteBuffer` 在扫描对象字段前调用，可用于刷新派生数据。Binary Reader 会先完成负载校验和延迟引用解析，再执行读取回调，避免损坏数据触发半成品对象的业务逻辑。回调不应修改已经扫描过的其他对象，也不应递归发起同一对象的序列化。

## 低层 Reader/Writer

特殊协议可以创建实现 `IBufferWriter` / `IBufferReader` 的类型并调用 `WriteObject` / `ReadObject`。Writer 必须实现统一 `Init(BufferScan)`；调用者不应直接调 Converter 的 `Read/Scan/Write`，这些方法是 internal 调度接口。一般业务优先使用 `To*` 和 `From*`，因为它们负责池化、清理、完整消费校验和异常路径回收。
