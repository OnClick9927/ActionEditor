using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace ActionEditor
{
    class TimelineTrackItemView : ViewBase, IPointerClickHandler, IPointerDownHandler
    {
        public static IDirectable MoveToItem;

        private Rect _leftRect, rightRect;

        public IDirectable Data;

        private TimelineTrackItemRightView _rightView;

        protected static GUIStyle NameStyle;

        public void SetData(IDirectable asset)
        {
            Data = asset;
            _rightView = Window.CreateView<TimelineTrackItemRightView>();
            _rightView.SetData(Data);
        }

        public override void OnDraw()
        {
            GUI.color = Color.white;
            GUI.Box(Position, "", App.IsSelect(Data) ? EditorStyles.selectionRect : GUI.skin.box);


            _leftRect = new Rect(Position.x, Position.y, Styles.TimelineLeftWidth, Position.height);
            DrawLeft(_leftRect);
            rightRect = new Rect(Position.x + Styles.TimelineLeftTotalWidth, Position.y,
               Position.width - Styles.TimelineLeftTotalWidth, Position.height);

            GUI.BeginClip(rightRect);
            _rightView.OnGUI(rightRect);
            GUI.EndClip();
            if (Data == MoveToItem)
            {
                GUI.DrawTexture(new Rect(Position.x, Position.y + Position.height - 2, Styles.TimelineLeftWidth, 2),
                    Styles.WhiteTexture);
            }
            DrawLockedAndActive();
        }


        private void DrawLockedAndActive()
        {
            if (!Data.IsActive || Data.IsLocked)
            {
                if (NameStyle == null)
                {
                    NameStyle = new GUIStyle(GUI.skin.label)
                    {
                        fontStyle = FontStyle.Bold,
                        alignment = TextAnchor.MiddleCenter
                    };
                }
                GUI.color = Color.black.WithAlpha(!Data.IsActive ? 0.2f : 0.1f);
                if (!Data.IsActive)
                    GUI.DrawTexture(Position, Styles.WhiteTexture);

                GUI.DrawTextureWithTexCoords(Position, Styles.Stripes,
                new Rect(0, 0, Position.width / 20, Position.height / 20));
                GUI.color = Color.white;

                string overlayLabel = null;

                if (!Data.IsActive)
                    overlayLabel = Lan.ins.Disable;
                if (Data.IsLocked)
                    if (string.IsNullOrEmpty(overlayLabel))
                        overlayLabel += Lan.ins.Locked;
                    else
                        overlayLabel += $" & {Lan.ins.Locked}";


                var stampRect = new Rect(rightRect.center.x, rightRect.y, 200, rightRect.height);
                stampRect.center = rightRect.center;
                GUI.Box(stampRect, overlayLabel, NameStyle);
            }




        }


        private void DrawLeft(Rect rect)
        {
            GUILayout.BeginArea(rect);
            GUILayout.BeginHorizontal();
            GUILayout.Space(4);
            {
                bool isTrack = false;
                if (Data is Group group)
                {
                    var collapseAllRect = EditorGUILayout.GetControlRect(GUILayout.Width(15), GUILayout.Height(16));
                    var temp = !EditorGUI.Foldout(collapseAllRect, !Data.IsCollapsed, "");
                    if (temp != Data.IsCollapsed)
                    {
                        group.IsCollapsed = !Data.IsCollapsed;
                        App.Refresh();
                        Event.current.Use();
                    }
                    GUILayout.Label(group.name, GUILayout.Height(20));

                }
                else if (Data is Track track)
                {
                    isTrack = true;
                    GUILayout.Space(18);
                    GUI.color = track.GetColor();

                    GUI.DrawTexture(EditorGUILayout.GetControlRect(GUILayout.Width(4), GUILayout.Height(20)), Styles.WhiteTexture);
                    GUI.color = Color.white;

                    GUILayout.Space(2);

                    GUI.DrawTexture(EditorGUILayout.GetControlRect(GUILayout.Width(20), GUILayout.Height(20)), track.GetIcon()); // 绘制图标
                    GUILayout.Label(EditorEX.GetTypeName(Data.GetType()), GUILayout.Height(20));

                }




                if (Data.IsLocked)
                {
                    GUILayout.Space(2);
                    if (GUI.Button(EditorGUILayout.GetControlRect(GUILayout.Width(16)), EditorGUIUtility.TrIconContent("InspectorLock"), GUIStyle.none))
                    {
                        Data.IsLocked = !Data.IsLocked;
                        Event.current.Use();
                    }
                }

                if (!Data.IsActive)
                {
                    GUILayout.Space(2);
                    if (GUI.Button(EditorGUILayout.GetControlRect(GUILayout.Width(16)), EditorGUIUtility.TrIconContent("animationvisibilitytoggleoff"), GUIStyle.none))
                    {
                        Data.IsActive = !Data.IsActive;
                        Event.current.Use();
                    }
                }
                if (!isTrack)
                {
                    if (GUI.Button(EditorGUILayout.GetControlRect(GUILayout.Width(16), GUILayout.Height(20)), "", EditorStyles.foldoutHeaderIcon))
                    {
                        OnGroupContextMenu();
                        Event.current.Use();
                    }
                }


            }

            GUILayout.EndHorizontal();
            GUILayout.EndArea();
        }



        public void OnPointerClick(PointerEventData ev)
        {
            if (Data.IsLocked) return;
            if (ev.IsRight())
            {
                //Debug.Log($"OnPointerClick 右键点击=== Asset={Data.Name}");
                if (_leftRect.Contains(ev.MousePosition))
                {
                    OnGroupContextMenu();
                    ev.StopPropagation();
                    return;
                }
            }
            else
            {
                //App.Select(Data);
                //App.Repaint();
            }
        }

        public void OnPointerDown(PointerEventData ev)
        {
            if (App.SelectCount > 1) return;
            if (ev.IsLeft())
            {
                App.Select(Data);
                App.Repaint();
            }
        }


        public void OnGroupContextMenu()
        {
            if (Data.IsLocked)
                return;
            if (Data is Track track)
            {
                OnTrackLeftContextMenu(track);
            }
            else if (Data is Group group)
            {
                OnGroupContextMenu(group);
            }
        }

        private void OnGroupContextMenu(Group group)
        {
            var genericMenu = new GenericMenu();

            var trackTypes = EditorEX.GetTypeMetaDerivedFrom(typeof(Track));
            foreach (var metaInfo in trackTypes)
            {
                var info = metaInfo;
                if (info.attachableTypes == null || !info.attachableTypes.Contains(group.GetType()))
                {
                    continue;
                }

                var canAdd = !info.isUnique ||
                             (group.Tracks.Find(track => track.GetType() == info.type) == null);
                if (group.IsLocked)
                {
                    canAdd = false;
                }

                var path = $"{Lan.ins.MenuAddTrack}/" + info.name;
                if (canAdd)
                {
                    genericMenu.AddItem(new GUIContent(path), false, () =>
                    {
                        var track = group.AddTrack(info.type);
                        App.Select(track);
                        App.Refresh();
                    });
                }
                else
                {
                    genericMenu.AddDisabledItem(new GUIContent(path));
                }
            }

            genericMenu.AddSeparator("");


            if (App.CopyAsset is Track copyTrack)
            {
                if (group.CanAddTrack(copyTrack))
                {
                    genericMenu.AddItem(new GUIContent(string.Format(Lan.ins.Paste, EditorEX.GetTypeName(copyTrack))), false, () =>
                    {
                        if (group.CanAddTrack(copyTrack))
                        {
                            App.AddCopyTrackToGroup(group);
                            App.Refresh();
                        }
                    });
                }
                else
                {
                    genericMenu.AddDisabledItem(new GUIContent(Lan.ins.Paste));
                }
            }

            genericMenu.AddItem(new GUIContent(Lan.ins.Disable), !group.IsActive, ActiveContextMenuCmd);
            genericMenu.AddItem(new GUIContent(Lan.ins.Locked), group.IsLocked, LockedContextMenuCmd);
            //genericMenu.AddItem(new GUIContent(Lan.ins.Rename), false, RenameContextMenuCmd);
            genericMenu.AddSeparator("");

            if (group.IsLocked)
            {
                genericMenu.AddDisabledItem(new GUIContent(Lan.ins.Delete));
            }
            else
            {
                genericMenu.AddItem(new GUIContent(Lan.ins.Delete), false, DeleteContextMenu);
            }

            genericMenu.ShowAsContext();
        }

        private void OnTrackLeftContextMenu(Track track)
        {
            var menu = new GenericMenu();
            menu.AddItem(new GUIContent(Lan.ins.Disable), !track.IsActive, ActiveContextMenuCmd);
            menu.AddItem(new GUIContent(Lan.ins.Locked), track.IsLocked, LockedContextMenuCmd);

            //menu.AddSeparator("");


            menu.AddItem(new GUIContent(Lan.ins.Copy), false, () => { App.SetCopyAsset(track, false); });
            menu.AddItem(new GUIContent(Lan.ins.Cut), false, () =>
               {
                   App.SetCopyAsset(track, true);
               });


            //menu.AddSeparator("/");
            if (track.IsLocked)
            {
                menu.AddDisabledItem(new GUIContent(Lan.ins.Delete));
            }
            else
            {
                menu.AddItem(new GUIContent(Lan.ins.Delete), false, DeleteContextMenu);
            }

            menu.ShowAsContext();
        }


        private void LockedContextMenuCmd()
        {
            Data.IsLocked = !Data.IsLocked;
            App.Refresh();
        }

        private void ActiveContextMenuCmd()
        {
            Data.IsActive = !Data.IsActive;
            App.Refresh();
        }

        private void DeleteContextMenu()
        {
            if (Data is Group group)
            {
                //if (EditorUtility.DisplayDialog(Lan.ins.Delete, Lan.ins.GroupDeleteTips, Lan.ins.TipsConfirm,
                //        Lan.ins.TipsCancel))
                //{
                //}
                group.Root.DeleteGroup(group);
            }
            else if (Data is Track track)
            {
                //if (EditorUtility.DisplayDialog(Lan.ins.Delete, Lan.ins.TrackDeleteTips, Lan.ins.TipsConfirm,
                //        Lan.ins.TipsCancel))
                //{
                //}
                if (track.Parent is Group g)
                {
                    g.DeleteTrack(track);
                }
            }

            App.Refresh();
        }

    }

    class TimelineTrackItemRightView : ViewBase, IPointerClickHandler, IPointerDownHandler
    {
        public IDirectable Data;

        public static Rect TrackRightRect { get; private set; }

        public void SetData(IDirectable asset)
        {
            Data = asset;
        }

        public override void OnGUI(Rect rect)
        {

            TrackRightRect = rect;
            var localRect = new Rect(0, 0, rect.width, rect.height);
            base.OnGUI(localRect);
        }

        public override void OnDraw()
        {
            if (Data is Track track)
            {
                DrawClips(track);
            }

        }

        private void DrawClips(Track track)
        {

            TrySortClips(track);
            for (int clipIndex = 0; clipIndex < track.Clips.Count; clipIndex++)
            {
                var clip = track.Clips[clipIndex];
                var nextClip = clipIndex < track.Clips.Count - 1 ? track.Clips[clipIndex + 1] : null;
                var previousClip = clipIndex != 0 ? track.Clips[clipIndex - 1] : null;
                var draw = ClipDrawer.GetDraw(clip);

                if (draw != null)
                {
                    draw.PreviousClip = previousClip;
                    draw.NextClip = nextClip;
                    draw.Draw(Window, Position, TrackRightRect, clip, App.IsSelect(clip), App.CopyAsset == clip);
                }
            }
            if (App.CopyAsset == track)
            {
                if ((EditorApplication.timeSinceStartup) % 0.5 > 0.25)
                {
                    GUI.Box(Position, "");
                }
            }
        }

        private void TrySortClips(Track track)
        {
            var preTime = -1f;
            bool needSort = false;
            foreach (var clip in track.Clips)
            {
                if (clip.StartTime > preTime)
                {
                    preTime = clip.StartTime;
                }
                else
                {
                    needSort = true;
                    break;
                }
            }

            if (needSort)
            {
                track.Clips.Sort((a, b) => a.StartTime - b.StartTime > 0 ? 1 : -1);
            }
        }


        public void OnPointerClick(PointerEventData ev)
        {
            if (ev.IsRight())
            {
                var clip = ClipDrawer.GetClipByTrackPosition(Data, ev.MousePosition);
                if (clip != null)
                {
                    ClickClip(clip);
                    OnClipContextMenu(clip);
                    ev.StopPropagation();
                    return;
                }

                if (Position.Contains(ev.MousePosition) && Data is Track track)
                {
                    if (TimelineMiddleView.MutiSelecting) return;
                    if (Data.IsLocked) return;
                    OnTrackRightContextMenu(track, ev);
                    ev.StopPropagation();
                }
            }
        }

        public void OnPointerDown(PointerEventData ev)
        {

            if (ev.IsLeft())
            {
                var clip = ClipDrawer.GetClipByTrackPosition(Data, ev.MousePosition);
                if (clip != null)
                {
                    if (App.SelectCount > 1)
                    {
                        if (App.SelectItems.Contains(clip)) return;
                    }

                    ClickClip(clip);
                    ev.StopPropagation();
                    App.Repaint();
                }
            }
        }


        private void ClickClip(Clip clip)
        {
            App.Select(clip);
        }

        private void OnTrackRightContextMenu(Track track, PointerEventData ev)
        {
            var attachableTypeInfos = new List<EditorEX.TypeMetaInfo>();

            var existing = track.Clips.FirstOrDefault();


            var time = track.Root.PosToTime(ev.MousePosition.x - Position.x, Position.width);
            var cursorTime = track.Root.SnapTime(time);

            foreach (var clip in EditorEX.GetTypeMetaDerivedFrom(typeof(Clip)))
            {
                if (clip.attachableTypes != null && !clip.attachableTypes.Contains(track.GetType()))
                {
                    continue;
                }

                attachableTypeInfos.Add(clip);

            }

            if (attachableTypeInfos.Count > 0)
            {
                var menu = new GenericMenu();
                foreach (var metaInfo in attachableTypeInfos)
                {
                    var info = metaInfo;
                    var tName = info.name;
                    menu.AddItem(new GUIContent(tName), false,
                        () => { track.AddClip(info.type, cursorTime); });
                }

                if (App.CopyAsset != null && App.CopyAsset is Clip copyClip)
                {
                    var copyType = copyClip.GetType();
                    if (attachableTypeInfos.Select(i => i.type).Contains(copyType))
                    {
                        menu.AddSeparator("/");
                        menu.AddItem(new GUIContent(string.Format(Lan.ins.Paste, EditorEX.GetTypeName(copyType))), false,
                            () =>
                            {
                                App.AddCopyClipToTrack(track);
                                App.Refresh();
                                //track.PasteClip(DirectorUtility.CopyClip, cursorTime);
                            });
                    }
                }

                menu.ShowAsContext();
            }
        }

        private void OnClipContextMenu(Clip clip)
        {
            if (TimelineMiddleView.MutiSelecting) return;
            if (clip.IsLocked) return;

            var menu = new GenericMenu();
            menu.AddItem(new GUIContent(Lan.ins.Copy), false, () =>
            {
                App.SetCopyAsset(clip, false);
            });
            menu.AddItem(new GUIContent(Lan.ins.Cut), false, () =>
            {
                App.SetCopyAsset(clip, true);
            });
            menu.AddItem(new GUIContent(Lan.ins.Delete), false, () =>
            {
                if (clip.Parent is Track track) track.DeleteAction(clip);
                App.Refresh();
            });
            if (clip is ILengthMatchAble subContainable && subContainable.MatchAbleLength > 0)
            {
                //menu.AddSeparator("");
                //menu.AddItem(new GUIContent(Lan.ins.MatchPreviousLoop), false,
                //    subContainable.TryMatchPreviousSubClipLoop);
                menu.AddItem(new GUIContent(Lan.ins.MatchClipLength), false, subContainable.TryMatchSubClipLength);
                //menu.AddItem(new GUIContent(Lan.ins.MatchNextLoop), false, subContainable.TryMatchNextSubClipLoop);
            }

            menu.ShowAsContext();
        }

    }
}