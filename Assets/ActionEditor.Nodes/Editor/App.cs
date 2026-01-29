using ActionEditor;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UIElements;

namespace ActionEditor.Nodes
{
    public static class App
    {
        private static Dictionary<Type, Type> nodeDic = new Dictionary<Type, Type>();
        private static Dictionary<Type, Type> nodeDic_Reverse = new Dictionary<Type, Type>();
        internal static DateTime LastSaveTime => _lastSaveTime;

        private static DateTime _lastSaveTime = DateTime.Now;
        internal static GraphWindow window;
        internal static string[] AssetNames;
        internal static Dictionary<string, Type> AssetTypes;
        private static string key => Prefs.CONFIG_PATH;
        private static string openPath = string.Empty;
        private static GraphAsset _asset;

        public static GraphAsset asset => _asset;
        public static string assetPath => openPath;
        public static NodeGraphView view;




        internal static NodeGraphView CreateView(VisualElement root)
        {
            GraphAsset asset = App.asset;
            if (asset == null) return null;
            var find = AppDomain.CurrentDomain.GetAssemblies()
                        .SelectMany(item => item.GetTypes())
                        .Where(item => !item.IsAbstract && item.BaseType != null && item.IsSubclassOf(typeof(NodeGraphView)))
                        .Where(x => x.BaseType.GetGenericArguments()[0] == asset.GetType())
                        .FirstOrDefault();
            var _view = Activator.CreateInstance(find) as NodeGraphView;

            _view.StretchToParentSize();
            root.Add(_view);
            App.view = _view;
            _view.Load(asset);
            return _view;
        }

        public static void OnObjectPickerConfig(string path)
        {
            if (string.IsNullOrEmpty(path) || !path.EndsWith(GraphAsset.FileEx)) return;
            try
            {
                var txt = AssetDatabase.LoadAssetAtPath<TextAsset>(path);
                _asset = GraphAsset.FromBytes(typeof(GraphAsset), txt.bytes);
                openPath = path;
                window?.ShowGraph();
            }
            catch (Exception e)
            {
                Debug.LogException(e);
                _asset = null;
                throw;
            }
        }


        internal static void OnWindowEnable()
        {
            Lan.Load();
            Prefs.Valid();
            AssetTypes = EditorEX.GetImplementationsOf(typeof(GraphAsset)).ToDictionary(x => x.Name, y => y);
            AssetNames = AssetTypes.Keys.ToArray();

            var find = AppDomain.CurrentDomain.GetAssemblies()
                             .SelectMany(item => item.GetTypes())
                             .Where(item => !item.IsAbstract && item.IsSubclassOf(typeof(GraphNode)))
                             .Select(x => new { dataType = x.BaseType.GetGenericArguments()[0], node = x });

            nodeDic = find.ToDictionary(x => x.dataType, x => x.node);
            nodeDic_Reverse = find.ToDictionary(x => x.node, x => x.dataType);
            OnObjectPickerConfig(PlayerPrefs.GetString(key));

        }
        internal static void OnWindowDisable()
        {
            PlayerPrefs.SetString(key, openPath);
        }




        public static List<Type> GetNodeEditorTypes() => nodeDic.Values.ToList();

        public static Type GetNodeEditorType(Type node) => nodeDic[node];
        public static Type GetNodeDataType(Type node) => nodeDic_Reverse[node];




        internal static void Update()
        {

            if (view != null)
            {
                view.Update();
                foreach (var item in view.nodes)
                {
                    item.OnUpdate();
                }
            }
            TryAutoSave();
        }
        private static void TryAutoSave()
        {
            var timespan = DateTime.Now - _lastSaveTime;
            if (timespan.Seconds > Prefs.autoSaveSeconds)
            {
                Save();
            }
        }
        public static void SaveAs()
        {
            if (_asset == null || view == null) return;
            var srcname = System.IO.Path.GetFileName(App.assetPath);
            srcname = srcname.Remove(srcname.IndexOf(GraphAsset.FileEx) - 1);
            string path = EditorUtility.SaveFilePanel(Lan.ins.SaveAs, Prefs.savePath, srcname + "_", GraphAsset.FileEx);

            if (!string.IsNullOrEmpty(path))
            {
                while (true)
                {
                    var index = path.IndexOf(Asset.FileEx);
                    if (index == -1) break;
                    path = path.Remove(index - 1);
                }
                path = $"{path}.{GraphAsset.FileEx}";
                if (path != App.assetPath)
                {
                    var txt = App.asset.ToBytes();
                    File.WriteAllBytes(path, txt);
                    AssetDatabase.Refresh();
                }
            }
        }


        internal static void Save()
        {
            if (_asset == null || view == null) return;
            _asset.position = view.viewTransform.position;
            _asset.scale = view.viewTransform.scale;
            var connections = view.connections;
            connections.RemoveAll(x => x.output == null || x.input == null);

            connections.Sort((a, b) =>
            {
                if (a.input.node == b.input.node)
                {
                    return a.output.node.GetPosition().center.y
                    .CompareTo(b.output.node.GetPosition().center.y);
                }
                else if (a.output.node == b.output.node)
                {
                    return a.input.node.GetPosition().center.y
                    .CompareTo(b.input.node.GetPosition().center.y);
                }

                return 0;
            });



            asset.Read(connections.ConvertAll(x => Connection2Data(x)),
                         view.groups.ConvertAll(x => Group2Data(x)),
                         view.nodes.ConvertAll(x => Node2Data(x)));
            File.WriteAllBytes(openPath, _asset.ToBytes());
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            _lastSaveTime = DateTime.Now;
            window.Repaint();
        }




