using System;
using ActionEditor;
using UnityEngine;

namespace ActionEditor
{
    [Name("行为轨道")]
    [TrackIcon(typeof(Transform))]
    [Color(70f / 255f, 1, 140f / 255f)]
    public class ActionTrack : Track
    {
        [Name("测试1")] public int Test1;

        [Name("测试2")] public float Test2;

        [Name("测试3")] public string Test3;
    }
}