# Unity 类型支持

Unity 扩展源码位于 `Assets/ActionBuffer/Unity`，核心 ActionBuffer 不直接引用 UnityEngine。Editor 与 Player 使用不同的对象引用解析策略，但使用同一套 `BuffSettings` 注册入口。

## 创建设置

```csharp
using ActionBuffer;
using ActionBuffer.Unity;

var resolver = new RuntimeUnityObjectResolver();
var settings = UnityObjectSerialization.CreateRuntimeSettings(resolver);

resolver.Register("player-root", playerGameObject);
byte[] data = BuffSerializer.ToBytes(save, settings);
SaveData copy = BuffSerializer.FromBytes<SaveData>(data, settings);
```

也可对现有设置调用 `RegisterUnityConverters(resolver)`、`RegisterRuntimeUnityConverters()` 或只注册值类型的 `RegisterUnityValueConverters()`。`RemoveUnityConverters()` 移除 Unity Object、UnityEvent 和值类型注册。

## UnityEngine.Object

所有 `UnityEngine.Object` 派生类型通过统一 factory 支持，包括 `GameObject`、`Component`、`MonoBehaviour`、`ScriptableObject`、Texture、Material、AnimationClip 等。它们不会复制原生对象内容，而是写 resolver 返回的稳定字符串 ID。

`RuntimeUnityObjectResolver` 支持：

- `Register(id, object)`：业务稳定 ID。
- `RegisterResource(path, object)`：可跨 Player 重启的 Resources 路径。
- `AutoRegister = true`：同进程往返的临时 ID；重启后不能恢复。
- `Remove` / `Clear`：清理注册表。

正式存档不要依赖自动生成 ID，它包含 InstanceID 和递增计数，只保证 resolver 生命周期内可回查。场景对象应由业务在加载场景后用网络实体 ID、配置 ID 或层级稳定键注册。

Editor 下 `AssetDatabaseUnityObjectResolver` 使用 AssetDatabase 信息恢复资产引用，适合编辑工具，不可进入 Player。`UnityObjectRegistry` 提供显式注册的轻量映射。

## UnityEvent

支持 `UnityEvent` 和 0 至 4 个参数的具体 UnityEvent 派生类型。持久监听记录方法、调用模式、参数及目标引用；目标通过同一 resolver 恢复。运行时临时监听不等同于 Unity 序列化持久调用，跨进程保存前应明确所需语义。

## 已注册值类型

每种值类型同时注册其本身、`T[]` 和 `List<T>` Converter。主要包括：

- 数学与几何：Vector2/3/4、Vector2Int/3Int、Quaternion、Matrix4x4、Rect/RectInt、Bounds/BoundsInt、Ray/Ray2D、Plane、RangeInt、Pose、FrustumPlanes。
- 颜色与动画：Color、Color32、Keyframe、AnimationCurve、GradientColorKey、GradientAlphaKey、Gradient、MatchTargetWeightMask。
- 渲染与资源：LayerMask、Hash128、PropertyName、BoundingSphere、LightBakingOutput、CustomRenderTextureUpdateZone、GPUCacheSetting、TreeInstance。
- 物理：JointLimits、JointDrive、JointMotor、JointSpring、SoftJointLimit、SoftJointLimitSpring、WheelFrictionCurve、ArticulationDrive、BoneWeight、BoneWeight1、ClothSkinningCoefficient。
- 2D 物理：ContactFilter2D、JointMotor2D、JointAngleLimits2D、JointTranslationLimits2D、JointSuspension2D。
- 系统与文本：AudioConfiguration、Resolution、BuildCompression、CreateSceneParameters、LoadSceneParameters、GlyphMetrics、GlyphRect、GlyphValueRecord、GlyphAdjustmentRecord、RectOffset。

具体可用类型受 Unity 版本编译条件影响。没有稳定跨会话含义的 `Scene` 句柄、NativeArray、原生资源句柄、PlayableHandle 等不会按内存镜像序列化；请保存可重建描述并编写业务 Converter。

## GC 与生命周期

Unity 值 Converter、数组 Converter、列表 Converter 使用泛型静态缓存，不会随每个 settings 重建。Resolver 字典会持有 Unity 对象引用，长生命周期服务必须在场景卸载时 Remove/Clear，否则对象不能及时释放。`ToBytes` 为返回所有权会分配新数组；高频路径可以池化 `BufferWriter` 并调用统一 `WriteObject`，但必须在 finally 中 Clear 和归还。
