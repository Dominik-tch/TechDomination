using Game.Core.Data;
using Game.Core.State;

namespace Game.Core.Commands;

/// <summary>Prüfen, Bezahlen und Erstatten von Kosten (Geld und Ressourcen), optional für mehrere Stück.</summary>
internal static class Costs
{
    public static ValidationResult CanAfford(NationState nation, GameData data, BuildingLevelDefinition cost) =>
        CanAfford(nation, data, cost.Money, cost.Resources, count: 1);

    /// <summary>
    /// Kann die Nation <paramref name="count"/>-mal die Kosten bezahlen?
    /// Bei Mangel mit Grund, z. B. „Nicht genug Stahl (benötigt 20, vorhanden 12)“.
    /// </summary>
    public static ValidationResult CanAfford(NationState nation, GameData data, long money, IReadOnlyList<long> resources, int count)
    {
        long neededMoney = money * count;
        if (nation.Money < neededMoney)
        {
            return ValidationResult.Invalid(
                $"Nicht genug Geld (benötigt {Quantities.FormatMoney(neededMoney)}, vorhanden {Quantities.FormatMoney(nation.Money)}).");
        }

        foreach (var resource in data.Resources)
        {
            long needed = resources[resource.Id.Value] * count;
            long available = nation.GetResource(resource.Id);
            if (available < needed)
            {
                return ValidationResult.Invalid(
                    $"Nicht genug {resource.Name} (benötigt {Quantities.FormatResource(needed)}, vorhanden {Quantities.FormatResource(available)}).");
            }
        }

        return ValidationResult.Valid;
    }

    public static void Pay(NationState nation, BuildingLevelDefinition cost) => Transfer(nation, cost.Money, cost.Resources, -1);

    public static void Refund(NationState nation, BuildingLevelDefinition cost) => Transfer(nation, cost.Money, cost.Resources, 1);

    public static void Pay(NationState nation, long money, IReadOnlyList<long> resources, int count) =>
        Transfer(nation, money, resources, -count);

    public static void Refund(NationState nation, long money, IReadOnlyList<long> resources, int count) =>
        Transfer(nation, money, resources, count);

    private static void Transfer(NationState nation, long money, IReadOnlyList<long> resources, int factor)
    {
        nation.Money += factor * money;
        for (int i = 0; i < resources.Count; i++)
        {
            nation.AddResource(new ResourceId(i), factor * resources[i]);
        }
    }
}
