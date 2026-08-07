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
    private readonly BattleBlackboard data = new BattleBlackboard();
    protected override Blackboard blackboard => data;
    public BattleBlackboard Data => data;
}
```

内置变量节点按字段名访问 BlackBoard 的可序列化字段并缓存 TypeFields。缺失字段通常在节点初始化时抛出，避免运行几百 Tick 后才暴露配置错误。运行时数据字段为 private，外部只能通过树 API 驱动，不能直接修改节点 current/running 等内部状态。

## 准备运行

```csharp
tree.PrepareForRuntime(path => LoadTree(path));
```

准备阶段：

1. 清理上一次运行链接并重建 Graph 端口连接。
2. 找到唯一 BTRoot。
3. 为 Composite/Decorator 设置固定顺序子节点。
4. 递归加载 BTSubTree，检查路径循环、类型和 IsSubTree。
5. 验证运行结构没有共享节点或环。
6. 初始化事件表、中断表、信号量和黑板。

主树需要一个连接到子节点的 Root。子树必须和父树具体类型相同、`IsSubTree=true`，共享父树 Blackboard。loader 为 null 但图中存在子树时会报错。

## 更新和状态

`tree.Update()` 返回 `BTNode.State`：Inactive 只作为内部静止状态，业务更新结果为 Running、Success 或 Failure。节点首次进入调用 OnStart；离开 Running 时调用 OnStop 并回到内部 Inactive；Abort 只对 Running 节点生效并传播到活动子分支。

按固定逻辑 Tick 调用：

```csharp
for (int tick = 0; tick < simulationTicks; tick++)
{
    ApplyCommands(tick, tree.Blackboard);
    BTNode.State state = tree.Update();
    SaveRollbackState(tick, tree.CollectStatus(reusableStatus));
}
```

## 事件、中断和信号量

- `PushEvent(name)` 同步通知所有同名 `IBTEventReceiver`，返回是否存在接收者。
- `Abort(flag)` 查找唯一 BTInterrupt 并触发，重复标识在初始化时拒绝。
- `Abort()` 中止根活动分支。
- Semaphore 在 BTTree 配置名称和最大数；BTSemaphore 申请/归还整数占用，当前值进入快照。

事件是本树运行时内的同步标记，不是线程消息队列。事件名称和中断名称是协议键，重命名会使旧资源失效。

## 自定义节点

继承 BTAction 或 BTCondition，实现 `OnUpdate` 和 `OnAbort`。需要私有运行数据时重写 `OnCollectStatus(List<int>)` 和 `OnReadStatus(List<int>, ref index)`；两者顺序、数量必须固定。不得提供外部单节点状态 setter，完整状态只能由 BTTree 递归收集和恢复。

用于帧同步的节点不要读取 Time、随机数、线程时钟和浮点业务状态。需要随机选择时从确定性命令流或业务黑板传入已经同步的整数结果。
