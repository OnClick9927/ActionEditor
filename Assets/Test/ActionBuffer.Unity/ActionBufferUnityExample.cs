using System;
using ActionBuffer;
using ActionBuffer.Unity;
using UnityEngine;

public sealed class ActionBufferUnityExample : MonoBehaviour
{
    [Serializable]
    private sealed class Payload
    {
        public UnityEngine.Object anyObject;
        public Texture2D texture;
        public GameObject sceneObject;
    }

    [SerializeField] private Texture2D texture;
    [SerializeField] private GameObject sceneObject;

    [ContextMenu("Verify ActionBuffer Unity References")]
    private void VerifyReferences()
    {
        var registry = new UnityObjectRegistry();
        if (texture != null) registry.Register("texture", texture);
        if (sceneObject != null) registry.Register("scene", sceneObject);

        BuffSettings settings = UnityObjectSerialization.CreateSettings(registry);
        var source = new Payload
        {
            anyObject = texture != null
                ? (UnityEngine.Object)texture
                : sceneObject,
            texture = texture,
            sceneObject = sceneObject
        };

        byte[] bytes = BuffSerializer.ToBytes(source, settings);
        var binaryCopy = BuffSerializer.FromBytes<Payload>(bytes, settings);
        string json = BuffSerializer.ToJson(source, settings);
        var jsonCopy = BuffSerializer.FromJson<Payload>(json, settings);

        Debug.Assert(ReferenceEquals(source.anyObject, binaryCopy.anyObject));
        Debug.Assert(ReferenceEquals(source.texture, binaryCopy.texture));
        Debug.Assert(ReferenceEquals(source.sceneObject, binaryCopy.sceneObject));
        Debug.Assert(ReferenceEquals(source.anyObject, jsonCopy.anyObject));
        Debug.Assert(ReferenceEquals(source.texture, jsonCopy.texture));
        Debug.Assert(ReferenceEquals(source.sceneObject, jsonCopy.sceneObject));
    }
}
