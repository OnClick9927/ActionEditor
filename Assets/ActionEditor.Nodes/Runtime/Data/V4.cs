namespace ActionEditor.Nodes
{
    [System.Serializable]
    internal class V4
    {
        public float x, y, z, w;


# if UNITY_EDITOR
        public static implicit operator UnityEngine.Rect(V4 value) => new UnityEngine.Rect(value.x, value.y, value.z, value.w);
        public static implicit operator V4(UnityEngine.Rect value) => new V4 { x = value.x, y = value.y, z = value.width, w = value.height };
        public static implicit operator UnityEngine.Color(V4 value) => new UnityEngine.Color(value.x, value.y, value.z, value.w);
        public static implicit operator V4(UnityEngine.Color value) => new V4 { x = value.r, y = value.g, z = value.b, w = value.a };
        public static implicit operator UnityEngine.Vector3(V4 value) => new UnityEngine.Vector3(value.x, value.y, value.z);
        public static implicit operator V4(UnityEngine.Vector3 value) => new V4 { x = value.x, y = value.y, z = value.z };

        public static implicit operator UnityEngine.Vector4(V4 value) => new UnityEngine.Vector4(value.x, value.y, value.z, value.w);
        public static implicit operator V4(UnityEngine.Vector4 value) => new V4 { x = value.x, y = value.y, z = value.z, w = value.w };
        public static implicit operator UnityEngine.Vector2(V4 value) => new UnityEngine.Vector2(value.x, value.y);
        public static implicit operator V4(UnityEngine.Vector2 value) => new V4 { x = value.x, y = value.y };
#endif
    }

}