using Pkl.Evaluation;

namespace Pkl.Tests.Evaluation;

/// <summary>
/// Pkl's Int is a 64-bit signed integer, but MessagePack encodes it in the smallest width that fits.
/// These evaluate real Pkl so the wire encoding is whatever Pkl actually produces.
/// </summary>
public class IntegerDecodingTests
{
    public class Numbers
    {
        public long? SmallNullable { get; set; }
        public long? BigNullable { get; set; }
        public long? Unset { get; set; }
        public long Plain { get; set; }
        public sbyte Int8Positive { get; set; }
        public sbyte Int8Negative { get; set; }
        public short Int16FromUInt16Width { get; set; }
        public int Int32FromUInt32Width { get; set; }
        public byte UInt8 { get; set; }
        public ushort UInt16 { get; set; }
        public uint UInt32 { get; set; }
        public ulong UInt { get; set; }
        public long MinInt { get; set; }
        public long MaxInt { get; set; }
        public double IntegerAsNumber { get; set; }
        public long?[] NullableList { get; set; } = default!;
        public Dictionary<int, short> Widths { get; set; } = default!;
        public HashSet<short> Set { get; set; } = default!;
        public (int, long) Pair { get; set; }
    }

    private const string Module = """
        smallNullable: Int? = 5
        bigNullable: Int? = 1000
        unset: Int? = null
        plain: Int = 70000
        int8Positive: Int8 = 100
        int8Negative: Int8 = -100
        int16FromUInt16Width: Int16 = 300
        int32FromUInt32Width: Int32 = 100000
        uInt8: UInt8 = 200
        uInt16: UInt16 = 40000
        uInt32: UInt32 = 3000000000
        uInt: UInt = 9223372036854775807
        minInt: Int = -9223372036854775808
        maxInt: Int = 9223372036854775807
        integerAsNumber: Number = 5
        nullableList: Listing<Int?> = new { 1; null; 1000; -1000; 5000000000 }
        widths: Mapping<Int32, Int16> = new { [1] = 2; [300] = 300; [-70000] = -300 }
        set: Set<Int16> = Set(1, 300, -300)
        pair: Pair<Int32, Int> = Pair(300, 5000000000)
        """;

    private static async Task<(EvaluatorManager.EvaluatorManager Manager, IEvaluator Evaluator)> NewEvaluator()
    {
        var manager = new EvaluatorManager.EvaluatorManager();
        var evaluator = await manager.NewEvaluator(EvaluatorOptions.PreconfiguredOptons());
        return (manager, evaluator);
    }

    [Fact]
    public async Task IntegersOfEveryWireWidthDecodeIntoTheDeclaredType()
    {
        var (manager, evaluator) = await NewEvaluator();
        try
        {
            var n = await evaluator.EvaluateModule<Numbers>(ModuleSource.TextSource(Module));

            Assert.Equal(5L, n.SmallNullable);
            Assert.Equal(1000L, n.BigNullable);
            Assert.Null(n.Unset);
            Assert.Equal(70000L, n.Plain);
            Assert.Equal((sbyte)100, n.Int8Positive);
            Assert.Equal((sbyte)-100, n.Int8Negative);
            Assert.Equal((short)300, n.Int16FromUInt16Width);
            Assert.Equal(100000, n.Int32FromUInt32Width);
            Assert.Equal((byte)200, n.UInt8);
            Assert.Equal((ushort)40000, n.UInt16);
            Assert.Equal(3000000000u, n.UInt32);
            Assert.Equal(9223372036854775807UL, n.UInt);
            Assert.Equal(long.MinValue, n.MinInt);
            Assert.Equal(long.MaxValue, n.MaxInt);
            Assert.Equal(5.0, n.IntegerAsNumber);
            Assert.Equal(new long?[] { 1, null, 1000, -1000, 5000000000 }, n.NullableList);
            Assert.Equal(new Dictionary<int, short> { [1] = 2, [300] = 300, [-70000] = -300 }, n.Widths);
            Assert.Equal(new HashSet<short> { 1, 300, -300 }, n.Set);
            Assert.Equal((300, 5000000000L), n.Pair);
        }
        finally
        {
            manager.Close();
        }
    }

    [Fact]
    public async Task UntypedIntegersAreAlwaysLong()
    {
        var (manager, evaluator) = await NewEvaluator();
        try
        {
            var result = await evaluator.EvaluateModule<dynamic>(
                ModuleSource.TextSource("small = 5\nmedium = 1000\nnegative = -5\nlarge = 5000000000"));

            Assert.IsType<long>(result.Small);
            Assert.IsType<long>(result.Medium);
            Assert.IsType<long>(result.Negative);
            Assert.Equal(5000000000L, result.Large);
        }
        finally
        {
            manager.Close();
        }
    }

    public class Narrow
    {
        public byte Value { get; set; }
    }

    [Fact]
    public async Task ValuesThatDoNotFitTheDeclaredTypeGiveAClearError()
    {
        var (manager, evaluator) = await NewEvaluator();
        try
        {
            var ex = await Record.ExceptionAsync(() =>
                evaluator.EvaluateModule<Narrow>(ModuleSource.TextSource("value = 300")));

            Assert.NotNull(ex);
            Assert.Contains("300", ex.Message);
            Assert.Contains("Byte", ex.Message);
        }
        finally
        {
            manager.Close();
        }
    }
}
