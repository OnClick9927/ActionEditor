using System;
using UnityEditor;
using UnityEngine;

namespace ActionAttribute
{
    internal readonly struct SearchPopupItem
    {
        internal readonly string Label;
        internal readonly object Value;

        internal SearchPopupItem(string label, object value)
        {
            Label = label ?? "NULL";
            Value = value;
        }
    }

    internal sealed class SearchPopupWindow : EditorWindow
    {
        private string[] items;
        private Action<int> selected;
        private Func<string, SearchPopupItem[]> source;
        private SearchPopupItem[] sourceItems;
        private Action<SearchPopupItem> sourceSelected;
        private object currentValue;
        private string search = string.Empty;
        private Vector2 scroll;
        private int currentIndex;

        internal static void Show(Rect activatorRect, string[] items,
            int currentIndex, Action<int> selected)
        {
            var window = CreateInstance<SearchPopupWindow>();
            window.items = items ?? Array.Empty<string>();
            window.currentIndex = currentIndex;
            window.selected = selected;
            Vector2 size = new Vector2(Mathf.Max(240, activatorRect.width),
                Mathf.Clamp(window.items.Length * 20 + 34, 120, 360));
            window.ShowAsDropDown(GUIUtility.GUIToScreenRect(activatorRect), size);
        }

        internal static void Show(Rect activatorRect,
            Func<string, SearchPopupItem[]> source, object currentValue,
            Action<SearchPopupItem> selected)
        {
            var window = CreateInstance<SearchPopupWindow>();
            window.source = source;
            window.currentValue = currentValue;
            window.sourceSelected = selected;
            window.RefreshSource();
            Vector2 size = new Vector2(Mathf.Max(240, activatorRect.width),
                Mathf.Clamp(window.sourceItems.Length * 20 + 34, 120, 360));
            window.ShowAsDropDown(GUIUtility.GUIToScreenRect(activatorRect), size);
        }

        private void OnGUI()
        {
            GUI.SetNextControlName("Search");
            EditorGUI.BeginChangeCheck();
            search = EditorGUILayout.TextField(search,
                EditorStyles.toolbarSearchField);
            if (EditorGUI.EndChangeCheck() && source != null) RefreshSource();
            if (Event.current.type == EventType.Repaint &&
                string.IsNullOrEmpty(search))
                EditorGUI.FocusTextInControl("Search");

            scroll = EditorGUILayout.BeginScrollView(scroll);
            if (source != null)
            {
                DrawSourceItems();
                EditorGUILayout.EndScrollView();
                return;
            }
            for (int i = 0; i < items.Length; i++)
            {
                string item = items[i] ?? string.Empty;
                if (!string.IsNullOrEmpty(search) &&
                    item.IndexOf(search, StringComparison.OrdinalIgnoreCase) < 0)
                    continue;
                GUIStyle style = i == currentIndex
                    ? EditorStyles.selectionRect
                    : EditorStyles.label;
                Rect rect = EditorGUILayout.GetControlRect(false,
                    EditorGUIUtility.singleLineHeight);
                if (GUI.Button(rect, item, style))
                {
                    selected?.Invoke(i);
                    Close();
                    GUIUtility.ExitGUI();
                }
            }
            EditorGUILayout.EndScrollView();
        }

        private void RefreshSource()
        {
            try
            {
                sourceItems = source?.Invoke(search) ??
                    Array.Empty<SearchPopupItem>();
            }
            catch (Exception exception)
            {
                Debug.LogException(exception.InnerException ?? exception);
                sourceItems = Array.Empty<SearchPopupItem>();
            }
        }

        private void DrawSourceItems()
        {
            for (int i = 0; i < sourceItems.Length; i++)
            {
                SearchPopupItem item = sourceItems[i];
                GUIStyle style = Equals(item.Value, currentValue)
                    ? EditorStyles.selectionRect
                    : EditorStyles.label;
                Rect rect = EditorGUILayout.GetControlRect(false,
                    EditorGUIUtility.singleLineHeight);
                if (!GUI.Button(rect, item.Label, style)) continue;
                sourceSelected?.Invoke(item);
                Close();
                GUIUtility.ExitGUI();
            }
        }
    }
}
