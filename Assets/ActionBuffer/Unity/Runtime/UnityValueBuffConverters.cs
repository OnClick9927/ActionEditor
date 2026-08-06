using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using ActionBuffer;
using UnityEngine;

namespace ActionBuffer.Unity
{
    [StructLayout(LayoutKind.Explicit, Size = 16)]
    internal struct UnityValueBlock
    {
        [FieldOffset(0)] internal Guid Guid;
        [FieldOffset(0)] internal float F0;
        [FieldOffset(4)] internal float F1;
        [FieldOffset(8)] internal float F2;
        [FieldOffset(12)] internal float F3;
        [FieldOffset(0)] internal int I0;
        [FieldOffset(4)] internal int I1;
        [FieldOffset(8)] internal int I2;
        [FieldOffset(12)] internal int I3;
        [FieldOffset(0)] internal byte B0;
        [FieldOffset(1)] internal byte B1;
        [FieldOffset(2)] internal byte B2;
        [FieldOffset(3)] internal byte B3;

        internal static UnityValueBlock From(Guid value) =>
            new UnityValueBlock { Guid = value };
    }

    [StructLayout(LayoutKind.Explicit, Size = 16)]
    internal struct Hash128Block
    {
        [FieldOffset(0)] internal Hash128 Hash;
        [FieldOffset(0)] internal Guid Guid;
    }

    internal sealed class UnityValueBlockCollection
    {
        private readonly Guid[] values;

        internal UnityValueBlockCollection(int count)
        {
            if (count <= 0) throw new ArgumentOutOfRangeException(nameof(count));
            values = new Guid[count];
        }

        internal int Count => values.Length;

        internal void Set(int index, UnityValueBlock value) =>
            values[index] = value.Guid;

        internal Guid Get(int index) => values[index];
        internal void SetGuid(int index, Guid value) => values[index] = value;
    }

    internal readonly struct UnityBlockPair
    {
        internal readonly Guid First;
        internal readonly Guid Second;

        internal UnityBlockPair(Guid first, Guid second)
        {
            First = first;
            Second = second;
        }
    }

    internal sealed class UnityBlockPairBuffConverter :
        BuffConverter<UnityBlockPair>
    {
        internal static readonly UnityBlockPairBuffConverter Instance =
            new UnityBlockPairBuffConverter();

        private UnityBlockPairBuffConverter() { }

        protected override void OnScan(BufferScan scan, UnityBlockPair value) { }

        protected override void OnWrite(IBufferWriter writer, BufferScan scan,
            UnityBlockPair value) => writer.WriteKeyValuePair(scan,
                new KeyValuePair<Guid, Guid>(value.First, value.Second),
                UnityGuidBuffConverter.Instance, UnityGuidBuffConverter.Instance);

        protected override UnityBlockPair OnRead(IBufferReader reader, Type type)
        {
            KeyValuePair<Guid, Guid> value = reader.ReadKeyValuePair(
                UnityGuidBuffConverter.Instance, UnityGuidBuffConverter.Instance);
            return new UnityBlockPair(value.Key, value.Value);
        }
    }

    internal sealed class UnityGuidBuffConverter : AtomicBuffConverter<Guid>
    {
        internal static readonly UnityGuidBuffConverter Instance =
            new UnityGuidBuffConverter();

        private UnityGuidBuffConverter() { }

        protected override Guid OnRead(IBufferReader reader, Type type) =>
            reader.ReadGuid();

        protected override void OnWrite(IBufferWriter writer, BufferScan scan,
            Guid value) => writer.WriteGuid(value);
    }

    internal sealed class UnityStringBuffConverter : AtomicBuffConverter<string>
    {
        internal static readonly UnityStringBuffConverter Instance =
            new UnityStringBuffConverter();

        private UnityStringBuffConverter() { }

        protected override string OnRead(IBufferReader reader, Type type) =>
            reader.ReadUTF8();

        protected override void OnWrite(IBufferWriter writer, BufferScan scan,
            string value) => writer.WriteUTF8(value);
    }

    internal sealed class UnityArrayBuffConverter<T> : BuffConverter<T[]>
    {
        private readonly BuffConverter<T> elementConverter;

        internal UnityArrayBuffConverter(BuffConverter<T> elementConverter)
        {
            this.elementConverter = elementConverter ??
                throw new ArgumentNullException(nameof(elementConverter));
        }

        protected override void OnScan(BufferScan scan, T[] value) =>
            scan.ScanEnumerable(value, elementConverter);

        protected override void OnWrite(IBufferWriter writer, BufferScan scan,
            T[] value) => writer.WriteIEnumerable(scan, elementConverter);

        protected override T[] OnRead(IBufferReader reader, Type type) =>
            reader.ReadArray(elementConverter);
    }

