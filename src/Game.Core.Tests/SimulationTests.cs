using Game.Core.Commands;
using Game.Core.Data;
using Game.Core.Determinism;
using Game.Core.Events;
using Game.Core.Serialization;
using Game.Core.State;
using static Game.Core.Tests.TestIds;

namespace Game.Core.Tests;

public class SimulationTests
{
    private readonly GameData _data = TestData.Load();

    [Fact]
    public void Step_WithoutCommands_IncrementsTickByOne()
    {
        var state = NewState();

        Simulation.Step(state, _data, []);
        Simulation.Step(state, _data, []);

        Assert.Equal(2, state.Tick);
    }

    [Fact]
    public void Step_AppliesValidCommand()
    {
        var state = NewState();

        var events = Simulation.Step(state, _data, [Envelope(state, Red, 0, new TransferProvinceCommand(A, Blue))]);

        Assert.Empty(events);
        Assert.Equal(Blue, state.GetProvince(A).Owner);
    }

    [Fact]
    public void Step_InvalidCommand_IsRejectedWithReasonAndChangesNothing()
    {
        var state = NewState();
        string hashBefore = HashWithoutTick(state);
        var envelope = Envelope(state, Red, 0, new TransferProvinceCommand(B, Red));

        var events = Simulation.Step(state, _data, [envelope]);

        var rejected = Assert.IsType<CommandRejected>(Assert.Single(events));
        Assert.Same(envelope, rejected.Envelope);
        Assert.Equal(0, rejected.Tick);
        Assert.Equal("Die Provinz gehört nicht der eigenen Nation.", rejected.Reason);
        Assert.Equal(hashBefore, HashWithoutTick(state));
        Assert.Equal(1, state.Tick);
    }

    [Fact]
    public void Step_UnknownIssuer_IsRejected()
    {
        var state = NewState();

        var events = Simulation.Step(state, _data, [Envelope(state, new NationId(99), 0, new TransferProvinceCommand(A, Blue))]);

        Assert.Equal("Unbekannte Nation.", Assert.IsType<CommandRejected>(Assert.Single(events)).Reason);
    }

    [Fact]
    public void Step_LaterIssuer_SeesEffectOfEarlierIssuer()
    {
        // Blau will a weitergeben, das Rot im selben Tick erst an Blau gibt.
        // Gültig nur, weil Rot (ID 0) vor Blau (ID 1) ausgeführt wird – unabhängig von der Eingabereihenfolge.
        var state = NewState();
        var blueGivesAway = Envelope(state, Blue, 0, new TransferProvinceCommand(A, Red));
        var redGivesToBlue = Envelope(state, Red, 1, new TransferProvinceCommand(A, Blue));

        var events = Simulation.Step(state, _data, [blueGivesAway, redGivesToBlue]);

        Assert.Empty(events);
        Assert.Equal(Red, state.GetProvince(A).Owner);
    }

    [Fact]
    public void Step_SameIssuer_IsOrderedBySequence()
    {
        // Sequenz 0 gibt b an Rot ab; Sequenz 1 versucht danach, b erneut zu vergeben, und scheitert.
        var state = NewState();
        var second = Envelope(state, Blue, 1, new TransferProvinceCommand(B, Blue));
        var first = Envelope(state, Blue, 0, new TransferProvinceCommand(B, Red));

        var events = Simulation.Step(state, _data, [second, first]);

        Assert.Equal(Red, state.GetProvince(B).Owner);
        Assert.Same(second, Assert.IsType<CommandRejected>(Assert.Single(events)).Envelope);
    }

    [Fact]
    public void Step_InputOrder_DoesNotChangeResult()
    {
        var forward = NewState();
        var backward = NewState();
        CommandEnvelope[] commands =
        [
            Envelope(forward, Blue, 0, new TransferProvinceCommand(C, Red)),
            Envelope(forward, Red, 1, new TransferProvinceCommand(A, Blue)),
            Envelope(forward, Blue, 2, new TransferProvinceCommand(A, Red)),
            Envelope(forward, Red, 3, new TransferProvinceCommand(B, Blue)),
        ];

        var forwardEvents = Simulation.Step(forward, _data, commands);
        var backwardEvents = Simulation.Step(backward, _data, commands.Reverse().ToArray());

        Assert.Equal(StateHasher.ComputeHash(forward), StateHasher.ComputeHash(backward));
        Assert.Equal(forwardEvents, backwardEvents);
    }

