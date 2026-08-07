# 内置节点参考

## 入口与子树

| 节点 | 行为 |
| --- | --- |
| BTRoot | 唯一入口，把 Update 转发给唯一子节点 |
| BTSubTree | 加载同类型子树，共享父黑板，拒绝循环路径 |

## Composite

| 节点 | 行为 |
| --- | --- |
| BTSequence | 成功后推进，失败立即失败，Running 记住 current |
| BTSelector | 失败后推进，成功立即成功，Running 记住 current |
| BTReactiveSequence | 每 Tick 从第一个前置节点重评，失效时中止旧后续分支 |
| BTReactiveSelector | 每 Tick 从最高优先级重评，高优先级接管时中止旧低优先级分支 |
| BTParallel | 固定顺序更新未完成分支；任一失败即失败，全部成功才成功 |
| BTParallelSelector | 任一成功即成功，全部失败才失败 |
| BTParallelComplete | 第一个结束结果决定整体，并中止其他分支 |
| BTRoundRobin | 每次进入执行一个索引，根据结果决定是否推进 |
| BTSwitchInteger | 用黑板 int/enum 直接选择子索引，支持 Failure/Success/Clamp 越界策略 |

普通 Sequence/Selector 在 Running 后从保存索引继续；Reactive 版本每 Tick 从头重评，这正是高优先级条件能抢占的原因。Reactive 节点会记录 runningIndex，切换分支时只中止此前实际 Running 的子节点。

## Condition

| 节点 | 行为 |
| --- | --- |
| BTVariableCondition | 黑板字段与配置值比较，支持多种基础类型 |
| BTCompareVariables | 两个同类型确定性字段 Equal/NotEqual |
| BTRecEventCondition | 检查并消费命名事件标记 |

BTVariableCondition 对字符串使用 Ordinal；整数不经浮点转换；float/double 路径仅用于不要求跨平台帧同步的树。

## Action

| 节点 | 行为 |
| --- | --- |
| BTSetVariable | 设置/修改基础类型黑板字段；整数 unchecked 回绕 |
| BTCopyVariable | 复制同类型确定性字段，拒绝浮点 |
| BTSwapVariables | 交换同类型确定性字段，拒绝浮点 |
| BTWaitTicks | 等待固定 Update 次数 |
| BTWaitEvent | Running 到收到并消费事件 |
| BTPushEvent | 同步广播命名事件并成功 |
| BTPerformInterrupt | 按 flag 触发中断节点 |

BTSetVariable 支持 bool、全部整数、enum、char、string、decimal，也可显式使用 float/double。帧同步树只选整数、布尔、枚举、字符、Ordinal 字符串或经过业务确认的 decimal 路径。

## 单子装饰

| 节点 | 行为 |
| --- | --- |
| BTInverter | Success/Failure 互换，Running 不变 |
| BTSuccess / BTFailure | 子节点结束后强制固定结果 |
| BTOnce | 本次运行会话只执行一次，缓存首次结束结果 |
| BTRepeat | 按成功/失败开关持续重启 |
| BTRepeatCount | 固定次数执行，记录已完成数 |
| BTRetry | 失败重试到成功或最大尝试数 |
| BTUtilSuccess / BTUtilFailure | 分别重复直到成功/直到失败 |
| BTDelayTicks | 先等待 Tick 再运行子节点 |
| BTTimeoutTicks | 子节点 Running 超过 Tick 上限则中止并失败 |
| BTCooldownTicks | 子节点结束后进入整数 Tick 冷却 |
| BTExecutionLimit | 整个会话限制子节点完成次数 |
| BTSemaphore | 申请树级整数信号量，结束/中止归还 |
| BTInterrupt | 注册可按 flag 触发的中断分支 |

## 多子装饰

| 节点 | 行为 |
| --- | --- |
| BTAnd | 固定顺序求值，全部成功才成功 |
| BTOR | 固定顺序求值，任一成功即成功 |
| BTIF | 第一子节点为条件，符合期望才进入行为；可持续检查 |

## 选择建议

- 需要保留进度：Sequence/Selector。
- 条件必须每 Tick 保持有效：ReactiveSequence/ReactiveSelector 或 BTIF 持续检查。
- 多任务全部必须完成：Parallel。
- 多任务任意一个成功：ParallelSelector。
- 只等待最先结束：ParallelComplete。
- 逻辑计时：所有 `*Ticks` 节点，不使用真实时间。
