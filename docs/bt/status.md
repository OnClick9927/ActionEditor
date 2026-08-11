# 状态快照与确定性

## API

`BTTree` 是可共享的节点配置；每个使用者由自己的 `Blackboard` 保存业务字段和行为树运行状态：

```csharp
BTTree.loader = loader;
tree.PrepareForRuntime();
actorBlackboard.Initialize(tree);

tree.Update(actorBlackboard);
restoredBlackboard.Initialize(tree, actorBlackboard.RuntimeValues);
```

多个对象共享一棵树时，分别调用各自黑板的 `Initialize(tree)`，之后通过树的 `Update/Abort/PushEvent` 传入对应黑板，无需克隆树和节点。黑板不对外提供缺少树上下文的驱动方法。

`tree.blackboard` 是编辑器保存业务默认值的 helper，不是运行实例。运行 API 不会读取、初始化或修改它；调用方应创建独立黑板，必要时先从 helper 复制业务默认值。

## 布局

`PrepareForRuntime` 构建节点关系，并在与黑板无关的节点 `Init` 中为每个节点分配固定偏移。树只创建一份共享只读布局，保存默认运行状态、合法范围、事件/中断索引、自动中止节点和信号量上限。每个黑板通过 `Initialize(tree, sourceRuntimeValues)` 创建自身的当前值 `int[]`；传入状态时用于实例复制或回滚，未传入时使用树默认值，配置型数据不会进入黑板：每个节点先占一个基础 State 槽，再占节点声明的私有槽，最后是信号量槽。典型扩展数据包括：

`RuntimeValues` 是当前数组的只读实时视图，可直接传给另一个黑板的 `Initialize`，不产生中间分配。需要保留到未来帧的回滚数据时，由调用方在 Tick 边界把该视图复制到自己的快照缓冲区，避免后续更新改变其中的值。

- Sequence/Selector current。
- Parallel 每个子分支记录状态。
- Reactive/Switch runningIndex。
- Tick wait/delay/timeout/cooldown 计数。
- Once 完成标记和结果。
- Repeat/Retry/ExecutionLimit 次数。
- Event 接收标记。
- Semaphore 当前占用。

运行中节点按预先计算的偏移 O(1) 访问状态，不查字典、不装箱且不产生临时对象。调用方直接读取 `RuntimeValues`；恢复时把其他实例的视图或调用方保存的快照传给 `Initialize(tree, sourceRuntimeValues)`。

## 恢复校验

`Initialize(tree, sourceRuntimeValues)` 会校验：

- 输入非 null。
- Tree 已 `PrepareForRuntime`。
- State 在枚举范围内。
- 节点私有索引和计数在配置范围内。
- Semaphore 在 0..max。
- 没有缺少值和多余值。

范围校验完整通过后才复制状态，避免半写入。业务应保证快照来自完全相同的树结构、节点顺序和配置版本；框架不会把不同布局快照自动迁移。树重新 `PrepareForRuntime` 后，旧黑板会因布局版本不一致而拒绝继续运行，必须重新 `Initialize`。

## 自定义状态

```csharp
private int GetRemaining(Blackboard blackboard) =>
    GetRuntimeData(blackboard, 0);
private void SetRemaining(Blackboard blackboard, int value) =>
    SetRuntimeData(blackboard, 0, value);

protected override int RuntimeDataSize => 1;
protected override int GetMinRuntimeData(int index) => 0;
protected override int GetMaxRuntimeData(int index) => limit;
```

槽位数量、初始值和范围在树准备阶段写入共享布局，黑板不再复制这些校验数组。不要把配置字段、黑板完整数据或可由其他状态推导的缓存放入槽位；每个自定义整数都会增加每个黑板和快照的大小。

## 帧同步清单

- 固定节点和连接顺序。
- 固定 Tick 次数，不读取 deltaTime/realtime。
- 不在通用节点中调用随机数；随机结果作为同步整数输入。
- 整数溢出采用明确 unchecked 规则。
- 字符串比较使用 Ordinal，不依赖系统区域。
- BTSetVariable/BTVariableCondition 只接受 bool、int、float、string；帧同步树不使用其中的 float 条件和运算，double 不受这两个节点支持。
- 状态快照和业务 Blackboard 快照在同一 Tick 边界采集。
- 树资源变更时提升布局版本，旧回放不能直接套用新树。

仓库包含 1000 轮快照恢复压力测试，并覆盖同一节点图的多个独立黑板、状态复制、信号量隔离以及截断、多余和越界值拒绝。
