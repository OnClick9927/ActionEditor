using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using NUnit.Framework;

namespace ActionBuffer.Tests
{
    [TestFixture]
    public sealed class ActionBufferSerializationTests
    {
        private static readonly string[] Formats = { "Json", "Yaml", "Xml", "Binary" };

        private sealed class LateRegisteredConverter : AtomicBuffConverter<LateRegisteredValue>
        {
            internal static int WriteCount;

            protected override LateRegisteredValue OnRead(IBufferReader reader, Type type) =>
                new LateRegisteredValue { Value = reader.ReadInt32() - 1000 };

            protected override void OnWrite(IBufferWriter writer, BufferScan scan,
                LateRegisteredValue value)
            {
                WriteCount++;
                writer.WriteInt32(value.Value + 1000);
            }
        }

        [TestCaseSource(nameof(Formats))]
        public void PrimitiveTypesRoundTrip(string format)
        {
            var result = RoundTrip(new PrimitiveModel(), format);

            Assert.That(result.Bool, Is.True);
            Assert.That(result.Byte, Is.EqualTo(250));
            Assert.That(result.SByte, Is.EqualTo(-100));
            Assert.That(result.Char, Is.EqualTo('\u4e2d'));
            Assert.That(result.Short, Is.EqualTo(-32000));
            Assert.That(result.UShort, Is.EqualTo(65000));
            Assert.That(result.Int, Is.EqualTo(-123456789));
            Assert.That(result.UInt, Is.EqualTo(4000000000));
            Assert.That(result.Long, Is.EqualTo(-9000000000000));
            Assert.That(result.ULong, Is.EqualTo(18000000000000));
            Assert.That(float.IsNaN(result.Float), Is.True);
            Assert.That(double.IsPositiveInfinity(result.Double), Is.True);
            Assert.That(result.Decimal, Is.EqualTo(7922816251426433759354395033.5m));
            Assert.That(result.String, Is.EqualTo("ActionBuffer \"text\"\nUnicode"));
            Assert.That(result.DateTime, Is.EqualTo(new DateTime(638600000000000000, DateTimeKind.Utc)));
            Assert.That(result.TimeSpan, Is.EqualTo(TimeSpan.FromTicks(-9876543210)));
            Assert.That(result.Guid, Is.EqualTo(new Guid("763ddb22-325e-4d16-805e-f669dc1349ad")));
            Assert.That(result.Enum, Is.EqualTo(SampleEnum.Negative));
            Assert.That(result.Struct.Number, Is.EqualTo(17));
            Assert.That(result.Struct.Text, Is.EqualTo("struct"));
        }

        [TestCaseSource(nameof(Formats))]
        public void CollectionTypesRoundTrip(string format)
        {
            var result = RoundTrip(new CollectionModel(), format);

            CollectionAssert.AreEqual(new[] { 1, 2, 3 }, result.Array);
            CollectionAssert.AreEqual(new[] { "a", "b" }, result.List);
            CollectionAssert.AreEqual(new[] { 4, 5 }, result.Enumerable);
            CollectionAssert.AreEqual(new[] { 6, 7 }, result.Collection);
            CollectionAssert.AreEqual(new[] { 8, 9 }, result.ListInterface);
            CollectionAssert.AreEqual(new[] { 10, 11 }, result.ReadOnlyCollection);
            CollectionAssert.AreEqual(new[] { 12, 13 }, result.ReadOnlyList);
            Assert.That(result.Dictionary, Is.EquivalentTo(new Dictionary<string, int>
            {
                { "one", 1 }, { "two", 2 }
            }));
            Assert.That(result.ConcurrentDictionary["three"], Is.EqualTo(3));
            Assert.That(result.ConcurrentDictionary["four"], Is.EqualTo(4));
            Assert.That(result.DictionaryInterface["five"], Is.EqualTo(5));
            Assert.That(result.ReadOnlyDictionary["six"], Is.EqualTo(6));
            CollectionAssert.AreEquivalent(new[] { 9, 3, 7 }, result.HashSet);
            CollectionAssert.AreEquivalent(new[] { 8, 2, 6 }, result.SetInterface);
            CollectionAssert.AreEqual(new[] { 1, 2, 3 }, result.Queue.ToArray());
            CollectionAssert.AreEqual(new[] { 3, 2, 1 }, result.Stack.ToArray());
            CollectionAssert.AreEqual(new[] { 4, 5 }, result.Segment.ToArray());
            Assert.That(result.Pair.Key, Is.EqualTo("pair"));
            Assert.That(result.Pair.Value, Is.EqualTo(42));
        }

