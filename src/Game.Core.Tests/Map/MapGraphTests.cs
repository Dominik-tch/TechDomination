using System.Text.Json.Nodes;
using Game.Core.Data;
using Game.Core.Map;

namespace Game.Core.Tests.Map;

/// <summary>
/// Wegenetz auf einer eigenen Karte: vier Provinzen im Quadrat, Städte bei
/// p0 (5,5), p1 (15,5), p2 (5,15), p3 (15,15); Pfade 0–1, 0–2, 1–3, 2–3 je 10 000 Tausendstel lang.
/// Dazu eine Insel p4 ohne Pfade bei (45,45).
/// </summary>
public class MapGraphTests
{
    private const long Edge = 10_000;

    private static readonly ProvinceId P0 = new(0);
    private static readonly ProvinceId P1 = new(1);
    private static readonly ProvinceId P2 = new(2);
    private static readonly ProvinceId P3 = new(3);
    private static readonly ProvinceId Island = new(4);

    private readonly MapGraph _graph = SquareMap().Graph;

    [Fact]
    public void Length_IsDistanceBetweenCitiesInThousandths()
    {
        Assert.Equal(Edge, _graph.Length(P0, P1));
        Assert.Equal(Edge, _graph.Length(P1, P0));
    }

    [Fact]
    public void Edges_ListsEachPathOnce()
    {
        Assert.Equal([(P0, P1), (P0, P2), (P1, P3), (P2, P3)], _graph.Edges);
    }

    [Fact]
    public void PointOf_InterpolatesAlongPath()
    {
        Assert.Equal(new FinePoint(5000, 5000), _graph.PointOf(PathPosition.AtCity(P0)));
        Assert.Equal(new FinePoint(7500, 5000), _graph.PointOf(new PathPosition(P0, P1, 2500)));
        Assert.Equal(new FinePoint(5000, 12_000), _graph.PointOf(new PathPosition(P0, P2, 7000)));
    }

    [Fact]
    public void Normalize_TurnsPathEndsIntoCities()
    {
        Assert.Equal(PathPosition.AtCity(P0), _graph.Normalize(new PathPosition(P0, P1, 0)));
        Assert.Equal(PathPosition.AtCity(P1), _graph.Normalize(new PathPosition(P0, P1, Edge)));
        Assert.Equal(new PathPosition(P0, P1, 3), _graph.Normalize(new PathPosition(P0, P1, 3)));
    }

    [Theory]
    [InlineData(0, 1, 5000, true)]
    [InlineData(0, 3, 10, false)]
    [InlineData(0, 1, -1, false)]
    [InlineData(0, 1, Edge + 1, false)]
    [InlineData(9, null, 0, false)]
    [InlineData(4, null, 0, true)]
    public void IsValid_ChecksPathAndProgress(int from, int? to, long progress, bool expected)
    {
        var position = new PathPosition(new ProvinceId(from), to is { } t ? new ProvinceId(t) : null, progress);

        Assert.Equal(expected, _graph.IsValid(position));
    }

    [Fact]
    public void NearestPosition_ProjectsOntoPath()
    {
        // (8, 6) liegt neben dem Pfad p0–p1 (y = 5) bei x = 8.
        Assert.Equal(new PathPosition(P0, P1, 3000), _graph.NearestPosition(new FinePoint(8000, 6000)));
    }

    [Fact]
    public void NearestPosition_NearCity_IsCity()
    {
        Assert.Equal(PathPosition.AtCity(P3), _graph.NearestPosition(new FinePoint(15_200, 15_300)));
    }

    [Fact]
    public void NearestPosition_WithinSnapDistance_IsCity()
    {
        // (15, 7) liegt auf dem Pfad p1–p3 nahe p1; mit Fangradius 3 wird daraus die Stadt p1.
        var point = new FinePoint(15_000, 7000);

        Assert.Equal(new PathPosition(P1, P3, 2000), _graph.NearestPosition(point));
        Assert.Equal(PathPosition.AtCity(P1), _graph.NearestPosition(point, citySnapDistance: 3000));
        Assert.Equal(new PathPosition(P1, P3, 2000), _graph.NearestPosition(point, citySnapDistance: 1999));
    }

    [Fact]
    public void NearestPosition_CanBeIslandCity()
    {
        Assert.Equal(PathPosition.AtCity(Island), _graph.NearestPosition(new FinePoint(44_000, 46_000)));
    }

    [Fact]
    public void FindRoute_CityToNeighborCity()
    {
        var route = _graph.FindRoute(PathPosition.AtCity(P0), PathPosition.AtCity(P1))!;

        Assert.Equal(PathPosition.AtCity(P0), route.Start);
        Assert.Equal([new MoveLeg(P0, P1, Edge)], route.Legs);
        Assert.Equal(Edge, route.Distance);
    }

    [Fact]
    public void FindRoute_EqualLengths_PrefersLowerIds()
    {
        // p0 → p3 ist über p1 und über p2 gleich lang; die Wahl muss immer dieselbe sein.
        var route = _graph.FindRoute(PathPosition.AtCity(P0), PathPosition.AtCity(P3))!;

        Assert.Equal([new MoveLeg(P0, P1, Edge), new MoveLeg(P1, P3, Edge)], route.Legs);
        Assert.Equal(2 * Edge, route.Distance);
    }

