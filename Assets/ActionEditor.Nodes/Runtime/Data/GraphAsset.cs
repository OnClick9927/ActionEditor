using System;
using System.Collections.Generic;
using System.Linq;

namespace ActionEditor.Nodes
{


    public abstract class GraphAsset
    {

        public GraphAsset()
        {
            guid = Guid.NewGuid().ToString();
        }
        [ReadOnly]public string guid;
        public const string FileEx = "graph.bytes";
        [ReadOnly][Buffer] internal V4 position = new V4();
        [ReadOnly][Buffer] internal V4 scale = new V4() { x = 1, z = 1, w = 1, y = 1 };

        [ReadOnly] public List<ConnectionData> connections = new List<ConnectionData>();
        [ReadOnly] public List<GroupData> groups = new List<GroupData>();
        [ReadOnly] public List<NodeData> nodes = new List<NodeData>();
        private Dictionary<string, NodeData> nodeDic;
        public NodeData FindNode(string guid)
        {
            nodeDic = nodeDic ?? new Dictionary<string, NodeData>();
            if (!nodeDic.TryGetValue(guid, out var result))
            {
                result = this.nodes.FirstOrDefault(x => x.guid == guid);
                nodeDic.Add(guid, result);
            }
            return result;
        }
        public T FindNode<T>(string guid) where T : NodeData => FindNode(guid) as T;

        internal void Read(List<ConnectionData> connections, List<GroupData> groups, List<NodeData> nodes)
        {

            this.connections.Clear();
            this.groups.Clear();
            this.nodes.Clear();
            this.connections.AddRange(connections.Where(x => x != null));
            this.nodes.AddRange(nodes);
            this.groups.AddRange(groups);

        }
        public byte[] ToBytes() => BuffConverter.ToBytes(this);

        public static GraphAsset FromBytes(Type type, byte[] buffer)
        {
            var asset = BuffConverter.ToObject(buffer, type) as GraphAsset;
            return asset;
        }


        private PortData CreateNodePort(NodeData node, string type, string name, PortDirection direction)
        {
            var ports = direction == PortDirection.In ? node.inPorts : node.outPorts;

            PortData result = ports.Find(x => x.name == name && x.direction == direction && x.type == type);
            if (result == null)
            {
                result = new PortData()
                {
                    direction = direction,
                    type = type,
                    name = name,
                    node = node
                };
                ports.Add(result);
            }
            return result;
        }

        public virtual void PrepareForRuntime()
        {
            nodeDic = nodes.ToDictionary(x => x.guid);
            for (int i = 0; i < connections.Count; i++)
            {
                var conn = connections[i];
                var _out = nodeDic[conn.outNodeGuid];
                var _in = nodeDic[conn.InNodeGuid];
                PortData out_p = CreateNodePort(_out, conn.outPortType, conn.outputPortName, PortDirection.Out);
                PortData in_p = CreateNodePort(_in, conn.inPortType, conn.InPortName, PortDirection.In);

                out_p.connections.Add(conn);
                in_p.connections.Add(conn);
                conn.output = out_p;
                conn.input = in_p;




            }

        }
    }




}