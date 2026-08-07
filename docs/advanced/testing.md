# 测试与验证

## 测试程序集

| 目录 | 重点 |
| --- | --- |
| `Assets/Test/ActionBuffer/Editor` | 四格式、集合、属性、多态、引用、委托、事件、限制、Converter、性能 |
| `Assets/Test/ActionBuffer.Unity/Editor` | Unity 值类型、Object resolver、UnityEvent、Editor/Runtime 路径 |
| `Assets/Test/ActionAttribute/Editor` | 特性元数据、组合高度、fallback Inspector 与脚本定位 |
| `Assets/Test/BT/Editor` | 变量节点、状态快照、GUID、文件后缀和确定性压力 |

## Unity Test Runner

打开 `Window > General > Test Runner`，选择 EditMode，运行全部测试。CI 可用 Unity batchmode：

```bash
Unity -batchmode -nographics -quit \
  -projectPath . \
  -runTests -testPlatform EditMode \
  -testResults Temp/EditModeResults.xml \
  -logFile Temp/EditMode.log
```

项目已经被另一个 Unity Editor 打开时，不要并行启动 batchmode 使用同一 Library；复制工作区或关闭交互 Editor，否则项目锁和导入并发会污染结果。

## 当前压力覆盖

`OneThousandMixedFormatRoundTripsRemainStable` 连续 1000 轮轮换 JSON/YAML/XML/Binary，验证：

- 公开和私有 `[Buffer]` 属性。
- 共享对象保持同一引用。
- 两对象互相引用。
- 三维与五维数组。
- 反复租借/归还 Reader、Writer、BufferScan 和 CachedField 后没有残留。

`OneThousandRuntimeSnapshotsRestoreDeterministically` 连续构建 1000 组行为树，在 WaitTicks Running 状态采集快照、恢复到另一实例并继续执行，验证 Sequence 索引、节点 State 和私有 Tick 计数一致。

纯运行时独立验证当前展开为 132 个测试实例，0 失败。编辑器 GUI 测试仍需 Unity Editor 上下文运行。

## 新功能最低测试矩阵

| 改动 | 最低测试 |
| --- | --- |
| 新基础/集合 Converter | 四格式往返、null、边界值、重复写、损坏输入 |
| 引用相关 | SupportReferences 开/关，共享、自环、互环、集合环 |
| 属性采集 | public/private getter/setter、未标记跳过、协议别名 |
| Unity Converter | 值、数组、List、null、运行时 resolver、Editor resolver |
| Inspector 特性 | 高度=实际绘制、嵌套列表、窄 Inspector、Script 行 |
| Graph 操作 | 保存/重开、Undo/Redo、另存为 GUID、连线和 Group |
| BT 节点 | Success/Failure/Running、Abort、状态收集/读取、非法快照 |

## 性能测试

先预热至少两轮，再测固定对象图。记录 Unity 版本、后端、Development/Release、格式、对象数、迭代数和分配统计 API。性能阈值应捕获数量级回退，不要写成只在开发机当前负载下才通过的毫秒数。

## 提交前命令

```bash
git diff --check
git status --short
```

再查看 Unity `Editor.log` 最近一次 `Tundra build success`，确保没有被后续脚本编译错误覆盖。资源移动必须同时提交 `.meta`，测试程序集新增时提交 asmdef 和 meta。