    internal sealed class UnityListBuffConverter<T> : BuffConverter<List<T>>
    {
        private readonly BuffConverter<T> elementConverter;

        internal UnityListBuffConverter(BuffConverter<T> elementConverter)
        {
            this.elementConverter = elementConverter ??
                throw new ArgumentNullException(nameof(elementConverter));
        }

        protected override void OnScan(BufferScan scan, List<T> value) =>
            scan.ScanEnumerable(value, elementConverter);

        protected override void OnWrite(IBufferWriter writer, BufferScan scan,
            List<T> value) => writer.WriteIEnumerable(scan, elementConverter);

        protected override List<T> OnRead(IBufferReader reader, Type type) =>
            reader.ReadList(elementConverter);
    }

    internal sealed class PackedUnityBuffConverter<T> : BuffConverter<T>
    {
        private readonly UnityValueBlockCollection writeValues;
        private readonly Action<T, UnityValueBlockCollection> encode;
        private readonly Func<UnityValueBlockCollection, T> decode;
        private readonly Func<T, bool> isNull;
        private static readonly Guid PresentMarker =
            new Guid(1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0);

        internal PackedUnityBuffConverter(int blockCount,
            Action<T, UnityValueBlockCollection> encode,
            Func<UnityValueBlockCollection, T> decode, Func<T, bool> isNull = null)
        {
            if (blockCount < 1 || blockCount > 4)
                throw new ArgumentOutOfRangeException(nameof(blockCount));
            writeValues = new UnityValueBlockCollection(blockCount);
            this.encode = encode ?? throw new ArgumentNullException(nameof(encode));
            this.decode = decode ?? throw new ArgumentNullException(nameof(decode));
            this.isNull = isNull;
        }

        protected override void OnScan(BufferScan scan, T value) { }

        protected override void OnWrite(IBufferWriter writer, BufferScan scan,
            T value)
        {
            bool hasValue = isNull == null || !isNull(value);
            if (hasValue) encode(value, writeValues);
            if (isNull != null)
            {
                if (writeValues.Count != 1)
                    throw new NotSupportedException(
                        "Nullable packed Unity values must fit in one block.");
                writer.WriteKeyValuePair(scan,
                    new KeyValuePair<Guid, Guid>(hasValue
                        ? PresentMarker : Guid.Empty,
                        hasValue ? writeValues.Get(0) : Guid.Empty),
                    UnityGuidBuffConverter.Instance,
                    UnityGuidBuffConverter.Instance);
                return;
            }
            WriteBlocks(writer, scan, writeValues);
        }

        protected override T OnRead(IBufferReader reader, Type type)
        {
            if (isNull != null)
            {
                KeyValuePair<Guid, Guid> value = reader.ReadKeyValuePair(
                    UnityGuidBuffConverter.Instance,
                    UnityGuidBuffConverter.Instance);
                if (value.Key == Guid.Empty) return default;
                if (value.Key != PresentMarker)
                    throw new FormatException(
                        $"Unity value '{typeof(T)}' has an invalid null marker.");
                writeValues.SetGuid(0, value.Value);
            }
            else
                ReadBlocks(reader, writeValues);
            return decode(writeValues);
        }

        private static void WriteBlocks(IBufferWriter writer, BufferScan scan,
            UnityValueBlockCollection values)
        {
            switch (values.Count)
            {
                case 2:
                    writer.WriteKeyValuePair(scan,
                        new KeyValuePair<Guid, Guid>(values.Get(0), values.Get(1)),
                        UnityGuidBuffConverter.Instance,
                        UnityGuidBuffConverter.Instance);
                    return;
                case 3:
                    writer.WriteKeyValuePair(scan,
                        new KeyValuePair<Guid, UnityBlockPair>(values.Get(0),
                            new UnityBlockPair(values.Get(1), values.Get(2))),
                        UnityGuidBuffConverter.Instance,
                        UnityBlockPairBuffConverter.Instance);
                    return;
                case 4:
                    writer.WriteKeyValuePair(scan,
                        new KeyValuePair<UnityBlockPair, UnityBlockPair>(
                            new UnityBlockPair(values.Get(0), values.Get(1)),
                            new UnityBlockPair(values.Get(2), values.Get(3))),
                        UnityBlockPairBuffConverter.Instance,
                        UnityBlockPairBuffConverter.Instance);
                    return;
                default:
                    throw new NotSupportedException(
                        $"Unsupported Unity block count '{values.Count}'.");
            }
        }

