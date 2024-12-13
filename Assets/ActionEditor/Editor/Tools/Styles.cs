using UnityEditor;
using UnityEngine;

namespace ActionEditor
{
    static class Styles
    {
        private static float _timelineLeftWidth = 240;
        private static float _timelineRightWidth;

        public static Texture2D WhiteTexture => EditorGUIUtility.whiteTexture;

        private static Texture2D _Stripes;


        public static Texture2D Stripes
        {
            get
            {
                if (_Stripes == null)
                    _Stripes = (Texture2D)Resources.Load("Stripes");
                return _Stripes;
            }
        }

        public static Color EndPointerColor = new Color(57 / 255f, 122 / 255f, 234 / 255f, 1);
        public static Color TimeStepRectColor = new Color(57 / 255f, 122 / 255f, 234 / 255f, 50 / 255f);

        public static Color ClipBackColor = new Color(0.3f, 0.3f, 0.3f);

        public static Color ClipBlendColor = new Color(144 / 255f, 144 / 255f, 144 / 255f, 0.5f);


        public const int SplitterWidth = 5;
        public const int RightGapWidth = 4;
        public const int HeaderHeight = 20;
        public const int PlayControlHeight = 40;
        public const int Space = 2;
        public const int LineHeight = 26;
        public const int ClipBottomRectHeight = 4;
        public const int ClipScaleRectWidth = 5;

        public const int BottomHeight = 16;


        public static float TimelineLeftWidth
        {
            get => _timelineLeftWidth;
            set
            {
                _timelineLeftWidth = value;
            }
        }

        public static float TimelineLeftTotalWidth => TimelineLeftWidth + SplitterWidth + RightGapWidth;

        public static float TimelineRightWidth
        {
            get => _timelineRightWidth;
            set => _timelineRightWidth = value;
        }

        public static Vector2 TimelineScrollPos;

    }
}

