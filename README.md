# ActionEditor.Nodes.BT

[在线完整文档](https://onclick9927.github.io/ActionEditor/#/bt/runtime) · [内置节点参考](../../docs/bt/nodes.md) · [状态快照与确定性](../../docs/bt/status.md)

ActionEditor.Nodes.BT 是引擎无关的行为树运行时与 Unity 图编辑器。运行时节点不引用 UnityEngine，可用于服务器、测试程序和确定性帧同步逻辑。

## 基本运行流程

1. 定义继承 `Blackboard` 的业务黑板，只暴露需要被节点访问的字段。
2. 定义继承 `BTTree` 的具体树类型。默认文件后缀为 `.bt.bytes`；只有确有协议隔离需求时才用 `AssetFileExtensionAttribute` 覆盖。
3. 从字节加载树，设置黑板并初始化运行树。
4. 由业务逻辑在固定 Tick 中更新，不使用真实时间驱动确定性节点。

节点状态只有 `Inactive`、`Running`、`Success`、`Failure`。组合节点决定子节点遍历顺序，装饰节点改变唯一/多个子节点的执行语义，Action 与 Condition 承载业务动作和判断。

## 状态快照

```csharp
List<int> snapshot = tree.CollectStatus();
// 保存或传输 snapshot
tree.ReadStatus(snapshot);
```

快照递归包含每个节点的基础状态，以及 Sequence/Selector 当前索引、Parallel 运行集合、等待/超时/冷却 Tick、事件接收标记、重复次数、信号量占用等私有运行数据。列表布局由树结构决定，必须对同一份树资源读取；长度、枚举范围和节点状态不一致会抛出异常。

## 确定性规则

- 帧同步节点只使用整数 Tick、布尔、字符、字符串（Ordinal）、枚举和明确的整数溢出规则。
- 不在通用运行时节点中读取 `Time`、系统时钟或随机数。
- 新增的 `BTCopyVariable`、`BTSwapVariables`、`BTCompareVariables` 会在初始化时拒绝 Single/Double 字段并要求类型完全一致。
- `BTCooldownTicks` 以 Update 次数冷却；`BTOnce` 的首次结束结果会进入快照。
- 已有 `BTSetVariable`/`BTVariableCondition` 为兼容通用业务仍可显式处理浮点字段；帧同步树不要选择这些浮点路径。

## 常用节点

- Composite：Sequence、Selector、Reactive Sequence/Selector、Parallel、Parallel Selector、Round Robin、Integer Switch。
- Decorator：Inverter、Repeat、Retry、Execution Limit、Delay/Timeout/Cooldown Ticks、Semaphore、Once、Interrupt。
- Action：Wait Ticks、Wait Event、Push Event、Perform Interrupt、Set/Copy/Swap Variable。
- Condition：Variable Condition、Compare Variables、Receive Event。

每个可创建节点都有与语义对应的独立图标。图标位于 `Editor/Resources`，运行时程序集不会加载纹理。

## 子树、事件和信号量

子树必须与父树为同一具体类型并标记为子树，共享父树黑板。编辑器可同步中断标记、事件和信号量定义。非静态回调目标必须属于已注册的运行时对象范围，不应通过隐式全局查找恢复。

信号量限制并行分支的占用数量；事件按精确字符串键派发。名称属于序列化协议，发布后重命名需要迁移旧资源。

## 扩展节点

运行数据使用 `[NonSerialized] private` 字段。需要快照的节点重写 `OnCollectStatus(List<int>)` 与 `OnReadStatus(List<int>, ref int)`，写入基础整数并严格验证读取范围。子节点遍历由基类统一递归，不要在自定义节点中重复收集子节点状态。

新增确定性节点时禁止使用浮点数和随机数；时间语义统一换算为调用方定义的整数 Tick。
