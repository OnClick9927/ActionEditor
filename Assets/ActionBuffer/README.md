# ActionBuffer

[在线完整文档](https://onclick9927.github.io/ActionEditor/#/buffer/serialization) · [设置与安全](../../docs/buffer/settings.md) · [Unity 类型支持](../../docs/buffer/unity.md)

ActionBuffer 提供二进制、JSON、YAML 和 XML 的统一序列化流程。每次写入严格执行 `Scan -> 创建 Writer -> Write`：扫描阶段收集实际访问对象、引用关系和需要的类型元数据，写入阶段只消费扫描结果。

## 基础用法

```csharp
using ActionBuffer;

byte[] bytes = BuffSerializer.ToBytes(value);
MyData copy = BuffSerializer.FromBytes<MyData>(bytes);

string json = BuffSerializer.ToJson(value);
MyData jsonCopy = BuffSerializer.FromJson<MyData>(json);
```

YAML/XML 使用 `ToYaml`、`FromYaml`、`ToXml`、`FromXml`。读取端从数据中的元信息和目标泛型类型恢复对象，不接收写入设置。

默认继续序列化公开字段以及标记了 `[Buffer]` 的非公开字段。属性无论公开还是非公开，都必须显式标记 `[Buffer]`，并且同时提供 getter 和 setter；`[Buffer("name")]` 可指定协议成员名。索引器不参与序列化。

## 写入设置

`BuffSettings` 控制类型信息、字段范围、互相引用、自定义 Converter、深度、节点数量、集合长度和文本/二进制上限。建议为一个协议创建并复用设置实例：

```csharp
var settings = new BuffSettings();
settings.RegisterConverter(new MyDataConverter());

byte[] first = BuffSerializer.ToBytes(a, settings);
byte[] second = BuffSerializer.ToBytes(b, settings);
```

互相引用关闭时，扫描发现循环会抛出明确错误；开启时使用对象引用表恢复共享引用和环。不要在网络协议发布后随意修改类型名、字段名或 Converter 的数据布局。

## 自定义 Converter

继承 `BuffConverter<T>`，实现受保护的 `OnScan`、`OnWrite`、`OnRead`。Converter 的 `Scan/Write/Read` 调度由框架内部控制，外部不应绕过 `BuffSerializer` 入口。自定义 Converter 必须在扫描和写入阶段以完全相同的顺序访问值。

## Unity 扩展

Unity 支持位于 `Assets/ActionBuffer/Unity`，通过独立 asmdef 隔离：核心 `ActionBuffer` 保持 `noEngineReferences`，服务器、命令行工具和非 Unity .NET 平台不会加载 UnityEngine。

```csharp
using ActionBuffer.Unity;

BuffSettings settings = UnityObjectSerialization.CreateRuntimeSettings();
byte[] data = BuffSerializer.ToBytes(payload, settings);
```

支持范围：

- 所有 `UnityEngine.Object` 子类，包括 `GameObject`、`Component`、`MonoBehaviour`、`ScriptableObject`、纹理、材质、动画等，统一通过 `IUnityObjectResolver` 写稳定引用 ID，不复制原生对象内存。
- `UnityEvent` 及 0 到 4 个参数的 UnityEvent 子类；持久监听目标同样通过 resolver 恢复。
- 常用数学、颜色、几何、动画、物理/2D 物理、场景参数、TextCore、树实例和渲染值类型，以及它们的一维数组和 `List<T>`。
- Editor 下可使用 Asset GUID/局部 ID resolver；Player 下使用 `RuntimeUnityObjectResolver`，需提前注册场景对象、Resources 地址或业务稳定 ID。

不序列化 `Scene`、`NativeArray`、`PlayableHandle` 等瞬时原生句柄本身。这些类型没有跨进程或跨平台稳定含义，应由业务 Converter 写可重建描述。

## 性能与 GC

- Unity 值类型 Converter、数组 Converter 和列表 Converter 为泛型静态缓存，不会在每个 `BuffSettings` 中重复创建。
- Writer、Reader、扫描缓存和字段缓存由框架池管理；高频场景应复用 `BuffSettings` 和 resolver。
- `ToBytes` 必须返回新的 `byte[]`；需要最低分配时使用 `WriteObject` 写入复用的 `BufferWriter`。
- UnityEvent 监听列表和临时参数数组在 Converter 内复用。
- 当前实现不承诺同一个可变 settings/resolver 被多线程同时写入；并行任务应使用独立实例。

## 安全限制

反序列化不可信数据时务必设置合理的最大深度、节点数、集合数和文本/二进制长度。只注册允许实例化的业务类型与 Converter，不要把任意网络输入直接交给拥有宽泛类型权限的设置。
