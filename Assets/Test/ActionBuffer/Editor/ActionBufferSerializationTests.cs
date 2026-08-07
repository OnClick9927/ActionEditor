using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Threading.Tasks;
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
        public void OnlyBufferedPropertiesRoundTrip(string format)
        {
            var source = new BufferedPropertyModel
            {
                PublicValue = 17,
                UnmarkedValue = 99
            };
            source.SetPrivateValue("private");
            source.SetManualValue(23);

            BufferedPropertyModel result = RoundTrip(source, format);

            Assert.That(result.PublicValue, Is.EqualTo(17));
            Assert.That(result.GetPrivateValue(), Is.EqualTo("private"));
            Assert.That(result.GetManualValue(), Is.EqualTo(23));
            Assert.That(result.UnmarkedValue, Is.EqualTo(5));
        }

        [Test]
        public void ReadersAndWritersUseTheirStaticPools()
        {
            var binaryWriter = BufferWriter.Get();
            binaryWriter.WriteByte(1);
            BufferWriter.Back(binaryWriter);

            var reusedBinaryWriter = BufferWriter.Get();
            try
            {
                Assert.That(reusedBinaryWriter, Is.SameAs(binaryWriter));
                Assert.That(reusedBinaryWriter.length, Is.Zero);
            }
            finally
            {
                BufferWriter.Back(reusedBinaryWriter);
            }

            AssertPooled(BufferReader.Get, BufferReader.Back);
            AssertPooled(JsonWriter.Get, JsonWriter.Back);
            AssertPooled(JsonReader.Get, JsonReader.Back);
            AssertPooled(YamlWriter.Get, YamlWriter.Back);
            AssertPooled(YamlReader.Get, YamlReader.Back);
            AssertPooled(XmlWriter.Get, XmlWriter.Back);
            AssertPooled(XmlReader.Get, XmlReader.Back);
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
            CollectionAssert.AreEqual(new object[] { 1, "two", 3L }, result.ArrayList);
            Assert.That(result.Hashtable["number"], Is.EqualTo(4));
            Assert.That(result.Hashtable[5], Is.EqualTo("value"));
            CollectionAssert.AreEqual(new[] { 14, 15 }, result.LinkedList);
            Assert.That(result.SortedDictionary["a"], Is.EqualTo(1));
            CollectionAssert.AreEqual(new[] { 16, 17, 18 }, result.SortedSet);
            CollectionAssert.AreEqual(new[] { 19, 20 }, result.ObservableCollection);
            Assert.That(result.CustomList, Is.TypeOf<CustomIntList>());
            CollectionAssert.AreEqual(new[] { 21, 22 }, result.CustomList);
            Assert.That(result.CustomDictionary, Is.TypeOf<CustomStringDictionary>());
            Assert.That(result.CustomDictionary["custom"], Is.EqualTo(23));
            Assert.That(result.CustomArrayList, Is.TypeOf<CustomArrayList>());
            CollectionAssert.AreEqual(new object[] { "custom", 24 }, result.CustomArrayList);
        }

        [TestCaseSource(nameof(Formats))]
        public void CustomCollectionSubclassesPreserveCycles(string format)
        {
            var source = new CustomArrayList();
            source.Add(source);
            var result = RoundTrip(source, format,
                new BuffSettings { SupportReferences = true });

            Assert.That(result, Is.TypeOf<CustomArrayList>());
            Assert.That(ReferenceEquals(result, result[0]), Is.True);
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

            var readonlyAndInit = RoundTrip(
                new ReadonlyAndInitModel(10, "init"), format);
            Assert.That(readonlyAndInit.ReadonlyNumber, Is.EqualTo(10));
            Assert.That(readonlyAndInit.InitText, Is.EqualTo("init"));
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
        public void PolymorphicAtomicStringCollectionAndCustomConvertersRoundTrip(
            string format)
        {
            var settings = new BuffSettings();
            settings.RegisterConverter(new CustomAtomicValueConverter());
            var result = RoundTrip(new PolymorphicValueModel(), format, settings);

            Assert.That(result.Atomic, Is.EqualTo(31));
            Assert.That(result.Text, Is.EqualTo("polymorphic"));
            Assert.That(result.Collection, Is.TypeOf<List<int>>());
            CollectionAssert.AreEqual(new[] { 32, 33 }, (IEnumerable)result.Collection);
            Assert.That(result.AtomicInterface, Is.EqualTo(34));
            Assert.That(result.TextInterface, Is.EqualTo("interface text"));
            Assert.That(result.CollectionInterface,
                Is.TypeOf<System.Collections.ObjectModel.ObservableCollection<int>>());
            CollectionAssert.AreEqual(new[] { 35, 36 }, result.CollectionInterface);
            Assert.That(((CustomAtomicValue)result.CustomAtomic).Number, Is.EqualTo(37));
            Assert.That(result.CustomAtomicBase.Number, Is.EqualTo(38));
            Assert.That(result.CustomAtomicInterface.Number, Is.EqualTo(39));
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

        private sealed class SettingsScopedConverter : AtomicBuffConverter<SettingsScopedValue>
        {
            protected override SettingsScopedValue OnRead(IBufferReader reader, Type type) =>
                new SettingsScopedValue { Value = reader.ReadInt32() - 2000 };

            protected override void OnWrite(IBufferWriter writer, BufferScan scan,
                SettingsScopedValue value) => writer.WriteInt32(value.Value + 2000);
        }

        [TestCaseSource(nameof(Formats))]
        public void TwoDimensionalArraysRoundTrip(string format)
        {
            var result = RoundTrip(new Array2DModel(), format);

            Assert.That(result.Numbers.Rank, Is.EqualTo(2));
            Assert.That(result.Numbers.GetLength(0), Is.EqualTo(2));
            Assert.That(result.Numbers.GetLength(1), Is.EqualTo(3));
            Assert.That(result.Numbers[0, 0], Is.EqualTo(1));
            Assert.That(result.Numbers[1, 2], Is.EqualTo(6));
            Assert.That(result.Text[0, 0], Is.EqualTo("a"));
            Assert.That(result.Text[0, 1], Is.Null);
            Assert.That(result.Text[1, 1], Is.EqualTo("d"));
            Assert.That(result.Empty.GetLength(0), Is.Zero);
            Assert.That(result.Empty.GetLength(1), Is.Zero);
            Assert.That(result.EmptyRows.GetLength(0), Is.Zero);
            Assert.That(result.EmptyRows.GetLength(1), Is.EqualTo(3));
            Assert.That(result.EmptyColumns.GetLength(0), Is.EqualTo(2));
            Assert.That(result.EmptyColumns.GetLength(1), Is.Zero);
            Assert.That(result.Null, Is.Null);
        }

        [TestCaseSource(nameof(Formats))]
        public void ExplicitDelegateFieldsRoundTrip(string format)
        {
            var source = new SerializableDelegateModel();
            source.Configure();
            SerializableDelegateModel.StaticValue = 0;

            var result = RoundTrip(source, format);
            result.Callback(3);

            Assert.That(result.Value, Is.EqualTo(3));
            Assert.That(SerializableDelegateModel.StaticValue, Is.EqualTo(3));
        }

        [TestCaseSource(nameof(Formats))]
        public void StaticRootDelegatesRoundTrip(string format)
        {
            SerializableDelegateModel.StaticValue = 0;
            var result = RoundTrip(SerializableDelegateModel.CreateStaticCallback(), format);

            result(4);
            Assert.That(SerializableDelegateModel.StaticValue, Is.EqualTo(4));
        }

        [TestCaseSource(nameof(Formats))]
        public void DelegatesBoundToOtherObjectTypesRoundTrip(string format)
        {
            var source = new ExternalDelegateModel();
            source.Configure(10);
            var settings = new BuffSettings { SupportReferences = true };

            var result = RoundTrip(source, format, settings);
            result.Callback(7);

            var target = result.Callback.Target as ExternalDelegateTarget;
            Assert.That(target, Is.Not.Null);
            Assert.That(target.Value, Is.EqualTo(17));
            Assert.That(ReferenceEquals(target, result.Target), Is.True);
        }

        [TestCaseSource(nameof(Formats))]
        public void DelegatesBoundToObjectsOutsideTheRootGraphRoundTrip(string format)
        {
            var source = new DetachedDelegateTargetModel();
            source.Configure();
            var result = RoundTrip(source, format);
            result.Callback(7);

            Assert.That(result.Callback.Target, Is.TypeOf<ExternalDelegateTarget>());
            Assert.That(((ExternalDelegateTarget)result.Callback.Target).Value, Is.EqualTo(7));
        }

        [TestCaseSource(nameof(Formats))]
        public void DelegateClosuresRoundTrip(string format)
        {
            var source = new SerializableDelegateModel();
            source.ConfigureClosure();
            var settings = new BuffSettings { SupportReferences = true };
            var result = RoundTrip(source, format, settings);
            result.Callback(5);

            Assert.That(result.Value, Is.EqualTo(6));
        }

        [TestCaseSource(nameof(Formats))]
        public void GenericMethodsAndValueTypeDelegateTargetsRoundTrip(string format)
        {
            var source = new AdvancedDelegateModel();
            source.Configure();
            AdvancedDelegateModel.GenericValue = 0;

            var result = RoundTrip(source, format);
            result.GenericCallback(41);

            Assert.That(AdvancedDelegateModel.GenericValue, Is.EqualTo(41));
            Assert.That(result.ValueTargetCallback(2), Is.EqualTo(42));
            Assert.That(result.ValueTargetCallback.Target, Is.TypeOf<ValueDelegateTarget>());
            Assert.That(result.ClosedNullCallback(42), Is.EqualTo(43));
        }

        [Test]
        public void DynamicMethodsAreRejectedWithAStableMetadataError()
        {
            var method = new DynamicMethod("DynamicValue", typeof(int), Type.EmptyTypes);
            var il = method.GetILGenerator();
            il.Emit(OpCodes.Ldc_I4_7);
            il.Emit(OpCodes.Ret);
            var callback = (Func<int>)method.CreateDelegate(typeof(Func<int>));

            var exception = Assert.Throws<NotSupportedException>(
                () => BuffSerializer.ToBytes(callback));
            Assert.That(exception.Message, Does.Contain("cannot be reconstructed"));
        }

        [TestCaseSource(nameof(Formats))]
        public void ArraysUpToFiveDimensionsRoundTrip(string format)
        {
            var rank3 = new int[2, 1, 2];
            rank3[1, 0, 1] = 31;
            var result3 = RoundTrip(rank3, format);
            Assert.That(result3[1, 0, 1], Is.EqualTo(31));

            var rank4 = new int[1, 2, 1, 2];
            rank4[0, 1, 0, 1] = 41;
            var result4 = RoundTrip(rank4, format);
            Assert.That(result4[0, 1, 0, 1], Is.EqualTo(41));

            var rank5 = new int[1, 1, 2, 1, 2];
            rank5[0, 0, 1, 0, 1] = 51;
            var result5 = RoundTrip(rank5, format);
            Assert.That(result5[0, 0, 1, 0, 1], Is.EqualTo(51));

            var empty = new int[2, 0, 3, 0, 4];
            var emptyResult = RoundTrip(empty, format);
            Assert.That(emptyResult.GetLength(0), Is.EqualTo(2));
            Assert.That(emptyResult.GetLength(1), Is.Zero);
            Assert.That(emptyResult.GetLength(2), Is.EqualTo(3));
            Assert.That(emptyResult.GetLength(3), Is.Zero);
            Assert.That(emptyResult.GetLength(4), Is.EqualTo(4));
        }

        [TestCaseSource(nameof(Formats))]
        public void ExplicitEventsRoundTrip(string format)
        {
            var source = new SerializableEventModel();
            source.Configure();

            var result = RoundTrip(source, format);
            result.Raise(9);
            Assert.That(result.Value, Is.EqualTo(9));
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

        [TestCaseSource(nameof(Formats))]
        public void ReferenceModePreservesSharedAndMutuallyReferencingObjects(string format)
        {
            var settings = new BuffSettings { SupportReferences = true };
            var leaf = new SharedLeaf { Value = 5 };
            var shared = RoundTrip(new SharedReferenceModel { First = leaf, Second = leaf },
                format, settings);
            Assert.That(ReferenceEquals(shared.First, shared.Second), Is.True);

            var first = new ReferenceNode { Name = "first" };
            var second = new ReferenceNode { Name = "second" };
            first.Next = second;
            second.Next = first;
            var result = RoundTrip(first, format, settings);
            Assert.That(result.Next.Name, Is.EqualTo("second"));
            Assert.That(ReferenceEquals(result.Next.Next, result), Is.True);
        }

        private sealed class CustomAtomicValueConverter :
            AtomicBuffConverter<CustomAtomicValue>
        {
            protected override CustomAtomicValue OnRead(IBufferReader reader, Type type) =>
                new CustomAtomicValue(reader.ReadInt32());

            protected override void OnWrite(IBufferWriter writer, BufferScan scan,
                CustomAtomicValue value) => writer.WriteInt32(value.Number);
        }

        [TestCaseSource(nameof(Formats))]
        public void ReferenceModePreservesCollectionsAndCollectionCycles(string format)
        {
            var settings = new BuffSettings { SupportReferences = true };
            var list = new List<int> { 1, 2, 3 };
            var array = new[] { 4, 5 };
            var shared = RoundTrip(new SharedCollectionModel
            {
                First = list,
                Second = list,
                FirstArray = array,
                SecondArray = array
            }, format, settings);

            Assert.That(ReferenceEquals(shared.First, shared.Second), Is.True);
            Assert.That(ReferenceEquals(shared.FirstArray, shared.SecondArray), Is.True);

            var cycle = new CollectionCycleModel { Name = "root" };
            cycle.Items = new List<CollectionCycleModel> { cycle };
            var cycleResult = RoundTrip(cycle, format, settings);
            Assert.That(ReferenceEquals(cycleResult, cycleResult.Items[0]), Is.True);
        }

        [TestCaseSource(nameof(Formats))]
        public void ReservedMetadataFieldNamesRoundTrip(string format)
        {
            var source = new ReservedFieldNameModel
            {
                LegacyReferenceMarker = 1,
                ReferenceMarker = 2,
                Id = 3,
                Values = "field value"
            };
            var result = RoundTrip(source, format,
                new BuffSettings { SupportReferences = true });

            Assert.That(result.LegacyReferenceMarker, Is.EqualTo(1));
            Assert.That(result.ReferenceMarker, Is.EqualTo(2));
            Assert.That(result.Id, Is.EqualTo(3));
            Assert.That(result.Values, Is.EqualTo("field value"));
        }

        [Test]
        public void ConverterRegistrationIsFrozenDuringWriteAndRead()
        {
            var writeSettings = new BuffSettings();
            writeSettings.RegisterConverter(new OperationMutatingConverter(writeSettings));
            Assert.Throws<InvalidOperationException>(() => BuffSerializer.ToJson(
                new LateRegisteredValue { Value = 7 }, writeSettings));
            Assert.DoesNotThrow(writeSettings.ClearConverters);

            var readSettings = new BuffSettings();
            readSettings.RegisterConverter(new OperationMutatingConverter(readSettings)
            {
                MutateOnRead = true
            });
            Assert.Throws<InvalidOperationException>(() =>
                BuffSerializer.FromJson<LateRegisteredValue>("7", readSettings));
            Assert.DoesNotThrow(readSettings.ClearConverters);
        }

        [TestCaseSource(nameof(Formats))]
        public void DateTimeAndTimeSpanDoNotUseRegisteredLongConverter(string format)
        {
            var settings = new BuffSettings();
            settings.RegisterConverter(new OffsetLongConverter());
            var source = new TemporalModel
            {
                DateTime = new DateTime(638700000000000000, DateTimeKind.Utc),
                TimeSpan = TimeSpan.FromTicks(-123456789)
            };

            var result = RoundTrip(source, format, settings);
            Assert.That(result.DateTime, Is.EqualTo(source.DateTime));
            Assert.That(result.TimeSpan, Is.EqualTo(source.TimeSpan));
        }

        [Test]
        public void BinaryCallbacksRunOnlyAfterThePayloadIsValidated()
        {
            CallbackCounterModel.AfterReadCount = 0;
            byte[] valid = BuffSerializer.ToBytes(new CallbackCounterModel { Value = 9 });
            var invalid = new byte[valid.Length + 1];
            Buffer.BlockCopy(valid, 0, invalid, 0, valid.Length);
            invalid[invalid.Length - 1] = 1;

            Assert.Throws<FormatException>(() =>
                BuffSerializer.FromBytes<CallbackCounterModel>(invalid));
            Assert.That(CallbackCounterModel.AfterReadCount, Is.Zero);
        }

        [TestCaseSource(nameof(Formats))]
        public void RestrictedPolymorphicTypesMustBeRegistered(string format)
        {
            var settings = new BuffSettings { RestrictTypes = true };
            Assert.Throws<FormatException>(() =>
                RoundTrip(new PolymorphicModel(), format, settings));

            settings.RegisterType<Dog>();
            settings.RegisterType<Rectangle>();
            settings.RegisterType<DerivedValue>();
            var result = RoundTrip(new PolymorphicModel(), format, settings);
            Assert.That(result.AbstractValue, Is.TypeOf<Dog>());
            Assert.That(result.InterfaceValue, Is.TypeOf<Rectangle>());
            Assert.That(result.BaseValue, Is.TypeOf<DerivedValue>());
        }

        [Test]
        public void MissingFieldsResetConstructorValuesToTypeDefaults()
        {
            var source = new DefaultValueModel { Zero = 0, Null = null };
            string json = BuffSerializer.ToJson(source);
            Assert.That(json, Does.Not.Contain("\"Zero\""));
            Assert.That(json, Does.Not.Contain("\"Null\""));

            var result = BuffSerializer.FromJson<DefaultValueModel>(json);
            Assert.That(result.Zero, Is.Zero);
            Assert.That(result.Null, Is.Null);
        }

        [Test]
        public void WriteSettingsControlFormattingFieldsLimitsAndConverters()
        {
            int previousLimit = BuffSettings.MaxScalarLength;
            var settings = new BuffSettings
            {
                TypeInfo = false,
                FullField = true,
                PrettyPrint = true
            };
            try
            {
                BuffSettings.MaxScalarLength = 8;
                settings.RegisterConverter(new SettingsScopedConverter());

                string json = BuffSerializer.ToJson(new DefaultValueModel
                {
                    Zero = 0,
                    Null = null
                }, settings);
                Assert.That(json, Does.Not.Contain("$type"));
                Assert.That(json, Does.Contain("\n"));
                Assert.That(json, Does.Contain("\"Zero\""));
                Assert.That(json, Does.Contain("\"Null\""));

                string converted = BuffSerializer.ToJson(
                    new SettingsScopedValue { Value = 7 }, settings);
                Assert.That(converted, Is.EqualTo("2007"));
                Assert.That(BuffSerializer.FromJson<SettingsScopedValue>(converted, settings).Value,
                    Is.EqualTo(7));
                string defaultJson = BuffSerializer.ToJson(
                    new SettingsScopedValue { Value = 7 });
                Assert.That(defaultJson, Is.Not.EqualTo(converted));
                Assert.That(BuffSerializer.FromJson<SettingsScopedValue>(defaultJson).Value,
                    Is.EqualTo(7));
                Assert.Throws<FormatException>(() => BuffSerializer.ToJson(
                    new LongStringModel { Value = "123456789" }, settings));
            }
            finally
            {
                settings.RemoveConverter<SettingsScopedValue>();
                BuffSettings.MaxScalarLength = previousLimit;
            }
        }

        private sealed class OperationMutatingConverter : AtomicBuffConverter<LateRegisteredValue>
        {
            private readonly BuffSettings _settings;
            internal bool MutateOnRead;

            internal OperationMutatingConverter(BuffSettings settings) =>
                _settings = settings;

            protected override void OnScan(BufferScan scan, LateRegisteredValue value)
            {
                if (!MutateOnRead) _settings.ClearConverters();
            }

            protected override LateRegisteredValue OnRead(IBufferReader reader, Type type)
            {
                if (MutateOnRead) _settings.ClearConverters();
                return new LateRegisteredValue { Value = reader.ReadInt32() };
            }

            protected override void OnWrite(IBufferWriter writer, BufferScan scan,
                LateRegisteredValue value) => writer.WriteInt32(value.Value);
        }

        private sealed class OffsetLongConverter : AtomicBuffConverter<long>
        {
            protected override long OnRead(IBufferReader reader, Type type) =>
                reader.ReadInt64() - 1;
            protected override void OnWrite(IBufferWriter writer, BufferScan scan, long value) =>
                writer.WriteInt64(value + 1);
        }

        [TestCaseSource(nameof(Formats))]
        public void EventsCanBeDisabledPerWrite(string format)
        {
            var source = new SerializableEventModel();
            source.Configure();
            var settings = new BuffSettings { SerializeEvents = false };

            var result = RoundTrip(source, format, settings);
            result.Raise(9);
            Assert.That(result.Value, Is.Zero);
        }

        [Test]
        public void BinarySupportsStringsLargerThanUShort()
        {
            string text = new string('x', 70000);
            var result = BuffSerializer.FromBytes<LongStringModel>(
                BuffSerializer.ToBytes(new LongStringModel { Value = text }));
            Assert.That(result.Value, Is.EqualTo(text));
        }

        [Test]
        public void BinaryNodeLimitMatchesWriterNodeCounting()
        {
            int previous = BuffSettings.MaxNodeCount;
            try
            {
                BuffSettings.MaxNodeCount = 7;
                var source = new NodeLimitModel();
                for (int i = 0; i < 5; i++) source.Nodes.Add(new EmptyNode());
                var result = BuffSerializer.FromBytes<NodeLimitModel>(BuffSerializer.ToBytes(source));
                Assert.That(result.Nodes.Count, Is.EqualTo(5));
            }
            finally
            {
                BuffSettings.MaxNodeCount = previous;
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
            var settings = new BuffSettings();
            settings.RegisterConverter(new LateRegisteredConverter());
            try
            {
                foreach (string format in Formats)
                {
                    var result = RoundTrip(source, format, settings);
                    Assert.That(result[0].Value, Is.EqualTo(27));
                }
                Assert.That(LateRegisteredConverter.WriteCount, Is.EqualTo(Formats.Length));
            }
            finally
            {
                settings.RemoveConverter<LateRegisteredValue>();
            }
        }

        [Test]
        public void ConcurrentSettingsScopedConvertersDoNotLeakBetweenOperations()
        {
            var settings = new BuffSettings();
            settings.RegisterConverter(new SettingsScopedConverter());
            var source = new SettingsScopedValue { Value = 31 };

            Assert.DoesNotThrow(() => Parallel.For(0, 512, index =>
            {
                if ((index & 1) == 0)
                {
                    string json = BuffSerializer.ToJson(source, settings);
                    if (json != "2031" ||
                        BuffSerializer.FromJson<SettingsScopedValue>(json, settings).Value != 31)
                        throw new InvalidOperationException("Settings converter round-trip mismatch.");
                    return;
                }

                string defaultJson = BuffSerializer.ToJson(source);
                if (defaultJson == "2031" ||
                    BuffSerializer.FromJson<SettingsScopedValue>(defaultJson).Value != 31)
                    throw new InvalidOperationException("Settings converter leaked to another write.");
            }));
        }

        [Test]
        public void OneThousandMixedFormatRoundTripsRemainStable()
        {
            var settings = new BuffSettings
            {
                SupportReferences = true,
                DeterministicCollectionOrder = true
            };
            var firstNode = new ReferenceNode { Name = "first" };
            var secondNode = new ReferenceNode { Name = "second" };
            firstNode.Next = secondNode;
            secondNode.Next = firstNode;
            var shared = new SharedLeaf();
            var source = new StressSerializationModel
            {
                Root = firstNode,
                First = shared,
                Second = shared,
                Cube = new int[2, 1, 2],
                RankFive = new int[1, 1, 2, 1, 2]
            };

            for (int iteration = 0; iteration < 1000; iteration++)
            {
                string format = Formats[iteration % Formats.Length];
                string label = "iteration-" + iteration;
                source.SetValues(iteration, label);
                shared.Value = iteration * 3;
                source.Cube[1, 0, 1] = iteration + 10;
                source.RankFive[0, 0, 1, 0, 1] = iteration + 20;

                StressSerializationModel result = RoundTrip(source, format, settings);
                if (result.Iteration != iteration || result.GetLabel() != label ||
                    result.First.Value != iteration * 3 ||
                    !ReferenceEquals(result.First, result.Second) ||
                    result.Root.Next.Name != "second" ||
                    !ReferenceEquals(result.Root.Next.Next, result.Root) ||
                    result.Cube[1, 0, 1] != iteration + 10 ||
                    result.RankFive[0, 0, 1, 0, 1] != iteration + 20)
                {
                    Assert.Fail($"Round-trip mismatch at iteration {iteration} ({format}).");
                }
            }
        }

        [TestCaseSource(nameof(Formats))]
        public void RepeatedPooledWritesProduceIdenticalOutput(string format)
        {
            var source = new PrimitiveModel();
            object expected = Write(source, format);
            for (int iteration = 0; iteration < 100; iteration++)
            {
                object actual = Write(source, format);
                if (expected is byte[] expectedBytes)
                    CollectionAssert.AreEqual(expectedBytes, (byte[])actual,
                        $"Binary output changed at iteration {iteration}.");
                else
                    Assert.That(actual, Is.EqualTo(expected),
                        $"{format} output changed at iteration {iteration}.");
            }
        }

        [Test]
        public void CustomCollectionComparersAreRejectedInsteadOfSilentlyChangingBehavior()
        {
            var dictionary = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase)
            {
                { "Key", 1 }
            };
            var set = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "Value" };
            var sortedDictionary = new SortedDictionary<string, int>(
                StringComparer.OrdinalIgnoreCase) { { "Key", 1 } };
            var sortedSet = new SortedSet<string>(StringComparer.OrdinalIgnoreCase) { "Value" };
            var hashtable = new Hashtable(StringComparer.OrdinalIgnoreCase)
            {
                { "Key", 1 }
            };

            Assert.Throws<NotSupportedException>(() => BuffSerializer.ToJson(dictionary));
            Assert.Throws<NotSupportedException>(() => BuffSerializer.ToBytes(set));
            Assert.Throws<NotSupportedException>(() => BuffSerializer.ToJson(sortedDictionary));
            Assert.Throws<NotSupportedException>(() => BuffSerializer.ToBytes(sortedSet));
            Assert.Throws<NotSupportedException>(() => BuffSerializer.ToJson(hashtable));

            var settings = new BuffSettings { DeterministicCollectionOrder = true };
            Assert.Throws<NotSupportedException>(() => BuffSerializer.ToBytes(
                new HashSet<EmptyNode> { new EmptyNode() }, settings));
        }

        [Test]
        public void NullRootWritesRequireARuntimeType()
        {
            Assert.Throws<ArgumentNullException>(() => BuffSerializer.ToJson(null));
            Assert.Throws<ArgumentNullException>(() => BuffSerializer.ToYaml(null));
            Assert.Throws<ArgumentNullException>(() => BuffSerializer.ToXml(null));
            Assert.Throws<ArgumentNullException>(() => BuffSerializer.ToBytes(null));
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

        private static T RoundTrip<T>(T source, string format,
            BuffSettings settings = null) =>
            (T)RoundTripObject(source, typeof(T), format, settings);

        private static void AssertPooled<T>(Func<T> get, Action<T> back)
            where T : class
        {
            T first = get();
            back(first);
            T second = get();
            try
            {
                Assert.That(second, Is.SameAs(first));
            }
            finally
            {
                back(second);
            }
        }

        private static object RoundTripObject(object source, Type type, string format,
            BuffSettings settings = null)
        {
            switch (format)
            {
                case "Json": return BuffSerializer.FromJson(
                    BuffSerializer.ToJson(source, settings), type, settings);
                case "Yaml": return BuffSerializer.FromYaml(
                    BuffSerializer.ToYaml(source, settings), type, settings);
                case "Xml": return BuffSerializer.FromXml(
                    BuffSerializer.ToXml(source, settings), type, settings);
                case "Binary": return BuffSerializer.FromBytes(
                    BuffSerializer.ToBytes(source, settings), type, settings);
                default: throw new ArgumentOutOfRangeException(nameof(format));
            }
        }

        private static object Write(object source, string format,
            BuffSettings settings = null)
        {
            switch (format)
            {
                case "Json": return BuffSerializer.ToJson(source, settings);
                case "Yaml": return BuffSerializer.ToYaml(source, settings);
                case "Xml": return BuffSerializer.ToXml(source, settings);
                case "Binary": return BuffSerializer.ToBytes(source, settings);
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
