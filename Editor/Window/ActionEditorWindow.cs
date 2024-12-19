using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace ActionEditor
{
    class ActionEditorWindow : EditorWindow
    {
        [MenuItem("Tools/Action Editor", false, 0)]
        public static void OpenDirectorWindow()
        {
            var window = GetWindow(typeof(ActionEditorWindow)) as ActionEditorWindow;
            if (window == null) return;
            window.Show();
        }

        private TimelineView _timelineView;

        void OnEnable()
        {
            Lan.Load();
            App.Window = this;


            titleContent = new GUIContent(Lan.ins.Title);
            minSize = new Vector2(500, 250);
            EditorEX.InitializeAssetTypes();
            App.OnInitialize?.Invoke();
            _timelineView = this.CreateView<TimelineView>();
        }

        void OnDisable()
        {
            App.Window = null;
            App.OnDisable?.Invoke();
        }
        private void Update()
        {
            UpdateViews();
            if (App.NeedForceRefresh)
                this.Repaint();
            App.OnUpdate();
        }


        void OnGUI()
        {
            _timelineView.OnGUI(this.position);
            App.OnGUIEnd();
            App.KeyBoardEvent(Event.current);
            if (App.CopyAsset != null)
            {
                this.Repaint();
            }
        }



        private List<ViewBase> _views =
            new List<ViewBase>();


        internal T CreateView<T>() where T : ViewBase, new()
        {
            var cls = new T();
            cls.Init(this);
            if (!_views.Contains(cls))
                _views.Add(cls);
            return cls;
        }

        private void UpdateViews()
        {
            foreach (var view in _views)
                view.Update();
        }
    }
}