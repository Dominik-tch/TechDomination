using System.Text.Json.Nodes;
using Game.Core.Data;

namespace Game.Core.Tests.Data;

public class EconomyLoadingTests
{
    [Fact]
    public void Economy_IsLoadedInFixedUnits()
    {
        var economy = TestData.Load().Economy;

        Assert.Equal(4, economy.IntervalTicks);
        Assert.Equal(250, economy.TaxPerProvince);
        Assert.Equal(100_00, economy.StartMoney);
        Assert.Equal([10_000, 500, 0], economy.StartResources);
    }

    [Fact]
    public void StartResources_Missing_StartAtZero()
    {
        var economy = TestData.Economy();
        economy.Remove("startResources");

        var data = GameDataLoader.Load(TestData.Files(economy: economy));

        Assert.All(data.Economy.StartResources, amount => Assert.Equal(0, amount));
    }

    [Theory]
    [InlineData("intervalTicks")]
    [InlineData("taxPerProvince")]
    [InlineData("startMoney")]
    public void MissingField_Throws(string field)
    {
        var economy = TestData.Economy();
        economy.Remove(field);

        var error = LoadFails(economy);

        Assert.Contains($"'{field}'", error.Message);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-4)]
    public void IntervalBelowOne_Throws(int intervalTicks)
    {
        var economy = TestData.Economy();
        economy["intervalTicks"] = intervalTicks;

        var error = LoadFails(economy);

        Assert.Contains("'intervalTicks'", error.Message);
    }

    [Fact]
    public void MoneyWithThreeDecimals_Throws()
    {
        var economy = TestData.Economy();
        economy["taxPerProvince"] = JsonNode.Parse("2.555");

        var error = LoadFails(economy);

        Assert.Contains("zu viele Nachkommastellen (höchstens 2)", error.Message);
    }

    [Fact]
    public void NegativeStartMoney_Throws()
    {
        var economy = TestData.Economy();
        economy["startMoney"] = -1;

        var error = LoadFails(economy);

        Assert.Contains("nicht negativ", error.Message);
    }

    [Fact]
    public void UnknownStartResource_Throws()
    {
        var economy = TestData.Economy();
        economy["startResources"]!["gold"] = 5;

        var error = LoadFails(economy);

        Assert.Contains("unbekannte Ressource 'gold'", error.Message);
    }

    [Fact]
    public void NegativeStartResource_Throws()
    {
        var economy = TestData.Economy();
        economy["startResources"]!["wood"] = -5;

        var error = LoadFails(economy);

        Assert.Contains("'wood'", error.Message);
        Assert.Contains("nicht negativ", error.Message);
    }

    private static GameDataException LoadFails(JsonObject economy) =>
        Assert.Throws<GameDataException>(() => GameDataLoader.Load(TestData.Files(economy: economy)));
}
