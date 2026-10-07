using Game.Core.Data;
using Game.Core.Map;

namespace Game.Core.Tests.Map;

public class MapGeometryTests
{
    private static readonly MapPoint[] Square = [new(0, 0), new(10, 0), new(10, 10), new(0, 10)];

    // U-Form: Die Aussparung (4..6, 0..7) liegt außerhalb.
    private static readonly MapPoint[] UShape =
    [
        new(0, 0), new(4, 0), new(4, 7), new(6, 7), new(6, 0), new(10, 0), new(10, 10), new(0, 10),
    ];

    [Theory]
    [InlineData(5, 5)]
    [InlineData(1, 9)]
    public void Contains_PointInside_IsTrue(int x, int y)
    {
        Assert.True(MapGeometry.Contains(Square, new MapPoint(x, y)));
    }

    [Theory]
    [InlineData(-1, 5)]
    [InlineData(11, 5)]
    [InlineData(5, -1)]
    [InlineData(5, 11)]
    [InlineData(20, 20)]
    public void Contains_PointOutside_IsFalse(int x, int y)
    {
        Assert.False(MapGeometry.Contains(Square, new MapPoint(x, y)));
    }

    [Theory]
    [InlineData(0, 5)]
    [InlineData(10, 5)]
    [InlineData(5, 0)]
    [InlineData(5, 10)]
    [InlineData(0, 0)]
    [InlineData(10, 10)]
    public void Contains_PointOnEdgeOrVertex_IsTrue(int x, int y)
    {
        Assert.True(MapGeometry.Contains(Square, new MapPoint(x, y)));
    }

    [Fact]
    public void Contains_NonConvexNotch_IsOutside()
    {
        Assert.False(MapGeometry.Contains(UShape, new MapPoint(5, 3)));
    }

    [Theory]
    [InlineData(2, 3)]
    [InlineData(8, 3)]
    [InlineData(5, 9)]
    public void Contains_NonConvexArms_AreInside(int x, int y)
    {
        Assert.True(MapGeometry.Contains(UShape, new MapPoint(x, y)));
    }

    [Fact]
    public void Contains_PointAtHeightOfVertex_CountsCrossingOnce()
    {
        // Der Strahl nach rechts trifft genau die Ecke (10, 5) eines Rauten-Polygons.
        MapPoint[] diamond = [new(5, 0), new(10, 5), new(5, 10), new(0, 5)];

        Assert.True(MapGeometry.Contains(diamond, new MapPoint(5, 5)));
        Assert.False(MapGeometry.Contains(diamond, new MapPoint(-3, 5)));
    }

    [Fact]
    public void Contains_LargeCoordinates_DoNotOverflow()
    {
        MapPoint[] large = [new(0, 0), new(1_000_000, 0), new(1_000_000, 1_000_000), new(0, 1_000_000)];

        Assert.True(MapGeometry.Contains(large, new MapPoint(999_999, 999_999)));
        Assert.False(MapGeometry.Contains(large, new MapPoint(1_000_001, 500_000)));
    }

    [Theory]
    [InlineData(5, 5, 0)]
    [InlineData(15, 5, 1)]
    [InlineData(29, 9, 2)]
    public void FindProvinceAt_ReturnsProvinceContainingPoint(int x, int y, int expectedId)
    {
        var data = TestData.Load();

        Assert.Equal(new ProvinceId(expectedId), MapGeometry.FindProvinceAt(data, new MapPoint(x, y)));
    }

    [Fact]
    public void FindProvinceAt_OutsideAllProvinces_ReturnsNull()
    {
        var data = TestData.Load();

        Assert.Null(MapGeometry.FindProvinceAt(data, new MapPoint(50, 50)));
    }

    [Fact]
    public void FindProvinceAt_OnSharedBorder_ReturnsLowerId()
    {
        var data = TestData.Load();

        // x = 10 ist die Grenze zwischen a (ID 0) und b (ID 1).
        Assert.Equal(new ProvinceId(0), MapGeometry.FindProvinceAt(data, new MapPoint(10, 5)));
    }
}
