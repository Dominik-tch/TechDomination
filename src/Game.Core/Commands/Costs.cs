using Game.Core.Data;
using Game.Core.State;

namespace Game.Core.Commands;

/// <summary>Prüfen, Bezahlen und Erstatten von Kosten (Geld und Ressourcen).</summary>
internal static class Costs
{
    /// <summary>Kann die Nation die Kosten bezahlen? Bei Mangel mit Grund, z. B. „Nicht genug Stahl (benötigt 20, vorhanden 12)“.</summary>
    public static ValidationResult CanAfford(NationState nation, GameData data, BuildingLevelDefinition cost)
    {
        if (nation.Money < cost.Money)
        {
            return ValidationResult.Invalid(
                $"Nicht genug Geld (benötigt {Quantities.FormatMoney(cost.Money)}, vorhanden {Quantities.FormatMoney(nation.Money)}).");
        }

        foreach (var resource in data.Resources)
        {
            long needed = cost.Resources[resource.Id.Value];
            long available = nation.GetResource(resource.Id);
            if (available < needed)
            {
                return ValidationResult.Invalid(
                    $"Nicht genug {resource.Name} (benötigt {Quantities.FormatResource(needed)}, vorhanden {Quantities.FormatResource(available)}).");
            }
        }

        return ValidationResult.Valid;
    }

    public static void Pay(NationState nation, BuildingLevelDefinition cost) => Transfer(nation, cost, sign: -1);

    public static void Refund(NationState nation, BuildingLevelDefinition cost) => Transfer(nation, cost, sign: 1);

    private static void Transfer(NationState nation, BuildingLevelDefinition cost, int sign)
    {
        nation.Money += sign * cost.Money;
        for (int i = 0; i < cost.Resources.Count; i++)
        {
            nation.AddResource(new ResourceId(i), sign * cost.Resources[i]);
        }
    }
}
