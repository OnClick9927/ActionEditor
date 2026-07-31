using System;
using System.Collections.Concurrent;
using System.Collections.Generic;

namespace System.Runtime.CompilerServices
{
    internal static class IsExternalInit { }
}

namespace ActionBuffer.Tests
{
    public enum SampleEnum : long
    {
        None,
        Negative = -7,
        Large = 9000000000L
    }

    public struct SmallStruct
    {
        public int Number;
        public string Text;
    }

    public sealed class PrimitiveModel
    {
        public bool Bool = true;
        public byte Byte = 250;
        public sbyte SByte = -100;
        public char Char = '\u4e2d';
        public short Short = -32000;
        public ushort UShort = 65000;
        public int Int = -123456789;
        public uint UInt = 4000000000;
        public long Long = -9000000000000;
        public ulong ULong = 18000000000000;
        public float Float = float.NaN;
        public double Double = double.PositiveInfinity;
        public decimal Decimal = 7922816251426433759354395033.5m;
        public string String = "ActionBuffer \"text\"\nUnicode";
        public DateTime DateTime = new DateTime(638600000000000000, DateTimeKind.Utc);
        public TimeSpan TimeSpan = TimeSpan.FromTicks(-9876543210);
        public Guid Guid = new Guid("763ddb22-325e-4d16-805e-f669dc1349ad");
        public SampleEnum Enum = SampleEnum.Negative;
        public SmallStruct Struct = new SmallStruct { Number = 17, Text = "struct" };
    }

    public sealed class CollectionModel
    {
        public int[] Array = { 1, 2, 3 };
        public List<string> List = new List<string> { "a", "b" };
        public IEnumerable<int> Enumerable = new List<int> { 4, 5 };
        public ICollection<int> Collection = new List<int> { 6, 7 };
        public IList<int> ListInterface = new List<int> { 8, 9 };
        public IReadOnlyCollection<int> ReadOnlyCollection = new List<int> { 10, 11 };
        public IReadOnlyList<int> ReadOnlyList = new List<int> { 12, 13 };
        public Dictionary<string, int> Dictionary = new Dictionary<string, int>
        {
            { "one", 1 }, { "two", 2 }
        };
        public ConcurrentDictionary<string, int> ConcurrentDictionary =
            new ConcurrentDictionary<string, int>(new[]
            {
                new KeyValuePair<string, int>("three", 3),
                new KeyValuePair<string, int>("four", 4)
            });
        public IDictionary<string, int> DictionaryInterface =
            new Dictionary<string, int> { { "five", 5 } };
        public IReadOnlyDictionary<string, int> ReadOnlyDictionary =
            new Dictionary<string, int> { { "six", 6 } };
        public HashSet<int> HashSet = new HashSet<int> { 9, 3, 7 };
        public ISet<int> SetInterface = new HashSet<int> { 8, 2, 6 };
        public Queue<int> Queue = new Queue<int>(new[] { 1, 2, 3 });
        public Stack<int> Stack = new Stack<int>(new[] { 1, 2, 3 });
        public ArraySegment<int> Segment = new ArraySegment<int>(new[] { 0, 4, 5, 0 }, 1, 2);
        public KeyValuePair<string, int> Pair = new KeyValuePair<string, int>("pair", 42);
    }

    public abstract class Animal
    {
        public string Name;
    }

    public sealed class Dog : Animal
    {
        public int Age;
    }

    public interface IShape
    {
        int Area { get; }
    }

    public sealed class Rectangle : IShape
    {
        public int Width;
        public int Height;
        public int Area => Width * Height;
    }

    public class BaseValue
    {
        public int BaseNumber;
    }

    public sealed class DerivedValue : BaseValue
    {
        public string DerivedText;
    }

    public sealed class PolymorphicModel
    {
        public Animal AbstractValue = new Dog { Name = "dog", Age = 6 };
        public IShape InterfaceValue = new Rectangle { Width = 4, Height = 5 };
        public BaseValue BaseValue = new DerivedValue { BaseNumber = 12, DerivedText = "derived" };
    }

    public sealed class NullableAndTupleModel
    {
        public int? HasValue = 13;
        public int? NoValue;
        public SmallStruct? NullableStruct = new SmallStruct { Number = 5, Text = "nullable" };
        public Tuple<int, string> Tuple = new Tuple<int, string>(2, "tuple");
        public (int Number, string Text) ValueTuple = (3, "value tuple");
        public ValueTuple EmptyTuple;
    }

    public sealed class ParameterizedOnlyModel
    {
        public int Number;
        public string Text;

        public ParameterizedOnlyModel(int number, string text)
        {
            Number = number;
            Text = text;
        }
    }

    public record RecordModel(int Id, string Name);

    public sealed class DelegateModel
    {
        public int Invocations;
        public Action Callback;
        public event Action Changed;

        public DelegateModel()
        {
            Callback = OnInvoked;
            Changed += OnInvoked;
        }

