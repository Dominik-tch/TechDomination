using Game.Core.Data;
using Game.Core.State;

namespace Game.Core.Commands;

/// <summary>Bricht die Baustelle in einer eigenen Provinz ab. Die Kosten werden voll erstattet.</summary>
public sealed record CancelConstructionCommand(ProvinceId Province) : Command
{
    public override ValidationResult Validate(GameState state, GameData data, NationId issuer)
    {
        if (!data.Contains(Province))
        {
            return ValidationResult.Invalid("Unbekannte Provinz.");
        }

        var province = state.GetProvince(Province);
        if (province.Owner != issuer)
        {
            return ValidationResult.Invalid("Die Provinz gehört nicht dir.");
        }

        return province.Construction is null
            ? ValidationResult.Invalid("In dieser Provinz wird nicht gebaut.")
            : ValidationResult.Valid;
    }

    internal override void Apply(GameState state, GameData data, NationId issuer)
    {
        var province = state.GetProvince(Province);
        var construction = province.Construction!;

        Costs.Refund(state.GetNation(issuer), data.GetBuilding(construction.Building).Level(construction.TargetLevel));
        province.Construction = null;
    }
}
