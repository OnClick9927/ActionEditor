using System;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using ActionBuffer.Unity.Editor;

namespace ActionBuffer.Unity.Tests
{
    [TestFixture]
    public sealed class UnityObjectSerializationTests
    {
        private static readonly string[] Formats =
            { "Binary", "Json", "Yaml", "Xml" };

        private sealed class RuntimeAsset : ScriptableObject { }

        private sealed class RuntimePayload
        {
            public UnityEngine.Object BaseReference;
            public RuntimeAsset ConcreteReference;
            public UnityEngine.Object NullReference;
        }

        private sealed class EditorPayload
        {
            public UnityEngine.Object BaseReference;
            public MonoScript ConcreteReference;
        }

        [TestCaseSource(nameof(Formats))]
        public void RegistryRoundTripsBaseAndConcreteReferences(string format)
        {
            var asset = ScriptableObject.CreateInstance<RuntimeAsset>();
            try
            {
                var registry = new UnityObjectRegistry();
                registry.Register("runtime-asset", asset);
                BuffSettings settings = UnityObjectSerialization.CreateSettings(registry);
                var source = new RuntimePayload
                {
                    BaseReference = asset,
                    ConcreteReference = asset
                };

                RuntimePayload result = RoundTrip(source, format, settings);

                Assert.That(result.BaseReference, Is.SameAs(asset));
                Assert.That(result.ConcreteReference, Is.SameAs(asset));
                Assert.That(result.NullReference, Is.Null);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(asset);
            }
        }

        [TestCaseSource(nameof(Formats))]
        public void AssetDatabaseResolverRoundTripsPersistentSubclasses(string format)
        {
            const string path =
                "Assets/Test/ActionBuffer.Unity/ActionBufferUnityExample.cs";
            var script = AssetDatabase.LoadAssetAtPath<MonoScript>(path);
            Assert.That(script, Is.Not.Null);
            var settings = UnityObjectSerialization.CreateSettings(
                new AssetDatabaseUnityObjectResolver());
            var source = new EditorPayload
            {
                BaseReference = script,
                ConcreteReference = script
            };

            EditorPayload result = RoundTrip(source, format, settings);

            Assert.That(result.BaseReference, Is.SameAs(script));
            Assert.That(result.ConcreteReference, Is.SameAs(script));
        }

        [Test]
        public void RegistryRejectsUnknownReferencesDuringScan()
        {
            var asset = ScriptableObject.CreateInstance<RuntimeAsset>();
            try
            {
                BuffSettings settings = UnityObjectSerialization.CreateSettings(
                    new UnityObjectRegistry());
                Assert.Throws<InvalidOperationException>(() =>
                    BuffSerializer.ToBytes(new RuntimePayload
                    {
                        BaseReference = asset
                    }, settings));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(asset);
            }
        }

        private static T RoundTrip<T>(T value, string format,
            BuffSettings settings)
        {
            switch (format)
            {
                case "Binary":
                    return BuffSerializer.FromBytes<T>(
                        BuffSerializer.ToBytes(value, settings), settings);
                case "Json":
                    return BuffSerializer.FromJson<T>(
                        BuffSerializer.ToJson(value, settings), settings);
                case "Yaml":
                    return BuffSerializer.FromYaml<T>(
                        BuffSerializer.ToYaml(value, settings), settings);
                case "Xml":
                    return BuffSerializer.FromXml<T>(
                        BuffSerializer.ToXml(value, settings), settings);
                default:
                    throw new ArgumentOutOfRangeException(nameof(format), format, null);
            }
        }
    }
}