        private void OnInvoked() => Invocations++;

        public void Raise()
        {
            Callback?.Invoke();
            Changed?.Invoke();
        }
    }

    public sealed class Array2DModel
    {
        public int[,] Numbers =
        {
            { 1, 2, 3 },
            { 4, 5, 6 }
        };
        public string[,] Text =
        {
            { "a", null },
            { "c", "d" }
        };
        public int[,] Empty = new int[0, 0];
        public int[,] EmptyRows = new int[0, 3];
        public int[,] EmptyColumns = new int[2, 0];
        public int[,] Null;
    }

    public sealed class SerializableDelegateModel
    {
        public static int StaticValue;
        public int Value;
        [Buffer] public Action<int> Callback;

        public void Configure()
        {
            Callback = Add;
            Callback += AddStatic;
        }

        public void ConfigureClosure()
        {
            int captured = 1;
            Callback = value => Value += value + captured;
        }

        public static Action<int> CreateStaticCallback() => AddStatic;

        private void Add(int value) => Value += value;
        private static void AddStatic(int value) => StaticValue += value;
    }

    public sealed class ExternalDelegateTarget
    {
        public int Value;

        public Action<int> CreateCallback() => Add;
        private void Add(int value) => Value += value;
    }

    public sealed class ExternalDelegateModel
    {
        [Buffer] public Action<int> Callback;
        public ExternalDelegateTarget Target;

        public void Configure(int initialValue)
        {
            Target = new ExternalDelegateTarget
            {
                Value = initialValue
            };
            Callback = Target.CreateCallback();
        }
    }

    public sealed class DetachedDelegateTargetModel
    {
        [Buffer] public Action<int> Callback;

        public void Configure()
        {
            Callback = new ExternalDelegateTarget().CreateCallback();
        }
    }

    public sealed class SerializableEventModel
    {
        public int Value;
        [field: Buffer] public event Action<int> Changed;

        public void Configure() => Changed = Add;
        public void Raise(int value) => Changed?.Invoke(value);
        private void Add(int value) => Value += value;
    }

    public sealed class CallbackChild : IBufferObject
    {
        public int Number;
        [NonSerialized] public bool AfterReadCalled;
        public void BeforeWriteBuffer() { }
        public void AfterReadBuffer() => AfterReadCalled = true;
    }

    public sealed class CallbackParent : IBufferObject
    {
        public CallbackChild Child = new CallbackChild();
        [NonSerialized] public bool ChildWasComplete;
        public void BeforeWriteBuffer() { }
        public void AfterReadBuffer() => ChildWasComplete = Child != null && Child.AfterReadCalled;
    }

    public sealed class DefaultValueModel
    {
        public int Zero = 7;
        public string Null = "constructor";
    }

    public sealed class SharedLeaf
    {
        public int Value;
    }

    public sealed class SharedReferenceModel
    {
        public SharedLeaf First;
        public SharedLeaf Second;
    }

    public sealed class CircularModel
    {
        public CircularModel Self;
    }

    public sealed class ReferenceNode
    {
        public string Name;
        public ReferenceNode Next;
    }

    public sealed class SharedCollectionModel
    {
        public List<int> First;
        public List<int> Second;
        public int[] FirstArray;
        public int[] SecondArray;
    }

    public sealed class CollectionCycleModel
    {
        public string Name;
        public List<CollectionCycleModel> Items;
    }

    public sealed class ReservedFieldNameModel
    {
        [Buffer("$ActionBuffer.Reference.v1")]
        public int LegacyReferenceMarker;
        [Buffer("$ActionBuffer.Reference.v2")]
        public int ReferenceMarker;
        [Buffer("$id")]
        public int Id;
        [Buffer("$values")]
        public string Values;
    }

    public sealed class CallbackCounterModel : IBufferObject
    {
        public static int AfterReadCount;
        public int Value;
        public void BeforeWriteBuffer() { }
        public void AfterReadBuffer() => AfterReadCount++;
    }

    public sealed class TemporalModel
    {
        public DateTime DateTime;
        public TimeSpan TimeSpan;
    }

    public sealed class EmptyNode { }

    public sealed class NodeLimitModel
    {
        public List<EmptyNode> Nodes = new List<EmptyNode>();
    }

    public sealed class LongStringModel
    {
        public string Value;
    }

    public sealed class LateRegisteredValue
    {
        public int Value;
    }

    public sealed class SettingsScopedValue
    {
        public int Value;
    }

    public sealed class PerformanceModel
    {
        public PrimitiveModel Primitive = new PrimitiveModel();
        public List<SmallStruct> Items = new List<SmallStruct>();
        public Dictionary<int, string> Lookup = new Dictionary<int, string>();
    }

    public abstract class GenericDiscoveryBase { }

    public sealed class GenericDiscoveryTemplate<T> : GenericDiscoveryBase { }
}
