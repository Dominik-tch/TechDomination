using Game.Core.Data;
using Game.Core.State;

namespace Game.Core.Commands;

/// <summary>
/// Gibt Einheiten in einer eigenen Provinz in Ausbildung. Die Kosten werden sofort bezahlt; die Einheiten werden
/// nacheinander ausgebildet und erscheinen in der Stadt.
/// </summary>
public sealed record TrainUnitsCommand(ProvinceId Province, UnitTypeId UnitType, int Count) : Command
{
    /// <summary>Höchstens so viele Einheiten pro Auftrag.</summary>
    public const int MaxCount = 50;

    public override ValidationResult Validate(GameState state, GameData data, NationId issuer)
    {
        if (!data.Contains(Province))
        {
            return ValidationResult.Invalid("Unbekannte Provinz.");
        }

        if (UnitType.Value < 0 || UnitType.Value >= data.UnitTypes.Count)
        {
            return ValidationResult.Invalid("Unbekannte Einheit.");
        }

        var province = state.GetProvince(Province);
        if (province.Owner != issuer)
        {
            return ValidationResult.Invalid("Die Provinz gehört nicht dir.");
        }

        var unitType = data.GetUnitType(UnitType);
        if (!unitType.IsTrainable)
        {
            return ValidationResult.Invalid(unitType.DailyPerProvince > 0
                ? $"{unitType.Name} wird automatisch zum Tageswechsel ausgebildet."
                : $"{unitType.Name} kann nicht ausgebildet werden.");
        }

        if (unitType.RequiredBuilding is { } required && province.GetBuildingLevel(required) == 0)
        {
            return ValidationResult.Invalid($"Benötigt: {data.GetBuilding(required).Name}.");
        }

        if (Count is < 1 or > MaxCount)
        {
            return ValidationResult.Invalid($"Die Anzahl muss zwischen 1 und {MaxCount} liegen.");
        }

        return Costs.CanAfford(state.GetNation(issuer), data, unitType.CostMoney, unitType.CostResources, Count);
    }

    internal override void Apply(GameState state, GameData data, NationId issuer)
    {
        var unitType = data.GetUnitType(UnitType);
        var province = state.GetProvince(Province);

        Costs.Pay(state.GetNation(issuer), unitType.CostMoney, unitType.CostResources, Count);
        if (province.TrainingQueue.Count == 0)
        {
            province.TrainingRemainingTicks = unitType.TrainingTicks;
        }

        province.EnqueueTraining(UnitType, Count);
    }
}

/// <summary>Bricht einen Ausbildungsauftrag (eine Einheit der Schlange) ab. Die Kosten werden voll erstattet.</summary>
/// <param name="Index">Position in der Schlange; 0 ist die Einheit, die gerade ausgebildet wird.</param>
public sealed record CancelTrainingCommand(ProvinceId Province, int Index) : Command
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

        return Index >= 0 && Index < province.TrainingQueue.Count
            ? ValidationResult.Valid
            : ValidationResult.Invalid("Kein Ausbildungsauftrag an dieser Stelle.");
    }

    internal override void Apply(GameState state, GameData data, NationId issuer)
    {
        var province = state.GetProvince(Province);
        var unitType = data.GetUnitType(province.TrainingQueue[Index]);

        Costs.Refund(state.GetNation(issuer), unitType.CostMoney, unitType.CostResources, 1);
        province.RemoveTrainingAt(Index);

        if (Index == 0)
        {
            province.TrainingRemainingTicks = province.TrainingQueue.Count > 0
                ? data.GetUnitType(province.TrainingQueue[0]).TrainingTicks
                : 0;
        }
    }
}
