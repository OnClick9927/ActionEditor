using ActionAttribute;
using ActionBuffer;
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
        [ReadOnly, Name("资源标识", "用于唯一识别当前图资源的只读标识。")]
        public string guid;
        [Obsolete("Use AssetFileExtensionUtility.Get(assetType) instead.")]
        public const string FileEx = "graph.bytes";
        [ReadOnly][Buffer] internal V4 position = new V4();
        [ReadOnly][Buffer] internal V4 scale = new V4() { x = 1, z = 1, w = 1, y = 1 };
        [ReadOnly, Buffer(nameof(connections))] private List<ConnectionData> _connections = new List<ConnectionData>();
        [ReadOnly, Buffer(nameof(groups))] private List<GroupData> _groups = new List<GroupData>();
        [ReadOnly, Buffer(nameof(nodes))] private List<NodeData> _nodes = new List<NodeData>();

        public IReadOnlyList<ConnectionData> connections => _connections;
        public IReadOnlyList<GroupData> groups => _groups;
        public IReadOnlyList<NodeData> nodes => _nodes;
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

            this._connections.Clear();
            this._groups.Clear();
            this._nodes.Clear();
            this._connections.AddRange(connections.Where(x => x != null));
            this._nodes.AddRange(nodes);
            this._groups.AddRange(groups);
            nodeDic = null;

        }

        public void RegenerateGuids()
        {
            guid = Guid.NewGuid().ToString();
            var remap = new Dictionary<string, string>(StringComparer.Ordinal);
            var visited = new HashSet<NodeData>();

            void Regenerate(NodeData node)
            {
                if (node == null || !visited.Add(node)) return;
                string previous = node.guid;
                string current = Guid.NewGuid().ToString();
                node.guid = current;
                if (!string.IsNullOrEmpty(previous)) remap[previous] = current;
            }

            for (int i = 0; i < _nodes.Count; i++) Regenerate(_nodes[i]);
            for (int i = 0; i < _groups.Count; i++) Regenerate(_groups[i]);

            for (int i = 0; i < _connections.Count; i++)
            {
                ConnectionData connection = _connections[i];
                if (remap.TryGetValue(connection.outNodeGuid, out string output))
                    connection.outNodeGuid = output;
                if (remap.TryGetValue(connection.InNodeGuid, out string input))
                    connection.InNodeGuid = input;
            }
            for (int i = 0; i < _groups.Count; i++)
            {
                List<string> members = _groups[i]._nodes;
                for (int j = 0; j < members.Count; j++)
                    if (remap.TryGetValue(members[j], out string member))
                        members[j] = member;
            }
            nodeDic = null;
        }
        public byte[] ToBytes() => BuffSerializer.ToBytes(this);

        public static GraphAsset FromBytes(Type type, byte[] buffer)
        {
            var asset = BuffSerializer.FromBytes(buffer, type) as GraphAsset;
            asset.Valid();
            return asset;
        }


        private PortData CreateNodePort(NodeData node, string type, string name, PortDirection direction)
        {
            var ports = (direction == PortDirection.In ? node.inPorts : node.outPorts) as List<PortData>;

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

        internal void Valid()
        {
            _nodes.RemoveAll(x => x == null);
        }
    }




}
