using Game.Core.Commands;
using Game.Core.Data;
using Game.Core.Determinism;
using Game.Core.Map;
using Game.Core.Military;
using Game.Core.Serialization;
using Game.Core.State;
using static Game.Core.Tests.TestIds;

namespace Game.Core.Tests.Military;

/// <summary>
/// Bewegung auf der Testkarte: Städte a (5,5), b (15,5), c (25,5), Pfade je 10 000 Tausendstel.
/// Rote Armee: 2 Infanterie in a (1000 pro Tick). Blaue Armee: 1 Kavallerie in c (2500 pro Tick).
/// </summary>
public class MovementTests
{
    private readonly GameData _data = TestData.Load();

    [Fact]
    public void Infantry_ReachesNeighborCityAfterExactlyTenTicks()
    {
        var state = NewState();
        Execute(state, Red, new MoveArmyCommand(RedArmy, PathPosition.AtCity(B)));
        Assert.Equal(new PathPosition(A, B, 1000), Army(state, RedArmy).Position);

        Step(state, 8);
        Assert.Equal(new PathPosition(A, B, 9000), Army(state, RedArmy).Position);
        Assert.True(Army(state, RedArmy).IsMoving);

        Step(state, 1);
        Assert.Equal(PathPosition.AtCity(B), Army(state, RedArmy).Position);
        Assert.False(Army(state, RedArmy).IsMoving);
    }

    [Fact]
    public void Movement_CarriesRemainingSpeedIntoNextLeg()
    {
        // Kavallerie (2500/Tick) von c nach a: 20 000 Tausendstel über b, ohne Halt in b → genau 8 Ticks.
        var state = NewState();
        Execute(state, Blue, new MoveArmyCommand(BlueArmy, PathPosition.AtCity(A)));
        Step(state, 3);
        Assert.Equal(PathPosition.AtCity(B), Army(state, BlueArmy).Position);
        Assert.True(Army(state, BlueArmy).IsMoving);

        Step(state, 4);

        Assert.Equal(PathPosition.AtCity(A), Army(state, BlueArmy).Position);
    }

    [Fact]
    public void Movement_LeftoverSpeedContinuesOnNextPath()
    {
        // Infanterie (1000/Tick) mit Ziel 500 hinter b: Der Tick, der b erreicht, geht mit dem Rest weiter.
        var state = NewState();
        var army = Army(state, RedArmy);
        army.SetRoute(new PathPosition(A, B, 9500), [new MoveLeg(A, B, 10_000), new MoveLeg(B, C, 2000)]);

        Step(state, 1);

        Assert.Equal(new PathPosition(B, C, 500), army.Position);
    }

    [Fact]
    public void Movement_StopsAtPointInMiddleOfPath()
    {
        var state = NewState();
        Execute(state, Red, new MoveArmyCommand(RedArmy, new PathPosition(A, B, 3500)));

        Step(state, 10);

        Assert.Equal(new PathPosition(A, B, 3500), Army(state, RedArmy).Position);
        Assert.False(Army(state, RedArmy).IsMoving);
    }

    [Fact]
    public void SlowestUnit_DeterminesSpeed()
    {
        var state = NewState();
        AddArmy(state, Red, A, infantry: 1, cavalry: 0, artillery: 1);

        Assert.Equal(500, ArmyRules.Speed(_data, state.Armies[^1]));
        Assert.Equal(1000, ArmyRules.Speed(_data, Army(state, RedArmy)));
    }

    [Fact]
    public void Halt_StopsInMiddleOfPath()
    {
        var state = NewState();
        Execute(state, Red, new MoveArmyCommand(RedArmy, PathPosition.AtCity(B)));
        Step(state, 3);

        Execute(state, Red, new HaltArmyCommand(RedArmy));
        Step(state, 5);

        Assert.Equal(new PathPosition(A, B, 4000), Army(state, RedArmy).Position);
        Assert.False(Army(state, RedArmy).IsMoving);
    }

    [Fact]
    public void NewOrder_TurnsAroundFromMiddleOfPath()
    {
        var state = NewState();
        Execute(state, Red, new MoveArmyCommand(RedArmy, PathPosition.AtCity(B)));
        Step(state, 3);

        Execute(state, Red, new MoveArmyCommand(RedArmy, PathPosition.AtCity(A)));
        Assert.Equal(new PathPosition(B, A, 7000), Army(state, RedArmy).Position);
        Step(state, 3);

        Assert.Equal(PathPosition.AtCity(A), Army(state, RedArmy).Position);
    }

