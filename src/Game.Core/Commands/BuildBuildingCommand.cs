using Game.Core.Data;
using Game.Core.State;

namespace Game.Core.Commands;

/// <summary>
/// Baut die nächste Stufe eines Gebäudes in einer eigenen Provinz (Neubau = Stufe 1).
/// Die Kosten werden sofort bezahlt; fertig ist das Gebäude nach der Bauzeit der Stufe.
/// </summary>
public sealed record BuildBuildingCommand(ProvinceId Province, BuildingId Building) : Command
{
    public override ValidationResult Validate(GameState state, GameData data, NationId issuer)
    {
        if (!data.Contains(Province))
        {
            return ValidationResult.Invalid("Unbekannte Provinz.");
        }

        if (!data.Contains(Building))
        {
            return ValidationResult.Invalid("Unbekanntes Gebäude.");
        }

        var province = state.GetProvince(Province);
        if (province.Owner != issuer)
        {
            return ValidationResult.Invalid("Die Provinz gehört nicht dir.");
        }

        if (province.Construction is not null)
        {
            return ValidationResult.Invalid("In dieser Provinz wird bereits gebaut.");
        }

        var building = data.GetBuilding(Building);
        int nextLevel = province.GetBuildingLevel(Building) + 1;
        if (nextLevel > building.MaxLevel)
        {
            return ValidationResult.Invalid($"{building.Name} hat bereits die höchste Stufe.");
        }

        return Costs.CanAfford(state.GetNation(issuer), data, building.Level(nextLevel));
    }

    internal override void Apply(GameState state, GameData data, NationId issuer)
    {
        var province = state.GetProvince(Province);
        int nextLevel = province.GetBuildingLevel(Building) + 1;
        var level = data.GetBuilding(Building).Level(nextLevel);

        Costs.Pay(state.GetNation(issuer), level);
        province.Construction = new ConstructionState(Building, nextLevel, level.BuildTicks);
    }
}
