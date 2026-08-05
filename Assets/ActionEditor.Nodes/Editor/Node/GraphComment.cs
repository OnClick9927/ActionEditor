using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UIElements;

namespace ActionEditor.Nodes
{
    public sealed class GraphComment : StickyNote
    {
        private static readonly Vector2 DefaultCommentSize = new Vector2(300, 190);
        private const float MinimumAutoWidth = 220;
        private const float MinimumAutoHeight = 100;
        private const float HorizontalTextPadding = 32;
        private const float VerticalTextPadding = 24;
        private const float EmptyTitleHeight = 12;
        private static readonly Color GroupSurfaceColor =
            new Color(0.18f, 0.18f, 0.18f, 0.96f);
        private static readonly Color GroupBorderColor =
            new Color(0.38f, 0.38f, 0.38f, 0.9f);
        private static readonly Color GroupTitleColor =
            new Color(0.88f, 0.88f, 0.88f, 1);
        private static readonly Color GroupContentColor =
            new Color(0.72f, 0.72f, 0.72f, 1);
        private bool applyingData;
        private bool fitToTextPending;
        private bool fitToTextDirty;

        public GraphCommentData Data { get; }
        public string GUID => Data.guid;

        internal GraphComment(GraphCommentData data)
        {
            Data = data ?? throw new ArgumentNullException(nameof(data));
            capabilities &= ~Capabilities.Groupable;

            applyingData = true;
            title = Data.title ?? string.Empty;
            contents = Data.content ?? string.Empty;
            theme = StickyNoteTheme.Black;
            fontSize = ReadEnum(Data.fontSize, StickyNoteFontSize.Large);
            Rect rect = Data.position;
            if (rect.width < 1 || rect.height < 1)
                rect.size = DefaultCommentSize;
            SetPosition(rect);
            applyingData = false;

            ApplyGroupStyle();
            DisableCanvasEditing();
            RegisterCallback<StickyNoteChangeEvent>(OnStickyNoteChanged);
            RegisterCallback<ContextualMenuPopulateEvent>(BuildReadOnlyMenu);
        }

        internal void OnInspectorGUI()
        {
            EditorGUI.BeginChangeCheck();
            string newTitle = EditorGUILayout.TextField("标题", title);
            string newContents = EditorGUILayout.TextArea(contents,
                GUILayout.MinHeight(120));
            StickyNoteFontSize newFontSize = (StickyNoteFontSize)
                EditorGUILayout.EnumPopup("字号", fontSize);
            if (!EditorGUI.EndChangeCheck()) return;

            applyingData = true;
            title = newTitle;
            contents = newContents;
            fontSize = newFontSize;
            applyingData = false;
            ApplyGroupStyle();
            SyncData();
            ScheduleFitToText();
        }

        internal void FocusContent()
        {
            schedule.Execute(Focus);
        }

        internal GraphCommentData WriteData()
        {
            SyncData();
            return Data;
        }

        private void OnStickyNoteChanged(StickyNoteChangeEvent evt)
        {
            if (applyingData) return;
            if (evt.change != StickyNoteChange.Position)
            {
                applyingData = true;
                title = Data.title ?? string.Empty;
                contents = Data.content ?? string.Empty;
                theme = StickyNoteTheme.Black;
                fontSize = ReadEnum(Data.fontSize, StickyNoteFontSize.Large);
                applyingData = false;
                ApplyGroupStyle();
                return;
            }
            SyncData();
            App.RequestUndoCommit("Move Comment");
        }

        private void SyncData()
        {
            Data.title = title;
            Data.content = contents;
            Data.theme = (int)StickyNoteTheme.Black;
            Data.fontSize = (int)fontSize;
            Data.position = GetPosition();
        }

        private void ScheduleFitToText()
        {
            if (fitToTextPending)
            {
                fitToTextDirty = true;
                return;
            }
            fitToTextPending = true;
            schedule.Execute(() =>
            {
                applyingData = true;
                FitSizeToText();
                applyingData = false;
                ApplyGroupStyle();
                SyncData();

                bool repeat = fitToTextDirty;
                fitToTextDirty = false;
                fitToTextPending = false;
                if (repeat)
                    ScheduleFitToText();
                else
                    App.RequestInspectorUndoCommit("Edit Comment");
            }).ExecuteLater(1);
        }

