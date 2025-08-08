using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using System.Linq;
using System.IO;

namespace ActionEditor
{

    [InitializeOnLoad]
    static class AppInternal
    {
        const string key = "ActionEditor.APP";
        public static string assetPath => EditorPrefs.GetString(key);
        static AppInternal()
        {
            Prefs.Valid();
            OnObjectPickerConfig(assetPath);
        }
        public static event Action OnSave;

        private static Asset _asset;
        public static Asset AssetData => _asset;

     
        public static EditorWindow Window;

        public static long Frame;

        public static float Width;

        public static void OnObjectPickerConfig(string path)
        {
            if (!File.Exists(path)) return;
            var text = File.ReadAllText(path);
            try
            {
                var asset = Asset.Deserialize(typeof(Asset),text);
                asset.Validate();
                _asset = asset;
            }
            catch (Exception)
            {
                _asset = null;
                return;
            }
            EditorPrefs.SetString(key, path);
            AppInternal.Refresh();
        }

        public static void SaveAsset()
        {
            if (AssetData == null) return;
            var path = assetPath;
            if (string.IsNullOrEmpty(path)) return;
            OnSave?.Invoke();
            System.IO.File.WriteAllText(path, AssetData.Serialize());
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


        public static void TryAutoSave()
        {
            var timespan = DateTime.Now - _lastSaveTime;
            if (timespan.Seconds > Prefs.autoSaveSeconds)
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

        public static IDirectable CopyAsset { get; set; }
        public static bool IsCut { get; set; }


        public static void SetCopyAsset(IDirectable asset, bool cut)
        {
            CopyAsset = asset;
            IsCut = cut;
        }


        public static void PasteCopyTo(IDirectable target)
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
                clip = clip.DeepCopy();

            var rect = TimelineTrackItemRightView.TrackRightRect;
            var time = track.Root.PosToTime(Event.current.mousePosition.x - rect.x, rect.width);
            clip.StartTime = track.Root.SnapTime(time);

            track.AddClip(clip);
            AppInternal.Select(clip);
            //CopyAsset = null;
        }
        static void AddCopyTrackToGroup(Group group)
        {
            Track track = CopyAsset as Track;
            if (track == null) return;

            if (!IsCut)
                track = track.DeepCopy();


            if (group.CanAddTrack(track))
                group.AddTrack(track);
            AppInternal.Select(track);
            //CopyAsset = null;

        }

        #endregion

        #region Select

        public static IDirectable[] SelectItems => _selectList.ToArray();
        public static int SelectCount => _selectList.Count;
        private static readonly List<IDirectable> _selectList = new List<IDirectable>();

        public static IDirectable FistSelect => _selectList.Count > 0 ? _selectList.First() : null;

        public static bool CanMultipleSelect { get; set; }

        public static void Select(params IDirectable[] objs)
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

        public static bool IsSelect(IDirectable directable)
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

            _player.Sample();

            if (!IsPlay) return;

            if (IsPause) return;

            if (_player.CurrentTime >= AppInternal.AssetData.Length)
            {
                _player.Sample(0);
                _player.Sample(delta);
                return;
            }

            _player.CurrentTime += delta;
            Repaint();
        }

        internal static void KeyBoardEvent(Event eve)
        {
            if (AssetData == null) return;
            if (eve.control && eve.type == EventType.KeyDown)
            {
                if (eve.keyCode == KeyCode.S)
                {
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
                        group.DeleteAction(track);
                    }
                }
                AppInternal.Select();
                AppInternal.Refresh();
                eve.Use();
            }
            #endregion
        }
    }
}