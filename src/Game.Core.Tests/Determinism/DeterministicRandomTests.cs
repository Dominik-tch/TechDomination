using Game.Core.Determinism;

namespace Game.Core.Tests.Determinism;

public class DeterministicRandomTests
{
    [Fact]
    public void FromSeed_MatchesPcg32ReferenceOutput()
    {
        // Referenzwerte aus der PCG32-Demo (pcg32_srandom_r mit initstate 42, initseq 54).
        var random = DeterministicRandom.FromSeed(seed: 42, stream: 54);

        uint[] expected = [0xa15c02b7, 0x7b47f409, 0xba1d3330, 0x83d2f293, 0xbfa4784b, 0xcbed606e];
        uint[] actual = expected.Select(_ => random.NextUInt()).ToArray();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void SameSeed_ProducesSameSequence()
    {
        var first = DeterministicRandom.FromSeed(1234);
        var second = DeterministicRandom.FromSeed(1234);

        Assert.Equal(Draw(first, 100), Draw(second, 100));
    }

    [Fact]
    public void DifferentSeeds_ProduceDifferentSequences()
    {
        var first = DeterministicRandom.FromSeed(1);
        var second = DeterministicRandom.FromSeed(2);

        Assert.NotEqual(Draw(first, 10), Draw(second, 10));
    }

    [Fact]
    public void DifferentStreams_ProduceDifferentSequences()
    {
        var first = DeterministicRandom.FromSeed(1, stream: 1);
        var second = DeterministicRandom.FromSeed(1, stream: 2);

        Assert.NotEqual(Draw(first, 10), Draw(second, 10));
    }

    [Fact]
    public void RestoredState_ContinuesIdentically()
    {
        var original = DeterministicRandom.FromSeed(99);
        Draw(original, 17);

        var restored = new DeterministicRandom(original.State, original.Increment);

        Assert.Equal(Draw(original, 50), Draw(restored, 50));
    }

    [Fact]
    public void Constructor_RejectsEvenIncrement()
    {
        Assert.Throws<ArgumentException>(() => new DeterministicRandom(0, 2));
    }

    [Theory]
    [InlineData(1)]
    [InlineData(7)]
    [InlineData(int.MaxValue)]
    public void NextInt_StaysBelowMax(int maxExclusive)
    {
        var random = DeterministicRandom.FromSeed(5);

        for (int i = 0; i < 1000; i++)
        {
            int value = random.NextInt(maxExclusive);
            Assert.InRange(value, 0, maxExclusive - 1);
        }
    }

    [Theory]
    [InlineData(-5, 5)]
    [InlineData(10, 11)]
    [InlineData(int.MinValue, int.MaxValue)]
    public void NextInt_WithMin_StaysInRange(int minInclusive, int maxExclusive)
    {
        var random = DeterministicRandom.FromSeed(5);

        for (int i = 0; i < 1000; i++)
        {
            int value = random.NextInt(minInclusive, maxExclusive);
            Assert.InRange(value, minInclusive, maxExclusive - 1);
        }
    }

    [Fact]
    public void NextInt_HitsEveryValueOfSmallRange()
    {
        var random = DeterministicRandom.FromSeed(5);

        var seen = Enumerable.Range(0, 1000).Select(_ => random.NextInt(6)).ToHashSet();

        Assert.Equal(6, seen.Count);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void NextInt_RejectsNonPositiveMax(int maxExclusive)
    {
        var random = DeterministicRandom.FromSeed(5);

        Assert.Throws<ArgumentOutOfRangeException>(() => random.NextInt(maxExclusive));
    }

    [Fact]
    public void NextInt_RejectsEmptyRange()
    {
        var random = DeterministicRandom.FromSeed(5);

        Assert.Throws<ArgumentOutOfRangeException>(() => random.NextInt(3, 3));
    }

    private static uint[] Draw(DeterministicRandom random, int count) =>
        Enumerable.Range(0, count).Select(_ => random.NextUInt()).ToArray();
}