        private static void ReadBlocks(IBufferReader reader,
            UnityValueBlockCollection values)
        {
            switch (values.Count)
            {
                case 2:
                    var pair2 = reader.ReadKeyValuePair(
                        UnityGuidBuffConverter.Instance,
                        UnityGuidBuffConverter.Instance);
                    values.SetGuid(0, pair2.Key);
                    values.SetGuid(1, pair2.Value);
                    return;
                case 3:
                    var pair3 = reader.ReadKeyValuePair(
                        UnityGuidBuffConverter.Instance,
                        UnityBlockPairBuffConverter.Instance);
                    values.SetGuid(0, pair3.Key);
                    values.SetGuid(1, pair3.Value.First);
                    values.SetGuid(2, pair3.Value.Second);
                    return;
                case 4:
                    var pair4 = reader.ReadKeyValuePair(
                        UnityBlockPairBuffConverter.Instance,
                        UnityBlockPairBuffConverter.Instance);
                    values.SetGuid(0, pair4.Key.First);
                    values.SetGuid(1, pair4.Key.Second);
                    values.SetGuid(2, pair4.Value.First);
                    values.SetGuid(3, pair4.Value.Second);
                    return;
                default:
                    throw new NotSupportedException(
                        $"Unsupported Unity block count '{values.Count}'.");
            }
        }
    }

    internal sealed class AtomicPackedUnityBuffConverter<T> :
        AtomicBuffConverter<T>
    {
        private readonly UnityValueBlockCollection writeValue =
            new UnityValueBlockCollection(1);
        private readonly Action<T, UnityValueBlockCollection> encode;
        private readonly Func<UnityValueBlockCollection, T> decode;

        internal AtomicPackedUnityBuffConverter(
            Action<T, UnityValueBlockCollection> encode,
            Func<UnityValueBlockCollection, T> decode)
        {
            this.encode = encode ?? throw new ArgumentNullException(nameof(encode));
            this.decode = decode ?? throw new ArgumentNullException(nameof(decode));
        }

        protected override void OnWrite(IBufferWriter writer, BufferScan scan,
            T value)
        {
            encode(value, writeValue);
            writer.WriteGuid(writeValue.Get(0));
        }

        protected override T OnRead(IBufferReader reader, Type type)
        {
            writeValue.SetGuid(0, reader.ReadGuid());
            return decode(writeValue);
        }
    }

    internal static class UnityValueBlocks
    {
        internal static UnityValueBlock Floats(float f0, float f1 = 0,
            float f2 = 0, float f3 = 0) => new UnityValueBlock
            { F0 = f0, F1 = f1, F2 = f2, F3 = f3 };

        internal static UnityValueBlock Ints(int i0, int i1 = 0,
            int i2 = 0, int i3 = 0) => new UnityValueBlock
            { I0 = i0, I1 = i1, I2 = i2, I3 = i3 };

        internal static UnityValueBlock FloatInts(float f0, float f1,
            int i2 = 0, int i3 = 0) => new UnityValueBlock
            { F0 = f0, F1 = f1, I2 = i2, I3 = i3 };

        internal static UnityValueBlock Bytes(byte b0, byte b1, byte b2,
            byte b3) => new UnityValueBlock
            { B0 = b0, B1 = b1, B2 = b2, B3 = b3 };

        internal static UnityValueBlock Get(UnityValueBlockCollection values,
            int index) => UnityValueBlock.From(values.Get(index));
    }

