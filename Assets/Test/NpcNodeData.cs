using ActionEditor;
using ActionEditor.Nodes;
using System;

[Serializable]
[Name("NPC")]
[Attachable(typeof(TestGraph))]
[Node("Test/Npc")]

public class NpcNodeData : NodeData
{
    public string NpcName;
}

[Serializable]
[Name("我的")]
[Attachable(typeof(TestGraph))]

[Node("Test/My")]

public class MyData : NodeData
{
    public int NpcLevel;
}
