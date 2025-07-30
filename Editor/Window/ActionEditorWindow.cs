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
            AppInternal.Window = this;


            titleContent = new GUIContent(Lan.ins.Title);
            minSize = new Vector2(500, 250);
            EditorEX.InitializeAssetTypes();
            _timelineView = this.CreateView<TimelineView>();
        }

        void OnDisable()
        {
            AppInternal.Window = null;
        }
        private void Update()
        {
            UpdateViews();
            if (AppInternal.NeedForceRefresh)
                this.Repaint();
            AppInternal.OnUpdate();
        }

        private Event eve;
        public PointerEventData _eventData = new PointerEventData();

        void OnGUI()
        {
            eve = Event.current;
            _eventData.SetEvent(eve);
            var pos = this.position;
            pos.position = Vector2.zero;
            _timelineView.OnGUI(pos);
            AppInternal.OnGUIEnd();
            AppInternal.KeyBoardEvent(eve);
            if (AppInternal.CopyAsset != null)
                this.Repaint();
        }
        private void OnInspectorUpdate()
        {
            if (AppInternal.SelectCount > 0)
                this.Repaint();
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