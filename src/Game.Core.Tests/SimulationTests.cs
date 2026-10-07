using Game.Core.Data;
using Game.Core.Determinism;
using Game.Core.Serialization;
using Game.Core.State;

namespace Game.Core.Tests;

public class SimulationTests
{
    private readonly GameData _data = TestData.Load();

    [Fact]
    public void NewState_StartsAtTickZero()
    {
        var state = GameStateFactory.CreateNew(1);

        Assert.Equal(0, state.Tick);
        Assert.Equal(1UL, state.Seed);
    }

    [Fact]
    public void Step_IncrementsTickByOne()
    {
        var state = GameStateFactory.CreateNew(1);

        Simulation.Step(state, _data);
        Simulation.Step(state, _data);

        Assert.Equal(2, state.Tick);
    }

    [Fact]
    public void TwoRuns_WithSameSeed_ProduceSameHash()
    {
        var first = Run(GameStateFactory.CreateNew(42), 500);
        var second = Run(GameStateFactory.CreateNew(42), 500);

        Assert.Equal(StateHasher.ComputeHash(first), StateHasher.ComputeHash(second));
    }

    [Fact]
    public void Runs_WithDifferentSeeds_ProduceDifferentHashes()
    {
        var first = Run(GameStateFactory.CreateNew(1), 100);
        var second = Run(GameStateFactory.CreateNew(2), 100);

        Assert.NotEqual(StateHasher.ComputeHash(first), StateHasher.ComputeHash(second));
    }

    [Fact]
    public void SaveAndLoadMidRun_ProducesSameHashAsContinuousRun()
    {
        // Kerntest aus docs/architecture.md, Abschnitt 8: N Ticks → speichern → laden → M Ticks ≡ N+M Ticks.
        const int ticksBeforeSave = 300;
        const int ticksAfterLoad = 200;

        var continuous = Run(GameStateFactory.CreateNew(42), ticksBeforeSave + ticksAfterLoad);

        var beforeSave = Run(GameStateFactory.CreateNew(42), ticksBeforeSave);
        var loaded = GameStateSerializer.Deserialize(GameStateSerializer.SerializeToUtf8Bytes(beforeSave));
        var resumed = Run(loaded, ticksAfterLoad);

        Assert.Equal(continuous.Tick, resumed.Tick);
        Assert.Equal(StateHasher.ComputeHash(continuous), StateHasher.ComputeHash(resumed));
    }

    // Zieht pro Tick eine Zufallszahl, wie es spätere Systeme tun werden,
    // damit die Tests auch die Wiederherstellung des RNG-Zustands prüfen.
    private GameState Run(GameState state, int ticks)
    {
        for (int i = 0; i < ticks; i++)
        {
            Simulation.Step(state, _data);
            state.Rng.NextUInt();
        }

        return state;
    }
}
