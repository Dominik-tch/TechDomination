using Game.Core.Commands;
using Game.Core.Data;
using Game.Core.Events;
using Game.Core.Map;
using Game.Core.Serialization;
using Game.Core.State;
using static Game.Core.Tests.TestIds;

namespace Game.Core.Tests.Military;

/// <summary>
/// Zusammenführen und Aufteilen auf der Testkarte: Städte a, b, c, Pfade je 10 000 Tausendstel.
/// Rote Start-Armee (ID 0): 2 Infanterie in a (1000 pro Tick).
/// </summary>
public class MergeAndSplitTests
{
    private readonly GameData _data = TestData.Load();

    [Fact]
    public void ArrivingArmy_MergesWithOwnStandingArmy()
    {
        var state = NewState();
        var other = state.CreateArmy(Red, PathPosition.AtCity(B), [1, 1, 0]);
        Execute(state, Red, new MoveArmyCommand(RedArmy, PathPosition.AtCity(B)));

        var events = Step(state, 9);

        Assert.Null(state.FindArmy(RedArmy));
        Assert.Equal([3, 1, 0], state.FindArmy(other.Id)!.Units);
        Assert.Equal(new ArmiesMerged(9, RedArmy, other.Id), Assert.IsType<ArmiesMerged>(Assert.Single(events)));
    }

    [Fact]
    public void ArrivingArmy_MergesAtPointInMiddleOfPath_InEitherDirection()
    {
        // Stehende Armee bei 4000 ab b Richtung a; die ankommende zielt auf denselben Punkt, beschrieben ab a.
        var state = NewState();
        var other = state.CreateArmy(Red, new PathPosition(B, A, 4000), [1, 0, 0]);
        Execute(state, Red, new MoveArmyCommand(RedArmy, new PathPosition(A, B, 6000)));

        Step(state, 5);

        Assert.Null(state.FindArmy(RedArmy));
        Assert.Equal([3, 0, 0], state.FindArmy(other.Id)!.Units);
    }

    [Fact]
    public void ArmyPassingThrough_DoesNotMerge()
    {
        var state = NewState();
        state.CreateArmy(Red, PathPosition.AtCity(B), [1, 0, 0]);
        Execute(state, Red, new MoveArmyCommand(RedArmy, PathPosition.AtCity(C)));

        Step(state, 12);

        Assert.NotNull(state.FindArmy(RedArmy));
        Assert.True(state.FindArmy(RedArmy)!.IsMoving);
    }

    [Fact]
    public void ArrivingArmy_DoesNotMergeWithForeignArmy()
    {
        var state = NewState();
        state.CreateArmy(Blue, PathPosition.AtCity(B), [1, 0, 0]);
        Execute(state, Red, new MoveArmyCommand(RedArmy, PathPosition.AtCity(B)));

        Step(state, 9);

        Assert.NotNull(state.FindArmy(RedArmy));
        Assert.Equal(3, state.Armies.Count);
    }

    [Fact]
    public void ArrivingArmy_DoesNotMergeWithArmyThatIsStillMoving()
    {
        var state = NewState();
        var other = state.CreateArmy(Red, PathPosition.AtCity(B), [1, 0, 0]);
        other.SetRoute(PathPosition.AtCity(B), [new MoveLeg(B, C, 10_000)]);
        Execute(state, Red, new MoveArmyCommand(RedArmy, PathPosition.AtCity(B)));

        Step(state, 3);

        Assert.NotNull(state.FindArmy(RedArmy));
    }

    [Fact]
    public void Split_CreatesNewArmyAtSamePointAndSendsItToTarget()
    {
        var state = NewState();
        state.FindArmy(RedArmy)!.AddUnits([3, 1, 0]);

        Execute(state, Red, new SplitArmyCommand(RedArmy, [2, 1, 0], PathPosition.AtCity(B)));

        var original = state.FindArmy(RedArmy)!;
        var split = state.Armies[^1];
        Assert.Equal([3, 0, 0], original.Units);
        Assert.False(original.IsMoving);
        Assert.Equal([2, 1, 0], split.Units);
        Assert.Equal(Red, split.Owner);
        Assert.Equal(new PathPosition(A, B, 1000), split.Position);
        Assert.True(split.IsMoving);
    }

