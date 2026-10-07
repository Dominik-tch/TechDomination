using System.Text.Json.Nodes;
using Game.Core.Data;

namespace Game.Core.Tests.Data;

public class ResourceAndNationLoadingTests
{
    [Fact]
    public void Resources_AreLoadedInFileOrderWithIds()
    {
        var data = TestData.Load();

        Assert.Equal(["wood", "fish", "rails"], data.Resources.Select(r => r.Key));
        Assert.Equal([0, 1, 2], data.Resources.Select(r => r.Id.Value));
        Assert.Equal("Fisch", data.GetResource(new ResourceId(1)).Name);
        Assert.Equal(ResourceTier.Advanced, data.GetResource(new ResourceId(2)).Tier);
    }

    [Fact]
    public void Nations_AreLoadedInFileOrderWithIds()
    {
        var data = TestData.Load();

        Assert.Equal(["red", "blue"], data.Nations.Select(n => n.Key));
        Assert.Equal("Blauland", data.GetNation(new NationId(1)).Name);
        Assert.Equal("#0000ff", data.GetNation(new NationId(1)).Color);
    }

    [Fact]
    public void Resources_EmptyList_Throws()
    {
        var error = LoadFails(resources: new JsonObject { ["resources"] = new JsonArray() });

        Assert.Contains(GameDataLoader.ResourcesFileName, error.Message);
    }

    [Fact]
    public void Resources_DuplicateId_Throws()
    {
        var resources = TestData.Resources();
        resources["resources"]![1]!["id"] = "wood";

        var error = LoadFails(resources: resources);

        Assert.Contains("'wood' ist doppelt", error.Message);
    }

    [Fact]
    public void Resources_MissingName_Throws()
    {
        var resources = TestData.Resources();
        resources["resources"]![0]!.AsObject().Remove("name");

        var error = LoadFails(resources: resources);

        Assert.Contains("'name'", error.Message);
    }

    [Theory]
    [InlineData("medium")]
    [InlineData("Basic")]
    [InlineData(null)]
    public void Resources_InvalidTier_Throws(string? tier)
    {
        var resources = TestData.Resources();
        resources["resources"]![0]!["tier"] = tier;

        var error = LoadFails(resources: resources);

        Assert.Contains("'tier'", error.Message);
    }

    [Fact]
    public void Nations_MissingId_Throws()
    {
        var nations = TestData.Nations();
        nations["nations"]![0]!.AsObject().Remove("id");

        var error = LoadFails(nations: nations);

        Assert.Contains("ohne 'id'", error.Message);
    }

    [Fact]
    public void Nations_DuplicateId_Throws()
    {
        var nations = TestData.Nations();
        nations["nations"]![1]!["id"] = "red";

        var error = LoadFails(nations: nations);

        Assert.Contains("'red' ist doppelt", error.Message);
    }

    [Theory]
    [InlineData("red")]
    [InlineData("#FFF")]
    [InlineData("#GG0000")]
    [InlineData("FF0000")]
    public void Nations_InvalidColor_Throws(string color)
    {
        var nations = TestData.Nations();
        nations["nations"]![0]!["color"] = color;

        var error = LoadFails(nations: nations);

        Assert.Contains("'color'", error.Message);
    }

    [Fact]
    public void Nations_NullEntry_Throws()
    {
        var nations = TestData.Nations();
        nations["nations"]!.AsArray().Add(null);

        var error = LoadFails(nations: nations);

        Assert.Contains("leeren Eintrag", error.Message);
    }

    private static GameDataException LoadFails(JsonObject? resources = null, JsonObject? nations = null) =>
        Assert.Throws<GameDataException>(
            () => GameDataLoader.Load(TestData.Files(resources: resources, nations: nations)));
}
