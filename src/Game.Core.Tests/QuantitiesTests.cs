namespace Game.Core.Tests;

public class QuantitiesTests
{
    [Theory]
    [InlineData("0", 1000, 0)]
    [InlineData("1", 1000, 1000)]
    [InlineData("0.5", 1000, 500)]
    [InlineData("0.001", 1000, 1)]
    [InlineData("12.25", 100, 1225)]
    [InlineData("-3.5", 100, -350)]
    [InlineData("1.500", 100, 150)]
    public void ToFixed_ConvertsExactly(string value, long scale, long expected)
    {
        Assert.Equal(expected, Quantities.ToFixed(decimal.Parse(value, System.Globalization.CultureInfo.InvariantCulture), scale));
    }

    [Theory]
    [InlineData("0.0005", 1000)]
    [InlineData("0.125", 100)]
    [InlineData("99999999999999999999", 1000)]
    public void ToFixed_RejectsValuesThatDoNotFit(string value, long scale)
    {
        Assert.Null(Quantities.ToFixed(decimal.Parse(value, System.Globalization.CultureInfo.InvariantCulture), scale));
    }
}
