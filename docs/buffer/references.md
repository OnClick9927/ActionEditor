# 引用、委托与事件

## 共享引用和循环引用

默认 `SupportReferences = false`。同一个对象从两个字段访问时按值写两份，读取后是两个实例；如果访问路径形成循环，扫描阶段抛出明确异常。

开启引用模式后，每个实际引用对象分配稳定的本次写入 ID。第一次出现写对象内容，后续只写引用；读取端先注册实例再填充字段，因此支持自引用、互相引用、集合循环和共享数组。

```csharp
var a = new Node { name = "A" };
var b = new Node { name = "B" };
a.next = b;
b.next = a;

var settings = new BuffSettings { SupportReferences = true };
byte[] bytes = BuffSerializer.ToBytes(a, settings);
Node copy = BuffSerializer.FromBytes<Node>(bytes, settings);
Debug.Assert(ReferenceEquals(copy, copy.next.next));
```

引用 ID 只在单个负载内有效，不能作为存档实体 ID 或网络对象 ID。

## 委托

委托字段默认跳过，显式标记 `[Buffer]` 后可保存 invocation list。协议记录声明类型、程序集、方法名、稳定签名、泛型参数和绑定方式，支持：

- 静态方法。
- 当前包含对象的私有/公开实例方法。
- 其他对象类型的非静态方法。
- 多播委托。
- 已闭合泛型方法和可重建闭包对象。
- 某些 closed static null target 绑定。

动态方法、开放泛型方法和没有稳定声明类型的方法不能在应用重启后解析，会在写入时抛出 `NotSupportedException`。

当实例方法 target 已经位于正在扫描的对象图中，引用模式会复用同一对象定义，不再次展开一份 target，从而避免循环和身份分裂。target 不在根对象图中时，它会作为委托数据的一部分嵌入；这意味着 target 类型本身也必须可序列化。对网络协议而言，序列化可执行方法具有更高风险，应限制数据来源和允许类型。

## 事件

C# event 通常由私有 delegate 后备字段实现。只有能与事件对应、且符合字段采集规则的后备字段才可能被写入。`SerializeEvents = false` 会在本次写入完全跳过事件字段；构造函数中重新建立的订阅仍保留。

```csharp
public sealed class Signals
{
    [Buffer] public event Action<int> Changed;
    public void Raise(int value) => Changed?.Invoke(value);
}
```

不建议把进程级服务、窗口对象、线程同步对象或 Unity 临时对象作为普通委托 target。UnityEvent 使用专门 Converter 和 `IUnityObjectResolver`，见 [Unity 类型支持](unity.md)。

## 常见失败

- 循环引用未开启：扫描报 `Circular reference detected`。
- 方法重命名或签名变化：旧数据读取时无法解析 delegate method。
- target 类型已删除：类型解析失败。
- 使用动态生成方法：写入时拒绝，因为没有稳定元数据。
- 读取时没传自定义设置：target 或参数类型的自定义 Converter 缺失。