        private void FitSizeToText()
        {
            Label titleLabel = this.Q<Label>("title");
            Label contentLabel = this.Q<Label>("contents");
            float titleWidth = MeasureLongestLine(titleLabel, title);
            float contentWidth = MeasureLongestLine(contentLabel, contents);
            float width = Mathf.Max(MinimumAutoWidth, Mathf.Ceil(
                Mathf.Max(titleWidth, contentWidth) + HorizontalTextPadding));

            float titleHeight = string.IsNullOrEmpty(title)
                ? EmptyTitleHeight
                : CountLines(title) * MeasureLineHeight(titleLabel);
            float contentHeight = CountLines(contents) *
                MeasureLineHeight(contentLabel);
            float height = Mathf.Max(MinimumAutoHeight, Mathf.Ceil(
                titleHeight + contentHeight + VerticalTextPadding));

            Rect rect = GetPosition();
            rect.width = width;
            rect.height = height;
            SetPosition(rect);
        }

        private static float MeasureLongestLine(Label label, string value)
        {
            if (label == null || string.IsNullOrEmpty(value)) return 0;

            float width = 0;
            string[] lines = value.Replace("\r", string.Empty).Split('\n');
            for (int i = 0; i < lines.Length; i++)
            {
                float lineWidth = label.MeasureTextSize(lines[i], 0,
                    MeasureMode.Undefined, 0, MeasureMode.Undefined).x;
                if (lineWidth > width) width = lineWidth;
            }
            return width;
        }

        private static float MeasureLineHeight(Label label)
        {
            if (label == null) return 1;

            float height = label.MeasureTextSize("Ag\nAg", 0,
                MeasureMode.Undefined, 0, MeasureMode.Undefined).y * 0.5f;
            if (!float.IsNaN(height) && !float.IsInfinity(height) &&
                height > 0)
                return height;
            return Mathf.Max(1, label.resolvedStyle.fontSize * 1.2f);
        }

        private static int CountLines(string value)
        {
            if (string.IsNullOrEmpty(value)) return 1;

            int count = 1;
            for (int i = 0; i < value.Length; i++)
            {
                if (value[i] == '\n') count++;
            }
            return count;
        }

        private void ApplyGroupStyle()
        {
            style.backgroundColor = GroupSurfaceColor;

            VisualElement border = this.Q<VisualElement>("node-border");
            if (border != null)
            {
                border.style.borderLeftColor = GroupBorderColor;
                border.style.borderTopColor = GroupBorderColor;
                border.style.borderRightColor = GroupBorderColor;
                border.style.borderBottomColor = GroupBorderColor;
            }

            Label titleLabel = this.Q<Label>("title");
            if (titleLabel != null)
            {
                titleLabel.style.color = GroupTitleColor;
                titleLabel.style.whiteSpace = WhiteSpace.NoWrap;
            }
            Label contentLabel = this.Q<Label>("contents");
            if (contentLabel != null)
            {
                contentLabel.style.color = GroupContentColor;
                contentLabel.style.whiteSpace = WhiteSpace.NoWrap;
            }
        }

        private void DisableCanvasEditing()
        {
            List<Label> labels = this.Query<Label>().ToList();
            for (int i = 0; i < labels.Count; i++)
                labels[i].pickingMode = PickingMode.Ignore;

            List<TextField> fields = this.Query<TextField>().ToList();
            for (int i = 0; i < fields.Count; i++)
            {
                fields[i].isReadOnly = true;
                fields[i].focusable = false;
                fields[i].pickingMode = PickingMode.Ignore;
            }
        }

        private void BuildReadOnlyMenu(ContextualMenuPopulateEvent evt)
        {
            evt.menu.MenuItems().Clear();
            evt.menu.AppendAction("Delete", _ =>
                App.view.DeleteElements(new List<GraphElement> { this }),
                DropdownMenuAction.AlwaysEnabled);
            evt.StopImmediatePropagation();
        }

        private static T ReadEnum<T>(int value, T fallback) where T : struct
        {
            return Enum.IsDefined(typeof(T), value)
                ? (T)Enum.ToObject(typeof(T), value)
                : fallback;
        }

    }
}