    internal static class UnityValueBuffConverters
    {
        internal static void Register(BuffSettings settings)
        {
            Register(settings, 1,
                (Vector2 v, UnityValueBlockCollection b) =>
                    b.Set(0, UnityValueBlocks.Floats(v.x, v.y)),
                v => { var b = UnityValueBlocks.Get(v, 0); return new Vector2(b.F0, b.F1); });
            Register(settings, 1,
                (Vector3 v, UnityValueBlockCollection b) =>
                    b.Set(0, UnityValueBlocks.Floats(v.x, v.y, v.z)),
                v => { var b = UnityValueBlocks.Get(v, 0); return new Vector3(b.F0, b.F1, b.F2); });
            Register(settings, 1,
                (Vector4 v, UnityValueBlockCollection b) =>
                    b.Set(0, UnityValueBlocks.Floats(v.x, v.y, v.z, v.w)),
                v => { var b = UnityValueBlocks.Get(v, 0); return new Vector4(b.F0, b.F1, b.F2, b.F3); });
            Register(settings, 1,
                (Vector2Int v, UnityValueBlockCollection b) =>
                    b.Set(0, UnityValueBlocks.Ints(v.x, v.y)),
                v => { var b = UnityValueBlocks.Get(v, 0); return new Vector2Int(b.I0, b.I1); });
            Register(settings, 1,
                (Vector3Int v, UnityValueBlockCollection b) =>
                    b.Set(0, UnityValueBlocks.Ints(v.x, v.y, v.z)),
                v => { var b = UnityValueBlocks.Get(v, 0); return new Vector3Int(b.I0, b.I1, b.I2); });
            Register(settings, 1,
                (Quaternion v, UnityValueBlockCollection b) =>
                    b.Set(0, UnityValueBlocks.Floats(v.x, v.y, v.z, v.w)),
                v => { var b = UnityValueBlocks.Get(v, 0); return new Quaternion(b.F0, b.F1, b.F2, b.F3); });
            Register(settings, 1,
                (Color v, UnityValueBlockCollection b) =>
                    b.Set(0, UnityValueBlocks.Floats(v.r, v.g, v.b, v.a)),
                v => { var b = UnityValueBlocks.Get(v, 0); return new Color(b.F0, b.F1, b.F2, b.F3); });
            Register(settings, 1,
                (Color32 v, UnityValueBlockCollection b) =>
                    b.Set(0, UnityValueBlocks.Bytes(v.r, v.g, v.b, v.a)),
                v => { var b = UnityValueBlocks.Get(v, 0); return new Color32(b.B0, b.B1, b.B2, b.B3); });
            Register(settings, 1,
                (Rect v, UnityValueBlockCollection b) =>
                    b.Set(0, UnityValueBlocks.Floats(v.x, v.y, v.width, v.height)),
                v => { var b = UnityValueBlocks.Get(v, 0); return new Rect(b.F0, b.F1, b.F2, b.F3); });
            Register(settings, 1,
                (RectInt v, UnityValueBlockCollection b) =>
                    b.Set(0, UnityValueBlocks.Ints(v.x, v.y, v.width, v.height)),
                v => { var b = UnityValueBlocks.Get(v, 0); return new RectInt(b.I0, b.I1, b.I2, b.I3); });
            Register(settings, 2, EncodeBounds, DecodeBounds);
            Register(settings, 2, EncodeBoundsInt, DecodeBoundsInt);
            Register(settings, 4, EncodeMatrix, DecodeMatrix);
            Register(settings, 1,
                (LayerMask v, UnityValueBlockCollection b) =>
                    b.Set(0, UnityValueBlocks.Ints(v.value)),
                v => (LayerMask)UnityValueBlocks.Get(v, 0).I0);
            Register(settings, 2, EncodeRay, DecodeRay);
            Register(settings, 2, EncodeRay2D, DecodeRay2D);
            Register(settings, 1,
                (Plane v, UnityValueBlockCollection b) =>
                    b.Set(0, UnityValueBlocks.Floats(v.normal.x, v.normal.y,
                        v.normal.z, v.distance)),
                v => { var b = UnityValueBlocks.Get(v, 0); return new Plane { normal = new Vector3(b.F0, b.F1, b.F2), distance = b.F3 }; });
            Register(settings, 1,
                (RangeInt v, UnityValueBlockCollection b) =>
                    b.Set(0, UnityValueBlocks.Ints(v.start, v.length)),
                v => { var b = UnityValueBlocks.Get(v, 0); return new RangeInt(b.I0, b.I1); });

            var keyframe = CreateKeyframeConverter();
            RegisterValueAndCollections(settings, keyframe);
            var colorKey = CreateGradientColorKeyConverter();
            RegisterValueAndCollections(settings, colorKey);
            var alphaKey = CreateGradientAlphaKeyConverter();
            RegisterValueAndCollections(settings, alphaKey);
            RegisterValueAndCollections(settings,
                new AnimationCurveBuffConverter());
            RegisterValueAndCollections(settings, new GradientBuffConverter());

            Register(settings, 1,
                (RectOffset v, UnityValueBlockCollection b) =>
                    b.Set(0, UnityValueBlocks.Ints(v.left, v.right, v.top, v.bottom)),
                v => { var b = UnityValueBlocks.Get(v, 0); return new RectOffset(b.I0, b.I1, b.I2, b.I3); },
                v => v == null);
            UnityAdditionalValueBuffConverters.Register(settings);
        }

