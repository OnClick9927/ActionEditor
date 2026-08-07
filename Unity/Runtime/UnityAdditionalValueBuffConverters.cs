using System;
using ActionBuffer;
using UnityEngine;

namespace ActionBuffer.Unity
{
    internal static class UnityAdditionalValueBuffConverters
    {
        internal static void Register(BuffSettings settings)
        {
            Register(settings, 1,
                (BoundingSphere v, UnityValueBlockCollection b) =>
                    b.Set(0, UnityValueBlocks.Floats(v.position.x,
                        v.position.y, v.position.z, v.radius)),
                v =>
                {
                    var b = UnityValueBlocks.Get(v, 0);
                    return new BoundingSphere(new Vector3(b.F0, b.F1, b.F2),
                        b.F3);
                });
            Register(settings, 1,
                (ClothSkinningCoefficient v, UnityValueBlockCollection b) =>
                    b.Set(0, UnityValueBlocks.Floats(v.maxDistance,
                        v.collisionSphereDistance)),
                v =>
                {
                    var b = UnityValueBlocks.Get(v, 0);
                    return new ClothSkinningCoefficient
                    {
                        maxDistance = b.F0,
                        collisionSphereDistance = b.F1
                    };
                });
            Register(settings, 2,
                (ContactFilter2D v, UnityValueBlockCollection b) =>
                {
                    int flags = (v.useTriggers ? 1 : 0) |
                        (v.useLayerMask ? 2 : 0) |
                        (v.useDepth ? 4 : 0) |
                        (v.useOutsideDepth ? 8 : 0) |
                        (v.useNormalAngle ? 16 : 0) |
                        (v.useOutsideNormalAngle ? 32 : 0);
                    b.Set(0, UnityValueBlocks.Ints(flags, v.layerMask.value));
                    b.Set(1, UnityValueBlocks.Floats(v.minDepth, v.maxDepth,
                        v.minNormalAngle, v.maxNormalAngle));
                }, v =>
                {
                    var a = UnityValueBlocks.Get(v, 0);
                    var b = UnityValueBlocks.Get(v, 1);
                    return new ContactFilter2D
                    {
                        useTriggers = (a.I0 & 1) != 0,
                        useLayerMask = (a.I0 & 2) != 0,
                        useDepth = (a.I0 & 4) != 0,
                        useOutsideDepth = (a.I0 & 8) != 0,
                        useNormalAngle = (a.I0 & 16) != 0,
                        useOutsideNormalAngle = (a.I0 & 32) != 0,
                        layerMask = a.I1,
                        minDepth = b.F0,
                        maxDepth = b.F1,
                        minNormalAngle = b.F2,
                        maxNormalAngle = b.F3
                    };
                });
            Register(settings, 3,
                (CustomRenderTextureUpdateZone v,
                    UnityValueBlockCollection b) =>
                {
                    b.Set(0, UnityValueBlocks.Floats(v.updateZoneCenter.x,
                        v.updateZoneCenter.y, v.updateZoneCenter.z, v.rotation));
                    b.Set(1, UnityValueBlocks.Floats(v.updateZoneSize.x,
                        v.updateZoneSize.y, v.updateZoneSize.z));
                    b.Set(2, UnityValueBlocks.Ints(v.passIndex,
                        v.needSwap ? 1 : 0));
                }, v =>
                {
                    var a = UnityValueBlocks.Get(v, 0);
                    var b = UnityValueBlocks.Get(v, 1);
                    var c = UnityValueBlocks.Get(v, 2);
                    return new CustomRenderTextureUpdateZone
                    {
                        updateZoneCenter = new Vector3(a.F0, a.F1, a.F2),
                        rotation = a.F3,
                        updateZoneSize = new Vector3(b.F0, b.F1, b.F2),
                        passIndex = c.I0,
                        needSwap = c.I1 != 0
                    };
                });
            Register(settings, 2,
                (AudioConfiguration v, UnityValueBlockCollection b) =>
                {
                    b.Set(0, UnityValueBlocks.Ints((int)v.speakerMode,
                        v.dspBufferSize, v.sampleRate, v.numRealVoices));
                    b.Set(1, UnityValueBlocks.Ints(v.numVirtualVoices));
                }, v =>
                {
                    var a = UnityValueBlocks.Get(v, 0);
                    var b = UnityValueBlocks.Get(v, 1);
                    return new AudioConfiguration
                    {
                        speakerMode = (AudioSpeakerMode)a.I0,
                        dspBufferSize = a.I1,
                        sampleRate = a.I2,
                        numRealVoices = a.I3,
                        numVirtualVoices = b.I0
                    };
                });
            Register(settings, 1,
                (MatchTargetWeightMask v, UnityValueBlockCollection b) =>
                {
                    Vector3 weight = v.positionXYZWeight;
                    b.Set(0, UnityValueBlocks.Floats(weight.x, weight.y,
                        weight.z, v.rotationWeight));
                }, v =>
                {
                    var b = UnityValueBlocks.Get(v, 0);
                    return new MatchTargetWeightMask(
                        new Vector3(b.F0, b.F1, b.F2), b.F3);
                });
            Register(settings, 2,
                (JointLimits v, UnityValueBlockCollection b) =>
                {
                    b.Set(0, UnityValueBlocks.Floats(v.min, v.max,
                        v.bounciness, v.bounceMinVelocity));
                    b.Set(1, UnityValueBlocks.Floats(v.contactDistance));
                }, v =>
                {
                    var a = UnityValueBlocks.Get(v, 0);
                    var b = UnityValueBlocks.Get(v, 1);
                    return new JointLimits
                    {
                        min = a.F0,
                        max = a.F1,
                        bounciness = a.F2,
                        bounceMinVelocity = a.F3,
                        contactDistance = b.F0
                    };
                });
            Register(settings, 2,
                (LightBakingOutput v, UnityValueBlockCollection b) =>
                {
                    b.Set(0, UnityValueBlocks.Ints(v.probeOcclusionLightIndex,
                        v.occlusionMaskChannel, (int)v.lightmapBakeType,
                        (int)v.mixedLightingMode));
                    b.Set(1, UnityValueBlocks.Ints(v.isBaked ? 1 : 0));
                }, v =>
                {
                    var a = UnityValueBlocks.Get(v, 0);
                    var b = UnityValueBlocks.Get(v, 1);
                    return new LightBakingOutput
                    {
                        probeOcclusionLightIndex = a.I0,
                        occlusionMaskChannel = a.I1,
                        lightmapBakeType = (LightmapBakeType)a.I2,
                        mixedLightingMode = (MixedLightingMode)a.I3,
                        isBaked = b.I0 != 0
                    };
                });
            Register(settings, 1,
                (UnityEngine.Rendering.VirtualTexturing.GPUCacheSetting v,
                    UnityValueBlockCollection b) =>
                    b.Set(0, UnityValueBlocks.Ints((int)v.format,
                        unchecked((int)v.sizeInMegaBytes))),
                v =>
                {
                    var b = UnityValueBlocks.Get(v, 0);
                    return new UnityEngine.Rendering.VirtualTexturing.GPUCacheSetting
                    {
                        format = (UnityEngine.Experimental.Rendering.GraphicsFormat)b.I0,
                        sizeInMegaBytes = unchecked((uint)b.I1)
                    };
                });
            Register(settings, 2,
                (UnityEngine.TextCore.GlyphMetrics v,
                    UnityValueBlockCollection b) =>
                {
                    b.Set(0, UnityValueBlocks.Floats(v.width, v.height,
                        v.horizontalBearingX, v.horizontalBearingY));
                    b.Set(1, UnityValueBlocks.Floats(v.horizontalAdvance));
                }, v =>
                {
                    var a = UnityValueBlocks.Get(v, 0);
                    var b = UnityValueBlocks.Get(v, 1);
                    return new UnityEngine.TextCore.GlyphMetrics(a.F0, a.F1,
                        a.F2, a.F3, b.F0);
                });
            Register(settings, 1,
                (UnityEngine.TextCore.GlyphRect v,
                    UnityValueBlockCollection b) =>
                    b.Set(0, UnityValueBlocks.Ints(v.x, v.y, v.width,
                        v.height)),
                v =>
                {
                    var b = UnityValueBlocks.Get(v, 0);
                    return new UnityEngine.TextCore.GlyphRect(b.I0, b.I1,
                        b.I2, b.I3);
                });
            Register(settings, 1,
                (BuildCompression v, UnityValueBlockCollection b) =>
                    b.Set(0, UnityValueBlocks.Ints((int)v.compression,
                        (int)v.level, unchecked((int)v.blockSize),
                        v.enableProtect ? 1 : 0)),
                v =>
                {
                    var b = UnityValueBlocks.Get(v, 0);
                    return DecodeBuildCompression((CompressionType)b.I0,
                        (CompressionLevel)b.I1, unchecked((uint)b.I2),
                        b.I3 != 0);
                });
            Register(settings, 1,
                (UnityEngine.SceneManagement.CreateSceneParameters v,
                    UnityValueBlockCollection b) =>
                    b.Set(0, UnityValueBlocks.Ints((int)v.localPhysicsMode)),
                v => new UnityEngine.SceneManagement.CreateSceneParameters(
                    (UnityEngine.SceneManagement.LocalPhysicsMode)
                    UnityValueBlocks.Get(v, 0).I0));
            Register(settings, 1,
                (UnityEngine.SceneManagement.LoadSceneParameters v,
                    UnityValueBlockCollection b) =>
                    b.Set(0, UnityValueBlocks.Ints((int)v.loadSceneMode,
                        (int)v.localPhysicsMode)),
                v =>
                {
                    var b = UnityValueBlocks.Get(v, 0);
                    return new UnityEngine.SceneManagement.LoadSceneParameters(
                        (UnityEngine.SceneManagement.LoadSceneMode)b.I0,
                        (UnityEngine.SceneManagement.LocalPhysicsMode)b.I1);
                });
            Register(settings, 1,
                (UnityEngine.TextCore.LowLevel.GlyphValueRecord v,
                    UnityValueBlockCollection b) =>
                    b.Set(0, UnityValueBlocks.Floats(v.xPlacement,
                        v.yPlacement, v.xAdvance, v.yAdvance)),
                v =>
                {
                    var b = UnityValueBlocks.Get(v, 0);
                    return new UnityEngine.TextCore.LowLevel.GlyphValueRecord(
                        b.F0, b.F1, b.F2, b.F3);
                });
            Register(settings, 2,
                (UnityEngine.TextCore.LowLevel.GlyphAdjustmentRecord v,
                    UnityValueBlockCollection b) =>
                {
                    b.Set(0, UnityValueBlocks.Ints(
                        unchecked((int)v.glyphIndex)));
                    var value = v.glyphValueRecord;
                    b.Set(1, UnityValueBlocks.Floats(value.xPlacement,
                        value.yPlacement, value.xAdvance, value.yAdvance));
                }, v =>
                {
                    var a = UnityValueBlocks.Get(v, 0);
                    var b = UnityValueBlocks.Get(v, 1);
                    return new UnityEngine.TextCore.LowLevel.GlyphAdjustmentRecord(
                        unchecked((uint)a.I0),
                        new UnityEngine.TextCore.LowLevel.GlyphValueRecord(
                            b.F0, b.F1, b.F2, b.F3));
                });
            Register(settings, 3,
                (TreeInstance v, UnityValueBlockCollection b) =>
                {
                    b.Set(0, UnityValueBlocks.Floats(v.position.x,
                        v.position.y, v.position.z, v.widthScale));
                    b.Set(1, UnityValueBlocks.FloatInts(v.heightScale,
                        v.rotation, v.prototypeIndex));
                    b.Set(2, UnityValueBlocks.Ints(PackColor(v.color),
                        PackColor(v.lightmapColor)));
                }, v =>
                {
                    var a = UnityValueBlocks.Get(v, 0);
                    var b = UnityValueBlocks.Get(v, 1);
                    var c = UnityValueBlocks.Get(v, 2);
                    return new TreeInstance
                    {
                        position = new Vector3(a.F0, a.F1, a.F2),
                        widthScale = a.F3,
                        heightScale = b.F0,
                        rotation = b.F1,
                        prototypeIndex = b.I2,
                        color = UnpackColor(c.I0),
                        lightmapColor = UnpackColor(c.I1)
                    };
                });
            Register(settings, 2, (Pose v, UnityValueBlockCollection b) =>
            {
                b.Set(0, UnityValueBlocks.Floats(v.position.x, v.position.y,
                    v.position.z));
                b.Set(1, UnityValueBlocks.Floats(v.rotation.x, v.rotation.y,
                    v.rotation.z, v.rotation.w));
            }, v =>
            {
                var p = UnityValueBlocks.Get(v, 0);
                var r = UnityValueBlocks.Get(v, 1);
                return new Pose(new Vector3(p.F0, p.F1, p.F2),
                    new Quaternion(r.F0, r.F1, r.F2, r.F3));
            });
            Register(settings, 1,
                (Resolution v, UnityValueBlockCollection b) =>
                    b.Set(0, UnityValueBlocks.Ints(v.width, v.height,
                        v.refreshRate)),
                v =>
                {
                    var b = UnityValueBlocks.Get(v, 0);
                    return new Resolution { width = b.I0, height = b.I1,
                        refreshRate = b.I2 };
                });
            Register(settings, 2, (BoneWeight v, UnityValueBlockCollection b) =>
            {
                b.Set(0, UnityValueBlocks.Floats(v.weight0, v.weight1,
                    v.weight2, v.weight3));
                b.Set(1, UnityValueBlocks.Ints(v.boneIndex0, v.boneIndex1,
                    v.boneIndex2, v.boneIndex3));
            }, v =>
            {
                var weights = UnityValueBlocks.Get(v, 0);
                var indices = UnityValueBlocks.Get(v, 1);
                return new BoneWeight
                {
                    weight0 = weights.F0, weight1 = weights.F1,
                    weight2 = weights.F2, weight3 = weights.F3,
                    boneIndex0 = indices.I0, boneIndex1 = indices.I1,
                    boneIndex2 = indices.I2, boneIndex3 = indices.I3
                };
            });
            Register(settings, 1,
                (BoneWeight1 v, UnityValueBlockCollection b) =>
                    b.Set(0, UnityValueBlocks.FloatInts(v.weight, 0,
                        v.boneIndex)),
                v =>
                {
                    var b = UnityValueBlocks.Get(v, 0);
                    return new BoneWeight1 { weight = b.F0, boneIndex = b.I2 };
                });
            Register(settings, 2, (FrustumPlanes v, UnityValueBlockCollection b) =>
            {
                b.Set(0, UnityValueBlocks.Floats(v.left, v.right, v.bottom, v.top));
                b.Set(1, UnityValueBlocks.Floats(v.zNear, v.zFar));
            }, v =>
            {
                var a = UnityValueBlocks.Get(v, 0);
                var b = UnityValueBlocks.Get(v, 1);
                return new FrustumPlanes { left = a.F0, right = a.F1,
                    bottom = a.F2, top = a.F3, zNear = b.F0, zFar = b.F1 };
            });
            Register(settings, 1,
                (Hash128 v, UnityValueBlockCollection b) =>
                {
                    var hash = new Hash128Block { Hash = v };
                    b.Set(0, UnityValueBlock.From(hash.Guid));
                },
                v => new Hash128Block { Guid = v.Get(0) }.Hash);
            Register(settings, 1,
                (PropertyName v, UnityValueBlockCollection b) =>
                    b.Set(0, UnityValueBlocks.Ints(v.GetHashCode())),
                v => new PropertyName(UnityValueBlocks.Get(v, 0).I0));
            Register(settings, 1,
                (JointDrive v, UnityValueBlockCollection b) =>
                    b.Set(0, UnityValueBlocks.Floats(v.positionSpring,
                        v.positionDamper, v.maximumForce)),
                v =>
                {
                    var b = UnityValueBlocks.Get(v, 0);
                    return new JointDrive { positionSpring = b.F0,
                        positionDamper = b.F1, maximumForce = b.F2 };
                });
            Register(settings, 1,
                (JointMotor v, UnityValueBlockCollection b) =>
                    b.Set(0, UnityValueBlocks.FloatInts(v.targetVelocity,
                        v.force, v.freeSpin ? 1 : 0)),
                v =>
                {
                    var b = UnityValueBlocks.Get(v, 0);
                    return new JointMotor { targetVelocity = b.F0,
                        force = b.F1, freeSpin = b.I2 != 0 };
                });
            Register(settings, 1,
                (JointSpring v, UnityValueBlockCollection b) =>
                    b.Set(0, UnityValueBlocks.Floats(v.spring, v.damper,
                        v.targetPosition)),
                v =>
                {
                    var b = UnityValueBlocks.Get(v, 0);
                    return new JointSpring { spring = b.F0, damper = b.F1,
                        targetPosition = b.F2 };
                });
            Register(settings, 1,
                (SoftJointLimit v, UnityValueBlockCollection b) =>
                    b.Set(0, UnityValueBlocks.Floats(v.limit, v.bounciness,
                        v.contactDistance)),
                v =>
                {
                    var b = UnityValueBlocks.Get(v, 0);
                    return new SoftJointLimit { limit = b.F0,
                        bounciness = b.F1, contactDistance = b.F2 };
                });
            Register(settings, 1,
                (SoftJointLimitSpring v, UnityValueBlockCollection b) =>
                    b.Set(0, UnityValueBlocks.Floats(v.spring, v.damper)),
                v =>
                {
                    var b = UnityValueBlocks.Get(v, 0);
                    return new SoftJointLimitSpring { spring = b.F0,
                        damper = b.F1 };
                });
            Register(settings, 2,
                (WheelFrictionCurve v, UnityValueBlockCollection b) =>
                {
                    b.Set(0, UnityValueBlocks.Floats(v.extremumSlip,
                        v.extremumValue, v.asymptoteSlip, v.asymptoteValue));
                    b.Set(1, UnityValueBlocks.Floats(v.stiffness));
                }, v =>
                {
                    var a = UnityValueBlocks.Get(v, 0);
                    var b = UnityValueBlocks.Get(v, 1);
                    return new WheelFrictionCurve { extremumSlip = a.F0,
                        extremumValue = a.F1, asymptoteSlip = a.F2,
                        asymptoteValue = a.F3, stiffness = b.F0 };
                });
            Register(settings, 2,
                (ArticulationDrive v, UnityValueBlockCollection b) =>
                {
                    b.Set(0, UnityValueBlocks.Floats(v.lowerLimit,
                        v.upperLimit, v.stiffness, v.damping));
                    b.Set(1, UnityValueBlocks.Floats(v.forceLimit, v.target,
                        v.targetVelocity));
                }, v =>
                {
                    var a = UnityValueBlocks.Get(v, 0);
                    var b = UnityValueBlocks.Get(v, 1);
                    return new ArticulationDrive { lowerLimit = a.F0,
                        upperLimit = a.F1, stiffness = a.F2, damping = a.F3,
                        forceLimit = b.F0, target = b.F1,
                        targetVelocity = b.F2 };
                });
            Register(settings, 1,
                (JointMotor2D v, UnityValueBlockCollection b) =>
                    b.Set(0, UnityValueBlocks.Floats(v.motorSpeed,
                        v.maxMotorTorque)),
                v =>
                {
                    var b = UnityValueBlocks.Get(v, 0);
                    return new JointMotor2D { motorSpeed = b.F0,
                        maxMotorTorque = b.F1 };
                });
            Register(settings, 1,
                (JointAngleLimits2D v, UnityValueBlockCollection b) =>
                    b.Set(0, UnityValueBlocks.Floats(v.min, v.max)),
                v =>
                {
                    var b = UnityValueBlocks.Get(v, 0);
                    return new JointAngleLimits2D { min = b.F0, max = b.F1 };
                });
            Register(settings, 1,
                (JointTranslationLimits2D v, UnityValueBlockCollection b) =>
                    b.Set(0, UnityValueBlocks.Floats(v.min, v.max)),
                v =>
                {
                    var b = UnityValueBlocks.Get(v, 0);
                    return new JointTranslationLimits2D { min = b.F0,
                        max = b.F1 };
                });
            Register(settings, 1,
                (JointSuspension2D v, UnityValueBlockCollection b) =>
                    b.Set(0, UnityValueBlocks.Floats(v.dampingRatio,
                        v.frequency, v.angle)),
                v =>
                {
                    var b = UnityValueBlocks.Get(v, 0);
                    return new JointSuspension2D { dampingRatio = b.F0,
                        frequency = b.F1, angle = b.F2 };
                });
        }

