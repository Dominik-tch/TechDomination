using System.Text.Json.Nodes;
using Game.Core.Commands;
using Game.Core.Data;
using Game.Core.Determinism;
using Game.Core.Events;
using Game.Core.Map;
using Game.Core.Serialization;
using Game.Core.State;
using static Game.Core.Tests.TestIds;

namespace Game.Core.Tests.Military;

/// <summary>
/// Ausbildung auf der Testkarte: Ein Tag = 50 Ticks; jede Provinz bekommt dann 1 Infanterie (mit Kaserne 2).
/// Kavallerie per Auftrag: 5 Ticks, 10 Geld + 1 Schiene, braucht Kaserne. Artillerie: 3 Ticks, 5 Geld.
/// Startbestand: 100 Geld. Start-Armeen: Rot 2 Infanterie in a, Blau 1 Kavallerie in c.
/// </summary>
public class TrainingTests
{
    private readonly GameData _data = TestData.Load();

    [Fact]
    public void DayChange_AddsInfantryToStandingArmyInCity()
    {
        var state = NewState();

        var events = Step(state, 50);

        Assert.Equal([3, 0, 0], state.FindArmy(RedArmy)!.Units);
        Assert.Contains(new UnitsTrained(49, A, Infantry, 1, Red, Automatic: true), events);
    }

    [Fact]
    public void DayChange_CreatesNewArmyWhereNoneStands()
    {
        var state = NewState();

        Step(state, 50);

        var inB = state.Armies.Single(a => a.Position == PathPosition.AtCity(B));
        Assert.Equal(Blue, inB.Owner);
        Assert.Equal([1, 0, 0], inB.Units);
    }

    [Fact]
    public void DayChange_WithBarracks_AddsTwoInfantry()
    {
        var state = NewState();
        state.GetProvince(A).SetBuildingLevel(Barracks, 1);

        Step(state, 50);

        Assert.Equal([4, 0, 0], state.FindArmy(RedArmy)!.Units);
    }

    [Fact]
    public void DayChange_DoesNotJoinMarchingArmy()
    {
        var state = NewState();
        Execute(state, Red, new MoveArmyCommand(RedArmy, PathPosition.AtCity(C)));

        Step(state, 49);

        Assert.Equal([2, 0, 0], state.FindArmy(RedArmy)!.Units);
        Assert.Contains(state.Armies, a => a.Owner == Red && a.Position == PathPosition.AtCity(A));
    }

    [Fact]
    public void NoInfantryBetweenDays()
    {
        var state = NewState();

        Step(state, 49);

        Assert.Equal([2, 0, 0], state.FindArmy(RedArmy)!.Units);
    }

    [Fact]
    public void Train_PaysImmediatelyAndQueuesUnits()
    {
        var state = NewState();

        Execute(state, Red, new TrainUnitsCommand(A, Artillery, 2));

        var province = state.GetProvince(A);
        Assert.Equal([Artillery, Artillery], province.TrainingQueue);
        Assert.Equal(2, province.TrainingRemainingTicks);
        Assert.Equal(100_00 - 2 * 5_00, state.GetNation(Red).Money);
    }

    [Fact]
    public void Train_UnitsAreTrainedOneAfterAnother()
    {
        var state = NewState();
        Execute(state, Red, new TrainUnitsCommand(A, Artillery, 2));

        var events = Step(state, 2);
        Assert.Equal([2, 0, 1], state.FindArmy(RedArmy)!.Units);
        Assert.Contains(new UnitsTrained(2, A, Artillery, 1, Red, Automatic: false), events);

        Step(state, 2);
        Assert.Equal([2, 0, 1], state.FindArmy(RedArmy)!.Units);

        Step(state, 1);
        Assert.Equal([2, 0, 2], state.FindArmy(RedArmy)!.Units);
        Assert.Empty(state.GetProvince(A).TrainingQueue);
        Assert.Equal(0, state.GetProvince(A).TrainingRemainingTicks);
    }

    [Fact]
    public void Train_DifferentTypes_UseTheirOwnTrainingTime()
    {
        var state = NewState();
        state.GetProvince(A).SetBuildingLevel(Barracks, 1);
        state.GetNation(Red).AddResource(Rails, 5000);
        Execute(state, Red, new TrainUnitsCommand(A, Artillery, 1));
        Execute(state, Red, new TrainUnitsCommand(A, Cavalry, 1));

        Step(state, 1);
        Assert.Equal(5, state.GetProvince(A).TrainingRemainingTicks);

        Step(state, 5);
        Assert.Equal([2, 1, 1], state.FindArmy(RedArmy)!.Units);
    }