    [Fact]
    public void Step_CommandForOtherTick_ThrowsAndAppliesNothing()
    {
        var state = NewState();
        var valid = Envelope(state, Red, 0, new TransferProvinceCommand(A, Blue));
        var misplaced = new CommandEnvelope(5, Red, 1, new TransferProvinceCommand(A, Blue));

        Assert.Throws<ArgumentException>(() => Simulation.Step(state, _data, [valid, misplaced]));
        Assert.Equal(Red, state.GetProvince(A).Owner);
        Assert.Equal(0, state.Tick);
    }

    [Fact]
    public void Validate_CanBeCalledWithoutChangingState()
    {
        var state = NewState();
        string hashBefore = StateHasher.ComputeHash(state);

        var valid = new TransferProvinceCommand(A, Blue).Validate(state, _data, Red);
        var invalid = new TransferProvinceCommand(new ProvinceId(42), Blue).Validate(state, _data, Red);

        Assert.True(valid.IsValid);
        Assert.Equal("Unbekannte Provinz.", invalid.Reason);
        Assert.Equal(hashBefore, StateHasher.ComputeHash(state));
    }

    [Fact]
    public void TwoRuns_WithSameSeedAndCommands_ProduceSameHash()
    {
        var first = Run(NewState(42), 500);
        var second = Run(NewState(42), 500);

        Assert.Equal(StateHasher.ComputeHash(first), StateHasher.ComputeHash(second));
    }

    [Fact]
    public void Runs_WithDifferentSeeds_ProduceDifferentHashes()
    {
        Assert.NotEqual(StateHasher.ComputeHash(Run(NewState(1), 100)), StateHasher.ComputeHash(Run(NewState(2), 100)));
    }

    [Fact]
    public void SaveAndLoadMidRun_ProducesSameHashAsContinuousRun()
    {
        // Kerntest aus docs/architecture.md, Abschnitt 8: N Ticks → speichern → laden → M Ticks ≡ N+M Ticks.
        // Das Command-Log wirkt vor und nach dem Speicherzeitpunkt.
        const int ticksBeforeSave = 300;
        const int ticksAfterLoad = 200;

        var continuous = Run(NewState(42), ticksBeforeSave + ticksAfterLoad);

        var beforeSave = Run(NewState(42), ticksBeforeSave);
        var loaded = GameStateSerializer.Deserialize(GameStateSerializer.SerializeToUtf8Bytes(beforeSave));
        var resumed = Run(loaded, ticksAfterLoad);

        Assert.Equal(continuous.Tick, resumed.Tick);
        Assert.Equal(StateHasher.ComputeHash(continuous), StateHasher.ComputeHash(resumed));
    }

    private GameState NewState(ulong seed = 1) => GameStateFactory.CreateNew(_data, seed);

    private static CommandEnvelope Envelope(GameState state, NationId issuer, long sequence, Command command) =>
        new(state.Tick, issuer, sequence, command);

    // Bis auf den Tick identisch? Für Tests, die prüfen, dass ein abgelehnter Command nichts verändert.
    private static string HashWithoutTick(GameState state)
    {
        var copy = GameStateSerializer.Deserialize(GameStateSerializer.SerializeToUtf8Bytes(state));
        copy.Tick = 0;
        return StateHasher.ComputeHash(copy);
    }

    // Festes Command-Log abhängig vom Tick: Provinz a wandert regelmäßig zwischen Rot und Blau hin und her.
    // Zusätzlich wird pro Tick eine Zufallszahl gezogen, wie es spätere Systeme tun werden.
    private GameState Run(GameState state, int ticks)
    {
        for (int i = 0; i < ticks; i++)
        {
            CommandEnvelope[] commands = state.Tick % 7 == 0
                ? [Envelope(state, state.GetProvince(A).Owner, state.Tick, new TransferProvinceCommand(A, OtherNation(state)))]
                : [];

            Simulation.Step(state, _data, commands);
            state.Rng.NextUInt();
        }

        return state;
    }

    private static NationId OtherNation(GameState state) => state.GetProvince(A).Owner == Red ? Blue : Red;
}
