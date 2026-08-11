# ActionEditor.Nodes.BT

[在线完整文档](https://onclick9927.github.io/ActionEditor/#/bt/runtime) · [内置节点参考](../../docs/bt/nodes.md) · [状态快照与确定性](../../docs/bt/status.md)

ActionEditor.Nodes.BT 是引擎无关的行为树运行时与 Unity 图编辑器。运行时节点不引用 UnityEngine，可用于服务器、测试程序和确定性帧同步逻辑。

## 基本运行流程

1. 定义继承 `Blackboard` 的业务黑板，只暴露需要被节点访问的字段。
2. 定义继承 `BTTree` 的具体树类型。默认文件后缀为 `.bt.bytes`；只有确有协议隔离需求时才用 `AssetFileExtensionAttribute` 覆盖。
3. 从字节加载树，为每个使用者创建独立黑板并初始化。树公开的 `blackboard` 仅供编辑器保存默认值。
4. 由业务逻辑在固定 Tick 中更新，不使用真实时间驱动确定性节点。

节点状态只有 `Inactive`、`Running`、`Success`、`Failure`。组合节点决定子节点遍历顺序，装饰节点改变唯一/多个子节点的执行语义，Action 与 Condition 承载业务动作和判断。

## 状态快照

```csharp
BTTree.loader = loader;
tree.PrepareForRuntime();
actorBlackboard.Initialize(tree);
tree.Update(actorBlackboard);

restoredBlackboard.Initialize(tree, actorBlackboard.RuntimeValues);
```

多个对象可以共享同一棵树和全部节点配置，每个对象只创建自己的黑板：

```csharp
firstBlackboard.Initialize(tree);
secondBlackboard.Initialize(tree);
tree.Update(firstBlackboard);
tree.Update(secondBlackboard);
```

替换运行黑板并继承完整状态时，先调用 `newBlackboard.Initialize(tree)`，再调用 `newBlackboard.CopyFieldsFrom(oldBlackboard)`。该方法浅拷贝可序列化业务字段，并在双方属于同一棵行为树和同一运行布局时复制 `RuntimeValues`；运行数组仍由各黑板独立持有。

运行 API 不会读取或初始化 `tree.blackboard`。需要采用编辑器配置的业务默认值时，由运行对象创建方复制 helper；不要把 helper 本身作为运行黑板。

节点状态、计数、事件标记和信号量当前占用统一保存在黑板的一块连续整数数组中。节点按固定偏移 O(1) 访问；`RuntimeValues` 直接公开该数组的只读视图，不递归遍历节点。`PrepareForRuntime` 构建节点关系，并一次性建立共享的偏移、范围、默认运行状态和事件/中断索引；每个黑板通过 `Initialize(tree, optionalRuntimeValues)` 建立自己的当前状态区，可直接从其他实例或回滚帧恢复，不复制配置型数据。

## 确定性规则

- 帧同步节点只使用整数 Tick、布尔、字符、字符串（Ordinal）、枚举和明确的整数溢出规则。
- 不在通用运行时节点中读取 `Time`、系统时钟或随机数。
- 新增的 `BTCopyVariable`、`BTSwapVariables`、`BTCompareVariables` 会在初始化时拒绝 Single/Double 字段并要求类型完全一致。
- `BTCooldownTicks` 以 Update 次数冷却；`BTOnce` 的首次结束结果会进入快照。
- `BTSetVariable`/`BTVariableCondition` 支持 bool、int、float、string、enum；enum 只支持设置和相等性判断，float 路径仅服务非帧同步业务，double 不受支持。

## 常用节点

- Composite：Sequence、Selector、Reactive Sequence/Selector、Parallel、Parallel Selector、Round Robin、Integer Switch。
- Decorator：Inverter、Repeat、Retry、Execution Limit、Delay/Timeout/Cooldown Ticks、Semaphore、Once、Interrupt。
- Action：Wait Ticks、Wait Event、Push Event、Perform Interrupt、Set/Copy/Swap Variable。
- Condition：Variable Condition、Compare Variables、Receive Event。

每个可创建节点都有与语义对应的独立图标。图标位于 `Editor/Resources`，运行时程序集不会加载纹理。

## 变量节点

`BTVariableCondition` 对 bool、enum 提供 Equals/NotEquals；对 int、float、string 提供 Equals、NotEquals 和四种大小比较。string 固定按 Ordinal 比较。

`BTSetVariable` 对 bool 提供 Set/Not，对 string 提供 Set/Add，对 int 和 float 提供 Set、四则、余数、幂、绝对值、最值和取负。int 运算使用 unchecked 回绕；除法和余数拒绝零操作数。

Inspector 只列出上述四种类型的黑板公开字段，自动维护只读类型，并按字段类型过滤比较或操作下拉框。值区域只显示当前类型对应的控件，bool 显示为复选框；Not、Negate、Absolute 不显示操作数。其他类型不会出现在下拉框中，也不会被隐式转换；旧资源或字段类型变更造成不匹配时，运行时初始化会明确报错。

旧版资源中复用整数/浮点槽保存的 bool 操作数会自动迁移到新的 bool 字段，不需要手工重填。

## 子树、事件和信号量

子树必须与父树为同一具体类型并标记为子树，共享父树黑板。编辑器可同步中断标记、事件和信号量定义。非静态回调目标必须属于已注册的运行时对象范围，不应通过隐式全局查找恢复。

信号量限制并行分支的占用数量；事件按精确字符串键派发。名称属于序列化协议，发布后重命名需要迁移旧资源。

## 扩展节点

自定义运行数据通过 `RuntimeDataSize` 声明整数槽位数量，用 `GetRuntimeData(blackboard, index)`/`SetRuntimeData(blackboard, index, value)` 按局部索引访问，并可重写 `GetInitialRuntimeData/GetMinRuntimeData/GetMaxRuntimeData` 声明初始值和合法范围。不要再把可变运行状态放进节点字段。

新增确定性节点时禁止使用浮点数和随机数；时间语义统一换算为调用方定义的整数 Tick。