    [Fact]
    public void FindRoute_ToPointOnPath_StopsThere()
    {
        var route = _graph.FindRoute(PathPosition.AtCity(P0), new PathPosition(P1, P3, 4000))!;

        Assert.Equal([new MoveLeg(P0, P1, Edge), new MoveLeg(P1, P3, 4000)], route.Legs);
        Assert.Equal(Edge + 4000, route.Distance);
    }

    [Fact]
    public void FindRoute_ToPointOnPath_UsesNearerEnd()
    {
        // Ziel liegt auf p1–p3 kurz vor p3: über p2 → p3 und dann zurück ist kürzer als über p1.
        var route = _graph.FindRoute(PathPosition.AtCity(P2), new PathPosition(P1, P3, 9000))!;

        Assert.Equal([new MoveLeg(P2, P3, Edge), new MoveLeg(P3, P1, 1000)], route.Legs);
        Assert.Equal(Edge + 1000, route.Distance);
    }

    [Fact]
    public void FindRoute_FromMiddleOfPath_GoesForward()
    {
        var route = _graph.FindRoute(new PathPosition(P0, P1, 7000), PathPosition.AtCity(P3))!;

        Assert.Equal(new PathPosition(P0, P1, 7000), route.Start);
        Assert.Equal([new MoveLeg(P0, P1, Edge), new MoveLeg(P1, P3, Edge)], route.Legs);
        Assert.Equal(3000 + Edge, route.Distance);
    }

    [Fact]
    public void FindRoute_FromMiddleOfPath_TurnsAround()
    {
        var route = _graph.FindRoute(new PathPosition(P0, P1, 2000), PathPosition.AtCity(P2))!;

        Assert.Equal(new PathPosition(P1, P0, 8000), route.Start);
        Assert.Equal([new MoveLeg(P1, P0, Edge), new MoveLeg(P0, P2, Edge)], route.Legs);
        Assert.Equal(2000 + Edge, route.Distance);
    }

    [Fact]
    public void FindRoute_OnSamePath_Forward()
    {
        var route = _graph.FindRoute(new PathPosition(P0, P1, 2000), new PathPosition(P0, P1, 6000))!;

        Assert.Equal([new MoveLeg(P0, P1, 6000)], route.Legs);
        Assert.Equal(4000, route.Distance);
    }

    [Fact]
    public void FindRoute_OnSamePath_Backward_TurnsAround()
    {
        var route = _graph.FindRoute(new PathPosition(P0, P1, 6000), new PathPosition(P1, P0, 7000))!;

        // Ziel ist 3000 ab p0; die Armee steht bei 6000 und muss 3000 zurück.
        Assert.Equal(new PathPosition(P1, P0, 4000), route.Start);
        Assert.Equal([new MoveLeg(P1, P0, 7000)], route.Legs);
        Assert.Equal(3000, route.Distance);
    }

    [Fact]
    public void FindRoute_ToIsland_IsNull()
    {
        Assert.Null(_graph.FindRoute(PathPosition.AtCity(P0), PathPosition.AtCity(Island)));
    }

    [Fact]
    public void FindRoute_ToSamePoint_IsNull()
    {
        Assert.Null(_graph.FindRoute(PathPosition.AtCity(P0), new PathPosition(P0, P1, 0)));
        Assert.Null(_graph.FindRoute(new PathPosition(P0, P1, 4000), new PathPosition(P1, P0, 6000)));
    }

    private static GameData SquareMap()
    {
        var map = JsonNode.Parse("""
            {
              "provinces": [
                { "id": "p0", "name": "P0", "resource": "wood", "owner": "red", "neighbors": ["p1", "p2"],
                  "outline": [[0, 0], [10, 0], [10, 10], [0, 10]], "label": [5, 5], "city": [5, 5] },
                { "id": "p1", "name": "P1", "resource": "wood", "owner": "red", "neighbors": ["p0", "p3"],
                  "outline": [[10, 0], [20, 0], [20, 10], [10, 10]], "label": [15, 5], "city": [15, 5] },
                { "id": "p2", "name": "P2", "resource": "wood", "owner": "red", "neighbors": ["p0", "p3"],
                  "outline": [[0, 10], [10, 10], [10, 20], [0, 20]], "label": [5, 15], "city": [5, 15] },
                { "id": "p3", "name": "P3", "resource": "wood", "owner": "blue", "neighbors": ["p1", "p2"],
                  "outline": [[10, 10], [20, 10], [20, 20], [10, 20]], "label": [15, 15], "city": [15, 15] },
                { "id": "p4", "name": "Insel", "resource": "fish", "owner": "blue",
                  "outline": [[40, 40], [50, 40], [50, 50], [40, 50]], "label": [45, 45], "city": [45, 45] }
              ]
            }
            """)!.AsObject();

        return GameDataLoader.Load(TestData.Files(map: map));
    }
}
