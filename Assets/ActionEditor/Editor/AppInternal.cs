using ActionAttribute;
using ActionBuffer;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace ActionEditor
{
    [InitializeOnLoad]
    static partial class AppInternal
    {
        private sealed class UndoHistoryEntry
        {
            internal string Name;
            internal byte[] Data;
            internal DateTime CreatedAt;
            internal TimelineSelectionPath[] Selection;
            internal float CurrentTime;
        }

        private struct TimelineSelectionPath
        {
            internal byte Level;
            internal int GroupIndex;
            internal int TrackIndex;
            internal int ClipIndex;
        }

        private const int MaxUndoHistoryCount = 100;

        private const string LegacyAssetPathKey = "ActionEditor.APP";
        private static string _assetPath;
        public static string assetPath
        {
            get
            {
                if (_assetPath == null)
                {
                    _assetPath = Prefs.lastAssetPath;
                    if (string.IsNullOrEmpty(_assetPath))
                    {
                        _assetPath = EditorPrefs.GetString(LegacyAssetPathKey);
                        if (!string.IsNullOrEmpty(_assetPath))
                            Prefs.lastAssetPath = _assetPath;
                    }
                }
                return _assetPath;
            }
        }
        static AppInternal()
        {
            Prefs.Valid();
            //OnObjectPickerConfig(assetPath);
        }
        public static event Action OnSave;

        private static Asset _asset;
        public static Asset AssetData => _asset;
        private static bool _restoringUndo;
        private static bool _inspectorUndoPending;
        private static double _inspectorUndoDeadline;
        private static string _inspectorUndoName;
        private static readonly List<UndoHistoryEntry> _undoHistory =
            new List<UndoHistoryEntry>();
        private static int _undoHistoryIndex = -1;
        private static byte[] _savedData;

        internal static bool IsDirty { get; private set; }
        internal static int UndoHistoryCount => _undoHistory.Count;
        internal static int CurrentUndoIndex => _undoHistoryIndex;

        internal static string GetUndoHistoryName(int index) =>
            _undoHistory[index].Name;

        internal static string GetUndoHistoryTime(int index) =>
            _undoHistory[index].CreatedAt.ToString("HH:mm:ss");

        public static EditorWindow _window;
        public static EditorWindow Window
        {
            get { return _window; }
            set
            {
                _window = value;
                if (_window != null) OnObjectPickerConfig(assetPath);
            }
        }

        public static long Frame;

        public static float Width;

        public static bool OnObjectPickerConfig(string path)
        {
            if (string.IsNullOrEmpty(path) || !File.Exists(path) ||
                !IsSupportedAssetPath(path)) return false;
            if (path == assetPath && _asset != null && _undoHistory.Count > 0)
                return true;
            if (!ConfirmAssetSwitch()) return false;
            var text = File.ReadAllBytes(path);
            try
            {
                var asset = Asset.FromBytes(typeof(Asset), text);
                asset.Validate();
                ActonEditorView.ClearEditorCache();
                _asset = asset;
                if (Window && asset != null)
                    Window.titleContent = new GUIContent(EditorEX.GetTypeName(asset));
            }
            catch (Exception e)
            {
                UnityEngine.Debug.LogException(e);
                return false;
            }
            _assetPath = path;
            Prefs.lastAssetPath = path;
            AssetPlayer.Inst.Invalidate();
            CopyAsset = null;
            Select();
            AppInternal.Refresh();
            ResetUndo();
            return true;
        }

        internal static bool IsSupportedAssetPath(string path)
        {
            if (AssetFileExtensionUtility.Matches(path, typeof(Asset)))
                return true;
            IEnumerable<Type> types = AssetTypes.Count > 0
                ? AssetTypes.Values
                : TypeHelper.GetSubTypes(typeof(Asset));
            return types.Any(type => AssetFileExtensionUtility.Matches(path,
                type));
        }

        internal static string GetFileExtension(Type type) =>
            AssetFileExtensionUtility.Get(type ?? typeof(Asset));

        private static bool ConfirmAssetSwitch()
        {
            FlushPendingUndo();
            if (!IsDirty || _asset == null) return true;
            string fileName = Path.GetFileName(assetPath);
            int result = EditorUtility.DisplayDialogComplex(
                Lan.Text("UnsavedChanges", "Unsaved Changes"),
                string.Format(Lan.Text("SaveChangesPrompt",
                    "Save changes to \"{0}\" before opening another file?"),
                    fileName),
                Lan.ins.Save, Lan.Text("Cancel", "Cancel"),
                Lan.Text("DontSave", "Don't Save"));
            if (result == 1) return false;
            if (result == 0) SaveAsset();
            return true;
        }

        internal static void ShutdownUndo()
        {
            EditorApplication.update -= FlushInspectorUndo;
            _inspectorUndoPending = false;
            _undoHistory.Clear();
            _undoHistoryIndex = -1;
            _savedData = null;
            IsDirty = false;
        }

        private static void ResetUndo()
        {
            EditorApplication.update -= FlushInspectorUndo;
            _inspectorUndoPending = false;
            byte[] data = _asset == null ? null : _asset.ToBytes();
            _undoHistory.Clear();
            if (_asset != null)
            {
                _undoHistory.Add(CreateUndoEntry("Initial State", data));
                _undoHistoryIndex = 0;
            }
            else
            {
                _undoHistoryIndex = -1;
            }
            _savedData = data == null
                ? null
                : (byte[])data.Clone();
            SetDirty(false);
        }

        internal static void RequestInspectorUndoCommit(string name)
        {
            if (_restoringUndo || _asset == null) return;
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
            if (!_inspectorUndoPending) return;
            EditorApplication.update -= FlushInspectorUndo;
            _inspectorUndoPending = false;
            CommitUndo(_inspectorUndoName);
        }

        internal static void CommitUndo(string name)
        {
            if (_restoringUndo || _asset == null) return;
            byte[] data = _asset.ToBytes();
            if (_undoHistoryIndex >= 0 &&
                BytesEqual(_undoHistory[_undoHistoryIndex].Data, data))
            {
                CaptureTimelineContext(_undoHistory[_undoHistoryIndex]);
                return;
            }

            string undoName = string.IsNullOrEmpty(name) ? "Edit Timeline" : name;
            RemoveRedoHistory();
            _undoHistory.Add(CreateUndoEntry(undoName, data));
            _undoHistoryIndex = _undoHistory.Count - 1;
            TrimUndoHistory();
            UpdateDirty(data);
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
                _asset = Asset.FromBytes(typeof(Asset), data);
                _asset.Validate();
                ActonEditorView.ClearEditorCache();
                AssetPlayer.Inst.Invalidate();
                CopyAsset = null;
                Select();
                Refresh();
                RestoreTimelineContext(entry);
                _undoHistoryIndex = index;
                UpdateDirty(data);
                if (Window != null)
                {
                    Window.titleContent = new GUIContent(EditorEX.GetTypeName(_asset));
                    Window.Repaint();
                }
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
            if (_asset == null) return;
            byte[] data = _asset.ToBytes();
            _undoHistory.Clear();
            _undoHistory.Add(CreateUndoEntry("Current State", data));
            _undoHistoryIndex = 0;
            UpdateDirty(data);
            Window?.Repaint();
        }

        private static UndoHistoryEntry CreateUndoEntry(string name, byte[] data)
        {
            var entry = new UndoHistoryEntry
            {
                Name = name,
                Data = data,
                CreatedAt = DateTime.Now
            };
            CaptureTimelineContext(entry);
            return entry;
        }

        private static void CaptureTimelineContext(UndoHistoryEntry entry)
        {
            if (entry == null || _asset == null) return;
            var selection = new List<TimelineSelectionPath>(_selectList.Count);
            for (int i = 0; i < _selectList.Count; i++)
            {
                ISegment segment = _selectList[i];
                if (segment is Group group)
                {
                    int groupIndex = _asset.groups.IndexOf(group);
                    if (groupIndex >= 0)
                        selection.Add(new TimelineSelectionPath
                        {
                            Level = 0,
                            GroupIndex = groupIndex
                        });
                }
                else if (segment is Track track && track.Parent is Group parentGroup)
                {
                    int groupIndex = _asset.groups.IndexOf(parentGroup);
                    int trackIndex = parentGroup.Tracks.IndexOf(track);
                    if (groupIndex >= 0 && trackIndex >= 0)
                        selection.Add(new TimelineSelectionPath
                        {
                            Level = 1,
                            GroupIndex = groupIndex,
                            TrackIndex = trackIndex
                        });
                }
                else if (segment is Clip clip && clip.Parent is Track parentTrack &&
                         parentTrack.Parent is Group clipGroup)
                {
                    int groupIndex = _asset.groups.IndexOf(clipGroup);
                    int trackIndex = clipGroup.Tracks.IndexOf(parentTrack);
                    int clipIndex = parentTrack.Clips.IndexOf(clip);
                    if (groupIndex >= 0 && trackIndex >= 0 && clipIndex >= 0)
                        selection.Add(new TimelineSelectionPath
                        {
                            Level = 2,
                            GroupIndex = groupIndex,
                            TrackIndex = trackIndex,
                            ClipIndex = clipIndex
                        });
                }
            }
            entry.Selection = selection.ToArray();
            entry.CurrentTime = AssetPlayer.Inst.CurrentTime;
        }

        private static void UpdateCurrentUndoContext()
        {
            if (_undoHistoryIndex < 0 || _undoHistoryIndex >= _undoHistory.Count)
                return;
            CaptureTimelineContext(_undoHistory[_undoHistoryIndex]);
        }

        private static void RestoreTimelineContext(UndoHistoryEntry entry)
        {
            if (_asset == null || entry == null) return;
            var selection = new List<ISegment>();
            if (entry.Selection != null)
            {
                for (int i = 0; i < entry.Selection.Length; i++)
                {
                    TimelineSelectionPath path = entry.Selection[i];
                    if (path.GroupIndex < 0 || path.GroupIndex >= _asset.groups.Count)
                        continue;
                    Group group = _asset.groups[path.GroupIndex];
                    if (path.Level == 0)
                    {
                        selection.Add(group);
                        continue;
                    }
                    if (path.TrackIndex < 0 || path.TrackIndex >= group.Tracks.Count)
                        continue;
                    Track track = group.Tracks[path.TrackIndex];
                    if (path.Level == 1)
                    {
                        selection.Add(track);
                        continue;
                    }
                    if (path.ClipIndex >= 0 && path.ClipIndex < track.Clips.Count)
                        selection.Add(track.Clips[path.ClipIndex]);
                }
            }
            Select(selection.ToArray());
            AssetPlayer.Inst.CurrentTime = entry.CurrentTime;
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
            Window?.Repaint();
        }

        public static void SaveAsset()
        {
            if (AssetData == null) return;
            var path = assetPath;
            if (string.IsNullOrEmpty(path)) return;
            OnSave?.Invoke();
            byte[] data = AssetData.ToBytes();
            System.IO.File.WriteAllBytes(path, data);
            _savedData = data;
            SetDirty(false);
            var text = AssetDatabase.LoadAssetAtPath<TextAsset>(path);
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
            EditorUtility.SetDirty(text);
            AssetDatabase.SaveAssetIfDirty(text);

            AssetDatabase.Refresh();

        }

        public static void OnGUIEnd()
        {
            if (Frame > NeedForceRefreshFrame)
            {
                NeedForceRefresh = false;
            }

            Frame++;
            if (Frame >= long.MaxValue)
            {
                Frame = 0;
            }
        }


        public static void OnUpdate()
        {
            TryAutoSave();
            PlayerUpdate();
        }

        #region AutoSave

        public static DateTime LastSaveTime => _lastSaveTime;

        private static DateTime _lastSaveTime = DateTime.Now;
        private static double _nextAutoSaveCheck;


        public static void TryAutoSave()
        {
            double currentTime = EditorApplication.timeSinceStartup;
            if (currentTime < _nextAutoSaveCheck) return;
            _nextAutoSaveCheck = currentTime + 1;

            var timespan = DateTime.Now - _lastSaveTime;
            if (timespan.TotalSeconds > Prefs.autoSaveSeconds)
            {
                AutoSave();
            }
        }

        public static void AutoSave()
        {
            _lastSaveTime = DateTime.Now;
            SaveAsset();
        }

        #endregion

        #region Copy&Cut

        public static ISegment CopyAsset { get; set; }
        public static bool IsCut { get; set; }


        public static void SetCopyAsset(ISegment asset, bool cut)
        {
            CopyAsset = asset;
            IsCut = cut;
        }


        public static void PasteCopyTo(ISegment target)
        {
            if (target is Track track)
            {
                AddCopyClipToTrack(track);
            }
            else if (target is Group group)
            {
                AddCopyTrackToGroup(group);
            }
        }

        static void AddCopyClipToTrack(Track track)
        {
            Clip clip = CopyAsset as Clip;
            if (clip == null) return;
            if (!IsCut)
                clip = clip.DeepCopyByBuffer();

            var rect = TimelineTrackItemRightView.TrackRightRect;
            var time = track.Root.PosToTime(Event.current.mousePosition.x - rect.x, rect.width);
            clip.StartTime = track.Root.SnapTime(time);

            track.AddClip(clip);
            AppInternal.Select(clip);
            CommitUndo("Paste Clip");
            //CopyAsset = null;
        }
        static void AddCopyTrackToGroup(Group group)
        {
            Track track = CopyAsset as Track;
            if (track == null) return;

            if (!IsCut)
                track = track.DeepCopyByBuffer();


            if (group.CanAddTrack(track))
            {
                group.AddTrack(track);
                CommitUndo("Paste Track");
            }
            AppInternal.Select(track);
            //CopyAsset = null;

        }

        #endregion

        #region Select

        public static ISegment[] SelectItems => _selectList.ToArray();
        public static int SelectCount => _selectList.Count;
        private static readonly List<ISegment> _selectList = new List<ISegment>();

        public static ISegment FistSelect => _selectList.Count > 0 ? _selectList.First() : null;

        public static bool CanMultipleSelect { get; set; }

        public static void Select(params ISegment[] objs)
        {
            var change = false;
            if ((objs == null || objs.Length == 0) && _selectList.Count != 0)
                change = true;
            else
            {
                if (objs.Length != _selectList.Count)
                    change = true;
                else
                {
                    var pickCount = 0;
                    foreach (var obj in objs)
                    {
                        if (_selectList.Contains(obj)) pickCount++;
                    }

                    if (pickCount != objs.Length)
                    {
                        change = true;
                    }
                }
            }


            if (change)
            {

                _selectList.Clear();
                if (objs != null)
                    _selectList.AddRange(objs);


                if (_selectList.Count == 1 && (_selectList[0] as Clip) == null)
                    CanMultipleSelect = true;
                else
                    CanMultipleSelect = false;

            }
            //if (objs != null && objs.Length > 0)
            //{
            //    EditorUtility.SetDirty(CurrentInspectorPreviewAsset);
            //    Selection.activeObject = CurrentInspectorPreviewAsset;
            //}
        }

        public static bool IsSelect(ISegment directable)
        {
            return _selectList.Contains(directable);
        }

        #endregion

        #region Refresh

        public static bool NeedForceRefresh { get; private set; }
        public static long NeedForceRefreshFrame { get; private set; }

        public static void Refresh()
        {
            NeedForceRefresh = true;
            NeedForceRefreshFrame = Frame;
        }


        public static void Repaint()
        {
            if (Window != null)
            {
                Window.Repaint();
            }
        }

        #endregion

        #region 播放相关

        public static event Action OnPlay;
        public static event Action OnStop;

        private static AssetPlayer _player => AssetPlayer.Inst;

        public static bool IsPlay { get; private set; }
        public static bool IsPause { get; private set; }

        //public static bool IsRange { get; set; }

        private static float _editorPreviousTime;

        public static void Play()
        {
            if (Application.isPlaying)
            {
                return;
            }

            OnPlay?.Invoke();
            IsPlay = true;
        }

        public static void Pause(bool pause = true)
        {
            IsPause = pause;
        }

        public static void Stop()
        {
            if (AssetData != null)
                _player.CurrentTime = 0;

            OnStop?.Invoke();
            IsPlay = false;
            IsPause = false;
        }

        public static void StepForward()
        {
            if (Math.Abs(_player.CurrentTime - _player.Length) < 0.00001f)
            {
                _player.CurrentTime = 0;
                return;
            }

            _player.CurrentTime += Prefs.SnapInterval;
        }

        public static void StepBackward()
        {
            if (_player.CurrentTime == 0)
            {
                _player.CurrentTime = _player.Length;
                return;
            }

            _player.CurrentTime -= Prefs.SnapInterval;
        }


        private static void PlayerUpdate()
        {
            if (_player == null) return;
            var delta = (Time.realtimeSinceStartup - _editorPreviousTime) * Time.timeScale;

            _editorPreviousTime = Time.realtimeSinceStartup;

            if (Math.Abs(_player.CurrentTime - _player.previousTime) > 0.00001f)
                _player.Sample();

            if (!IsPlay) return;

            if (IsPause) return;

            if (_player.CurrentTime >= ((IAction)AppInternal.AssetData).Length)
            {
                _player.Sample(0);
                _player.Sample(delta);
                return;
            }

            _player.CurrentTime += delta;
            Repaint();
        }

        public static void SaveAs()
        {
            if (AssetData == null) return;
            string extension = GetFileExtension(AssetData.GetType());
            string srcname = Path.GetFileName(
                AssetFileExtensionUtility.WithoutExtension(assetPath,
                    extension));
            string path = EditorUtility.SaveFilePanel(Lan.ins.SaveAs,
                Prefs.savePath, srcname + "_", extension);

            if (!string.IsNullOrEmpty(path))
            {
                path = AssetFileExtensionUtility.WithExtension(path,
                    AssetData.GetType());
                if (path != AppInternal.assetPath)
                {
                    var txt = AppInternal.AssetData.ToBytes();
                    File.WriteAllBytes(path, txt);
                    AssetDatabase.Refresh();
                }
            }
        }
        internal static void KeyBoardEvent(Event eve)
        {
            if (AssetData == null) return;
            if (EditorGUIUtility.editingTextField) return;
            if ((eve.control || eve.command) && eve.type == EventType.KeyDown)
            {
                if (eve.keyCode == KeyCode.Z)
                {
                    if (eve.shift)
                        PerformRedo();
                    else
                        PerformUndo();
                    eve.Use();
                }
                else if (eve.keyCode == KeyCode.Y)
                {
                    PerformRedo();
                    eve.Use();
                }
                else if (eve.keyCode == KeyCode.S)
                {
                    if (eve.shift)
                        SaveAs();
                    else
                        AppInternal.AutoSave();
                    eve.Use();

                }
                else if (eve.keyCode == KeyCode.C)
                {
                    if (AppInternal.SelectCount == 1)
                    {
                        var _asset = AppInternal._selectList[0];
                        if (!_asset.IsLocked)

                            if (_asset is Clip || _asset is Track)
                            {
                                AppInternal.SetCopyAsset(_asset, false);
                                eve.Use();

                            }
                    }

                }
                else if (eve.keyCode == KeyCode.X)
                {
                    if (AppInternal.SelectCount == 1)
                    {
                        var _asset = AppInternal._selectList[0];
                        if (!_asset.IsLocked)

                            if (_asset is Clip || _asset is Track)
                            {

                                AppInternal.SetCopyAsset(AppInternal._selectList[0], true);
                                eve.Use();

                            }
                    }
                }
                else if (eve.keyCode == KeyCode.V)
                {
                    if (AppInternal.SelectCount == 1)
                    {
                        var _asset = AppInternal._selectList[0];
                        if (!_asset.IsLocked)

                            if (_asset is Group && AppInternal.CopyAsset is Track)
                            {
                                Group group = _asset as Group;
                                AppInternal.PasteCopyTo(group);
                                AppInternal.Refresh();
                                eve.Use();

                            }
                            else if (_asset is Track && AppInternal.CopyAsset is Clip)
                            {
                                Track track = _asset as Track;
                                AppInternal.PasteCopyTo(track);
                                AppInternal.Refresh();
                                eve.Use();

                            }
                    }
                }

            }
            if (!eve.isMouse && eve.type == EventType.KeyDown && eve.keyCode == KeyCode.Delete)
            {
                var ss = AppInternal.SelectItems.Where(x => !x.IsLocked).ToArray();
                for (int i = 0; i < ss.Length; i++)
                {
                    if (ss[i] is Group)
                    {
                        var group = ss[i] as Group;
                        AssetData.DeleteGroup(group);
                    }
                    else if (ss[i] is Track)
                    {
                        var track = ss[i] as Track;
                        Group group = track.Parent as Group;
                        group.DeleteTrack(track);
                    }
                    else if (ss[i] is Clip)
                    {
                        var track = ss[i] as Clip;
                        Track group = track.Parent as Track;
                        group.DeleteClip(track);
                    }
                }
                AppInternal.Select();
                AppInternal.Refresh();
                if (ss.Length > 0) CommitUndo("Delete Timeline Items");
                eve.Use();
            }
            #endregion
        }
    }

    partial class AppInternal
    {
        public static readonly Dictionary<string, Type> AssetTypes = new Dictionary<string, Type>();
        public static readonly List<string> AssetNames = new List<string>();
        public static void InitializeAssetTypes()
        {
            AssetTypes.Clear();


            AssetNames.Clear();
            var types = TypeHelper.GetSubTypes(typeof(Asset));
            foreach (var t in types)
            {
                var typeName = EditorEX.GetTypeName(t);
                AssetTypes[typeName] = t;
                AssetNames.Add(typeName);
            }
        }
        public static Color GetColor(this ISegment track)
        {
            if (track is Clip)
            {
                return Prefs.data.GetColor(track.GetType(),
                    AssetData?.GetType(), true).color;
            }
            else if (track is Track)
            {
                return Prefs.data.GetColor(track.GetType(),
                    AssetData?.GetType(), false).color;

            }
            return Color.white;
        }

        public static bool CanAddTrack(this Group group, Track track)
        {

            if (track == null) return false;
            return EditorEX.CanAttachTo(track.GetType(), group.GetType());
            //var type = track.GetType();
            //if (type == null || !type.IsSubclassOf(typeof(Track)) || type.IsAbstract) return false;
            ////if (type.IsDefined(typeof(UniqueTrackAttribute), true) &&
            ////    group.ExistSameTypeTrack(type))
            ////    return false;
            //var attachAtt = type.GetCustomAttribute<AttachableAttribute>(true);
            //if (attachAtt == null || attachAtt.Types == null || attachAtt.Types.All(t => t != group.GetType())) return false;

            //return true;
        }

        public static float ViewTime(this Asset asset) => asset.ViewTimeMax - asset.ViewTimeMin;

        public static float SnapTime(this Asset asset, float time) => Mathf.Round(time / Prefs.SnapInterval) * Prefs.SnapInterval;

        public static float TimeToPos(this Asset asset, float time, float width) => (time - asset.ViewTimeMin) / asset.ViewTime() * width;

        public static float PosToTime(this Asset asset, float pos, float width) => pos / width * asset.ViewTime() + asset.ViewTimeMin;

        public static float WidthToTime(this Asset asset, float pos, float width) => pos / width * asset.ViewTime();

        public static void TryMatchSubClipLength(this ILengthMatchAble subClipContainable)
        {
            subClipContainable.Length = subClipContainable.MatchAbleLength;
        }
    }
}