        [TestCaseSource(nameof(Formats))]
        public void NullableTupleRecordAndParameterizedClassesRoundTrip(string format)
        {
            var nullable = RoundTrip(new NullableAndTupleModel(), format);
            Assert.That(nullable.HasValue, Is.EqualTo(13));
            Assert.That(nullable.NoValue, Is.Null);
            Assert.That(nullable.NullableStruct.Value.Number, Is.EqualTo(5));
            Assert.That(nullable.Tuple.Item1, Is.EqualTo(2));
            Assert.That(nullable.Tuple.Item2, Is.EqualTo("tuple"));
            Assert.That(nullable.ValueTuple.Number, Is.EqualTo(3));
            Assert.That(nullable.ValueTuple.Text, Is.EqualTo("value tuple"));

            var parameterized = RoundTrip(new ParameterizedOnlyModel(7, "parameterized"), format);
            Assert.That(parameterized.Number, Is.EqualTo(7));
            Assert.That(parameterized.Text, Is.EqualTo("parameterized"));

            var record = RoundTrip(new RecordModel(9, "record"), format);
            Assert.That(record, Is.EqualTo(new RecordModel(9, "record")));
        }

        [TestCaseSource(nameof(Formats))]
        public void AbstractBaseAndInterfaceFieldsDoNotRequireRegistration(string format)
        {
            var result = RoundTrip(new PolymorphicModel(), format);

            Assert.That(result.AbstractValue, Is.TypeOf<Dog>());
            Assert.That(result.AbstractValue.Name, Is.EqualTo("dog"));
            Assert.That(((Dog)result.AbstractValue).Age, Is.EqualTo(6));
            Assert.That(result.InterfaceValue, Is.TypeOf<Rectangle>());
            Assert.That(result.InterfaceValue.Area, Is.EqualTo(20));
            Assert.That(result.BaseValue, Is.TypeOf<DerivedValue>());
            Assert.That(result.BaseValue.BaseNumber, Is.EqualTo(12));
            Assert.That(((DerivedValue)result.BaseValue).DerivedText, Is.EqualTo("derived"));
        }

        [TestCaseSource(nameof(Formats))]
        public void AnonymousAndTemporaryTypesRoundTrip(string format)
        {
            var source = new { Id = 11, Name = "anonymous" };
            object result = RoundTripObject(source, source.GetType(), format);

            Assert.That(result.GetType(), Is.EqualTo(source.GetType()));
            Assert.That(result.GetType().GetProperty("Id")?.GetValue(result), Is.EqualTo(11));
            Assert.That(result.GetType().GetProperty("Name")?.GetValue(result), Is.EqualTo("anonymous"));
        }

        [TestCaseSource(nameof(Formats))]
        public void DelegateAndEventFieldsAreSkippedWithoutClearingConstructorSubscriptions(string format)
        {
            var result = RoundTrip(new DelegateModel(), format);
            result.Raise();
            Assert.That(result.Invocations, Is.EqualTo(2));
        }

        [TestCaseSource(nameof(Formats))]
        public void CallbacksRunAfterNestedObjectsAreComplete(string format)
        {
            var source = new CallbackParent { Child = new CallbackChild { Number = 42 } };
            var result = RoundTrip(source, format);
            Assert.That(result.Child.AfterReadCalled, Is.True);
            Assert.That(result.ChildWasComplete, Is.True);
        }

