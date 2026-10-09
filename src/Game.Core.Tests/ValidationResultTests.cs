namespace Game.Core.Tests;

public class ValidationResultTests
{
    [Fact]
    public void Valid_HasNoReason()
    {
        Assert.True(ValidationResult.Valid.IsValid);
        Assert.Null(ValidationResult.Valid.Reason);
    }

    [Fact]
    public void Invalid_CarriesReason()
    {
        var result = ValidationResult.Invalid("Nicht genug Stahl.");

        Assert.False(result.IsValid);
        Assert.Equal("Nicht genug Stahl.", result.Reason);
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    public void Invalid_RequiresReason(string reason)
    {
        Assert.Throws<ArgumentException>(() => ValidationResult.Invalid(reason));
    }

    [Fact]
    public void Default_IsValid()
    {
        Assert.True(default(ValidationResult).IsValid);
    }
}
