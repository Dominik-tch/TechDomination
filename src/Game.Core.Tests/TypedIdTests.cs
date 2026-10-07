using System.Text.Json;

namespace Game.Core.Tests;

public class TypedIdTests
{
    [Fact]
    public void ProvinceId_SerializesAsPlainNumber()
    {
        Assert.Equal("7", JsonSerializer.Serialize(new ProvinceId(7)));
    }

    [Fact]
    public void TypedIds_RoundTrip()
    {
        Assert.Equal(new ProvinceId(3), JsonSerializer.Deserialize<ProvinceId>("3"));
        Assert.Equal(new NationId(4), JsonSerializer.Deserialize<NationId>("4"));
        Assert.Equal(new ResourceId(5), JsonSerializer.Deserialize<ResourceId>("5"));
    }

    [Fact]
    public void TypedIds_WithSameValue_AreEqual()
    {
        Assert.Equal(new NationId(1), new NationId(1));
        Assert.NotEqual(new NationId(1), new NationId(2));
    }
}