        [TestCaseSource(nameof(Formats))]
        public void SharedObjectsAreWrittenByValueAndCircularObjectsAreRejected(string format)
        {
            var leaf = new SharedLeaf { Value = 5 };
            var shared = RoundTrip(new SharedReferenceModel { First = leaf, Second = leaf }, format);
            Assert.That(shared.First.Value, Is.EqualTo(5));
            Assert.That(shared.Second.Value, Is.EqualTo(5));
            Assert.That(ReferenceEquals(shared.First, shared.Second), Is.False);

            var circular = new CircularModel();
            circular.Self = circular;
            Assert.Throws<InvalidOperationException>(() => Write(circular, format));
        }

        [Test]
        public void MissingFieldsResetConstructorValuesToTypeDefaults()
        {
            var source = new DefaultValueModel { Zero = 0, Null = null };
            string json = BufferSerializer.ToJson(source);
            Assert.That(json, Does.Not.Contain("\"Zero\""));
            Assert.That(json, Does.Not.Contain("\"Null\""));

            var result = BufferSerializer.ToObject<DefaultValueModel>(json);
            Assert.That(result.Zero, Is.Zero);
            Assert.That(result.Null, Is.Null);
        }

        [Test]
        public void BinarySupportsStringsLargerThanUShort()
        {
            string text = new string('x', 70000);
            var result = BufferSerializer.ToObject<LongStringModel>(
                BufferSerializer.ToBytes(new LongStringModel { Value = text }));
            Assert.That(result.Value, Is.EqualTo(text));
        }

        [Test]
        public void BinaryNodeLimitMatchesWriterNodeCounting()
        {
            int previous = BufferSerializer.MaxNodeCount;
            try
            {
                BufferSerializer.MaxNodeCount = 7;
                var source = new NodeLimitModel();
                for (int i = 0; i < 5; i++) source.Nodes.Add(new EmptyNode());
                var result = BufferSerializer.ToObject<NodeLimitModel>(BufferSerializer.ToBytes(source));
                Assert.That(result.Nodes.Count, Is.EqualTo(5));
            }
            finally
            {
                BufferSerializer.MaxNodeCount = previous;
            }
        }

        [Test]
        public void RegisterConverterUpdatesAlreadyCreatedCollectionConverters()
        {
            var source = new List<LateRegisteredValue>
            {
                new LateRegisteredValue { Value = 27 }
            };
            RoundTrip(source, "Json");

            LateRegisteredConverter.WriteCount = 0;
            BufferSerializer.RegisterConverter(new LateRegisteredConverter());
            foreach (string format in Formats)
            {
                var result = RoundTrip(source, format);
                Assert.That(result[0].Value, Is.EqualTo(27));
            }
            Assert.That(LateRegisteredConverter.WriteCount, Is.EqualTo(Formats.Length));
        }

        [Test]
        public void CustomCollectionComparersAreRejectedInsteadOfSilentlyChangingBehavior()
        {
            var dictionary = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase)
            {
                { "Key", 1 }
            };
            var set = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "Value" };

