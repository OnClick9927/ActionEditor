using System;
using UnityEditor;
using UnityEngine;

namespace ActionAttribute
{
    internal sealed class SearchPopupWindow : EditorWindow
    {
        private string[] items;
        private Action<int> selected;
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

        private void OnGUI()
        {
            GUI.SetNextControlName("Search");
            search = EditorGUILayout.TextField(search,
                EditorStyles.toolbarSearchField);
            if (Event.current.type == EventType.Repaint &&
                string.IsNullOrEmpty(search))
                EditorGUI.FocusTextInControl("Search");

            scroll = EditorGUILayout.BeginScrollView(scroll);
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
    }
}
