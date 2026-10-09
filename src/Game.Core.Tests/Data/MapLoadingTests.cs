using System.Text.Json.Nodes;
using Game.Core.Data;

namespace Game.Core.Tests.Data;

public class MapLoadingTests
{
    [Fact]
    public void Provinces_AreLoadedWithResolvedReferences()
    {
        var data = TestData.Load();

        var b = data.GetProvince(new ProvinceId(1));
        Assert.Equal("b", b.Key);
        Assert.Equal("B", b.Name);
        Assert.Equal("fish", data.GetResource(b.Resource).Key);
        Assert.Equal(new MapPoint(15, 5), b.City);
        Assert.Equal("blue", data.GetNation(b.StartOwner).Key);
        Assert.Equal(new MapPoint(15, 5), b.LabelPosition);
        Assert.Equal(
            [new MapPoint(10, 0), new MapPoint(20, 0), new MapPoint(20, 10), new MapPoint(10, 10)],
            b.Outline);
    }

    [Fact]
    public void Neighbors_AreSortedById()
    {
        var data = TestData.Load();

        // In der Datei steht ["c", "a"].
        Assert.Equal([new ProvinceId(0), new ProvinceId(2)], data.GetProvince(new ProvinceId(1)).Neighbors);
    }

    [Fact]
    public void Island_WithoutNeighbors_IsAllowed()
    {
        var map = TestData.Map();
        map["provinces"]!.AsArray().Add(JsonNode.Parse("""
            {
              "id": "island", "name": "Insel", "resource": "fish", "owner": "red",
              "outline": [[50, 50], [60, 50], [60, 60]], "label": [58, 55], "city": [58, 54]
            }
            """));

        var data = GameDataLoader.Load(TestData.Files(map: map));

        Assert.Empty(data.Provinces[^1].Neighbors);
    }

    [Fact]
    public void EmptyProvinceList_Throws()
    {
        var error = LoadFails(new JsonObject { ["provinces"] = new JsonArray() });

        Assert.Contains("'provinces'", error.Message);
    }

    [Fact]
    public void DuplicateId_Throws()
    {
        var map = TestData.Map();
        TestData.Province(map, "c")["id"] = "a";

        var error = LoadFails(map);

        Assert.Contains("'a' ist doppelt", error.Message);
    }

    [Theory]
    [InlineData("name")]
    [InlineData("resource")]
    [InlineData("city")]
    [InlineData("owner")]
    [InlineData("outline")]
    [InlineData("label")]
    public void MissingField_ThrowsWithProvinceAndField(string field)
    {
        var map = TestData.Map();
        TestData.Province(map, "b").Remove(field);

        var error = LoadFails(map);

        Assert.Contains("Provinz 'b'", error.Message);
        Assert.Contains($"'{field}'", error.Message);
    }

    [Fact]
    public void UnknownResource_Throws()
    {
        var map = TestData.Map();
        TestData.Province(map, "a")["resource"] = "gold";

        var error = LoadFails(map);

        Assert.Contains("unbekannte Ressource 'gold'", error.Message);
    }

    [Fact]
    public void AdvancedResource_Throws()
    {
        var map = TestData.Map();
        TestData.Province(map, "a")["resource"] = "rails";

        var error = LoadFails(map);

        Assert.Contains("kein Basis-Rohstoff", error.Message);
    }

    [Fact]
    public void UnknownOwner_Throws()
    {
        var map = TestData.Map();
        TestData.Province(map, "a")["owner"] = "green";

        var error = LoadFails(map);

        Assert.Contains("unbekannte Nation 'green'", error.Message);
    }

    [Fact]
    public void CityOutsideProvince_Throws()
    {
        var map = TestData.Map();
        TestData.Province(map, "a")["city"] = JsonNode.Parse("[15, 5]");

        var error = LoadFails(map);

        Assert.Contains("Provinz 'a'", error.Message);
        Assert.Contains("Stadt liegt nicht in der Provinz", error.Message);
    }

    [Fact]
    public void StartArmy_IsLoadedPerUnitType()
    {
        var data = TestData.Load();

        Assert.Equal([2, 0, 0], data.GetProvince(new ProvinceId(0)).StartArmy);
        Assert.Equal([0, 0, 0], data.GetProvince(new ProvinceId(1)).StartArmy);
    }

    [Fact]
    public void StartArmy_UnknownUnit_Throws()
    {
        var map = TestData.Map();
        TestData.Province(map, "a")["startArmy"] = JsonNode.Parse("""{ "tanks": 1 }""");

        var error = LoadFails(map);

        Assert.Contains("unbekannte Einheit 'tanks'", error.Message);
    }

    [Fact]
    public void StartArmy_NegativeCount_Throws()
    {
        var map = TestData.Map();
        TestData.Province(map, "a")["startArmy"] = JsonNode.Parse("""{ "infantry": -1 }""");

        var error = LoadFails(map);

        Assert.Contains("nicht negativ", error.Message);
    }

    [Fact]
    public void UnknownNeighbor_Throws()
    {
        var map = TestData.Map();
        TestData.Province(map, "a")["neighbors"] = new JsonArray("b", "x");

        var error = LoadFails(map);

        Assert.Contains("unbekannter Nachbar 'x'", error.Message);
    }

    [Fact]
    public void SelfAsNeighbor_Throws()
    {
        var map = TestData.Map();
        TestData.Province(map, "a")["neighbors"] = new JsonArray("b", "a");

        var error = LoadFails(map);

        Assert.Contains("eigener Nachbar", error.Message);
    }

    [Fact]
    public void DuplicateNeighbor_Throws()
    {
        var map = TestData.Map();
        TestData.Province(map, "a")["neighbors"] = new JsonArray("b", "b");

        var error = LoadFails(map);

        Assert.Contains("doppelt eingetragen", error.Message);
    }

    [Fact]
    public void OneSidedNeighbor_Throws()
    {
        var map = TestData.Map();
        TestData.Province(map, "a")["neighbors"] = new JsonArray("b", "c");

        var error = LoadFails(map);

        Assert.Contains("nicht gegenseitig", error.Message);
        Assert.Contains("'a' nennt 'c'", error.Message);
    }

    [Fact]
    public void OutlineWithTwoPoints_Throws()
    {
        var map = TestData.Map();
        TestData.Province(map, "a")["outline"] = JsonNode.Parse("[[0, 0], [10, 0]]");

        var error = LoadFails(map);

        Assert.Contains("mindestens 3 Punkte", error.Message);
    }

    [Theory]
    [InlineData("[[0, 0], [10], [10, 10]]")]
    [InlineData("[[0, 0], [10, 0, 5], [10, 10]]")]
    [InlineData("[[0, 0], null, [10, 10]]")]
    public void MalformedOutlinePoint_Throws(string outline)
    {
        var map = TestData.Map();
        TestData.Province(map, "a")["outline"] = JsonNode.Parse(outline);

        var error = LoadFails(map);

        Assert.Contains("[x, y]", error.Message);
    }

    [Fact]
    public void MalformedLabel_Throws()
    {
        var map = TestData.Map();
        TestData.Province(map, "a")["label"] = JsonNode.Parse("[5]");

        var error = LoadFails(map);

        Assert.Contains("'label'", error.Message);
    }

    private static GameDataException LoadFails(JsonObject map) =>
        Assert.Throws<GameDataException>(() => GameDataLoader.Load(TestData.Files(map: map)));
}
