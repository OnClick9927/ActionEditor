using ActionAttribute;
using ActionBuffer;
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
        private sealed class UndoHistoryEntry
        {
            internal string Name;
            internal byte[] Data;
            internal DateTime CreatedAt;
            internal string[] SelectedGuids;
            internal Vector2 InspectorScroll;
        }

        private const int MaxUndoHistoryCount = 100;

        private static Dictionary<Type, Type> nodeDic = new Dictionary<Type, Type>();
        //private static Dictionary<Type, Type> nodeDic_Reverse = new Dictionary<Type, Type>();
        internal static DateTime LastSaveTime => _lastSaveTime;

        private static DateTime _lastSaveTime = DateTime.Now;
        internal static GraphWindow window;
        internal static string[] AssetNames;
        internal static Dictionary<string, Type> AssetTypes;
        private static string key => Prefs.CONFIG_PATH;
        private static string openPath = string.Empty;
        private static GraphAsset _asset;
        private static bool _restoringUndo;
        private static bool _undoCommitScheduled;
        private static string _scheduledUndoName;
        private static bool _inspectorUndoPending;
        private static double _inspectorUndoDeadline;
        private static string _inspectorUndoName;
        private static bool _undoInitializationPending;
        private static int _undoInitializationDelay;
        private static string _undoInitializationPath;
        private static NodeGraphView _undoInitializationView;
        private static byte[] _undoInitializationCandidate;
        private static readonly List<UndoHistoryEntry> _undoHistory =
            new List<UndoHistoryEntry>();
        private static int _undoHistoryIndex = -1;
        private static byte[] _savedData;

        public static GraphAsset asset => _asset;
        public static string assetPath => openPath;
        public static NodeGraphView view;
        internal static bool IsDirty { get; private set; }
        internal static int UndoHistoryCount => _undoHistory.Count;
        internal static int CurrentUndoIndex => _undoHistoryIndex;

        internal static string GetUndoHistoryName(int index) =>
            _undoHistory[index].Name;

        internal static string GetUndoHistoryTime(int index) =>
            _undoHistory[index].CreatedAt.ToString("HH:mm:ss");




        internal static NodeGraphView CreateView(VisualElement root)
        {
            GraphAsset asset = App.asset;
            if (asset == null) return null;
            var find = TypeHelper.GetSubTypes(typeof(NodeGraphView))
              .Where(x => x.BaseType.GetGenericArguments().FirstOrDefault() == asset.GetType())
              .FirstOrDefault();

            var _view = Activator.CreateInstance(find) as NodeGraphView;

            _view.StretchToParentSize();
            root.Add(_view);
            App.view = _view;
            _view.Load(asset);
            _view.InitializeUndoTracking();
            return _view;
        }

        public static bool OnObjectPickerConfig(string path)
        {
            if (string.IsNullOrEmpty(path) || !File.Exists(path)
                || !path.EndsWith(GraphAsset.FileEx)) return false;
            if (path == openPath && _asset != null && _undoHistory.Count > 0)
                return true;
            if (!ConfirmAssetSwitch()) return false;
            try
            {
                var txt = AssetDatabase.LoadAssetAtPath<TextAsset>(path);
                GraphAsset loadedAsset = GraphAsset.FromBytes(
                    typeof(GraphAsset), txt.bytes);
                _asset = loadedAsset;
                openPath = path;
                window?.ShowGraph();
                ResetUndo();
                return true;
            }
            catch (Exception e)
            {
                Debug.LogException(e);
                return false;
            }
        }

        private static bool ConfirmAssetSwitch()
        {
            FlushPendingUndo();
            if (!IsDirty || _asset == null) return true;
            string fileName = Path.GetFileName(openPath);
            int result = EditorUtility.DisplayDialogComplex(
                "Unsaved Changes",
                $"Save changes to \"{fileName}\" before opening another file?",
                "Save", "Cancel", "Don't Save");
            if (result == 1) return false;
            if (result == 0) Save();
            return true;
        }

        internal static void RebuildCurrentView()
        {
            if (_asset == null || view == null || window == null) return;
            FlushPendingUndo();
            UpdateCurrentUndoContext();
            var context = new UndoHistoryEntry();
            CaptureGraphContext(context);
            bool restartInitialization = _undoInitializationPending;
            Vector3 viewPosition = view.viewTransform.position;
            Vector3 viewScale = view.viewTransform.scale;
            SyncGraphToAsset();
            window.ShowGraph();
            view?.UpdateViewTransform(viewPosition, viewScale);
            RestoreGraphContext(context);
            if (restartInitialization) ResetUndo();
            FocusGraphView();
            window.Repaint();
        }


        internal static void OnWindowEnable()
        {
            Lan.Load();
            Prefs.Valid();
            AssetTypes = TypeHelper.GetSubTypes(typeof(GraphAsset)).ToDictionary(x => x.Name, y => y);
            AssetNames = AssetTypes.Keys.ToArray();

            //var types = AppDomain.CurrentDomain.GetAssemblies()
            //                .SelectMany(item => item.GetTypes()).Where(x => !x.IsAbstract);

            var find = TypeHelper.GetSubTypes(typeof(GraphNode))

                             .Where(item => item.BaseType != typeof(GraphNode))
                             .Select(x => new { dataType = x.BaseType.GetGenericArguments()[0], node = x });


            nodeDic = find.ToDictionary(x => x.dataType.IsGenericParameter ? x.dataType.BaseType : x.dataType, x => x.node);
            var result = TypeHelper.GetSubTypes(typeof(NodeData)).Where(x =>
                x != typeof(GroupData) && x != typeof(GraphCommentData) &&
                !nodeDic.ContainsKey(x));
            foreach (var item in result)
            {
                var temp = item;
                while (true)
                {
                    if (temp != typeof(NodeData))
                    {
                        if (nodeDic.ContainsKey(temp))
                        {
                            nodeDic[item] = nodeDic[temp];
                            break;
                        }
                        else
                        {
                            temp = temp.BaseType;
                        }
                    }
                    else
                    {
                        nodeDic.Add(item, typeof(GraphNodeDefault));
                        break;
                    }
                }
            }


            //nodeDic_Reverse = nodeDic.ToDictionary(x => x.Value, x => x.Key);
            OnObjectPickerConfig(PlayerPrefs.GetString(key));

        }
        internal static void OnWindowDisable()
        {
            PlayerPrefs.SetString(key, openPath);
        }

        internal static void ShutdownUndo()
        {
            EditorApplication.delayCall -= FlushScheduledUndo;
            EditorApplication.update -= FlushInspectorUndo;
            EditorApplication.update -= FinalizeUndoInitialization;
            _undoCommitScheduled = false;
            _inspectorUndoPending = false;
            _undoInitializationPending = false;
            _undoInitializationCandidate = null;
            _undoHistory.Clear();
            _undoHistoryIndex = -1;
            _savedData = null;
            IsDirty = false;
        }

        private static void ResetUndo()
        {
            EditorApplication.delayCall -= FlushScheduledUndo;
            EditorApplication.update -= FlushInspectorUndo;
            EditorApplication.update -= FinalizeUndoInitialization;
            _undoCommitScheduled = false;
            _inspectorUndoPending = false;
            _undoHistory.Clear();
            _undoHistoryIndex = -1;
            _savedData = null;
            _undoInitializationCandidate = null;
            _undoInitializationDelay = 2;
            _undoInitializationPath = openPath;
            _undoInitializationView = view;
            _undoInitializationPending = _asset != null && view != null;
            if (_undoInitializationPending)
                EditorApplication.update += FinalizeUndoInitialization;
            SetDirty(false);
        }

        private static void FinalizeUndoInitialization()
        {
            if (!_undoInitializationPending || _asset == null || view == null ||
                _undoInitializationPath != openPath ||
                !ReferenceEquals(_undoInitializationView, view))
            {
                CancelUndoInitialization();
                return;
            }
            if (view.panel == null || _undoInitializationDelay-- > 0) return;

            byte[] data = CaptureGraphBytes();
            if (!BytesEqual(_undoInitializationCandidate, data))
            {
                _undoInitializationCandidate = data;
                return;
            }

            EditorApplication.update -= FinalizeUndoInitialization;
            _undoInitializationPending = false;
            _undoInitializationCandidate = null;
            _undoInitializationView = null;
            _undoHistory.Clear();
            _undoHistory.Add(CreateUndoEntry("Initial State", data));
            _undoHistoryIndex = 0;
            _savedData = (byte[])data.Clone();
            SetDirty(false);
            window?.Repaint();
        }

        private static void CancelUndoInitialization()
        {
            EditorApplication.update -= FinalizeUndoInitialization;
            _undoInitializationPending = false;
            _undoInitializationCandidate = null;
            _undoInitializationView = null;
        }

        internal static void RequestUndoCommit(string name)
        {
            if (_restoringUndo || _asset == null || view == null) return;
            _scheduledUndoName = name;
            if (_undoCommitScheduled) return;
            _undoCommitScheduled = true;
            EditorApplication.delayCall += FlushScheduledUndo;
        }

        private static void FlushScheduledUndo()
        {
            EditorApplication.delayCall -= FlushScheduledUndo;
            _undoCommitScheduled = false;
            CommitUndo(_scheduledUndoName);
        }

        internal static void RequestInspectorUndoCommit(string name)
        {
            if (_restoringUndo || _asset == null || view == null) return;
            _inspectorUndoName = name;
            _inspectorUndoDeadline = EditorApplication.timeSinceStartup + 0.2;
            if (_inspectorUndoPending) return;
            _inspectorUndoPending = true;
            EditorApplication.update += FlushInspectorUndo;
        }

        private static void FlushInspectorUndo()
        {
            if (EditorApplication.timeSinceStartup < _inspectorUndoDeadline) return;
            EditorApplication.update -= FlushInspectorUndo;
            _inspectorUndoPending = false;
            CommitUndo(_inspectorUndoName);
        }

        private static void FlushPendingUndo()
        {
            if (_undoCommitScheduled)
            {
                EditorApplication.delayCall -= FlushScheduledUndo;
                _undoCommitScheduled = false;
                CommitUndo(_scheduledUndoName);
            }
            if (!_inspectorUndoPending) return;
            EditorApplication.update -= FlushInspectorUndo;
            _inspectorUndoPending = false;
            CommitUndo(_inspectorUndoName);
        }

        internal static void CommitUndo(string name)
        {
            if (_restoringUndo || _asset == null || view == null) return;
            byte[] data = CaptureGraphBytes();
            if (_undoHistoryIndex >= 0 &&
                BytesEqual(_undoHistory[_undoHistoryIndex].Data, data))
            {
                CaptureGraphContext(_undoHistory[_undoHistoryIndex]);
                return;
            }

            string undoName = string.IsNullOrEmpty(name) ? "Edit Graph" : name;
            RemoveRedoHistory();
            _undoHistory.Add(CreateUndoEntry(undoName, data));
            _undoHistoryIndex = _undoHistory.Count - 1;
            TrimUndoHistory();
            UpdateDirty(data);
        }

        private static void PrepareUndoSnapshot()
        {
            if (_restoringUndo || _asset == null || view == null) return;
            FlushPendingUndo();
            CommitUndo("Edit Graph");
        }

        internal static void PerformUndo()
        {
            FlushPendingUndo();
            UpdateCurrentUndoContext();
            RestoreUndoHistoryCore(_undoHistoryIndex - 1);
        }

        internal static void PerformRedo()
        {
            FlushPendingUndo();
            UpdateCurrentUndoContext();
            RestoreUndoHistoryCore(_undoHistoryIndex + 1);
        }

        internal static void FlushUndoHistory()
        {
            FlushPendingUndo();
            UpdateCurrentUndoContext();
        }

        internal static void RestoreUndoHistory(int index)
        {
            FlushPendingUndo();
            UpdateCurrentUndoContext();
            RestoreUndoHistoryCore(index);
        }

        private static void RestoreUndoHistoryCore(int index)
        {
            if (index < 0 || index >= _undoHistory.Count ||
                index == _undoHistoryIndex)
                return;

            UndoHistoryEntry entry = _undoHistory[index];
            byte[] data = entry.Data;
            if (data == null) return;
            _restoringUndo = true;
            try
            {
                Vector3 viewPosition = view == null
                    ? Vector3.zero
                    : view.viewTransform.position;
                Vector3 viewScale = view == null
                    ? Vector3.one
                    : view.viewTransform.scale;
                _asset = GraphAsset.FromBytes(typeof(GraphAsset), data);
                window?.ShowGraph();
                view?.UpdateViewTransform(viewPosition, viewScale);
                RestoreGraphContext(entry);
                _undoHistoryIndex = index;
                UpdateDirty(data);
                window?.Repaint();
                FocusGraphView();
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
            }
            finally
            {
                _restoringUndo = false;
            }
        }

        internal static void ClearUndoHistory()
        {
            FlushPendingUndo();
            if (_asset == null || view == null) return;
            byte[] data = CaptureGraphBytes();
            _undoHistory.Clear();
            _undoHistory.Add(CreateUndoEntry("Current State", data));
            _undoHistoryIndex = 0;
            UpdateDirty(data);
            window?.Repaint();
        }

        private static UndoHistoryEntry CreateUndoEntry(string name, byte[] data)
        {
            var entry = new UndoHistoryEntry
            {
                Name = name,
                Data = data,
                CreatedAt = DateTime.Now
            };
            CaptureGraphContext(entry);
            return entry;
        }

        private static void CaptureGraphContext(UndoHistoryEntry entry)
        {
            if (entry == null || view == null) return;
            entry.SelectedGuids = view.selection
                .OfType<GraphNode>()
                .Select(x => x.GUID)
                .Concat(view.selection.OfType<GraphComment>()
                    .Select(x => x.GUID))
                .Concat(view.selection.OfType<GraphGroup>()
                    .Select(x => x.data.guid))
                .Distinct()
                .ToArray();
            entry.InspectorScroll = view.InspectorScrollPosition;
        }

        private static void UpdateCurrentUndoContext()
        {
            if (_undoHistoryIndex < 0 || _undoHistoryIndex >= _undoHistory.Count)
                return;
            CaptureGraphContext(_undoHistory[_undoHistoryIndex]);
        }

        private static void RestoreGraphContext(UndoHistoryEntry entry)
        {
            if (view == null || entry == null) return;
            view.InspectorScrollPosition = entry.InspectorScroll;
            view.ClearSelection();
            if (entry.SelectedGuids == null || entry.SelectedGuids.Length == 0)
                return;

            var selectedGuids = new HashSet<string>(entry.SelectedGuids);
            foreach (GraphNode node in view.nodes)
            {
                if (selectedGuids.Contains(node.GUID))
                    view.AddToSelection(node);
            }
            foreach (GraphComment comment in view.comments)
            {
                if (selectedGuids.Contains(comment.GUID))
                    view.AddToSelection(comment);
            }
            foreach (GraphGroup group in view.groups)
            {
                if (selectedGuids.Contains(group.data.guid))
                    view.AddToSelection(group);
            }
        }

        private static void FocusGraphView()
        {
            GraphWindow graphWindow = window;
            NodeGraphView graphView = view;
            graphWindow?.Focus();
            graphView?.Focus();
            graphView?.schedule.Execute(() =>
            {
                if (!ReferenceEquals(view, graphView)) return;
                graphWindow?.Focus();
                graphView.Focus();
            });
        }

        private static bool BytesEqual(byte[] left, byte[] right)
        {
            if (ReferenceEquals(left, right)) return true;
            if (left == null || right == null || left.Length != right.Length) return false;
            for (int i = 0; i < left.Length; i++)
            {
                if (left[i] != right[i]) return false;
            }
            return true;
        }

        private static void RemoveRedoHistory()
        {
            int firstRedoIndex = _undoHistoryIndex + 1;
            if (firstRedoIndex < _undoHistory.Count)
                _undoHistory.RemoveRange(firstRedoIndex,
                    _undoHistory.Count - firstRedoIndex);
        }

        private static void TrimUndoHistory()
        {
            int removeCount = _undoHistory.Count - MaxUndoHistoryCount;
            if (removeCount <= 0) return;

            int removableCount = Math.Min(removeCount, _undoHistory.Count - 1);
            if (removableCount <= 0) return;
            _undoHistory.RemoveRange(1, removableCount);
            if (_undoHistoryIndex > 0)
                _undoHistoryIndex = Math.Max(1,
                    _undoHistoryIndex - removableCount);
        }

        private static void UpdateDirty(byte[] data)
        {
            SetDirty(!BytesEqual(_savedData, data));
        }

        private static void SetDirty(bool value)
        {
            if (IsDirty == value) return;
            IsDirty = value;
            window?.Repaint();
        }




        //public static List<Type> GetNodeEditorTypes() => nodeDic.Values.ToList();
        public static List<Type> GetNodeTypes() => nodeDic.Keys.ToList();

        public static Type GetNodeEditorType(Type node) => nodeDic[node];
        //public static Type GetNodeDataType(Type node) => nodeDic_Reverse[node];
        public static NodeGraphView.UpdateType updateType => view != null ? view.updateType : NodeGraphView.UpdateType.Inspector;

        internal static void Update()
        {
            if (view != null)
            {
                view.Update();
                view.UpdateNodeViews();
            }
            TryAutoSave();
        }


        private static double _nextAutoSaveCheck;
        private static void TryAutoSave()
        {
            double currentTime = EditorApplication.timeSinceStartup;
            if (currentTime < _nextAutoSaveCheck) return;
            _nextAutoSaveCheck = currentTime + 1;

            var timespan = DateTime.Now - _lastSaveTime;
            if (timespan.TotalSeconds > Prefs.autoSaveSeconds)
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
                    var index = path.IndexOf(GraphAsset.FileEx);
                    if (index == -1) break;
                    path = path.Remove(index - 1);
                }
                path = $"{path}.{GraphAsset.FileEx}";
                if (path != App.assetPath)
                {
                    var tree = App.asset.DeepCopyByBuffer();
                    tree.guid = Guid.NewGuid().ToString();
                    File.WriteAllBytes(path, tree.ToBytes());
                    AssetDatabase.Refresh();
                }
            }
        }


        internal static void Save()
        {
            if (_asset == null || view == null) return;
            SyncGraphToAsset();
            byte[] data = _asset.ToBytes();
            File.WriteAllBytes(openPath, data);
            _savedData = data;
            SetDirty(false);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            _lastSaveTime = DateTime.Now;
            window.Repaint();
        }

        private static byte[] CaptureGraphBytes()
        {
            if (_asset == null) return null;
            SyncGraphToAsset();
            return _asset.ToBytes();
        }

        private static void SyncGraphToAsset()
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

                return a.output.node.GUID.CompareTo(b.output.node.GUID);
            });



            List<NodeData> nodeData = view.nodes.ConvertAll(x => Node2Data(x));
            nodeData.AddRange(view.comments.ConvertAll(x => Comment2Data(x)));
            asset.Read(connections.ConvertAll(x => Connection2Data(x)),
                         view.groups.ConvertAll(x => Group2Data(x)), nodeData);
        }




        public static Edge ConnectPort(GraphPort a, GraphPort b, bool recordUndo = true)
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
            if (recordUndo) RequestUndoCommit("Create Connection");
            return connection;
        }







        public static void SelectAll()
        {
            view.selection.Clear();
            foreach (var item in view.groups)
                view.AddToSelection(item);
            foreach (var item in view.connections)
                view.AddToSelection(item);
            foreach (var item in view.nodes)
                view.AddToSelection(item);
            foreach (var item in view.comments)
                view.AddToSelection(item);
        }
        public static List<GraphElement> Duplicate()
        {
            var result = Duplicate(view.selection.ConvertAll(x => x as GraphElement));
            view.selection.Clear();
            foreach (GraphElement e in result)
            {
                view.AddToSelection(e);
            }
            return result;
        }

        public static List<GraphElement> Duplicate(List<GraphElement> src)
        {
            PrepareUndoSnapshot();
            List<GraphElement> result = new List<GraphElement>();
            Vector2 offset = Vector2.one * 100;
            var groups = src.Where(x => x is GraphGroup).Select(x => x as GraphGroup).ToList();
            var nodes = src.Where(x => x is GraphNode).Select(x => x as GraphNode).ToList();
            var comments = src.OfType<GraphComment>().ToList();

            var connectedPorts = src.Select(x => x as Edge).Where(x => x != null)
                .Where(x => nodes.Contains(x.output.node) && nodes.Contains(x.input.node))
                .ToList();
            var datas = nodes.ConvertAll(x => App.Node2Data(x).DeepCopyByBuffer());
            datas.AddRange(comments.ConvertAll(x =>
                App.Comment2Data(x).DeepCopyByBuffer()));
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
                    var _nodes = _find.nodes as List<string>;
                    _nodes.Remove(oldGuid);
                    _nodes.Add(newGuid);
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
            RequestUndoCommit("Duplicate Graph Elements");
            //view.ClearSelection();
            //foreach (var item in result)
            //    view.AddToSelection(item);
            return result;
        }
        public static void CreateElements(List<GraphElement> result, IEnumerable<NodeData> nodes, IEnumerable<GroupData> groups, IEnumerable<ConnectionData> cons)
        {
            foreach (var data in nodes)
            {
                if (data is GraphCommentData commentData)
                    result.Add(CreateComment(commentData));
                else
                    result.Add(CreateNode(data.GetType(), data));
            }
            foreach (var item in cons)
                result.Add(CreateConnection(item));
            foreach (var data in groups)
                result.Add(CreateGroup(data));
        }
        public static GraphNode CreateNode(Type dataType, NodeData nodeData)
        {
            bool isNewNode = nodeData == null;
            GraphNode node = null;
            var nodeType = GetNodeEditorType(dataType);
            if (nodeType.IsGenericType)
                nodeType = nodeType.MakeGenericType(dataType);


            node = Activator.CreateInstance(nodeType) as GraphNode;
            if (nodeData == null)
                nodeData = Activator.CreateInstance(dataType) as NodeData;

            var field = node.GetType().GetField(nameof(GraphNode<NodeData>.data));
            field.SetValue(node, nodeData);
            node.SetPosition(nodeData.position);
            node.onSelected += view.OnSelectNode;
            node?.OnCreated(view);
            view.AddElement(node);
            if (isNewNode) RequestUndoCommit("Create Node");
            return node;
        }

        public static GraphComment CreateComment(GraphCommentData data = null)
        {
            bool isNewComment = data == null;
            var comment = new GraphComment(data ?? new GraphCommentData());
            view.AddElement(comment);
            if (isNewComment) RequestUndoCommit("Create Comment");
            return comment;
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
                return ConnectPort(input, output, false);
            }
            return null;
        }
        public static GraphGroup CreateGroup(GroupData data)
        {
            bool isNewGroup = data == null;
            var group = new GraphGroup(view);
            if (data != null)
            {
                group.SetData(data);
                group.SetPosition(data.position);
                group.AddElements(view.nodes.Where(x => data.nodes.Contains(x.GUID)));
            }
            view.AddElement(group);
            if (isNewGroup) RequestUndoCommit("Create Group");
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
        public static GraphCommentData Comment2Data(GraphComment comment) =>
            comment.WriteData();
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

        private static Dictionary<Type, string> node_paths = new();
        public static string GetNodePath(Type dataType)
        {
            if (node_paths.TryGetValue(dataType, out var result))
                return result;
            NodeAttribute attr = dataType.GetCustomAttribute(typeof(NodeAttribute)) as NodeAttribute;
            var name = EditorEX.GetTypeName(dataType);
            string path = string.Empty;
            if (attr == null || string.IsNullOrEmpty(attr.group))
            {
                path = $"{EditorEX.GetTypeName(dataType)}";
            }
            else
            {
                path = $"{attr.group}/{EditorEX.GetTypeName(dataType)}";
            }

            node_paths[dataType] = path;
            return path;
        }

        //internal static void UpdateGraphColor()
        //{
        //    if (view == null) return;
        //    view.UpdateGraphColor();
        //}
    }

}
