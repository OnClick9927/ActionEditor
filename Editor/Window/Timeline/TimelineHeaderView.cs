using System.IO;
using UnityEditor;
using UnityEngine;

namespace ActionEditor
{
    class TimelineHeaderView : ViewBase
    {
        private static GUIContent _firstKeyContent;
        private static GUIContent _previousKeyContent;
        private static GUIContent _playContent;
        private static GUIContent _pauseContent;
        private static GUIContent _nextKeyContent;
        private static GUIContent _lastKeyContent;
        private static GUIContent _createContent;
        private static GUIContent _folderContent;
        private static GUIContent _undoHistoryContent;
        private static GUIContent _saveAsContent;
        private static GUIContent _saveContent;
        private static GUIContent _settingsContent;
        private GUIStyle _customToolbarButtonStyle;
        private GUIContent _assetToolbarContent;
        private string _assetToolbarPath;
        private bool _assetToolbarDirty;
        private float _assetToolbarWidth;

        public override void OnDraw()
        {
            EnsureToolbarContent();
            using (new EditorGUI.DisabledScope(AppInternal.AssetData == null))
                DrawPlayControl();

            DrawPlayHeader();

        }

        #region Play control

        private float _buttonWidth = 30;

        private void DrawPlayControl()
        {
            if (_customToolbarButtonStyle == null)
            {
                _customToolbarButtonStyle = new GUIStyle(EditorStyles.toolbarButton)
                {
                    fixedHeight = Styles.PlayControlHeight
                };
            }

            var rect = new Rect(0, 0, Styles.TimelineLeftWidth, Styles.PlayControlHeight);
            GUILayout.BeginArea(rect);

            _buttonWidth = rect.width / 6;

            GUILayout.BeginHorizontal(EditorStyles.toolbar);

            //if (DrawButton(Styles.BackIcon, Lan.ins.BackMenuTips))
            //{
            //    App.TextAsset = null;
            //    // GUILayout.EndHorizontal();
            //    // return;
            //}

            if (DrawButton(_firstKeyContent))
            {
                AssetPlayer.Inst.CurrentTime = 0;
            }

            if (DrawButton(_previousKeyContent))
            {
                AppInternal.StepBackward();
            }

            EditorGUI.BeginChangeCheck();

            if (AppInternal.IsPlay)
                GUI.backgroundColor = Color.blue + Color.cyan;
            var isPlaying = DrawToggle(AppInternal.IsPlay, _playContent);
            GUI.backgroundColor = Color.white;


            if (EditorGUI.EndChangeCheck())
            {
                if (isPlaying)
                {
                    AppInternal.Pause(false);
                    AppInternal.Play();
                }
                else
                {
                    AppInternal.Stop();
                }
            }
            EditorGUI.BeginChangeCheck();
            if (AppInternal.IsPause)
                GUI.backgroundColor = Color.blue + Color.cyan;
            var isPause = DrawToggle(AppInternal.IsPause, _pauseContent);
            GUI.backgroundColor = Color.white;

            if (EditorGUI.EndChangeCheck())
            {
                AppInternal.Pause(isPause);
            }

            if (DrawButton(_nextKeyContent))
            {
                AppInternal.StepForward();
            }

            if (DrawButton(_lastKeyContent))
            {
                AssetPlayer.Inst.CurrentTime = AssetPlayer.Inst.Length;
            }
            //GUILayout.Button("sdsa");
            //App.IsRange = DrawToggle(App.IsRange, Styles.RangeIcon, Lan.ins.StepBackwardTips);

            GUILayout.EndHorizontal();
            GUILayout.EndArea();
        }

        private bool DrawButton(GUIContent content)
        {
            return GUILayout.Button(content,
                _customToolbarButtonStyle, GUILayout.Width(_buttonWidth));
        }

        private bool DrawToggle(bool value, GUIContent content)
        {
            return GUILayout.Toggle(value, content, _customToolbarButtonStyle,
                GUILayout.Width(_buttonWidth));
        }

        #endregion

        #region Header


        private void DrawPlayHeader()
        {
            var gap = Styles.TimelineLeftWidth + Styles.SplitterWidth;
            var _headerRect = new Rect(Position.x + gap, Position.y,
                   Position.width - gap,
                   Position.height - Styles.HeaderHeight);

            GUILayout.BeginArea(_headerRect, EditorStyles.toolbar);
            OnHeaderGUI();
            GUILayout.EndArea();



        }

