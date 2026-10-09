using Game.Core.Map;

namespace Game.Core.Tests.Map;

public class IntegerMathTests
{
    [Theory]
    [InlineData(0, 0)]
    [InlineData(1, 1)]
    [InlineData(2, 1)]
    [InlineData(3, 1)]
    [InlineData(4, 2)]
    [InlineData(99, 9)]
    [InlineData(100, 10)]
    [InlineData(101, 10)]
    [InlineData(1_000_000_000_000, 1_000_000)]
    [InlineData(long.MaxValue, 3_037_000_499)]
    public void Sqrt_ReturnsFloorOfSquareRoot(long value, long expected)
    {
        Assert.Equal(expected, IntegerMath.Sqrt(value));
    }

    [Fact]
    public void Sqrt_IsExactFloorForManyValues()
    {
        for (long value = 0; value < 5000; value++)
        {
            long root = IntegerMath.Sqrt(value);
            Assert.True(root * root <= value && (root + 1) * (root + 1) > value, $"Sqrt({value}) = {root}");
        }
    }

    [Fact]
    public void Sqrt_RejectsNegative()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => IntegerMath.Sqrt(-1));
    }
}