    [Fact]
    public void RemainingTicks_RoundsUp()
    {
        var state = NewState();
        Execute(state, Red, new MoveArmyCommand(RedArmy, new PathPosition(A, B, 4500)));

        // 1000 schon im ersten Tick zurückgelegt, 3500 verbleiben → 4 Ticks.
        Assert.Equal(3500, ArmyRules.RemainingDistance(Army(state, RedArmy)));
        Assert.Equal(4, ArmyRules.RemainingTicks(_data, Army(state, RedArmy)));
    }

    [Fact]
    public void ProvinceOf_FollowsPositionAcrossBorder()
    {
        var state = NewState();
        var army = Army(state, RedArmy);

        army.Position = new PathPosition(A, B, 4000);
        Assert.Equal(A, ArmyRules.ProvinceOf(_data, army));

        army.Position = new PathPosition(A, B, 6000);
        Assert.Equal(B, ArmyRules.ProvinceOf(_data, army));
    }

    [Fact]
    public void Validate_ForeignArmy_IsInvalid()
    {
        Assert.Equal("Die Armee gehört nicht dir.", Validate(new MoveArmyCommand(BlueArmy, PathPosition.AtCity(B)), Red));
    }

    [Fact]
    public void Validate_UnknownArmy_IsInvalid()
    {
        Assert.Equal("Unbekannte Armee.", Validate(new MoveArmyCommand(new ArmyId(99), PathPosition.AtCity(B)), Red));
    }

    [Fact]
    public void Validate_AlreadyThere_IsInvalid()
    {
        Assert.Equal("Die Armee ist bereits dort.", Validate(new MoveArmyCommand(RedArmy, new PathPosition(A, B, 0)), Red));
    }

    [Fact]
    public void Validate_InvalidTarget_IsInvalid()
    {
        Assert.Equal("Ungültiges Ziel.", Validate(new MoveArmyCommand(RedArmy, new PathPosition(A, C, 10)), Red));
    }

    [Fact]
    public void Validate_HaltWhileStanding_IsInvalid()
    {
        Assert.Equal("Die Armee marschiert nicht.", Validate(new HaltArmyCommand(RedArmy), Red));
    }

    [Fact]
    public void Validate_UnreachableTarget_IsInvalid()
    {
        var map = TestData.Map();
        map["provinces"]!.AsArray().Add(System.Text.Json.Nodes.JsonNode.Parse("""
            { "id": "island", "name": "Insel", "resource": "fish", "owner": "red",
              "outline": [[50, 50], [60, 50], [60, 60], [50, 60]], "label": [55, 55], "city": [55, 55] }
            """));
        var data = GameDataLoader.Load(TestData.Files(map: map));
        var state = GameStateFactory.CreateNew(data, seed: 1);

        var reason = new MoveArmyCommand(RedArmy, PathPosition.AtCity(new ProvinceId(3))).Validate(state, data, Red).Reason;

        Assert.Equal("Das Ziel ist auf dem Landweg nicht erreichbar.", reason);
    }

    [Fact]
    public void MarchingArmies_SurviveSaveAndLoadWithSameResult()
    {
        var continuous = NewState();
        var interrupted = NewState();
        foreach (var state in new[] { continuous, interrupted })
        {
            Execute(state, Red, new MoveArmyCommand(RedArmy, new PathPosition(B, C, 7000)));
            Execute(state, Blue, new MoveArmyCommand(BlueArmy, PathPosition.AtCity(A)));
            Step(state, 5);
        }

        var loaded = GameStateSerializer.Deserialize(GameStateSerializer.SerializeToUtf8Bytes(interrupted));
        Step(continuous, 12);
        Step(loaded, 12);

        Assert.Equal(StateHasher.ComputeHash(continuous), StateHasher.ComputeHash(loaded));
        Assert.Equal(new PathPosition(B, C, 7000), Army(loaded, RedArmy).Position);
    }

    private GameState NewState() => GameStateFactory.CreateNew(_data, seed: 1);

    private static ArmyState Army(GameState state, ArmyId id) => state.FindArmy(id)!;

    private static void AddArmy(GameState state, NationId owner, ProvinceId city, int infantry, int cavalry, int artillery) =>
        state.CreateArmy(owner, PathPosition.AtCity(city), [infantry, cavalry, artillery]);

    private string? Validate(Command command, NationId issuer) => command.Validate(NewState(), _data, issuer).Reason;

    private void Execute(GameState state, NationId issuer, Command command)
    {
        var events = Simulation.Step(state, _data, [new CommandEnvelope(state.Tick, issuer, 0, command)]);
        Assert.Empty(events);
    }

    private void Step(GameState state, int ticks)
    {
        for (int i = 0; i < ticks; i++)
        {
            Simulation.Step(state, _data, []);
        }
    }
}
