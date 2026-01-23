using ActionEditor.Nodes;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor.Experimental.GraphView;

public class TestWindow : NodeGraphView<TestGraph>
{

    public override void Load(GraphAsset data)
    {
        base.Load(data);
        //titleContent = new UnityEngine.GUIContent("Test");
        this.selection.ConvertAll(x => x as GraphNode);
        //Blackboard blackboard = new Blackboard()
        //{
        //    windowed = false,
        //};
        //root.Add(blackboard);

        //var _toolBar = new Toolbar();
        //_toolBar.Add(new Button(() =>
        //{
        //    Save();
        //})
        //{ text = "Save Data" });
        //rootVisualElement.Add(_toolBar);
    }

    protected override void AfterCreateNode(GraphElement element)
    {
        if (port == null) return;
        if (element.GetType() == port.node.GetType())
        {
            if (port.direction == Direction.Input)
            {
                App.ConnectPort(port, (element as GraphNode).ports.First(x => x.direction == Direction.Output));
            }
            else
            {
                App.ConnectPort(port, (element as GraphNode).ports.First(x => x.direction == Direction.Input));
            }
        }
    }
    GraphPort port;
    protected override List<Type> FitterNodeTypes(List<Type> src, GraphElement element)
    {
        if (element is GraphPort port)
        {
            this.port = port;
            src.RemoveAll(x => port.node.GetType() != x);
        }
        return src;
    }

    protected override bool OnCheckCouldLink(GraphNode startNode, GraphNode endNode, GraphPort start, GraphPort end)
    {
        return start.portType == end.portType;
    }

    public override void OnSelectNode(GraphNode obj)
    {

    }
}
