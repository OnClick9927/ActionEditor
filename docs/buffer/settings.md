# 设置与安全限制

## BuffSettings 选项

| 属性 | 默认值 | 作用 |
| --- | --- | --- |
| `TypeInfo` | `true` | 写实际类型信息，支持多态恢复 |
| `FullField` | `false` | 扫描更多非公开字段；协议发布后谨慎开启 |
| `SupportReferences` | `false` | 保存共享引用和循环引用；关闭时发现循环立即抛错 |
| `SerializeEvents` | `true` | 是否写显式标记的事件后备字段 |
| `PrettyPrint` | `false` | 格式化 JSON/YAML/XML 文本 |
| `DeterministicCollectionOrder` | `false` | 对可安全排序的无序集合生成稳定顺序 |
| `InvokeBeforeWriteCallbacks` | `true` | 是否调用 `IBufferObject.BeforeWriteBuffer` |
| `RestrictTypes` | `false` | 多态实际类型必须在白名单注册 |

`BuffSettings.DefaultSetting` 是未传设置时的默认实例。自定义 Converter 和类型白名单属于实例；深度、文本长度等上限当前是静态进程级限制。不要在并行序列化期间修改静态上限。

## 限制项

| 静态属性 | 默认值 | 合法范围/说明 |
| --- | --- | --- |
| `MaxDepth` | 256 | 1 到 1024 |
| `MaxTextLength` | 16 MiB | 正整数 |
| `MaxBinaryLength` | 64 MiB | 正整数 |
| `MaxNodeCount` | 100000 | 正整数 |
| `MaxCollectionCount` | 65534 | 1 到 65534 |
| `MaxObjectFieldCount` | 4096 | 正整数 |
| `MaxScalarLength` | 4 MiB | 正整数 |

这些限制同时保护扫描和读取，防止深度炸弹、超大集合、超长字符串与恶意节点数量耗尽内存。面向网络输入时应按实际协议缩小，而不是保留最大默认值。

```csharp
BuffSettings.MaxDepth = 64;
BuffSettings.MaxCollectionCount = 10000;
BuffSettings.MaxScalarLength = 256 * 1024;

var settings = new BuffSettings
{
    SupportReferences = true,
    RestrictTypes = true,
    DeterministicCollectionOrder = true
};
settings.RegisterType<PlayerMessage>();
settings.RegisterType<DamageMessage>();
```

## 自定义 Converter 与类型注册

`RegisterConverter<T>` 覆盖某个准确类型。`RegisterConverterFactory(baseType, factory)` 为派生类型按需创建 Converter，最近的派生基类注册优先。`RemoveConverter`、`RemoveConverterFactory`、`ClearConverters` 用于撤销注册。

当 `RestrictTypes` 开启，只有声明类型和实际类型相同，或实际类型已由 `RegisterType` 加入白名单时才能读取多态数据。白名单不能替代输入长度限制，也不能保证业务构造函数没有副作用。

## 设置生命周期

一个设置实例可跨多次串行操作复用，Converter 缓存也因此复用。序列化进行中不允许增删 Converter 或白名单类型，框架会抛出 `InvalidOperationException`。当前实现不承诺同一个可变 settings/resolver 被业务线程同时修改；并行任务使用各自实例最清晰。

读取自定义 Converter 数据时要传入包含同一 Converter 的设置：

```csharp
byte[] data = BuffSerializer.ToBytes(value, settings);
MyType copy = BuffSerializer.FromBytes<MyType>(data, settings);
```

## 不可信数据建议

- 默认 `RestrictTypes = true` 并注册精确消息类型。
- 不允许任意程序集限定类型名直接实例化业务对象。
- 缩小集合、标量、节点和总负载上限。
- 在协议外层增加版本、长度、校验和或认证标签。
- 反序列化到纯数据 DTO，再由业务代码校验并应用；不要直接反序列化拥有文件、网络或进程副作用的类型。
