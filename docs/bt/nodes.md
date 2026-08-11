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
| BTVariableCondition | 黑板字段与配置值比较，仅支持 bool、int、float、string |
| BTCompareVariables | 两个同类型确定性字段 Equal/NotEqual |
| BTRecEventCondition | 检查并消费命名事件标记 |

### BTVariableCondition

| 字段类型 | 可用比较 |
| --- | --- |
| bool | Equals、NotEquals |
| int | Equals、NotEquals、LessThan、LessOrEquals、GreaterThan、GreaterOrEquals |
| float | Equals、NotEquals、LessThan、LessOrEquals、GreaterThan、GreaterOrEquals |
| string | Equals、NotEquals、LessThan、LessOrEquals、GreaterThan、GreaterOrEquals |

字符串固定使用 `StringComparison.Ordinal` 语义，不受系统语言和区域设置影响；整数直接比较，不转换为浮点数。float 遵循 C# 浮点比较规则，包括 NaN 的行为，因此只应用于不要求跨平台帧同步的树。

Inspector 的字段下拉框只列出黑板中的 bool、int、float、string 公开字段。选择字段后，“参数类型”会自动同步并保持只读，同时只显示与该类型对应的值控件；bool 使用复选框。选择 bool 字段时，比较方式下拉框只显示 Equals 和 NotEquals。

## Action

| 节点 | 行为 |
| --- | --- |
| BTSetVariable | 设置/修改 bool、int、float、string 黑板字段；整数 unchecked 回绕 |
| BTCopyVariable | 复制同类型确定性字段，拒绝浮点 |
| BTSwapVariables | 交换同类型确定性字段，拒绝浮点 |
| BTWaitTicks | 等待固定 Update 次数 |
| BTWaitEvent | Running 到收到并消费事件 |
| BTPushEvent | 同步广播命名事件并成功 |
| BTPerformInterrupt | 按 flag 触发中断节点 |

### BTSetVariable

| 字段类型 | 可用操作 |
| --- | --- |
| bool | Set、Not |
| int | Set、Add、Subtract、Multiply、Divide、Remainder、Power、Absolute、Max、Min、Negate |
| float | Set、Add、Subtract、Multiply、Divide、Remainder、Power、Absolute、Max、Min、Negate |
| string | Set、Add |

`Set` 直接覆盖字段；`Add` 对 string 表示拼接。int 运算使用 unchecked 回绕，`Divide` 和 `Remainder` 拒绝零操作数，整数 `Power` 拒绝负指数。float 使用 C#/.NET 浮点运算语义，只适合非帧同步业务。

Inspector 会根据字段类型过滤“运算方式”，并只显示当前类型的操作数控件；bool 使用复选框，`Not`、`Negate`、`Absolute` 这类不需要操作数的操作不会显示值控件。“参数类型”由所选字段自动维护，不能手工指定。

旧版本把 bool 操作数复用在整数槽（早期 `BTSetVariable` 还可能复用浮点槽）。加载旧资源后，节点会在 Inspector 同步或运行时初始化时将该值一次性迁移到新的 bool 字段；迁移完成后复选框中的值就是唯一有效配置。

### 不支持类型的处理

两个变量节点都不支持 byte、sbyte、short、ushort、uint、long、ulong、double、decimal、char、enum、对象或其他自定义类型。这些字段不会出现在 Inspector 的字段下拉框中，也不会被隐式转换为受支持类型。旧资源仍记录不支持的类型，或黑板字段在资源保存后被改成不支持/不匹配的类型时，行为树初始化会抛出配置错误；应在编辑器中重新选择字段并保存资源。

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
