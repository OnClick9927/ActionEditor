# 自定义 Converter

## 模板方法

继承 `BuffConverter<T>` 实现三个 protected 方法：

```csharp
public sealed class IntPairConverter : BuffConverter<IntPair>
{
    protected override void OnScan(BufferScan scan, IntPair value)
    {
        // 两个 int 是原子值，无额外对象需要扫描。
    }

    protected override void OnWrite(
        IBufferWriter writer, BufferScan scan, IntPair value)
    {
        writer.WriteInt32(value.x);
        writer.WriteInt32(value.y);
    }

    protected override IntPair OnRead(IBufferReader reader, Type type)
    {
        return new IntPair(reader.ReadInt32(), reader.ReadInt32());
    }
}
```

纯原子类型可以继承 `AtomicBuffConverter<T>`，它提供空扫描实现。注册：

```csharp
var settings = new BuffSettings();
settings.RegisterConverter(new IntPairConverter());
```

`Read`、`Scan`、`Write` 是 internal 框架调度方法，扩展程序集只能通过 protected `OnRead/OnScan/OnWrite` 定制，并通过 `BuffSerializer` 发起操作。

## 顺序契约

OnScan 对每个嵌套值的访问次数和顺序，必须与 OnWrite 消费 scan 的顺序一致。例如集合在 Scan 中调用 `scan.ScanEnumerable`，Write 中调用 `writer.WriteIEnumerable`。不要在 Write 重新枚举源集合或再次读取业务属性，否则源对象变化会破坏布局并增加 GC。

读取顺序必须与写入顺序完全一致。已经发布的顺序不能直接调整；新增字段应通过版本字段或全新的 Converter 协议处理。

## 集合 Converter

框架已有 `IEnumerableConverter<T,TCollection>` 基类和 Reader/Writer 集合方法。需要自定义排序、比较器或容器构造方式时，建议保存：

1. 版本号。
2. comparer 的稳定业务标识。
3. 元素数量。
4. 固定顺序的元素。

`DeterministicCollectionOrder` 不能为任意对象发明稳定顺序；元素没有可比较稳定键时框架应拒绝而不是依赖哈希迭代顺序。

## Converter Factory

同一基类有大量派生类型时：

```csharp
settings.RegisterConverterFactory(typeof(MessageBase), actualType =>
    CreateMessageConverter(actualType));
```

factory 返回值必须实际继承 `BuffConverter<actualType>`。结果会在该 settings 中缓存。更具体的注册优先于宽泛基类；同一基类再次注册会替换 factory 并清空派生缓存。

## 版本建议

- 在 Converter 开头写小整数版本。
- 读取端至少保留一个发布周期的旧版本分支。
- 数字采用明确宽度，不把 int 改为 long 而不升版本。
- 文本协议同样是协议，不要只把 Binary 当成需要兼容的数据。
- 自定义 Converter 应有四格式往返测试、null/空集合测试、边界值测试、损坏数据测试和重复池化测试。
