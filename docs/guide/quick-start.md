# 五分钟上手

## 1. 序列化一个对象

```csharp
using ActionBuffer;

public sealed class PlayerSave
{
    public int level;
    public string name;

    [Buffer] public int Checkpoint { get; private set; }
    public void SetCheckpoint(int value) => Checkpoint = value;
}

var source = new PlayerSave { level = 12, name = "Alice" };
source.SetCheckpoint(4);

byte[] bytes = BuffSerializer.ToBytes(source);
PlayerSave copy = BuffSerializer.FromBytes<PlayerSave>(bytes);
string json = BuffSerializer.ToJson(source,
    new BuffSettings { PrettyPrint = true });
```

公开字段默认参与序列化；属性无论公开还是私有都必须标记 `[Buffer]`。属性需要 getter 和 setter，索引器不参与序列化。

## 2. 定义一种 Timeline 资源

```csharp
using ActionAttribute;
using ActionEditor;

[Name("战斗时间轴", "组织战斗演出的轨道和片段。")]
[AssetFileExtension("combat.action.bytes")]
public sealed class CombatTimeline : Asset { }
```

从 ActionEditor 窗口创建或打开资源。文件搜索框会按实际类型过滤，只显示以 `.combat.action.bytes` 结尾的文件。

## 3. 定义一种节点图

```csharp
using ActionAttribute;
using ActionEditor;
using ActionEditor.Nodes;

[Name("任务图")]
[AssetFileExtension("quest.graph.bytes")]
public sealed class QuestGraph : GraphAsset { }

[System.Serializable]
[Name("输出文本")]
[Node("行为")]
[Attachable(typeof(QuestGraph))]
public sealed class PrintNode : NodeData
{
    [Name("内容", "执行节点时输出的文本。")]
    public string text;
}
```

业务节点的 GraphView 视图继承 `GraphNode<PrintNode>`。`data` 是节点数据，`asset` 是当前顶层 `QuestGraph`，不需要从全局窗口反查。

## 4. 运行行为树

```csharp
using ActionEditor.Nodes.BT;

public sealed class GameBlackboard : Blackboard
{
    public int hp;
    public int targetCount;
}

public sealed class GameTree : BTTree
{
    private readonly GameBlackboard data = new GameBlackboard();
    protected override Blackboard blackboard => data;
    public GameBlackboard Data => data;
}

GameTree tree = LoadTreeBytes();
tree.PrepareForRuntime(path => LoadSubTree(path));
BTNode.State result = tree.Update();

List<int> snapshot = tree.CollectStatus();
tree.ReadStatus(snapshot);
```

主树必须正好有一个连接完整的 `BTRoot`。子树由 loader 返回、必须是同一具体 `BTTree` 类型并设置 `IsSubTree`。固定 Tick 中每次调用一次 `Update`；确定性逻辑使用 Tick 节点和整数黑板字段，不读取真实时间和随机数。

## 5. Inspector 特性组合

```csharp
using ActionAttribute;
using UnityEngine;

public sealed class CharacterConfig : MonoBehaviour
{
    [Title("基础数值")]
    [Name("生命值", "角色可承受的伤害总量。")]
    [Clamp(0, 100), ProgressBar(0, 100), SuffixLabel("点")]
    [SerializeField] private int health = 100;

    [ShowIf(nameof(advanced))]
    [FolderPath, Name("输出目录")]
    [SerializeField] private string output = "Assets";

    [SerializeField] private bool advanced;
}
```

多个特性由统一的 ActionPropertyDrawer 计算高度并组合绘制。条件回调、下拉数据源和校验回调会在 Inspector 重绘时频繁执行，必须快速且无副作用。