        internal static bool Remove(BuffSettings settings)
        {
            bool removed = false;
            removed |= RemoveValueAndCollections<Vector2>(settings);
            removed |= RemoveValueAndCollections<Vector3>(settings);
            removed |= RemoveValueAndCollections<Vector4>(settings);
            removed |= RemoveValueAndCollections<Vector2Int>(settings);
            removed |= RemoveValueAndCollections<Vector3Int>(settings);
            removed |= RemoveValueAndCollections<Quaternion>(settings);
            removed |= RemoveValueAndCollections<Color>(settings);
            removed |= RemoveValueAndCollections<Color32>(settings);
            removed |= RemoveValueAndCollections<Rect>(settings);
            removed |= RemoveValueAndCollections<RectInt>(settings);
            removed |= RemoveValueAndCollections<Bounds>(settings);
            removed |= RemoveValueAndCollections<BoundsInt>(settings);
            removed |= RemoveValueAndCollections<Matrix4x4>(settings);
            removed |= RemoveValueAndCollections<LayerMask>(settings);
            removed |= RemoveValueAndCollections<Ray>(settings);
            removed |= RemoveValueAndCollections<Ray2D>(settings);
            removed |= RemoveValueAndCollections<Plane>(settings);
            removed |= RemoveValueAndCollections<RangeInt>(settings);
            removed |= RemoveValueAndCollections<Keyframe>(settings);
            removed |= RemoveValueAndCollections<GradientColorKey>(settings);
            removed |= RemoveValueAndCollections<GradientAlphaKey>(settings);
            removed |= RemoveValueAndCollections<AnimationCurve>(settings);
            removed |= RemoveValueAndCollections<Gradient>(settings);
            removed |= RemoveValueAndCollections<RectOffset>(settings);
            removed |= UnityAdditionalValueBuffConverters.Remove(settings);
            return removed;
        }

        internal static PackedUnityBuffConverter<T> Create<T>(int blockCount,
            Action<T, UnityValueBlockCollection> encode,
            Func<UnityValueBlockCollection, T> decode,
            Func<T, bool> isNull = null) =>
            new PackedUnityBuffConverter<T>(blockCount, encode, decode, isNull);

        internal static BuffConverter<T> CreateValue<T>(int blockCount,
            Action<T, UnityValueBlockCollection> encode,
            Func<UnityValueBlockCollection, T> decode,
            Func<T, bool> isNull = null) =>
            blockCount == 1 && isNull == null
                ? (BuffConverter<T>)new AtomicPackedUnityBuffConverter<T>(encode,
                    decode)
                : new PackedUnityBuffConverter<T>(blockCount, encode, decode,
                    isNull);

        internal static void RegisterValueAndCollections<T>(BuffSettings settings,
            BuffConverter<T> converter)
        {
            settings.RegisterConverter(converter);
            settings.RegisterConverter(new UnityArrayBuffConverter<T>(converter));
            settings.RegisterConverter(new UnityListBuffConverter<T>(converter));
        }

        internal static bool RemoveValueAndCollections<T>(BuffSettings settings)
        {
            bool removed = settings.RemoveConverter<T>();
            removed |= settings.RemoveConverter<T[]>();
            removed |= settings.RemoveConverter<List<T>>();
            return removed;
        }

        private static void Register<T>(BuffSettings settings, int blockCount,
            Action<T, UnityValueBlockCollection> encode,
            Func<UnityValueBlockCollection, T> decode,
            Func<T, bool> isNull = null) =>
            RegisterValueAndCollections(settings,
                CreateValue(blockCount, encode, decode, isNull));

        private static PackedUnityBuffConverter<Keyframe> CreateKeyframeConverter() =>
            Create<Keyframe>(2, (v, b) =>
            {
                b.Set(0, UnityValueBlocks.Floats(v.time, v.value,
                    v.inTangent, v.outTangent));
                b.Set(1, UnityValueBlocks.FloatInts(v.inWeight,
                    v.outWeight, (int)v.weightedMode));
            }, v =>
            {
                var a = UnityValueBlocks.Get(v, 0);
                var b = UnityValueBlocks.Get(v, 1);
                return new Keyframe(a.F0, a.F1, a.F2, a.F3, b.F0, b.F1)
                    { weightedMode = (WeightedMode)b.I2 };
            });

        private static PackedUnityBuffConverter<GradientColorKey>
            CreateGradientColorKeyConverter() => Create<GradientColorKey>(2,
                (v, b) =>
                {
                    b.Set(0, UnityValueBlocks.Floats(v.color.r, v.color.g,
                        v.color.b, v.color.a));
                    b.Set(1, UnityValueBlocks.Floats(v.time));
                }, v =>
                {
                    var color = UnityValueBlocks.Get(v, 0);
                    var time = UnityValueBlocks.Get(v, 1);
                    return new GradientColorKey(new Color(color.F0, color.F1,
                        color.F2, color.F3), time.F0);
                });

        private static PackedUnityBuffConverter<GradientAlphaKey>
            CreateGradientAlphaKeyConverter() => Create<GradientAlphaKey>(1,
                (v, b) => b.Set(0, UnityValueBlocks.Floats(v.alpha, v.time)),
                v =>
                {
                    var b = UnityValueBlocks.Get(v, 0);
                    return new GradientAlphaKey(b.F0, b.F1);
                });

        private static void EncodeBounds(Bounds v, UnityValueBlockCollection b)
        {
            Vector3 center = v.center;
            Vector3 size = v.size;
            b.Set(0, UnityValueBlocks.Floats(center.x, center.y, center.z));
            b.Set(1, UnityValueBlocks.Floats(size.x, size.y, size.z));
        }

