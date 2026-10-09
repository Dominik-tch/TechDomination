using Game.Core.Data;
using Game.Core.Economy;
using Game.Core.State;

namespace Game.Core.Commands;

/// <summary>
/// Schieberegler der Fabrik-Verwaltung: legt fest, wie viele Fabriken eines Typs landesweit laufen.
/// <c>null</c> = alle, auch künftig gebaute.
/// </summary>
public sealed record SetFactoryActivityCommand(BuildingId Factory, int? ActiveCount) : Command
{
    public override ValidationResult Validate(GameState state, GameData data, NationId issuer)
    {
        if (!data.Contains(Factory) || !data.GetBuilding(Factory).IsFactory)
        {
            return ValidationResult.Invalid("Unbekannter Fabriktyp.");
        }

        if (ActiveCount is not { } count)
        {
            return ValidationResult.Valid;
        }

        int capacity = FactoryRules.Capacity(state, issuer, Factory);
        return count >= 0 && count <= capacity
            ? ValidationResult.Valid
            : ValidationResult.Invalid($"Es gibt nur {capacity} Fabriken dieses Typs.");
    }

    internal override void Apply(GameState state, GameData data, NationId issuer) =>
        state.GetNation(issuer).SetFactoryLimit(Factory, ActiveCount);
}
