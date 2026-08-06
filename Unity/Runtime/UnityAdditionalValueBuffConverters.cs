using System;
using ActionBuffer;
using UnityEngine;

namespace ActionBuffer.Unity
{
    internal static class UnityAdditionalValueBuffConverters
    {
        internal static void Register(BuffSettings settings)
        {
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
            return removed;
        }

        private static void Register<T>(BuffSettings settings, int blockCount,
            Action<T, UnityValueBlockCollection> encode,
            Func<UnityValueBlockCollection, T> decode) =>
            UnityValueBuffConverters.RegisterValueAndCollections(settings,
                UnityValueBuffConverters.CreateValue(blockCount, encode, decode));
    }
}
