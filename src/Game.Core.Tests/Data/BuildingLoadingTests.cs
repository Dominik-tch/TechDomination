using System.Text.Json.Nodes;
using Game.Core.Data;

namespace Game.Core.Tests.Data;

public class BuildingLoadingTests
{
    [Fact]
    public void Buildings_AreLoadedWithLevelsInFixedUnits()
    {
        var mine = TestData.Load().GetBuilding(TestIds.Mine);

        Assert.Equal("mine", mine.Key);
        Assert.Equal("Mine", mine.Name);
        Assert.Equal(2, mine.MaxLevel);
        Assert.Equal(1000, mine.ProductionBonusPerLevel);
        Assert.Equal(new BuildingLevelDefinition(10_00, [2000, 0, 0], 3), mine.Level(1), new LevelComparer());
        Assert.Equal(new BuildingLevelDefinition(20_00, [4000, 500, 0], 5), mine.Level(2), new LevelComparer());
    }

    [Fact]
    public void ProductionBonus_IsOptional()
    {
        var buildings = TestData.Buildings();
        buildings["buildings"]![0]!.AsObject().Remove("productionBonusPercent");

        var data = GameDataLoader.Load(TestData.Files(buildings: buildings));

        Assert.Equal(0, data.Buildings[0].ProductionBonusPerLevel);
    }

    [Fact]
    public void EmptyBuildingList_Throws()
    {
        var error = LoadFails(new JsonObject { ["buildings"] = new JsonArray() });

        Assert.Contains("'buildings'", error.Message);
    }

    [Fact]
    public void DuplicateId_Throws()
    {
        var buildings = TestData.Buildings();
        buildings["buildings"]!.AsArray().Add(buildings["buildings"]![0]!.DeepClone());

        var error = LoadFails(buildings);

        Assert.Contains("'mine' ist doppelt", error.Message);
    }

    [Fact]
    public void NoLevels_Throws()
    {
        var buildings = TestData.Buildings();
        buildings["buildings"]![0]!["levels"] = new JsonArray();

        var error = LoadFails(buildings);

        Assert.Contains("'levels'", error.Message);
    }

    [Theory]
    [InlineData("money")]
    [InlineData("buildTicks")]
    public void MissingLevelField_ThrowsWithLevel(string field)
    {
        var buildings = TestData.Buildings();
        buildings["buildings"]![0]!["levels"]![1]!.AsObject().Remove(field);

        var error = LoadFails(buildings);

        Assert.Contains("Gebäude 'mine', Stufe 2", error.Message);
        Assert.Contains($"'{field}'", error.Message);
    }

    [Fact]
    public void BuildTicksBelowOne_Throws()
    {
        var buildings = TestData.Buildings();
        buildings["buildings"]![0]!["levels"]![0]!["buildTicks"] = 0;

        var error = LoadFails(buildings);

        Assert.Contains("'buildTicks'", error.Message);
    }

    [Fact]
    public void UnknownCostResource_Throws()
    {
        var buildings = TestData.Buildings();
        buildings["buildings"]![0]!["levels"]![0]!["resources"]!["gold"] = 1;

        var error = LoadFails(buildings);

        Assert.Contains("unbekannte Ressource 'gold'", error.Message);
    }

    [Fact]
    public void NegativeCost_Throws()
    {
        var buildings = TestData.Buildings();
        buildings["buildings"]![0]!["levels"]![0]!["money"] = -1;

        var error = LoadFails(buildings);

        Assert.Contains("nicht negativ", error.Message);
    }

    [Fact]
    public void BonusWithTooManyDecimals_Throws()
    {
        var buildings = TestData.Buildings();
        buildings["buildings"]![0]!["productionBonusPercent"] = JsonNode.Parse("10.125");

        var error = LoadFails(buildings);

        Assert.Contains("'productionBonusPercent'", error.Message);
    }

    private static GameDataException LoadFails(JsonObject buildings) =>
        Assert.Throws<GameDataException>(() => GameDataLoader.Load(TestData.Files(buildings: buildings)));

    private sealed class LevelComparer : IEqualityComparer<BuildingLevelDefinition>
    {
        public bool Equals(BuildingLevelDefinition? x, BuildingLevelDefinition? y) =>
            x is not null && y is not null && x.Money == y.Money && x.BuildTicks == y.BuildTicks
            && x.Resources.SequenceEqual(y.Resources);

        public int GetHashCode(BuildingLevelDefinition obj) => obj.Money.GetHashCode();
    }
}