        internal static bool Remove(BuffSettings settings)
        {
            bool removed = false;
            removed |= UnityValueBuffConverters.RemoveValueAndCollections<Pose>(settings);
            removed |= UnityValueBuffConverters.RemoveValueAndCollections<Resolution>(settings);
            removed |= UnityValueBuffConverters.RemoveValueAndCollections<BoneWeight>(settings);
            removed |= UnityValueBuffConverters.RemoveValueAndCollections<BoneWeight1>(settings);
            removed |= UnityValueBuffConverters.RemoveValueAndCollections<FrustumPlanes>(settings);
            removed |= UnityValueBuffConverters.RemoveValueAndCollections<Hash128>(settings);
            removed |= UnityValueBuffConverters.RemoveValueAndCollections<PropertyName>(settings);
            removed |= UnityValueBuffConverters.RemoveValueAndCollections<JointDrive>(settings);
            removed |= UnityValueBuffConverters.RemoveValueAndCollections<JointMotor>(settings);
            removed |= UnityValueBuffConverters.RemoveValueAndCollections<JointSpring>(settings);
            removed |= UnityValueBuffConverters.RemoveValueAndCollections<SoftJointLimit>(settings);
            removed |= UnityValueBuffConverters.RemoveValueAndCollections<SoftJointLimitSpring>(settings);
            removed |= UnityValueBuffConverters.RemoveValueAndCollections<WheelFrictionCurve>(settings);
            removed |= UnityValueBuffConverters.RemoveValueAndCollections<ArticulationDrive>(settings);
            removed |= UnityValueBuffConverters.RemoveValueAndCollections<JointMotor2D>(settings);
            removed |= UnityValueBuffConverters.RemoveValueAndCollections<JointAngleLimits2D>(settings);
            removed |= UnityValueBuffConverters.RemoveValueAndCollections<JointTranslationLimits2D>(settings);
            removed |= UnityValueBuffConverters.RemoveValueAndCollections<JointSuspension2D>(settings);
            removed |= UnityValueBuffConverters.RemoveValueAndCollections<BoundingSphere>(settings);
            removed |= UnityValueBuffConverters.RemoveValueAndCollections<ClothSkinningCoefficient>(settings);
            removed |= UnityValueBuffConverters.RemoveValueAndCollections<ContactFilter2D>(settings);
            removed |= UnityValueBuffConverters.RemoveValueAndCollections<CustomRenderTextureUpdateZone>(settings);
            removed |= UnityValueBuffConverters.RemoveValueAndCollections<AudioConfiguration>(settings);
            removed |= UnityValueBuffConverters.RemoveValueAndCollections<MatchTargetWeightMask>(settings);
            removed |= UnityValueBuffConverters.RemoveValueAndCollections<JointLimits>(settings);
            removed |= UnityValueBuffConverters.RemoveValueAndCollections<LightBakingOutput>(settings);
            removed |= UnityValueBuffConverters.RemoveValueAndCollections<UnityEngine.Rendering.VirtualTexturing.GPUCacheSetting>(settings);
            removed |= UnityValueBuffConverters.RemoveValueAndCollections<UnityEngine.TextCore.GlyphMetrics>(settings);
            removed |= UnityValueBuffConverters.RemoveValueAndCollections<UnityEngine.TextCore.GlyphRect>(settings);
            removed |= UnityValueBuffConverters.RemoveValueAndCollections<BuildCompression>(settings);
            removed |= UnityValueBuffConverters.RemoveValueAndCollections<UnityEngine.SceneManagement.CreateSceneParameters>(settings);
            removed |= UnityValueBuffConverters.RemoveValueAndCollections<UnityEngine.SceneManagement.LoadSceneParameters>(settings);
            removed |= UnityValueBuffConverters.RemoveValueAndCollections<UnityEngine.TextCore.LowLevel.GlyphValueRecord>(settings);
            removed |= UnityValueBuffConverters.RemoveValueAndCollections<UnityEngine.TextCore.LowLevel.GlyphAdjustmentRecord>(settings);
            removed |= UnityValueBuffConverters.RemoveValueAndCollections<TreeInstance>(settings);
            return removed;
        }

