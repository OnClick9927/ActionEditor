using UnityEditor.Experimental.GraphView;
using UnityEngine.UIElements;
using UnityEditor.UIElements;
using ActionEditor.Nodes;

public class NpcNode : GraphNode<NpcNodeData>
{

    public override void OnCreated(NodeGraphView view)
    {
        base.OnCreated(view);
        //title = NodeName;

        var textField = new TextField("Npc Name")
        {
            value = data.NpcName,
        };
        textField.RegisterValueChangedCallback(evt =>
        {
            data.NpcName = evt.newValue;
        });
        mainContainer.Add(textField);

        var inputPort = GeneratePort(Direction.Input, typeof(string), Port.Capacity.Single, "Input");
 

        var outputPort = GeneratePort(Direction.Output, typeof(string), Port.Capacity.Multi, "Output");
     

        RefreshExpandedState();
        RefreshPorts();
    }
}


public class MyNode : GraphNode<MyData>
{

    public override void OnCreated(NodeGraphView view)
    {
        base.OnCreated(view);
        //title = NodeName;

        var intField = new IntegerField("Level")
        {
            value = data.NpcLevel,
        };
        intField.RegisterValueChangedCallback(evt =>
        {
            data.NpcLevel = evt.newValue;
        });
        mainContainer.Add(intField);

        var inputPort = GeneratePort(Direction.Input, typeof(int),name: "Input");
    
       

        var outputPort = GeneratePort(Direction.Output, typeof(int),name: "Output");


        RefreshExpandedState();
        RefreshPorts();
    }
}