        private static Bounds DecodeBounds(UnityValueBlockCollection v)
        {
            var center = UnityValueBlocks.Get(v, 0);
            var size = UnityValueBlocks.Get(v, 1);
            return new Bounds(new Vector3(center.F0, center.F1, center.F2),
                new Vector3(size.F0, size.F1, size.F2));
        }

        private static void EncodeBoundsInt(BoundsInt v,
            UnityValueBlockCollection b)
        {
            Vector3Int position = v.position;
            Vector3Int size = v.size;
            b.Set(0, UnityValueBlocks.Ints(position.x, position.y, position.z));
            b.Set(1, UnityValueBlocks.Ints(size.x, size.y, size.z));
        }

        private static BoundsInt DecodeBoundsInt(UnityValueBlockCollection v)
        {
            var position = UnityValueBlocks.Get(v, 0);
            var size = UnityValueBlocks.Get(v, 1);
            return new BoundsInt(new Vector3Int(position.I0, position.I1, position.I2),
                new Vector3Int(size.I0, size.I1, size.I2));
        }

        private static void EncodeMatrix(Matrix4x4 v,
            UnityValueBlockCollection b)
        {
            b.Set(0, UnityValueBlocks.Floats(v.m00, v.m01, v.m02, v.m03));
            b.Set(1, UnityValueBlocks.Floats(v.m10, v.m11, v.m12, v.m13));
            b.Set(2, UnityValueBlocks.Floats(v.m20, v.m21, v.m22, v.m23));
            b.Set(3, UnityValueBlocks.Floats(v.m30, v.m31, v.m32, v.m33));
        }

        private static Matrix4x4 DecodeMatrix(UnityValueBlockCollection v)
        {
            var r0 = UnityValueBlocks.Get(v, 0);
            var r1 = UnityValueBlocks.Get(v, 1);
            var r2 = UnityValueBlocks.Get(v, 2);
            var r3 = UnityValueBlocks.Get(v, 3);
            var result = new Matrix4x4();
            result.m00 = r0.F0; result.m01 = r0.F1;
            result.m02 = r0.F2; result.m03 = r0.F3;
            result.m10 = r1.F0; result.m11 = r1.F1;
            result.m12 = r1.F2; result.m13 = r1.F3;
            result.m20 = r2.F0; result.m21 = r2.F1;
            result.m22 = r2.F2; result.m23 = r2.F3;
            result.m30 = r3.F0; result.m31 = r3.F1;
            result.m32 = r3.F2; result.m33 = r3.F3;
            return result;
        }

        private static void EncodeRay(Ray v, UnityValueBlockCollection b)
        {
            b.Set(0, UnityValueBlocks.Floats(v.origin.x, v.origin.y, v.origin.z));
            b.Set(1, UnityValueBlocks.Floats(v.direction.x, v.direction.y,
                v.direction.z));
        }

        private static Ray DecodeRay(UnityValueBlockCollection v)
        {
            var origin = UnityValueBlocks.Get(v, 0);
            var direction = UnityValueBlocks.Get(v, 1);
            return new Ray
            {
                origin = new Vector3(origin.F0, origin.F1, origin.F2),
                direction = new Vector3(direction.F0, direction.F1, direction.F2)
            };
        }

        private static void EncodeRay2D(Ray2D v, UnityValueBlockCollection b)
        {
            b.Set(0, UnityValueBlocks.Floats(v.origin.x, v.origin.y));
            b.Set(1, UnityValueBlocks.Floats(v.direction.x, v.direction.y));
        }

        private static Ray2D DecodeRay2D(UnityValueBlockCollection v)
        {
            var origin = UnityValueBlocks.Get(v, 0);
            var direction = UnityValueBlocks.Get(v, 1);
            return new Ray2D
            {
                origin = new Vector2(origin.F0, origin.F1),
                direction = new Vector2(direction.F0, direction.F1)
            };
        }
    }

    internal readonly struct CurveRecord
    {
        internal readonly Keyframe Key;
        internal readonly int PreWrapMode;
        internal readonly int PostWrapMode;
        internal readonly bool IsHeader;

        internal CurveRecord(int preWrapMode, int postWrapMode)
        {
            Key = default;
            PreWrapMode = preWrapMode;
            PostWrapMode = postWrapMode;
            IsHeader = true;
        }

        internal CurveRecord(Keyframe key)
        {
            Key = key;
            PreWrapMode = 0;
            PostWrapMode = 0;
            IsHeader = false;
        }
    }

    internal sealed class CurveRecordCollection : ICollection<CurveRecord>
    {
        internal AnimationCurve Source;
        public int Count => Source == null ? 0 : Source.length + 1;
        public bool IsReadOnly => true;