        private void OnHeaderGUI()
        {

            GUILayout.BeginHorizontal(EditorStyles.toolbar);
            {
                var rect = EditorGUILayout.GetControlRect(GUILayout.Width(25));
                if (GUI.Button(rect, _createContent, EditorStyles.toolbarButton))
                    CreateAssetWindow.Show(rect);
            }
            {
                UpdateAssetToolbarContent();
                var rect = EditorGUILayout.GetControlRect(
                    GUILayout.Width(_assetToolbarWidth));
                if (GUI.Button(rect, _assetToolbarContent, EditorStyles.toolbarDropDown))
                {
                    AssetPick.ShowObjectPicker(rect, "Assets", "t:TextAsset", Prefs.pickListType, (o) =>
                    {
                        AppInternal.OnObjectPickerConfig(AssetDatabase.GetAssetPath(o));
                        GUIUtility.ExitGUI();
                    }, (x) =>
                    {
                        if (App.AssetData == null)
                            return x.EndsWith(Asset.FileEx);
                        //return x.EndsWith(Asset.FileEx);
                        return x.EndsWith(Asset.FileEx) && ActonEditorView.GetEditor(App.AssetData).IsFileFitAsset(x);

                    });
                }
                if (AppInternal.AssetData != null)
                {
                    var projectRect = EditorGUILayout.GetControlRect(GUILayout.Width(25));
                    if (GUI.Button(projectRect, _folderContent, EditorStyles.toolbarButton))
                    {
                        EditorGUIUtility.PingObject(AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(
                            App.assetPath));
                    }
                }
            }
            ActonEditorView.GetEditor(AppInternal.AssetData)?.OnAssetHeaderGUI();

            //var header = EditorCustomFactory.GetEditor(AppInternal.AssetData);
            //header?.OnGUI(AppInternal.AssetData);

            //DrawAssetsHeader();


            GUILayout.FlexibleSpace();






            if (AppInternal.AssetData != null)
            {
                var undoRect = EditorGUILayout.GetControlRect(GUILayout.Width(25));
                if (GUI.Button(undoRect, _undoHistoryContent, EditorStyles.toolbarButton))
                {
                    ShowUndoHistory(undoRect);
                }
                if (GUILayout.Button(_saveAsContent, EditorStyles.toolbarButton,
                        GUILayout.Width(25)))
                {
                    AppInternal.SaveAs();
                }
                if (GUILayout.Button(_saveContent, EditorStyles.toolbarButton,
                        GUILayout.Width(26)))
                {
                    AppInternal.AutoSave(); //先保存当前的
                }
            }
            {
                var rect = EditorGUILayout.GetControlRect(GUILayout.Width(25));
                if (GUI.Button(rect, _settingsContent, EditorStyles.toolbarButton))
                {
                    PreferencesWindow.Show(rect);
                }
            }

            GUILayout.EndHorizontal();
        }

        private static void EnsureToolbarContent()
        {
            if (_firstKeyContent != null) return;
            _firstKeyContent = EditorGUIUtility.TrIconContent("d_Animation.FirstKey");
            _previousKeyContent = EditorGUIUtility.TrIconContent("d_Animation.PrevKey");
            _playContent = EditorGUIUtility.TrIconContent("d_Animation.Play");
            _pauseContent = EditorGUIUtility.TrIconContent("d_PauseButton");
            _nextKeyContent = EditorGUIUtility.TrIconContent("d_Animation.NextKey");
            _lastKeyContent = EditorGUIUtility.TrIconContent("d_Animation.LastKey");
            _createContent = EditorGUIUtility.TrIconContent("Toolbar Plus", "Create");
            _folderContent = EditorGUIUtility.TrIconContent("d_Project", "Show in Project");
            _undoHistoryContent = EditorGUIUtility.TrIconContent(
                "d_UndoHistory", "Undo History");
            _saveAsContent = EditorGUIUtility.TrIconContent("SaveAs", "Save As");
            _saveContent = EditorGUIUtility.TrIconContent("SaveActive", "Save");
            _settingsContent = EditorGUIUtility.TrIconContent("Settings", "Settings");
        }

        private void UpdateAssetToolbarContent()
        {
            string path = AppInternal.assetPath ?? string.Empty;
            bool isDirty = AppInternal.IsDirty;
            if (_assetToolbarPath == path && _assetToolbarDirty == isDirty &&
                _assetToolbarContent != null) return;

            _assetToolbarPath = path;
            _assetToolbarDirty = isDirty;
            string name = Path.GetFileName(path);
            name = name.Replace($".{Asset.FileEx}", "");
            if (string.IsNullOrEmpty(name)) name = "None";
            _assetToolbarContent = new GUIContent($"[{name}]{(isDirty ? "*" : string.Empty)}");
            _assetToolbarWidth = Mathf.Max(80, GUI.skin.label.CalcSize(
                _assetToolbarContent).x + 8) + 20;
        }