    [Fact]
    public void Train_ForeignProvince_IsInvalid()
    {
        Assert.Equal("Die Provinz gehört nicht dir.", Validate(new TrainUnitsCommand(B, Artillery, 1), Red));
    }

    [Fact]
    public void Train_WithoutRequiredBuilding_IsInvalid()
    {
        Assert.Equal("Benötigt: Kaserne.", Validate(new TrainUnitsCommand(A, Cavalry, 1), Red));
    }

    [Fact]
    public void Train_Infantry_IsAutomaticOnly()
    {
        Assert.Equal("Infanterie wird automatisch zum Tageswechsel ausgebildet.", Validate(new TrainUnitsCommand(A, Infantry, 1), Red));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(TrainUnitsCommand.MaxCount + 1)]
    public void Train_CountOutOfRange_IsInvalid(int count)
    {
        Assert.StartsWith("Die Anzahl muss", Validate(new TrainUnitsCommand(A, Artillery, count), Red));
    }

    [Fact]
    public void Train_NotEnoughForAllUnits_NamesTotal()
    {
        Assert.Equal("Nicht genug Geld (benötigt 105, vorhanden 100).", Validate(new TrainUnitsCommand(A, Artillery, 21), Red));
    }

    [Fact]
    public void CancelCurrentTraining_RefundsAndStartsNext()
    {
        var state = NewState();
        Execute(state, Red, new TrainUnitsCommand(A, Artillery, 2));
        Step(state, 1);

        Execute(state, Red, new CancelTrainingCommand(A, 0));

        Assert.Equal([Artillery], state.GetProvince(A).TrainingQueue);
        Assert.Equal(100_00 - 5_00, state.GetNation(Red).Money);
        Assert.Equal(2, state.GetProvince(A).TrainingRemainingTicks);
    }

    [Fact]
    public void CancelWaitingTraining_KeepsCurrentTimer()
    {
        var state = NewState();
        Execute(state, Red, new TrainUnitsCommand(A, Artillery, 2));
        Step(state, 1);

        // Der Abbruch der wartenden Einheit läuft im selben Tick, in dem die erste fertig wird – ihr Zeitplan bleibt.
        Execute(state, Red, new CancelTrainingCommand(A, 1));

        Assert.Empty(state.GetProvince(A).TrainingQueue);
        Assert.Equal([2, 0, 1], state.FindArmy(RedArmy)!.Units);
        Assert.Equal(100_00 - 5_00, state.GetNation(Red).Money);
    }

    [Fact]
    public void Cancel_InvalidIndex_IsInvalid()
    {
        Assert.Equal("Kein Ausbildungsauftrag an dieser Stelle.", Validate(new CancelTrainingCommand(A, 0), Red));
    }

    [Fact]
    public void TrainingQueue_SurvivesSaveAndLoad()
    {
        var continuous = NewState();
        var interrupted = NewState();
        foreach (var state in new[] { continuous, interrupted })
        {
            Execute(state, Red, new TrainUnitsCommand(A, Artillery, 3));
            Step(state, 4);
        }

        var loaded = GameStateSerializer.Deserialize(GameStateSerializer.SerializeToUtf8Bytes(interrupted));
        Step(continuous, 60);
        Step(loaded, 60);

        Assert.Equal(StateHasher.ComputeHash(continuous), StateHasher.ComputeHash(loaded));
    }

    [Fact]
    public void Units_RequiringUnknownBuilding_FailToLoad()
    {
        var units = TestData.Units();
        units["units"]![1]!["requires"] = "castle";

        var error = Assert.Throws<GameDataException>(() => GameDataLoader.Load(TestData.Files(units: units)));

        Assert.Contains("unbekanntes Gebäude 'castle'", error.Message);
    }

    [Fact]
    public void Units_NeitherAutomaticNorTrainable_FailToLoad()
    {
        var units = TestData.Units();
        units["units"]![2]!.AsObject().Remove("trainingTicks");

        var error = Assert.Throws<GameDataException>(() => GameDataLoader.Load(TestData.Files(units: units)));

        Assert.Contains("'dailyPerProvince'", error.Message);
    }

    private GameState NewState() => GameStateFactory.CreateNew(_data, seed: 1);

    private string? Validate(Command command, NationId issuer) => command.Validate(NewState(), _data, issuer).Reason;

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
