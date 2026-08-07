# 状态快照与确定性

## API

BTTree 只提供整树级接口：

```csharp
List<int> values = tree.CollectStatus(reusableList);
tree.ReadStatus(values);
```

不公开单节点设置方法。收集和读取都从 Root 递归，不根据当前节点是否 Running 跳过节点，因此快照布局只由树结构和节点类型决定。

## 布局

每个 BTNode 先写一个基础 State 整数，再由 `OnCollectStatus` 追加私有运行数据，最后按固定子节点顺序递归。整棵树结束后追加 `semaphore_value`。典型扩展数据包括：

- Sequence/Selector current。
- Parallel 每个子分支记录状态。
- Reactive/Switch runningIndex。
- Tick wait/delay/timeout/cooldown 计数。
- Once 完成标记和结果。
- Repeat/Retry/ExecutionLimit 次数。
- Event 接收标记。
- Semaphore 当前占用。

框架层快照只使用 `List<int>`，没有 FloatStatusValue，也不把类型名、GUID 或对象引用写进每帧状态。

## 恢复校验

`ReadStatus` 会校验：

- 输入非 null。
- Tree 已 PrepareForRuntime。
- State 在枚举范围内。
- 节点私有索引和计数在配置范围内。
- Running 状态与 runningIndex 等内部值一致。
- Semaphore 在 0..max。
- 没有缺少值和多余值。

校验完整通过后再应用信号量值，减少半写入状态。业务应保证快照来自完全相同的树结构、节点顺序和配置版本；框架不会把不同布局快照自动迁移。

## 自定义状态

```csharp
private int remaining;

protected override void OnCollectStatus(List<int> values)
{
    values.Add(remaining);
}

protected override void OnReadStatus(List<int> values, ref int index)
{
    int value = ReadStatusValue(values, ref index);
    if (value < 0 || value > limit)
        throw new ArgumentException("Invalid remaining count", nameof(values));
    remaining = value;
}
```

不要写配置字段、黑板完整数据或可由其他状态推导的缓存。每个自定义整数都增加每帧快照大小。读取必须先验证再赋值，OnCollect/OnRead 的数量和顺序永久配对。

## 帧同步清单

- 固定节点和连接顺序。
- 固定 Tick 次数，不读取 deltaTime/realtime。
- 不在通用节点中调用随机数；随机结果作为同步整数输入。
- 整数溢出采用明确 unchecked 规则。
- 字符串比较使用 Ordinal，不依赖系统区域。
- 不使用 float/double 条件和运算；BTSetVariable/BTVariableCondition 的浮点路径只服务非帧同步业务。
- 状态快照和业务 Blackboard 快照在同一 Tick 边界采集。
- 树资源变更时提升布局版本，旧回放不能直接套用新树。

仓库包含 1000 轮快照恢复压力测试，验证 WaitTicks、Sequence 和节点递归状态在重复构建/恢复中保持一致，并测试截断、多余和非法 State 输入均被拒绝。
