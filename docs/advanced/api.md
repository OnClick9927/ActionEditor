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
BTTree.loader = subTreeLoader
tree.PrepareForRuntime()
blackboard.Initialize(tree)
blackboard.Initialize(tree, otherBlackboard.RuntimeValues)
blackboard.CopyFieldsFrom(otherBlackboard)
tree.Update(blackboard)
tree.Abort(blackboard)
tree.Abort(blackboard, flag)
tree.PushEvent(blackboard, name)
tree.FindRuntimeTreeNode<T>(guid)
blackboard.GetState(node) // 节点无可用状态时返回 null
tree.blackboard // 仅编辑器字段描述和业务默认值 helper
```

每个外部黑板保存完整实例运行状态并可直接驱动行为树；树上的驱动方法仅作无状态转发。`tree.blackboard` 不保存任何节点运行状态。

自定义：继承 `BTAction`、`BTCondition`、`BTComposite` 或 Decorator 基类；实现带 `Blackboard` 参数的生命周期方法，必要时声明 `RuntimeDataSize` 并通过带黑板参数的 `GetRuntimeData/SetRuntimeData` 访问运行状态。

## ActionAttribute

运行时公共面包括：

- `ActionConditionAttribute.Evaluate(context)`：自定义可见性或编辑状态。
- `ActionValidationAttribute.IsValid(context)`：自定义校验；可覆盖 `GetMessage(context)`。
- `ActionValueModifierAttribute.Modify(context)`：提交后修正值。
- `ActionAttributeContext`：`Target/Owner/Value/ValueType/PropertyPath/MemberName/IsPlaying`，并提供 `TryGetValue<T>`。
- `ActionAttributeBase.Priority`：同类扩展规则的稳定执行优先级，较小值先执行。
- `ValueDropdownList<T>`、`ValueDropdownItem<T>`：下拉候选数据。

具体特性均为 `sealed`，`ActionAttributeBase` 也不能从外部直接继承；不提供别名特性。Editor Drawer 是 internal，不作为外部继承点。根组合 Drawer 会自动处理三个抽象行为父类的 Runtime 派生特性；只有需要全新 IMGUI 控件时才需要 Editor 侧实现。完整用法见 [Runtime 逻辑特性扩展](../attribute/extensions.md)。