        private static void Register<T>(BuffSettings settings, int blockCount,
            Action<T, UnityValueBlockCollection> encode,
            Func<UnityValueBlockCollection, T> decode) =>
            UnityValueBuffConverters.RegisterValueAndCollections(settings,
                UnityValueConverterCache<T>.GetOrCreate(blockCount, encode,
                    decode));

        private static int PackColor(Color32 value) => value.r |
            (value.g << 8) | (value.b << 16) | (value.a << 24);

        private static Color32 UnpackColor(int value) => new Color32(
            (byte)value, (byte)(value >> 8), (byte)(value >> 16),
            (byte)(value >> 24));

        private static BuildCompression DecodeBuildCompression(
            CompressionType compression, CompressionLevel level,
            uint blockSize, bool enableProtect)
        {
            if (Matches(BuildCompression.Uncompressed, compression, level,
                    blockSize, enableProtect)) return BuildCompression.Uncompressed;
            if (Matches(BuildCompression.LZ4, compression, level, blockSize,
                    enableProtect)) return BuildCompression.LZ4;
            if (Matches(BuildCompression.LZMA, compression, level, blockSize,
                    enableProtect)) return BuildCompression.LZMA;
            if (Matches(BuildCompression.UncompressedRuntime, compression,
                    level, blockSize, enableProtect))
                return BuildCompression.UncompressedRuntime;
            if (Matches(BuildCompression.LZ4Runtime, compression, level,
                    blockSize, enableProtect)) return BuildCompression.LZ4Runtime;
            throw new FormatException(
                "BuildCompression value cannot be reconstructed by the current Unity API.");
        }

        private static bool Matches(BuildCompression value,
            CompressionType compression, CompressionLevel level,
            uint blockSize, bool enableProtect) =>
            value.compression == compression && value.level == level &&
            value.blockSize == blockSize &&
            value.enableProtect == enableProtect;
    }
}
