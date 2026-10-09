using Game.Core.Data;
using Game.Core.Map;
using Game.Core.State;

namespace Game.Core.Commands;

/// <summary>
/// Marschbefehl zu einem beliebigen Punkt im Wegenetz (Stadt oder Stelle auf einem Pfad).
/// Ersetzt einen laufenden Marsch; die Armee nimmt von ihrer aktuellen Position den kürzesten Weg, notfalls umkehrend.
/// </summary>
public sealed record MoveArmyCommand(ArmyId Army, PathPosition Target) : Command
{
    public override ValidationResult Validate(GameState state, GameData data, NationId issuer)
    {
        if (ArmyChecks.OwnArmy(state, Army, issuer) is { IsValid: false } invalid)
        {
            return invalid;
        }

        var army = state.FindArmy(Army)!;
        if (army.TotalUnits == 0)
        {
            return ValidationResult.Invalid("Die Armee hat keine Einheiten.");
        }

        return ArmyChecks.Target(data, army.Position, Target);
    }

    internal override void Apply(GameState state, GameData data, NationId issuer)
    {
        var army = state.FindArmy(Army)!;
        var route = data.Graph.FindRoute(army.Position, Target)!;
        army.SetRoute(route.Start, route.Legs);
    }
}

/// <summary>Hält eine marschierende Armee an ihrer aktuellen Position an – auch mitten auf einem Pfad.</summary>
public sealed record HaltArmyCommand(ArmyId Army) : Command
{
    public override ValidationResult Validate(GameState state, GameData data, NationId issuer)
    {
        if (ArmyChecks.OwnArmy(state, Army, issuer) is { IsValid: false } invalid)
        {
            return invalid;
        }

        return state.FindArmy(Army)!.IsMoving ? ValidationResult.Valid : ValidationResult.Invalid("Die Armee marschiert nicht.");
    }

    internal override void Apply(GameState state, GameData data, NationId issuer)
    {
        var army = state.FindArmy(Army)!;
        army.Halt();
        army.Position = data.Graph.Normalize(army.Position);
    }
}

/// <summary>
/// Teilt Einheiten in eine neue Armee ab und schickt diese sofort zum Ziel. Die alte Armee behält den Rest und ihren Marsch.
/// </summary>
/// <param name="Units">Anzahl je Einheitentyp für die neue Armee (Index = <see cref="UnitTypeId.Value"/>).</param>
public sealed record SplitArmyCommand(ArmyId Army, IReadOnlyList<int> Units, PathPosition Target) : Command
{
    public override ValidationResult Validate(GameState state, GameData data, NationId issuer)
    {
        if (ArmyChecks.OwnArmy(state, Army, issuer) is { IsValid: false } invalid)
        {
            return invalid;
        }

        var army = state.FindArmy(Army)!;
        if (Units is null || Units.Count != data.UnitTypes.Count || Units.Where((count, i) => count < 0 || count > army.Units[i]).Any())
        {
            return ValidationResult.Invalid("Ungültige Aufteilung.");
        }

        int moving = Units.Sum();
        if (moving == 0)
        {
            return ValidationResult.Invalid("Mindestens eine Einheit muss in die neue Armee.");
        }

        if (moving == army.TotalUnits)
        {
            return ValidationResult.Invalid("Mindestens eine Einheit muss in der bisherigen Armee bleiben.");
        }

        return ArmyChecks.Target(data, army.Position, Target);
    }

    internal override void Apply(GameState state, GameData data, NationId issuer)
    {
        var army = state.FindArmy(Army)!;
        army.RemoveUnits(Units);
        var split = state.CreateArmy(army.Owner, data.Graph.Normalize(army.Position), Units);
        var route = data.Graph.FindRoute(split.Position, Target)!;
        split.SetRoute(route.Start, route.Legs);
    }
}

internal static class ArmyChecks
{
    /// <summary>Gültiges, erreichbares Ziel, an dem die Armee nicht schon steht?</summary>
    public static ValidationResult Target(GameData data, PathPosition from, PathPosition target)
    {
        if (!data.Graph.IsValid(target))
        {
            return ValidationResult.Invalid("Ungültiges Ziel.");
        }

        if (data.Graph.IsSamePosition(from, target))
        {
            return ValidationResult.Invalid("Die Armee ist bereits dort.");
        }

        return data.Graph.FindRoute(from, target) is null
            ? ValidationResult.Invalid("Das Ziel ist auf dem Landweg nicht erreichbar.")
            : ValidationResult.Valid;
    }

    public static ValidationResult OwnArmy(GameState state, ArmyId id, NationId issuer)
    {
        if (state.FindArmy(id) is not { } army)
        {
            return ValidationResult.Invalid("Unbekannte Armee.");
        }

        return army.Owner == issuer ? ValidationResult.Valid : ValidationResult.Invalid("Die Armee gehört nicht dir.");
    }
}
