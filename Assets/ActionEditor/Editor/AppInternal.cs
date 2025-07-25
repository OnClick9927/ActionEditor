﻿using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;
using System.Linq;

namespace ActionEditor
{

    [InitializeOnLoad]
    static class AppInternal
    {
        static AppInternal()
        {
            Prefs.Valid();
            string key = "ActionEditor.APP";
            string path = EditorPrefs.GetString(key);
            OnObjectPickerConfig(AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(path));
            OnDisable += () =>
            {
                if (TextAsset != null)
                {
                    EditorPrefs.SetString(key, AssetDatabase.GetAssetPath(TextAsset));
                }
            };

        }
        private static TextAsset _textAsset;
        public static event Action OnSave;
        public static Action OnInitialize;
        public static Action OnDisable;

        public static Asset AssetData { get; private set; } = null;

        public static TextAsset TextAsset
        {
            get => _textAsset;
            set
            {
                _textAsset = value;

                if (_textAsset == null)
                {
                    AssetData = null;
                }
                else
                {
                    try
                    {
                        var asset = Asset.Deserialize(typeof(Asset), _textAsset.text);
                        AssetData = asset;
                        asset.Validate();
                    }
                    catch (Exception)
                    {
                        _textAsset = null;
                        AssetData = null;
                    }
                    AppInternal.Refresh();

                }
            }
        }

        public static EditorWindow Window;

        public static long Frame;

        public static float Width;

        public static void OnObjectPickerConfig(Object obj)
        {
            if (obj is TextAsset textAsset)
            {
                TextAsset = textAsset;
            }
        }

        public static void SaveAsset()
        {
            if (AssetData == null) return;
            var path = AssetDatabase.GetAssetPath(TextAsset);
            if (string.IsNullOrEmpty(path)) return;
            var json = AssetData.Serialize();

            System.IO.File.WriteAllText(path, json);
            //EditorUtility.SetDirty(TextAsset);
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
            AssetDatabase.Refresh();
            OnSave?.Invoke();
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

        /// <summary>
        /// 尝试自动保存
        /// </summary>
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
        public static void AddCopyClipToTrack(Track track)
        {
            Clip clip = CopyAsset as Clip;
            if (!IsCut)
                clip = clip.DeepCopy();

            var rect = TimelineTrackItemRightView.TrackRightRect;
            var time = track.Root.PosToTime(Event.current.mousePosition.x - rect.x, rect.width);
            clip.StartTime = track.Root.SnapTime(time);

            track.AddClip(clip);
            AppInternal.Select(clip);
            //CopyAsset = null;
        }
        public static void AddCopyTrackToGroup(Group group)
        {
            Track track = CopyAsset as Track;
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

        [System.NonSerialized] private static InspectorPreviewAsset _currentInspectorPreviewAsset;

        private static InspectorPreviewAsset CurrentInspectorPreviewAsset
        {
            get
            {
                if (_currentInspectorPreviewAsset == null)
                {
                    _currentInspectorPreviewAsset = ScriptableObject.CreateInstance<InspectorPreviewAsset>();
                }

                return _currentInspectorPreviewAsset;
            }
        }

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
            if (objs != null && objs.Length > 0)
            {
                EditorUtility.SetDirty(CurrentInspectorPreviewAsset);
                Selection.activeObject = CurrentInspectorPreviewAsset;
            }
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
                                AppInternal.AddCopyTrackToGroup(group);
                                AppInternal.Refresh();
                                eve.Use();

                            }
                            else if (_asset is Track && AppInternal.CopyAsset is Clip)
                            {
                                Track track = _asset as Track;
                                AppInternal.AddCopyClipToTrack(track);
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