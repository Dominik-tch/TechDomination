using System.Text.Json.Nodes;
using Game.Core.Data;
using static Game.Core.Tests.TestIds;

namespace Game.Core.Tests.Data;

public class MarketAndRecipeLoadingTests
{
    [Fact]
    public void Market_IsLoadedInBasisPoints()
    {
        Assert.Equal(new MarketDefinition(1000, 5000, 2000, 8000), TestData.Load().Market);
    }

    [Fact]
    public void BasePrices_AreLoadedInCents()
    {
        Assert.Equal([10_00, 4_00, 50_00], TestData.Load().Resources.Select(r => r.BasePrice));
    }

    [Fact]
    public void Recipe_IsLoadedInThousandths()
    {
        var recipe = TestData.Load().GetBuilding(Railworks).Recipe!;

        Assert.Equal([2000, 500, 0], recipe.Inputs);
        Assert.Equal(Rails, recipe.Output);
        Assert.Equal(1000, recipe.OutputAmount);
    }

    [Fact]
    public void BuildingWithoutRecipe_IsNoFactory()
    {
        var data = TestData.Load();

        Assert.False(data.GetBuilding(Mine).IsFactory);
        Assert.True(data.GetBuilding(Railworks).IsFactory);
    }

    [Theory]
    [InlineData("priceChangePercentPerUnit")]
    [InlineData("recoveryPercentPerInterval")]
    [InlineData("minPricePercent")]
    [InlineData("sellPricePercent")]
    public void MarketField_Missing_Throws(string field)
    {
        var market = TestData.Market();
        market.Remove(field);

        var error = LoadFails(market: market);

        Assert.Contains($"'{field}'", error.Message);
    }

    [Fact]
    public void MarketPercent_Above100_Throws()
    {
        var market = TestData.Market();
        market["sellPricePercent"] = 120;

        var error = LoadFails(market: market);

        Assert.Contains("höchstens 100", error.Message);
    }

    [Fact]
    public void BasePrice_Missing_Throws()
    {
        var resources = TestData.Resources();
        resources["resources"]![2]!.AsObject().Remove("basePrice");

        var error = LoadFails(resources: resources);

        Assert.Contains("Ressource 'rails'", error.Message);
        Assert.Contains("'basePrice'", error.Message);
    }

    [Fact]
    public void BasePrice_Zero_Throws()
    {
        var resources = TestData.Resources();
        resources["resources"]![0]!["basePrice"] = 0;

        var error = LoadFails(resources: resources);

        Assert.Contains("größer als 0", error.Message);
    }

    [Fact]
    public void Recipe_WithoutInputs_Throws()
    {
        var buildings = TestData.Buildings();
        buildings["buildings"]![1]!["recipe"]!["inputs"] = new JsonObject();

        var error = LoadFails(buildings: buildings);

        Assert.Contains("Gebäude 'railworks', Rezept", error.Message);
        Assert.Contains("mindestens eine Zutat", error.Message);
    }

    [Fact]
    public void Recipe_UnknownInput_Throws()
    {
        var buildings = TestData.Buildings();
        buildings["buildings"]![1]!["recipe"]!["inputs"]!["gold"] = 1;

        var error = LoadFails(buildings: buildings);

        Assert.Contains("unbekannte Ressource 'gold'", error.Message);
    }

    [Fact]
    public void Recipe_UnknownOutput_Throws()
    {
        var buildings = TestData.Buildings();
        buildings["buildings"]![1]!["recipe"]!["output"] = "tanks";

        var error = LoadFails(buildings: buildings);

        Assert.Contains("unbekannte Ressource 'tanks'", error.Message);
    }

    [Fact]
    public void Recipe_BasicOutput_Throws()
    {
        var buildings = TestData.Buildings();
        buildings["buildings"]![1]!["recipe"]!["output"] = "wood";

        var error = LoadFails(buildings: buildings);

        Assert.Contains("kein fortgeschrittenes Gut", error.Message);
    }

    [Fact]
    public void Recipe_ZeroAmount_Throws()
    {
        var buildings = TestData.Buildings();
        buildings["buildings"]![1]!["recipe"]!["amount"] = 0;

        var error = LoadFails(buildings: buildings);

        Assert.Contains("'amount'", error.Message);
    }

    private static GameDataException LoadFails(
        JsonObject? resources = null, JsonObject? buildings = null, JsonObject? market = null) =>
        Assert.Throws<GameDataException>(() => GameDataLoader.Load(
            TestData.Files(resources: resources, buildings: buildings, market: market)));
}
