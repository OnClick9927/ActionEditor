using System;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.Events;
using UnityEngine;
using UnityEngine.Events;
using ActionBuffer.Unity.Editor;

namespace ActionBuffer.Unity.Tests
{
    public sealed class UnityEventTestReceiver : MonoBehaviour
    {
        public int CallCount { get; private set; }
        public int LastValue { get; private set; }

        public void Receive()
        {
            CallCount++;
        }

        public void ReceiveInt(int value)
        {
            CallCount++;
            LastValue = value;
        }
    }

    [Serializable]
    public sealed class IntUnityEvent : UnityEvent<int> { }

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

        private sealed class ResourcePayload
        {
            public TextAsset Asset;
        }

        private sealed class EditorPayload
        {
            public UnityEngine.Object BaseReference;
            public MonoScript ConcreteReference;
        }

        private sealed class BehaviourPayload
        {
            public MonoBehaviour BaseReference;
            public UnityEventTestReceiver ConcreteReference;
        }

        private sealed class UnityValuePayload
        {
            public Vector3 Position;
            public Vector3Int Cell;
            public Quaternion Rotation;
            public Color32 Tint;
            public RectInt Area;
            public Bounds Bounds;
            public Matrix4x4 Matrix;
            public LayerMask LayerMask;
            public Ray Ray;
            public Plane Plane;
            public RangeInt Range;
            public AnimationCurve Curve;
            public Gradient Gradient;
            public RectOffset Padding;
            public Pose Pose;
            public Resolution Resolution;
            public BoneWeight BoneWeight;
            public BoneWeight1 BoneWeight1;
            public FrustumPlanes Frustum;
            public Hash128 Hash;
            public PropertyName PropertyName;
            public JointDrive JointDrive;
            public JointMotor JointMotor;
            public JointSpring JointSpring;
            public SoftJointLimit SoftLimit;
            public SoftJointLimitSpring SoftLimitSpring;
            public WheelFrictionCurve WheelFriction;
            public ArticulationDrive ArticulationDrive;
            public JointMotor2D JointMotor2D;
            public JointAngleLimits2D AngleLimits2D;
            public JointTranslationLimits2D TranslationLimits2D;
            public JointSuspension2D Suspension2D;
        }

        private sealed class UnityValueArrayPayload
        {
            public Vector3[] Values;
            public Matrix4x4[] Matrices;
            public BoneWeight[] Weights;
            public List<BoneWeight> WeightList;
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
        public void RuntimeResolverAutoRegistersSameProcessObjects(string format)
        {
            var asset = ScriptableObject.CreateInstance<RuntimeAsset>();
            try
            {
                var resolver = new RuntimeUnityObjectResolver();
                BuffSettings settings =
                    UnityObjectSerialization.CreateRuntimeSettings(resolver);
                var source = new RuntimePayload
                {
                    BaseReference = asset,
                    ConcreteReference = asset
                };

                RuntimePayload result = RoundTrip(source, format, settings);

                Assert.That(result.BaseReference, Is.SameAs(asset));
                Assert.That(result.ConcreteReference, Is.SameAs(asset));
                Assert.That(resolver.Count, Is.EqualTo(1));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(asset);
            }
        }

