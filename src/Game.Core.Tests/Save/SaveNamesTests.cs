using Game.Core.Save;

namespace Game.Core.Tests.Save;

public class SaveNamesTests
{
    [Theory]
    [InlineData("Spielstand 1")]
    [InlineData("Mein_Krieg-1914")]
    [InlineData("Großangriff Österreich")]
    [InlineData("a")]
    public void Validate_AcceptsAllowedNames(string name)
    {
        Assert.True(SaveNames.Validate(name).IsValid);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(" führend")]
    [InlineData("folgend ")]
    [InlineData("a/b")]
    [InlineData("a\\b")]
    [InlineData("..")]
    [InlineData("name.sav")]
    [InlineData("a:b")]
    [InlineData("CON")]
    [InlineData("lpt1")]
    public void Validate_RejectsInvalidNames(string? name)
    {
        var result = SaveNames.Validate(name);

        Assert.False(result.IsValid);
        Assert.False(string.IsNullOrEmpty(result.Reason));
    }

    [Fact]
    public void Validate_RejectsTooLongName()
    {
        Assert.True(SaveNames.Validate(new string('a', SaveNames.MaxLength)).IsValid);
        Assert.False(SaveNames.Validate(new string('a', SaveNames.MaxLength + 1)).IsValid);
    }

    [Fact]
    public void AutosaveNames_AreValidNames()
    {
        for (int slot = 1; slot <= SaveNames.AutosaveSlots; slot++)
        {
            Assert.True(SaveNames.Validate(SaveNames.AutosaveName(slot)).IsValid);
        }
    }

    [Fact]
    public void AutosaveInterval_IsTenMinutesAtDefaultSpeed()
    {
        // Testdaten: Standard "normal" mit 10 Ticks pro Sekunde.
        Assert.Equal(10 * 60 * 10, SaveNames.AutosaveIntervalTicks(TestData.Load()));
    }

    [Theory]
    [InlineData(0, false)]
    [InlineData(99, false)]
    [InlineData(100, true)]
    [InlineData(150, false)]
    [InlineData(300, true)]
    public void IsAutosaveTick_EveryIntervalButNotAtStart(long tick, bool expected)
    {
        Assert.Equal(expected, SaveNames.IsAutosaveTick(tick, intervalTicks: 100));
    }

    [Theory]
    [InlineData(100, 1)]
    [InlineData(200, 2)]
    [InlineData(300, 3)]
    [InlineData(400, 1)]
    [InlineData(700, 1)]
    [InlineData(800, 2)]
    public void AutosaveSlot_Rotates(long tick, int expectedSlot)
    {
        Assert.Equal(expectedSlot, SaveNames.AutosaveSlot(tick, intervalTicks: 100));
    }
}
