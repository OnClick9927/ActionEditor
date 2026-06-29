using ActionBuffer;
using ActionEditor;
using System;
using System.Collections.Generic;
using UnityEditor;

using UnityEngine;

[CustomActionView(typeof(TLAsset))]
class TLAssetEditor : ActionEditor.ActonEditorView
{
    public override void OnAssetFooterGUI()
    {
        GUILayout.Button("OnAssetFooterGUI");
    }
    public override void OnAssetHeaderGUI()
    {
        GUILayout.Button("OnAssetFooterGUI");
    }
    public override void OnInspectorGUI()
    {
        GUILayout.Button("OnInspectorGUI");
        base.OnInspectorGUI();
    }
}
[CustomActionView(typeof(TLActionTrack))]
class TLActionTrackEditor : ActionEditor.ActonEditorView
{
    public override void OnGroupTrackLeftGUI()
    {
        base.OnGroupTrackLeftGUI();
        GUILayout.Label("left");
    }
}

[CustomActionView(typeof(TLLogSignal))]

class TLLogSignalEditor : ActionEditor.ClipEditorView
{
    TLLogSignal signal => target as TLLogSignal;
    public override void OnPreviewEnter()
    {
        Debug.Log(signal.message);
    }
    public override void OnPreviewExit()
    {

    }

}

[CustomActionView(typeof(TLMoveClip))]

class TLMoveClipEditor : ActionEditor.ClipEditorView
{
    TLMoveClip signal => target as TLMoveClip;
    private GameObject _go;
    private GameObject go
    {
        get
        {
            if (_go == null)
            {
                _go = GameObject.Find(signal.targetName);
            }
            return _go;
        }
    }
    public override void OnPreviewEnter()
    {
        Debug.Log("OnPreviewEnter");
    }
    public override void OnPreviewExit()
    {
        Debug.Log("OnPreviewExit");

    }
    public override void OnPreviewReverse()
    {
        Debug.Log("OnPreviewReverse");
    }
    public override void OnPreviewUpdate(float time, float previousTime)
    {
        var len = target.EndTime - target.StartTime;
        go.transform.position = Vector3.Lerp(signal.from, signal.to, time / len);
        //Debug.Log($"OnPreviewReverse {time} {previousTime}");
    }
}




[Attachable(typeof(TLActionTrack)), Name("移动")]

public class TLMoveClip : Clip, ActionEditor.IResizeAble, ILengthMatchAble, ITLCLip
{
    public string targetName;
    public Vector3 from;
    public Vector3 to;
    public override bool IsValid => true;

    public float MatchAbleLength => 5;

    private float time;
    private GameObject _go;
    private GameObject go
    {
        get
        {
            if (_go == null)
            {
                _go = GameObject.Find(targetName);
            }
            return _go;
        }
    }
    void ITLCLip.Update()
    {
        time += Time.deltaTime;
        var length = this.EndTime - this.StartTime;
        if (time >= this.StartTime && time < this.EndTime)
        {
            go.transform.position = Vector3.Lerp(from, to, time - this.StartTime / length);
        }
    }
}

[Attachable(typeof(TLSignalTrack)), Name("日志")]

public class TLLogSignal : ActionEditor.ClipSignal, ITLCLip
{
    public string message;
    [Buffer, UnityEngine.SerializeField, Name("输出")]
    private string Test2;
    [Range(0, 1)] public float test;
    public override bool IsValid => !string.IsNullOrEmpty(message);
    private float time;
    private bool called;
    void ITLCLip.Update()
    {
        time += Time.deltaTime;
        if (!called && time >= this.StartTime)
        {
            Debug.Log(message);
            called = true;
        }
    }
}

interface ITLCLip
{
    void Update();
}
[Attachable(typeof(TLGroup)), Name("行为轨道"), ActionEditor.Icon(typeof(Transform))]
public class TLActionTrack : Track
{

}
[Attachable(typeof(TLGroup)), Name("信号轨道"), ActionEditor.Icon(typeof(Animation))]
public class TLSignalTrack : Track
{

}

[Attachable(typeof(TLAsset))]
public class TLGroup : Group
{


}
public class TLAsset : Asset
{
    List<ITLCLip> clips = new List<ITLCLip>();
    internal void Update()
    {
        foreach (var item in clips)
        {
            item.Update();
        }
    }

    internal void Prepare()
    {
        foreach (var group in groups)
        {
            foreach (var track in group.Tracks)
            {
                foreach (var item in track.Clips)
                {
                    clips.Add(item as ITLCLip);

                }
            }
        }
    }
}



public class TLTest : MonoBehaviour
{
    public TextAsset text;
    private TLAsset tl;
    void Start()
    {
        tl = TLAsset.FromBytes(typeof(TLAsset), text.bytes) as TLAsset;
        tl.Prepare();
    }

    // Update is called once per frame
    void Update()
    {
        tl?.Update();
    }
}