        private void ShowUndoHistory(Rect rect)
        {
            AppInternal.FlushUndoHistory();
            PopupWindow.Show(rect, new UndoHistoryPopup(Window.position.height));
        }

        private sealed class UndoHistoryPopup : PopupWindowContent
        {
            private const float Width = 440;
            private const float RowHeight = 22;
            private const float Padding = 4;
            private const float FooterHeight = 25;
            private readonly float _height;
            private Vector2 _scrollPosition;
            private GUIStyle _rowStyle;
            private GUIStyle _stateStyle;

            internal UndoHistoryPopup(float height)
            {
                _height = height;
            }

            public override Vector2 GetWindowSize()
            {
                return new Vector2(Width, _height);
            }

            public override void OnOpen()
            {
                editorWindow.wantsMouseMove = true;
                int row = AppInternal.UndoHistoryCount -
                    AppInternal.CurrentUndoIndex - 1;
                float viewportHeight = GetWindowSize().y - Padding * 2 - FooterHeight;
                _scrollPosition.y = Mathf.Max(0,
                    row * RowHeight - (viewportHeight - RowHeight) * 0.5f);
            }

            public override void OnGUI(Rect rect)
            {
                int count = AppInternal.UndoHistoryCount;
                if (count == 0)
                {
                    GUI.Label(rect, "No Undo History",
                        EditorStyles.centeredGreyMiniLabel);
                    return;
                }

                EnsureStyles();
                var viewport = new Rect(Padding, Padding,
                    rect.width - Padding * 2,
                    rect.height - Padding * 2 - FooterHeight);
                float contentHeight = count * RowHeight;
                float contentWidth = viewport.width -
                    (contentHeight > viewport.height ? 16 : 1);
                var content = new Rect(0, 0, contentWidth, contentHeight);
                _scrollPosition = GUI.BeginScrollView(viewport,
                    _scrollPosition, content);

                int clickedIndex = -1;
                int currentIndex = AppInternal.CurrentUndoIndex;
                Event currentEvent = Event.current;
                for (int row = 0; row < count; row++)
                {
                    int index = count - row - 1;
                    var rowRect = new Rect(0, row * RowHeight,
                        content.width, RowHeight);
                    bool isCurrent = index == currentIndex;
                    bool isHovered = rowRect.Contains(currentEvent.mousePosition);
                    if (isCurrent)
                    {
                        EditorGUI.DrawRect(rowRect,
                            new Color(0.24f, 0.49f, 0.9f, 0.65f));
                    }
                    else if (isHovered)
                    {
                        EditorGUI.DrawRect(rowRect,
                            EditorGUIUtility.isProSkin
                                ? new Color(1, 1, 1, 0.08f)
                                : new Color(0, 0, 0, 0.08f));
                    }

                    string state = index < currentIndex
                        ? "Undo"
                        : isCurrent ? "Current" : "Redo";
                    GUI.Label(new Rect(rowRect.x + 7, rowRect.y,
                            rowRect.width - 128, RowHeight),
                        $"{index}  {AppInternal.GetUndoHistoryName(index)}", _rowStyle);
                    GUI.Label(new Rect(rowRect.xMax - 120, rowRect.y,
                            114, RowHeight),
                        $"{AppInternal.GetUndoHistoryTime(index)}  {state}",
                        _stateStyle);
                    EditorGUIUtility.AddCursorRect(rowRect, MouseCursor.Link);

                    if (currentEvent.type == EventType.MouseDown &&
                        currentEvent.button == 0 && isHovered)
                        clickedIndex = index;
                }

                GUI.EndScrollView();
                var clearRect = new Rect(rect.xMax - Padding - 100,
                    rect.yMax - Padding - 20, 100, 20);
                if (GUI.Button(clearRect, "Clear History", EditorStyles.miniButton))
                {
                    AppInternal.ClearUndoHistory();
                    editorWindow.Close();
                    return;
                }
                if (clickedIndex < 0) return;
                currentEvent.Use();
                AppInternal.RestoreUndoHistory(clickedIndex);
                editorWindow.Close();
            }

            private void EnsureStyles()
            {
                if (_rowStyle != null) return;
                _rowStyle = new GUIStyle(EditorStyles.label)
                {
                    alignment = TextAnchor.MiddleLeft,
                    clipping = TextClipping.Clip
                };
                _stateStyle = new GUIStyle(EditorStyles.miniLabel)
                {
                    alignment = TextAnchor.MiddleRight
                };
            }
        }

        #endregion
    }
}
