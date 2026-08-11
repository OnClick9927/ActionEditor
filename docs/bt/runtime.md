# 行为树运行时接入

ActionEditor.Nodes.BT 的 Runtime 不引用 UnityEngine。它使用 GraphAsset 存储结构，在 `PrepareForRuntime` 时转换成严格树形父子关系，可用于服务器、单元测试和确定性 Tick 驱动逻辑。

## 黑板和树

```csharp
public sealed class BattleBlackboard : Blackboard
{
    public int hp;
    public int targetCount;
    public bool interrupted;
}

public sealed class BattleTree : BTTree
{
    private readonly BattleBlackboard defaults = new BattleBlackboard();
    public override Blackboard blackboard => defaults;
}
```

树公开的 `blackboard` 只供编辑器描述字段、编辑并保存业务默认值，不参与运行，也不会获得节点状态槽。运行对象必须持有自己的外部黑板；需要使用资源里的默认值时，由创建运行对象的一方复制一次：

```csharp
BattleBlackboard actorBlackboard =
    ((BattleBlackboard)tree.blackboard).DeepCopyByBuffer();
```

不需要资源默认值时可直接 `new BattleBlackboard()`。内置变量节点按字段名访问实际传入黑板的可序列化字段并缓存 TypeFields。缺失字段通常在黑板初始化时抛出，避免运行几百 Tick 后才暴露配置错误。运行时数据字段为 private，外部通过黑板 API 驱动，不能直接修改节点 current/running 等内部状态。

需要用新黑板继续当前业务字段、但重新开始行为树运行状态时：

```csharp
var nextBlackboard = new BattleBlackboard();
nextBlackboard.CopyFieldsFrom(actorBlackboard);
nextBlackboard.Initialize(tree);
```

`CopyFieldsFrom` 要求具体黑板类型一致，通过缓存字段访问器浅拷贝 ActionBuffer 可序列化成员；双方已经为同一棵树和同一运行布局初始化时，同时复制 `RuntimeValues`。引用类型字段仍引用同一个对象；需要深拷贝业务对象时由业务层单独处理。

## 准备运行

```csharp
BTTree.loader = path => LoadTree(path);
tree.PrepareForRuntime();
```

准备阶段：

1. 清理上一次运行链接并重建 Graph 端口连接。
2. 找到唯一 BTRoot。
3. 为 Composite/Decorator 设置固定顺序子节点。
4. 递归加载 BTSubTree，检查路径循环、类型和 IsSubTree。
5. 验证运行结构没有共享节点或环。
6. 调用每个节点的 `Init`，建立父节点引用和固定状态偏移。

主树需要一个连接到子节点的 Root。子树必须和父树具体类型相同、`IsSubTree=true`，共享父树 Blackboard。静态 `BTTree.loader` 为 null 但图中存在子树时会报错；不包含子树时可以不设置 loader。

准备完成后，为每个运行对象初始化自己的黑板：

```csharp
actorBlackboard.Initialize(tree);
```

`PrepareForRuntime` 会在树内一次性建立共享只读布局，包括节点偏移、默认运行状态、合法范围、事件接收表、中断表、自动中止列表以及信号量上限。每个黑板初始化时只校验实际业务字段类型并分配自己的连续状态区；传入 `sourceRuntimeValues` 时从其他实例或回滚帧恢复动态状态，未传入时才使用树级默认运行状态。配置型范围、字典和列表不会复制进黑板。这个过程不会读取或修改 `tree.blackboard`。

在 Unity 编辑器中查看某个对象的实时节点状态时，显式设置调试黑板：

```csharp
BTTree.SetAsInstance(tree, actorBlackboard);
```

同一棵树切换到另一个对象时再次调用即可；编辑器会按新的黑板刷新节点和黑板面板。

## 更新和状态

`tree.Update(blackboard)` 返回 `BTNode.State`：Inactive 只作为内部静止状态，业务更新结果为 Running、Success 或 Failure。节点首次进入调用 `OnStart(blackboard)`；离开 Running 时调用 `OnStop(blackboard)` 并回到内部 Inactive；Abort 只对 Running 节点生效并传播到活动子分支。黑板不对外提供无树上下文的驱动方法。

按固定逻辑 Tick 调用：

```csharp
for (int tick = 0; tick < simulationTicks; tick++)
{
    ApplyCommands(tick, actorBlackboard);
    BTNode.State state = tree.Update(actorBlackboard);
    SaveRollbackState(tick, actorBlackboard.RuntimeValues);
}
```

## 事件、中断和信号量

- `tree.PushEvent(blackboard, name)` 同步通知该黑板中所有同名 `IBTEventReceiver`，返回是否存在接收者。
- `tree.Abort(blackboard, flag)` 查找唯一 BTInterrupt 并触发，重复标识在准备树时拒绝。
- `tree.Abort(blackboard)` 中止该黑板中的根活动分支。
- Semaphore 在 BTTree 配置名称和最大数；BTSemaphore 申请/归还整数占用，当前值进入快照。

事件是本树运行时内的同步标记，不是线程消息队列。事件名称和中断名称是协议键，重命名会使旧资源失效。

## 自定义节点

继承 BTAction 或 BTCondition，实现 `OnUpdate(Blackboard)` 和 `OnAbort(Blackboard)`。需要私有运行数据时重写 `RuntimeDataSize` 以及初始值/范围方法，并通过 `GetRuntimeData(blackboard, index)` 和 `SetRuntimeData(blackboard, index, value)` 访问。不得把运行时可变数据放进节点字段；完整状态由 Blackboard 的连续状态区直接复制和恢复。

用于帧同步的节点不要读取 Time、随机数、线程时钟和浮点业务状态。需要随机选择时从确定性命令流或业务黑板传入已经同步的整数结果。