        public void CopyTo(CurveRecord[] array, int arrayIndex)
        {
            array[arrayIndex++] = new CurveRecord((int)Source.preWrapMode,
                (int)Source.postWrapMode);
            for (int i = 0; i < Source.length; i++)
                array[arrayIndex + i] = new CurveRecord(Source[i]);
        }

        public IEnumerator<CurveRecord> GetEnumerator()
        {
            if (Source == null) yield break;
            yield return new CurveRecord((int)Source.preWrapMode,
                (int)Source.postWrapMode);
            for (int i = 0; i < Source.length; i++)
                yield return new CurveRecord(Source[i]);
        }

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
        public void Add(CurveRecord item) => throw new NotSupportedException();
        public void Clear() => throw new NotSupportedException();
        public bool Contains(CurveRecord item) => false;
        public bool Remove(CurveRecord item) => throw new NotSupportedException();
    }

    internal sealed class AnimationCurveBuffConverter : BuffConverter<AnimationCurve>
    {
        private readonly CurveRecordCollection records = new CurveRecordCollection();
        private readonly List<CurveRecord> readRecords = new List<CurveRecord>();
        private readonly PackedUnityBuffConverter<CurveRecord> recordConverter;

        internal AnimationCurveBuffConverter()
        {
            recordConverter = UnityValueBuffConverters.Create<CurveRecord>(3,
                (v, b) =>
                {
                    b.Set(0, UnityValueBlocks.Ints(v.IsHeader ? 1 : 0,
                        v.PreWrapMode, v.PostWrapMode));
                    Keyframe key = v.Key;
                    b.Set(1, UnityValueBlocks.Floats(key.time, key.value,
                        key.inTangent, key.outTangent));
                    b.Set(2, UnityValueBlocks.FloatInts(key.inWeight,
                        key.outWeight, (int)key.weightedMode));
                }, v =>
                {
                    var header = UnityValueBlocks.Get(v, 0);
                    if (header.I0 != 0)
                        return new CurveRecord(header.I1, header.I2);
                    var a = UnityValueBlocks.Get(v, 1);
                    var b = UnityValueBlocks.Get(v, 2);
                    return new CurveRecord(new Keyframe(a.F0, a.F1, a.F2,
                        a.F3, b.F0, b.F1)
                        { weightedMode = (WeightedMode)b.I2 });
                });
        }

        protected override void OnScan(BufferScan scan, AnimationCurve value)
        {
            if (value == null)
            {
                scan.ScanEnumerable<CurveRecord>(null, recordConverter,
                    trackReference: false);
                return;
            }
            records.Source = value;
            try
            {
                scan.ScanEnumerable(records, recordConverter,
                    trackReference: false);
            }
            finally
            {
                records.Source = null;
            }
        }

        protected override void OnWrite(IBufferWriter writer, BufferScan scan,
            AnimationCurve value) => writer.WriteIEnumerable(scan, recordConverter);

        protected override AnimationCurve OnRead(IBufferReader reader, Type type)
        {
            readRecords.Clear();
            List<CurveRecord> values = reader.ReadIEnumerable(readRecords,
                recordConverter);
            if (values == null) return null;
            if (values.Count == 0 || !values[0].IsHeader)
                throw new FormatException("AnimationCurve header is missing.");
            var result = new AnimationCurve
            {
                preWrapMode = (WrapMode)values[0].PreWrapMode,
                postWrapMode = (WrapMode)values[0].PostWrapMode
            };
            for (int i = 1; i < values.Count; i++)
            {
                if (values[i].IsHeader)
                    throw new FormatException("AnimationCurve contains an invalid header.");
                result.AddKey(values[i].Key);
            }
            readRecords.Clear();
            return result;
        }
    }

    internal readonly struct GradientRecord
    {
        internal readonly byte Kind;
        internal readonly Color Color;
        internal readonly float Alpha;
        internal readonly float Time;
        internal readonly int Mode;

        internal GradientRecord(int mode)
        { Kind = 0; Color = default; Alpha = 0; Time = 0; Mode = mode; }
        internal GradientRecord(GradientColorKey key)
        { Kind = 1; Color = key.color; Alpha = 0; Time = key.time; Mode = 0; }
        internal GradientRecord(GradientAlphaKey key)
        { Kind = 2; Color = default; Alpha = key.alpha; Time = key.time; Mode = 0; }
    }

    internal sealed class GradientRecordCollection : ICollection<GradientRecord>
    {
        private GradientColorKey[] colorKeys;
        private GradientAlphaKey[] alphaKeys;
        private int mode;

        public int Count => colorKeys == null ? 0 : 1 + colorKeys.Length + alphaKeys.Length;
        public bool IsReadOnly => true;