        [TestCaseSource(nameof(Formats))]
        public void RuntimeResolverReloadsResourcesWithFreshResolver(string format)
        {
            const string resourcePath = "ActionBufferRuntimeResolver";
            TextAsset asset = Resources.Load<TextAsset>(resourcePath);
            Assert.That(asset, Is.Not.Null);

            var writeResolver = new RuntimeUnityObjectResolver();
            writeResolver.RegisterResource(
                "Assets/Test/ActionBuffer.Unity/Resources/" +
                resourcePath + ".txt", asset);
            BuffSettings writeSettings =
                UnityObjectSerialization.CreateRuntimeSettings(writeResolver);
            BuffSettings readSettings =
                UnityObjectSerialization.CreateRuntimeSettings();

            ResourcePayload result = RoundTrip(new ResourcePayload
                {
                    Asset = asset
                }, format, writeSettings, readSettings);

            Assert.That(result.Asset, Is.SameAs(asset));
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
        public void AssetDatabaseResolverCachesReferenceIds()
        {
            const string path =
                "Assets/Test/ActionBuffer.Unity/ActionBufferUnityExample.cs";
            var script = AssetDatabase.LoadAssetAtPath<MonoScript>(path);
            Assert.That(script, Is.Not.Null);
            var resolver = new AssetDatabaseUnityObjectResolver();

            string first = resolver.GetReferenceId(script);
            string second = resolver.GetReferenceId(script);

            Assert.That(second, Is.SameAs(first));
            Assert.That(resolver.ResolveReference(first, typeof(MonoScript)),
                Is.SameAs(script));
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

        [TestCaseSource(nameof(Formats))]
        public void RegistryRoundTripsMonoBehaviourReferences(string format)
        {
            var gameObject = new GameObject("ActionBuffer Unity test");
            try
            {
                var receiver = gameObject.AddComponent<UnityEventTestReceiver>();
                var registry = new UnityObjectRegistry();
                registry.Register("receiver", receiver);
                BuffSettings settings = UnityObjectSerialization.CreateSettings(registry);
                var source = new BehaviourPayload
                {
                    BaseReference = receiver,
                    ConcreteReference = receiver
                };

                BehaviourPayload result = RoundTrip(source, format, settings);

                Assert.That(result.BaseReference, Is.SameAs(receiver));
                Assert.That(result.ConcreteReference, Is.SameAs(receiver));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(gameObject);
            }
        }

        [TestCaseSource(nameof(Formats))]
        public void UnityEventRestoresPersistentMonoBehaviourListener(string format)
        {
            var gameObject = new GameObject("ActionBuffer UnityEvent test");
            try
            {
                var receiver = gameObject.AddComponent<UnityEventTestReceiver>();
                var source = new UnityEvent();
                UnityEventTools.AddPersistentListener(source, receiver.Receive);
                var registry = new UnityObjectRegistry();
                registry.Register("receiver", receiver);
                BuffSettings settings = UnityObjectSerialization.CreateSettings(registry);

                UnityEvent result = RoundTrip(source, format, settings);
                result.Invoke();

                Assert.That(receiver.CallCount, Is.EqualTo(1));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(gameObject);
            }
        }

        [TestCaseSource(nameof(Formats))]
        public void GenericUnityEventRestoresDynamicListener(string format)
        {
            var gameObject = new GameObject("ActionBuffer generic UnityEvent test");
            try
            {
                var receiver = gameObject.AddComponent<UnityEventTestReceiver>();
                var source = new IntUnityEvent();
                UnityEventTools.AddPersistentListener<int>(source, receiver.ReceiveInt);
                var registry = new UnityObjectRegistry();
                registry.Register("receiver", receiver);
                BuffSettings settings = UnityObjectSerialization.CreateSettings(registry);

                IntUnityEvent result = RoundTrip(source, format, settings);
                result.Invoke(27);

                Assert.That(receiver.CallCount, Is.EqualTo(1));
                Assert.That(receiver.LastValue, Is.EqualTo(27));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(gameObject);
            }
        }

        [TestCaseSource(nameof(Formats))]
        public void UnityValueTypesRoundTrip(string format)
        {
            var curve = new AnimationCurve(new Keyframe(0, 1, 2, 3),
                new Keyframe(4, 5, 6, 7))
            {
                preWrapMode = WrapMode.PingPong,
                postWrapMode = WrapMode.Loop
            };
            var gradient = new Gradient { mode = GradientMode.Fixed };
            gradient.SetKeys(new[]
            {
                new GradientColorKey(Color.red, 0),
                new GradientColorKey(Color.blue, 1)
            }, new[]
            {
                new GradientAlphaKey(0.25f, 0),
                new GradientAlphaKey(0.75f, 1)
            });
            var source = new UnityValuePayload
            {
                Position = new Vector3(1.25f, -2.5f, 9),
                Cell = new Vector3Int(-1, 2, 3),
                Rotation = new Quaternion(0.1f, 0.2f, 0.3f, 0.9f),
                Tint = new Color32(1, 127, 222, 255),
                Area = new RectInt(-3, 4, 50, 60),
                Bounds = new Bounds(new Vector3(1, 2, 3),
                    new Vector3(4, 5, 6)),
                Matrix = Matrix4x4.TRS(new Vector3(2, 3, 4),
                    Quaternion.Euler(10, 20, 30), new Vector3(2, 2, 2)),
                LayerMask = (LayerMask)0x1234,
                Ray = new Ray(new Vector3(1, 2, 3), new Vector3(4, 5, 6)),
                Plane = new Plane(new Vector3(0, 1, 0), -7),
                Range = new RangeInt(5, 12),
                Curve = curve,
                Gradient = gradient,
                Padding = new RectOffset(1, 2, 3, 4),
                Pose = new Pose(new Vector3(7, 8, 9),
                    Quaternion.Euler(15, 25, 35)),
                Resolution = new Resolution
                    { width = 1920, height = 1080, refreshRate = 144 },
                BoneWeight = new BoneWeight
                {
                    weight0 = 0.4f, weight1 = 0.3f,
                    weight2 = 0.2f, weight3 = 0.1f,
                    boneIndex0 = 1, boneIndex1 = 3,
                    boneIndex2 = 5, boneIndex3 = 7
                },
                BoneWeight1 = new BoneWeight1 { weight = 0.75f, boneIndex = 11 },
                Frustum = new FrustumPlanes
                {
                    left = -1, right = 1, bottom = -0.5f, top = 0.5f,
                    zNear = 0.3f, zFar = 1000
                },
                Hash = new Hash128(1, 2, 3, 4),
                PropertyName = new PropertyName(1234567),
                JointDrive = new JointDrive
                {
                    positionSpring = 12, positionDamper = 3, maximumForce = 99
                },
                JointMotor = new JointMotor
                {
                    targetVelocity = 20, force = 8, freeSpin = true
                },
                JointSpring = new JointSpring
                    { spring = 9, damper = 2, targetPosition = 0.25f },
                SoftLimit = new SoftJointLimit
                    { limit = 45, bounciness = 0.2f, contactDistance = 0.05f },
                SoftLimitSpring = new SoftJointLimitSpring
                    { spring = 6, damper = 1.5f },
                WheelFriction = new WheelFrictionCurve
                {
                    extremumSlip = 0.4f, extremumValue = 1,
                    asymptoteSlip = 0.8f, asymptoteValue = 0.5f,
                    stiffness = 1.25f
                },
                ArticulationDrive = new ArticulationDrive
                {
                    lowerLimit = -30, upperLimit = 60, stiffness = 10,
                    damping = 4, forceLimit = 100, target = 5,
                    targetVelocity = 2
                },
                JointMotor2D = new JointMotor2D
                    { motorSpeed = 30, maxMotorTorque = 12 },
                AngleLimits2D = new JointAngleLimits2D { min = -20, max = 35 },
                TranslationLimits2D = new JointTranslationLimits2D
                    { min = -2, max = 6 },
                Suspension2D = new JointSuspension2D
                    { dampingRatio = 0.7f, frequency = 5, angle = 90 }
            };
            BuffSettings settings = new BuffSettings()
                .RegisterUnityValueConverters();

            UnityValuePayload result = RoundTrip(source, format, settings);

            Assert.That(result.Position, Is.EqualTo(source.Position));
            Assert.That(result.Cell, Is.EqualTo(source.Cell));
            Assert.That(result.Rotation, Is.EqualTo(source.Rotation));
            Assert.That(result.Tint, Is.EqualTo(source.Tint));
            Assert.That(result.Area, Is.EqualTo(source.Area));
            Assert.That(result.Bounds, Is.EqualTo(source.Bounds));
            Assert.That(result.Matrix, Is.EqualTo(source.Matrix));
            Assert.That(result.LayerMask.value, Is.EqualTo(source.LayerMask.value));
            Assert.That(result.Ray.origin, Is.EqualTo(source.Ray.origin));
            Assert.That(result.Ray.direction, Is.EqualTo(source.Ray.direction));
            Assert.That(result.Plane.normal, Is.EqualTo(source.Plane.normal));
            Assert.That(result.Plane.distance, Is.EqualTo(source.Plane.distance));
            Assert.That(result.Range.start, Is.EqualTo(source.Range.start));
            Assert.That(result.Range.length, Is.EqualTo(source.Range.length));
            Assert.That(result.Curve.preWrapMode, Is.EqualTo(curve.preWrapMode));
            Assert.That(result.Curve.postWrapMode, Is.EqualTo(curve.postWrapMode));
            Assert.That(result.Curve.keys, Is.EqualTo(curve.keys));
            Assert.That(result.Gradient.mode, Is.EqualTo(gradient.mode));
            Assert.That(result.Gradient.colorKeys, Is.EqualTo(gradient.colorKeys));
            Assert.That(result.Gradient.alphaKeys, Is.EqualTo(gradient.alphaKeys));
            Assert.That(result.Padding.left, Is.EqualTo(1));
            Assert.That(result.Padding.right, Is.EqualTo(2));
            Assert.That(result.Padding.top, Is.EqualTo(3));
            Assert.That(result.Padding.bottom, Is.EqualTo(4));
            Assert.That(result.Pose.position, Is.EqualTo(source.Pose.position));
            Assert.That(result.Pose.rotation, Is.EqualTo(source.Pose.rotation));
            Assert.That(result.Resolution.width, Is.EqualTo(1920));
            Assert.That(result.Resolution.height, Is.EqualTo(1080));
            Assert.That(result.Resolution.refreshRate, Is.EqualTo(144));
            Assert.That(result.BoneWeight.weight0, Is.EqualTo(0.4f));
            Assert.That(result.BoneWeight.boneIndex3, Is.EqualTo(7));
            Assert.That(result.BoneWeight1.weight, Is.EqualTo(0.75f));
            Assert.That(result.BoneWeight1.boneIndex, Is.EqualTo(11));
            Assert.That(result.Frustum.zNear, Is.EqualTo(0.3f));
            Assert.That(result.Frustum.zFar, Is.EqualTo(1000));
            Assert.That(result.Hash, Is.EqualTo(source.Hash));
            Assert.That(result.PropertyName, Is.EqualTo(source.PropertyName));
            Assert.That(result.JointDrive.positionSpring, Is.EqualTo(12));
            Assert.That(result.JointMotor.freeSpin, Is.True);
            Assert.That(result.JointSpring.targetPosition, Is.EqualTo(0.25f));
            Assert.That(result.SoftLimit.limit, Is.EqualTo(45));
            Assert.That(result.SoftLimitSpring.damper, Is.EqualTo(1.5f));
            Assert.That(result.WheelFriction.stiffness, Is.EqualTo(1.25f));
            Assert.That(result.ArticulationDrive.upperLimit, Is.EqualTo(60));
            Assert.That(result.JointMotor2D.maxMotorTorque, Is.EqualTo(12));
            Assert.That(result.AngleLimits2D.min, Is.EqualTo(-20));
            Assert.That(result.TranslationLimits2D.max, Is.EqualTo(6));
            Assert.That(result.Suspension2D.angle, Is.EqualTo(90));
        }

        [TestCaseSource(nameof(Formats))]
        public void UnityValueCollectionsRoundTrip(string format)
        {
            var matrix = new Matrix4x4
            {
                m00 = 1, m01 = 2, m02 = 3, m03 = 4,
                m10 = 5, m11 = 6, m12 = 7, m13 = 8,
                m20 = 9, m21 = 10, m22 = 11, m23 = 12,
                m30 = 13, m31 = 14, m32 = 15, m33 = 16
            };
            var weight = new BoneWeight
            {
                weight0 = 0.4f, weight1 = 0.3f,
                weight2 = 0.2f, weight3 = 0.1f,
                boneIndex0 = 1, boneIndex1 = 3,
                boneIndex2 = 5, boneIndex3 = 7
            };
            var source = new UnityValueArrayPayload
            {
                Values = new[] { new Vector3(1, 2, 3) },
                Matrices = new[] { matrix },
                Weights = new[] { weight },
                WeightList = new List<BoneWeight> { weight }
            };
            BuffSettings settings = new BuffSettings()
                .RegisterUnityValueConverters();

            UnityValueArrayPayload result = RoundTrip(source, format, settings);

            Assert.That(result.Values, Is.EqualTo(source.Values));
            Assert.That(result.Matrices, Is.EqualTo(source.Matrices));
            Assert.That(result.Weights, Is.EqualTo(source.Weights));
            Assert.That(result.WeightList, Is.EqualTo(source.WeightList));
        }

        [Test]
        public void RepeatedUnityValueWritesDoNotAllocateAfterWarmup()
        {
            MethodInfo allocationMethod = typeof(GC).GetMethod(
                "GetAllocatedBytesForCurrentThread",
                BindingFlags.Public | BindingFlags.Static);
            if (allocationMethod == null)
                Assert.Ignore("The current runtime does not expose a thread allocation counter.");

            var readAllocatedBytes = (Func<long>)Delegate.CreateDelegate(
                typeof(Func<long>), allocationMethod);
            var values = new Vector3[512];
            var matrices = new Matrix4x4[512];
            var weights = new BoneWeight[512];
            for (int i = 0; i < values.Length; i++)
            {
                values[i] = new Vector3(i + 1, i + 2, i + 3);
                matrices[i] = new Matrix4x4
                {
                    m00 = i, m01 = 2, m02 = 3, m03 = 4,
                    m10 = 5, m11 = 6, m12 = 7, m13 = 8,
                    m20 = 9, m21 = 10, m22 = 11, m23 = 12,
                    m30 = 13, m31 = 14, m32 = 15, m33 = 16
                };
                weights[i] = new BoneWeight
                {
                    weight0 = 0.4f, weight1 = 0.3f,
                    weight2 = 0.2f, weight3 = 0.1f,
                    boneIndex0 = i, boneIndex1 = 3,
                    boneIndex2 = 5, boneIndex3 = 7
                };
            }

            var source = new UnityValueArrayPayload
            {
                Values = values,
                Matrices = matrices,
                Weights = weights,
                WeightList = new List<BoneWeight>(weights)
            };
            BuffSettings settings = new BuffSettings()
                .RegisterUnityValueConverters();
            var writer = BufferWriter.Get();
            try
            {
                for (int i = 0; i < 4; i++)
                    BuffSerializer.WriteObject(writer, source, settings);

                GC.Collect();
                GC.WaitForPendingFinalizers();
                long before = readAllocatedBytes();
                const int iterations = 8;
                for (int i = 0; i < iterations; i++)
                    BuffSerializer.WriteObject(writer, source, settings);
                long allocated = readAllocatedBytes() - before;

                TestContext.Progress.WriteLine(
                    $"ActionBuffer.Unity binary write: {allocated} allocated bytes, " +
                    $"{iterations} iterations, {values.Length} Vector3 and " +
                    $"Matrix4x4 values plus BoneWeight arrays/lists");
                Assert.That(allocated, Is.LessThan(32L * 1024),
                    "Repeated Unity value writes should not allocate after warmup.");
            }
            finally
            {
                BufferWriter.Back(writer);
            }
        }

        private static T RoundTrip<T>(T value, string format,
            BuffSettings settings)
        {
            return RoundTrip(value, format, settings, settings);
        }

        private static T RoundTrip<T>(T value, string format,
            BuffSettings writeSettings, BuffSettings readSettings)
        {
            switch (format)
            {
                case "Binary":
                    return BuffSerializer.FromBytes<T>(
                        BuffSerializer.ToBytes(value, writeSettings), readSettings);
                case "Json":
                    return BuffSerializer.FromJson<T>(
                        BuffSerializer.ToJson(value, writeSettings), readSettings);
                case "Yaml":
                    return BuffSerializer.FromYaml<T>(
                        BuffSerializer.ToYaml(value, writeSettings), readSettings);
                case "Xml":
                    return BuffSerializer.FromXml<T>(
                        BuffSerializer.ToXml(value, writeSettings), readSettings);
                default:
                    throw new ArgumentOutOfRangeException(nameof(format), format, null);
            }
        }
    }
}
