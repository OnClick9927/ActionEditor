# 公共 API 速查

## ActionBuffer

```csharp
BuffSerializer.ToBytes(obj, settings)
BuffSerializer.FromBytes<T>(bytes, settings)
BuffSerializer.ToJson / FromJson<T>
BuffSerializer.ToYaml / FromYaml<T>
BuffSerializer.ToXml / FromXml<T>
BuffSerializer.WriteObject(writer, obj, settings)
BuffSerializer.ReadObject(reader, type, settings)
value.DeepCopyByBuffer()
```

扩展点：`BuffConverter<T>`、`AtomicBuffConverter<T>`、`IBufferWriter`、`IBufferReader`、`IBufferObject`、`BufferAttribute`、`BuffSettings.RegisterConverter/RegisterConverterFactory/RegisterType`。

## Unity Serialization

```csharp
UnityObjectSerialization.CreateRuntimeSettings()
UnityObjectSerialization.CreateRuntimeSettings(resolver)
UnityObjectSerialization.CreateSettings(resolver)
settings.RegisterUnityConverters(resolver)
settings.RegisterRuntimeUnityConverters()
settings.RegisterUnityValueConverters()
settings.RemoveUnityConverters()
```

Resolver：`IUnityObjectResolver.GetReferenceId/ResolveReference`，`RuntimeUnityObjectResolver.Register/RegisterResource/Remove/Clear`。

## Timeline

数据：`Asset`、`Group`、`Track`、`Clip`、`ClipSignal`、`IAction`、`ISegment`、`IResizeAble`、`IBlendAble`、`ILengthMatchAble`。

扩展：`AttachableAttribute`、`AssetFileExtensionAttribute`、`AssetFileExtensionUtility`、`CustomActionViewAttribute`、`ActonEditorView`、`ClipEditorView<T>`。

序列化：`Asset.ToBytes()`、`Asset.FromBytes(type, bytes)`。

## Graph

数据：`GraphAsset.nodes/groups/connections`、`NodeData`、`GroupData`、`ConnectionData`、`PortData`。

运行：`GraphAsset.FindNode/FindNode<T>`、`PrepareForRuntime()`、`RegenerateGuids()`、`ToBytes()`、`FromBytes(type, bytes)`。

编辑器扩展：`GraphNode<T>.data/asset/view`、`NodeAttribute`、`NodePortAttribute`、`AttachableAttribute`、`NameAttribute`、`IconAttribute`。

## Behavior Tree

```csharp
tree.PrepareForRuntime(subTreeLoader)
tree.Update()
tree.Abort()
tree.Abort(flag)
tree.PushEvent(name)
tree.FindRuntimeTreeNode<T>(guid)
tree.CollectStatus(optionalDestination)
tree.ReadStatus(source)
tree.Blackboard
```

自定义：继承 `BTAction`、`BTCondition`、`BTComposite` 或 Decorator 基类；实现 `OnUpdate/OnAbort`，必要时实现 `OnCollectStatus/OnReadStatus`。

## ActionAttribute

运行时公共面是 `ActionAttributeBase` 派生的 100+ Attribute 与 `ValueDropdownList<T>`。Editor Drawer 是 internal，不作为外部继承点。业务扩展优先组合已有特性；需要专用 Inspector 时调用共享默认绘制流程，保留 Script 定位和 TypeInfoBox。