        internal void Prepare(Gradient source)
        {
            colorKeys = source.colorKeys;
            alphaKeys = source.alphaKeys;
            mode = (int)source.mode;
        }

        internal void Release()
        {
            colorKeys = null;
            alphaKeys = null;
        }

        public void CopyTo(GradientRecord[] array, int arrayIndex)
        {
            array[arrayIndex++] = new GradientRecord(mode);
            for (int i = 0; i < colorKeys.Length; i++)
                array[arrayIndex++] = new GradientRecord(colorKeys[i]);
            for (int i = 0; i < alphaKeys.Length; i++)
                array[arrayIndex++] = new GradientRecord(alphaKeys[i]);
        }

        public IEnumerator<GradientRecord> GetEnumerator()
        {
            if (colorKeys == null) yield break;
            yield return new GradientRecord(mode);
            for (int i = 0; i < colorKeys.Length; i++)
                yield return new GradientRecord(colorKeys[i]);
            for (int i = 0; i < alphaKeys.Length; i++)
                yield return new GradientRecord(alphaKeys[i]);
        }

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
        public void Add(GradientRecord item) => throw new NotSupportedException();
        public void Clear() => throw new NotSupportedException();
        public bool Contains(GradientRecord item) => false;
        public bool Remove(GradientRecord item) => throw new NotSupportedException();
    }

    internal sealed class GradientBuffConverter : BuffConverter<Gradient>
    {
        private readonly GradientRecordCollection records = new GradientRecordCollection();
        private readonly List<GradientRecord> readRecords = new List<GradientRecord>();
        private readonly PackedUnityBuffConverter<GradientRecord> recordConverter;

        internal GradientBuffConverter()
        {
            recordConverter = UnityValueBuffConverters.Create<GradientRecord>(2,
                (v, b) =>
                {
                    b.Set(0, UnityValueBlocks.Ints(v.Kind, v.Mode));
                    b.Set(1, v.Kind == 1
                        ? UnityValueBlocks.Floats(v.Color.r, v.Color.g,
                            v.Color.b, v.Color.a)
                        : UnityValueBlocks.Floats(v.Alpha, v.Time));
                    if (v.Kind == 1)
                        b.Set(0, UnityValueBlocks.FloatInts(v.Time, 0,
                            v.Kind, v.Mode));
                }, v =>
                {
                    var a = UnityValueBlocks.Get(v, 0);
                    var b = UnityValueBlocks.Get(v, 1);
                    byte kind = (byte)a.I2;
                    if (kind == 0) kind = (byte)a.I0;
                    if (kind == 0) return new GradientRecord(a.I1);
                    if (kind == 1)
                        return new GradientRecord(new GradientColorKey(
                            new Color(b.F0, b.F1, b.F2, b.F3), a.F0));
                    if (kind == 2)
                        return new GradientRecord(new GradientAlphaKey(b.F0, b.F1));
                    throw new FormatException($"Gradient record kind '{kind}' is invalid.");
                });
        }

        protected override void OnScan(BufferScan scan, Gradient value)
        {
            if (value == null)
            {
                scan.ScanEnumerable<GradientRecord>(null, recordConverter,
                    trackReference: false);
                return;
            }
            records.Prepare(value);
            try
            {
                scan.ScanEnumerable(records, recordConverter,
                    trackReference: false);
            }
            finally
            {
                records.Release();
            }
        }

        protected override void OnWrite(IBufferWriter writer, BufferScan scan,
            Gradient value) => writer.WriteIEnumerable(scan, recordConverter);

        protected override Gradient OnRead(IBufferReader reader, Type type)
        {
            readRecords.Clear();
            List<GradientRecord> values = reader.ReadIEnumerable(readRecords,
                recordConverter);
            if (values == null) return null;
            if (values.Count == 0 || values[0].Kind != 0)
                throw new FormatException("Gradient header is missing.");
            int colorCount = 0;
            int alphaCount = 0;
            for (int i = 1; i < values.Count; i++)
            {
                if (values[i].Kind == 1) colorCount++;
                else if (values[i].Kind == 2) alphaCount++;
                else throw new FormatException("Gradient contains an invalid record.");
            }
            var colors = new GradientColorKey[colorCount];
            var alphas = new GradientAlphaKey[alphaCount];
            colorCount = 0;
            alphaCount = 0;
            for (int i = 1; i < values.Count; i++)
            {
                GradientRecord record = values[i];
                if (record.Kind == 1)
                    colors[colorCount++] = new GradientColorKey(record.Color, record.Time);
                else
                    alphas[alphaCount++] = new GradientAlphaKey(record.Alpha, record.Time);
            }
            var result = new Gradient { mode = (GradientMode)values[0].Mode };
            result.SetKeys(colors, alphas);
            readRecords.Clear();
            return result;
        }
    }
}
