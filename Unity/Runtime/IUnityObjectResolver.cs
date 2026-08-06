using System;
using UnityEngine;

namespace ActionBuffer.Unity
{
    public interface IUnityObjectResolver
    {
        string GetReferenceId(UnityEngine.Object value);
        UnityEngine.Object ResolveReference(string referenceId, Type expectedType);
    }
}
