# ActionBuffer Unity

ActionBuffer 的 Unity 类型扩展包。创建 `BuffSettings` 时提供引用解析器，即可在二进制、JSON、YAML 和 XML 中使用相同的 Unity 引用语义。

```csharp
var registry = new UnityObjectRegistry();
registry.Register("player", playerMonoBehaviour);

BuffSettings settings = UnityObjectSerialization.CreateSettings(registry);
byte[] bytes = BuffSerializer.ToBytes(data, settings);
Data copy = BuffSerializer.FromBytes<Data>(bytes, settings);
```

在 Player 中进行同一进程内的保存和读取时，可以直接使用运行时设置，未知对象会在首次扫描时自动登记：

```csharp
BuffSettings settings = UnityObjectSerialization.CreateRuntimeSettings();
byte[] bytes = BuffSerializer.ToBytes(data, settings);
Data copy = BuffSerializer.FromBytes<Data>(bytes, settings);
```

需要跨启动恢复 Resources 资源时，写端登记资源路径；读端可以使用全新的 Resolver：

```csharp
var writeResolver = new RuntimeUnityObjectResolver();
writeResolver.RegisterResource("Configs/GameConfig", gameConfig);

BuffSettings writeSettings =
    UnityObjectSerialization.CreateRuntimeSettings(writeResolver);
BuffSettings readSettings =
    UnityObjectSerialization.CreateRuntimeSettings();
```

## 支持范围

- `UnityEngine.Object` 及全部子类，包括 `GameObject`、`Component`、`MonoBehaviour`、`ScriptableObject`、纹理、材质等。对象本身不被复制，而是通过解析器 ID 恢复原引用。
- `UnityEvent`、`UnityEvent<T>` 至 `UnityEvent<T0, T1, T2, T3>`，以及这些类型的具体派生类。
- 常用 Unity 值类型，包括向量、整数向量、颜色、四元数、矩形、边界、矩阵、射线、平面、LayerMask、Keyframe、AnimationCurve、Gradient 和 RectOffset。
- 更多纯数据结构，包括 Pose、Resolution、BoneWeight/BoneWeight1、Hash128、PropertyName、FrustumPlanes，以及 3D/2D 关节、车轮摩擦和 ArticulationDrive 参数。
- `UnityObjectRegistry` 用于显式注册场景对象和运行时对象。
- `RuntimeUnityObjectResolver` 支持 Player 内自动登记对象，并能通过 Resources 路径跨启动恢复资源。
- `AssetDatabaseUnityObjectResolver` 用于编辑器中的项目资源和子资源。

仅需要 Unity 值类型时，可以直接调用 `settings.RegisterUnityValueConverters()`，无需提供对象引用解析器。

自动生成的运行时对象 ID 只在当前进程有效。场景对象、Addressables 或业务资源需要跨启动恢复时，应通过 `RuntimeUnityObjectResolver.Register` 注册业务稳定 ID，或实现 `IUnityObjectResolver` 接入项目自己的资源系统。

## UnityEvent 限制

Unity 没有提供枚举运行时监听器的公共 API，因此通过 `AddListener` 添加的运行时匿名监听器不会被序列化。持久监听器必须是参数与 UnityEvent 动态参数完全匹配的方法；Inspector 中携带常量参数的监听器无法通过公开 API 还原，会在 Scan 阶段抛出 `NotSupportedException`，防止静默丢失行为。