            Assert.Throws<NotSupportedException>(() => BufferSerializer.ToJson(dictionary));
            Assert.Throws<NotSupportedException>(() => BufferSerializer.ToBytes(set));
        }

        [Test]
        public void NullableRootsAndSpanRootsRoundTripInEveryFormat()
        {
            Assert.That(BufferSerializer.ToObject<int?>(BufferSerializer.ToJson<int>((int?)null)), Is.Null);
            Assert.That(BufferSerializer.FromYaml<int?>(BufferSerializer.ToYaml<int>((int?)null)), Is.Null);
            Assert.That(BufferSerializer.FromXml<int?>(BufferSerializer.ToXml<int>((int?)null)), Is.Null);
            Assert.That(BufferSerializer.ToObject<int?>(BufferSerializer.ToBytes<int>((int?)null)), Is.Null);

            int[] values = { 2, 4, 6, 8 };
            CollectionAssert.AreEqual(values,
                BufferSerializer.ToSpan<int>(BufferSerializer.ToJson((ReadOnlySpan<int>)values)).ToArray());
            CollectionAssert.AreEqual(values,
                BufferSerializer.FromYamlSpan<int>(BufferSerializer.ToYaml((ReadOnlySpan<int>)values)).ToArray());
            CollectionAssert.AreEqual(values,
                BufferSerializer.FromXmlSpan<int>(BufferSerializer.ToXml((ReadOnlySpan<int>)values)).ToArray());
            CollectionAssert.AreEqual(values,
                BufferSerializer.ToSpan<int>(BufferSerializer.ToBytes((ReadOnlySpan<int>)values)).ToArray());
        }

        [TestCaseSource(nameof(Formats))]
        public void ReportsAllocatedBytesAndElapsedTime(string format)
        {
            var source = CreatePerformanceModel(250);
            RoundTrip(source, format);
            RoundTrip(source, format);

            GC.Collect();
            GC.WaitForPendingFinalizers();
            long before = AllocationCounter.Read();
            var stopwatch = Stopwatch.StartNew();
            const int iterations = 8;
            for (int i = 0; i < iterations; i++)
                RoundTrip(source, format);
            stopwatch.Stop();
            long allocated = Math.Max(0, AllocationCounter.Read() - before);

            TestContext.Progress.WriteLine(
                $"ActionBuffer {format}: {stopwatch.Elapsed.TotalMilliseconds:F2} ms, " +
                $"{allocated} allocated bytes ({AllocationCounter.Name}), {iterations} iterations");
            Assert.That(stopwatch.Elapsed, Is.LessThan(TimeSpan.FromSeconds(30)));
            Assert.That(allocated, Is.LessThan(512L * 1024 * 1024));
        }

        [Test]
        public void GetSubTypesIncludesOpenGenericTemplates()
        {
            CollectionAssert.Contains(TypeHelper.GetSubTypes(typeof(GenericDiscoveryBase)),
                typeof(GenericDiscoveryTemplate<>));
        }

        private static T RoundTrip<T>(T source, string format) =>
            (T)RoundTripObject(source, typeof(T), format);

        private static object RoundTripObject(object source, Type type, string format)
        {
            switch (format)
            {
                case "Json": return BufferSerializer.ToObject(BufferSerializer.ToJson(source), type);
                case "Yaml": return BufferSerializer.FromYaml(BufferSerializer.ToYaml(source), type);
                case "Xml": return BufferSerializer.FromXml(BufferSerializer.ToXml(source), type);
                case "Binary": return BufferSerializer.ToObject(BufferSerializer.ToBytes(source), type);
                default: throw new ArgumentOutOfRangeException(nameof(format));
            }
        }

        private static object Write(object source, string format)
        {
            switch (format)
            {
                case "Json": return BufferSerializer.ToJson(source);
                case "Yaml": return BufferSerializer.ToYaml(source);
                case "Xml": return BufferSerializer.ToXml(source);
                case "Binary": return BufferSerializer.ToBytes(source);
                default: throw new ArgumentOutOfRangeException(nameof(format));
            }
        }

        private static PerformanceModel CreatePerformanceModel(int count)
        {
            var result = new PerformanceModel();
            for (int i = 0; i < count; i++)
            {
                result.Items.Add(new SmallStruct { Number = i, Text = "item-" + i });
                result.Lookup.Add(i, "value-" + i);
            }
            return result;
        }

        private static class AllocationCounter
        {
            private static readonly Func<long> Counter;
            internal static readonly string Name;

            static AllocationCounter()
            {
                Counter = CreateCounter(out string name);
                Name = name;
            }
            internal static long Read() => Counter();

            private static Func<long> CreateCounter(out string name)
            {
                MethodInfo method = typeof(GC).GetMethod("GetAllocatedBytesForCurrentThread",
                    BindingFlags.Public | BindingFlags.Static);
                if (method != null)
                {
                    name = method.Name;
                    return (Func<long>)Delegate.CreateDelegate(typeof(Func<long>), method);
                }
                name = nameof(GC.GetTotalMemory);
                return () => GC.GetTotalMemory(false);
            }
        }
    }
}
