using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using ActionEditor.Nodes.Dialog;
using System;


public class DialogExample : MonoBehaviour
{
    public TextAsset asset;
    void Start()
    {
        var dialog = DialogAsset.FromBytes(typeof(DialogAsset), asset.bytes) as DialogAsset;
        dialog.PrepareForRuntime();
        Console.WriteLine();
    }

    // Update is called once per frame
    void Update()
    {

    }
}