        public static Edge ConnectPort(GraphPort a, GraphPort b)
        {
            GraphPort input = a.direction == Direction.Input ? a : b;
            GraphPort output = a.direction == Direction.Output ? a : b;
            var connection = new GraphConnection()
            {
                output = output,
                input = input
            };
            connection?.input.Connect(connection);
            connection?.output.Connect(connection);
            view.Add(connection);
            GraphPort.ValidConnection(view, connection);
            return connection;
        }








        public static void Duplicate() => Duplicate(view.selection.ConvertAll(x => x as GraphElement));
        public static List<GraphElement> Duplicate(List<GraphElement> src)
        {
            List<GraphElement> result = new List<GraphElement>();
            Vector2 offset = Vector2.one * 100;
            var groups = src.Where(x => x is GraphGroup).Select(x => x as GraphGroup).ToList();
            var nodes = src.Where(x => x is GraphNode).Select(x => x as GraphNode).ToList();

            var connectedPorts = src.Select(x => x as Edge).Where(x => x != null)
                .Where(x => nodes.Contains(x.output.node) && nodes.Contains(x.input.node))
                .ToList();
            var datas = nodes.ConvertAll(x => App.Node2Data(x).DeepCopyByBuffer());
            var groupDatas = groups.ConvertAll(x => App.Group2Data(x).DeepCopyByBuffer() as GroupData).Select(
                x =>
                {
                    var _rect = (Rect)x.position;
                    x.position = new Rect(_rect.position + offset, _rect.size);
                    return x;
                }
                );
            var conDatas = connectedPorts.ConvertAll(x => App.Connection2Data(x));
            foreach (var data in datas)
            {
                string oldGuid = data.guid;
                var newGuid = Guid.NewGuid().ToString();
                var find = groupDatas.Where(x => x.nodes.Contains(oldGuid));
                foreach (var _find in find)
                {
                    _find.nodes.Remove(oldGuid);
                    _find.nodes.Add(newGuid);
                }
                var find_in = conDatas.FindAll(x => x.InNodeGuid == oldGuid);
                for (int i = 0; i < find_in.Count; i++)
                    find_in[i].InNodeGuid = newGuid;
                var find_out = conDatas.FindAll(x => x.outNodeGuid == oldGuid);
                for (int i = 0; i < find_out.Count; i++)
                    find_out[i].outNodeGuid = newGuid;
                data.guid = newGuid;

                var rect = (Rect)data.position;

                data.position = new Rect(rect.position + offset, rect.size);
            }

            CreateElements(result, datas, groupDatas, conDatas);
            view.ClearSelection();
            foreach (var item in result)
                view.AddToSelection(item);
            return result;
        }
        public static void CreateElements(List<GraphElement> result, IEnumerable<NodeData> nodes, IEnumerable<GroupData> groups, IEnumerable<ConnectionData> cons)
        {
            foreach (var data in nodes)
                result.Add(CreateNode(GetNodeEditorType(data.GetType()), data));
            foreach (var item in cons)
                result.Add(CreateConnection(item));
            foreach (var data in groups)
                result.Add(CreateGroup(data));
        }
        public static GraphNode CreateNode(Type nodeType, NodeData nodeData)
        {
            GraphNode node = Activator.CreateInstance(nodeType) as GraphNode;
            if (nodeData == null)
                nodeData = Activator.CreateInstance(App.GetNodeDataType(nodeType)) as NodeData;

            var field = nodeType.GetField(nameof(GraphNode<NodeData>.data));
            field.SetValue(node, nodeData);
            node.SetPosition(nodeData.position);
            node.onSelected += view.OnSelectNode;
            node?.OnCreated(view);
            view.AddElement(node);
            return node;
        }


        public static Edge CreateConnection(ConnectionData data)
        {
            var input = view.ports.Find(x => x.node.GUID == data.InNodeGuid && x.direction == Direction.Input
                                    && x.portName == data.InPortName
                                    && x.portType.FullName == data.inPortType);
            var output = view.ports.Find(x => x.node.GUID == data.outNodeGuid && x.direction == Direction.Output
                                    && x.portName == data.outputPortName
                                    && x.portType.FullName == data.outPortType);
            if (input != null && output != null)
            {
                return ConnectPort(input, output);
            }
            return null;
        }
        public static GraphGroup CreateGroup(GroupData data)
        {
            var group = new GraphGroup(view);
            if (data != null)
            {
                group.SetData(data);
                group.SetPosition(data.position);
                group.AddElements(view.nodes.Where(x => data.nodes.Contains(x.GUID)));
            }
            view.AddElement(group);
            return group;
        }
        public static NodeData Node2Data(GraphNode node)
        {
            var nodeType = node.GetType();
            var field = nodeType.GetField(nameof(GraphNode<NodeData>.data));
            NodeData data = field.GetValue(node) as NodeData;
            data.position = node.GetPosition();
            return data;
        }
        public static GroupData Group2Data(GraphGroup group)
        {
            var guids = group.containedNodes.ConvertAll(x => x.GUID);
            var data = group.data;
            data.nodes = guids;
            data.position = group.GetPosition();
            return data;
        }
        public static ConnectionData Connection2Data(Edge edge)
        {
            //var connection = edge as GraphConnection;
            //GraphPort output = connection.output;
            //GraphPort input = connection.input;
            if (edge.input == null || edge.output == null) return null;
            GraphNode outputNode = edge.output.node as GraphNode;
            GraphNode inputNode = edge.input.node as GraphNode;
            if (outputNode == null || inputNode == null) return null;

            return new ConnectionData
            {
                outNodeGuid = outputNode.GUID,
                outputPortName = edge.output.portName,
                outPortType = edge.output.portType.FullName,
                inPortType = edge.input.portType.FullName,
                InNodeGuid = inputNode.GUID,
                InPortName = edge.input.portName
            };
        }






    }

}