    [Fact]
    public void Split_WhileMoving_KeepsOriginalMarch()
    {
        var state = NewState();
        Execute(state, Red, new MoveArmyCommand(RedArmy, PathPosition.AtCity(C)));
        Step(state, 2);

        Execute(state, Red, new SplitArmyCommand(RedArmy, [1, 0, 0], PathPosition.AtCity(A)));
        Step(state, 3);

        Assert.Equal(new PathPosition(A, B, 7000), state.FindArmy(RedArmy)!.Position);
        Assert.Equal(PathPosition.AtCity(A), state.Armies[^1].Position);
    }

    [Fact]
    public void Split_ThenReturningToOriginal_MergesBack()
    {
        var state = NewState();
        Execute(state, Red, new SplitArmyCommand(RedArmy, [1, 0, 0], new PathPosition(A, B, 3000)));
        var split = state.Armies[^1];
        Step(state, 2);

        Execute(state, Red, new MoveArmyCommand(split.Id, PathPosition.AtCity(A)));
        Step(state, 2);

        Assert.Null(state.FindArmy(split.Id));
        Assert.Equal([2, 0, 0], state.FindArmy(RedArmy)!.Units);
    }

    [Theory]
    [InlineData(new[] { 0, 0, 0 }, "Mindestens eine Einheit muss in die neue Armee.")]
    [InlineData(new[] { 2, 0, 0 }, "Mindestens eine Einheit muss in der bisherigen Armee bleiben.")]
    [InlineData(new[] { 3, 0, 0 }, "Ungültige Aufteilung.")]
    [InlineData(new[] { -1, 0, 0 }, "Ungültige Aufteilung.")]
    [InlineData(new[] { 1, 0 }, "Ungültige Aufteilung.")]
    public void Split_InvalidCounts_AreRejected(int[] units, string reason)
    {
        Assert.Equal(reason, new SplitArmyCommand(RedArmy, units, PathPosition.AtCity(B)).Validate(NewState(), _data, Red).Reason);
    }

    [Fact]
    public void Split_ToOwnPosition_IsRejected()
    {
        Assert.Equal(
            "Die Armee ist bereits dort.",
            new SplitArmyCommand(RedArmy, [1, 0, 0], PathPosition.AtCity(A)).Validate(NewState(), _data, Red).Reason);
    }

    [Fact]
    public void Split_ForeignArmy_IsRejected()
    {
        Assert.Equal(
            "Die Armee gehört nicht dir.",
            new SplitArmyCommand(BlueArmy, [0, 1, 0], PathPosition.AtCity(B)).Validate(NewState(), _data, Red).Reason);
    }

    [Fact]
    public void SplitCommand_SurvivesSerialization()
    {
        var command = new SplitArmyCommand(RedArmy, [1, 0, 0], new PathPosition(A, B, 3000));
        var envelope = new CommandEnvelope(0, Red, 0, command);

        var json = System.Text.Json.JsonSerializer.Serialize(envelope, CoreJson.Options);
        var loaded = (SplitArmyCommand)System.Text.Json.JsonSerializer.Deserialize<CommandEnvelope>(json, CoreJson.Options)!.Command;

        Assert.Equal(command.Army, loaded.Army);
        Assert.Equal(command.Units, loaded.Units);
        Assert.Equal(command.Target, loaded.Target);
    }

    private GameState NewState() => GameStateFactory.CreateNew(_data, seed: 1);

    private void Execute(GameState state, NationId issuer, Command command)
    {
        var events = Simulation.Step(state, _data, [new CommandEnvelope(state.Tick, issuer, 0, command)]);
        Assert.DoesNotContain(events, e => e is CommandRejected);
    }

    private List<GameEvent> Step(GameState state, int ticks)
    {
        var events = new List<GameEvent>();
        for (int i = 0; i < ticks; i++)
        {
            events.AddRange(Simulation.Step(state, _data, []));
        }

        return events;
    }
}
