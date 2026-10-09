using Game.Core.Data;
using Game.Core.Economy;
using Game.Core.State;

namespace Game.Core.Commands;

/// <summary>Kauft ganze Einheiten einer Ressource auf dem eigenen Markt. Der Preis steigt mit jeder Einheit.</summary>
public sealed record BuyResourceCommand(ResourceId Resource, int Amount) : Command
{
    public override ValidationResult Validate(GameState state, GameData data, NationId issuer)
    {
        if (TradeValidation.CheckInput(data, Resource, Amount) is { IsValid: false } invalid)
        {
            return invalid;
        }

        var nation = state.GetNation(issuer);
        long cost = MarketRules.BuyCost(nation, data, Resource, Amount);
        return nation.Money >= cost
            ? ValidationResult.Valid
            : ValidationResult.Invalid(
                $"Nicht genug Geld (benötigt {Quantities.FormatMoney(cost)}, vorhanden {Quantities.FormatMoney(nation.Money)}).");
    }

    internal override void Apply(GameState state, GameData data, NationId issuer)
    {
        var nation = state.GetNation(issuer);
        nation.Money -= MarketRules.BuyCost(nation, data, Resource, Amount);
        nation.SetMarketPrice(Resource, MarketRules.PriceAfterBuy(nation, data, Resource, Amount));
        nation.AddResource(Resource, Amount * Quantities.ResourceScale);
    }
}

/// <summary>Verkauft ganze Einheiten einer Ressource auf dem eigenen Markt. Der Preis sinkt mit jeder Einheit.</summary>
public sealed record SellResourceCommand(ResourceId Resource, int Amount) : Command
{
    public override ValidationResult Validate(GameState state, GameData data, NationId issuer)
    {
        if (TradeValidation.CheckInput(data, Resource, Amount) is { IsValid: false } invalid)
        {
            return invalid;
        }

        var nation = state.GetNation(issuer);
        long needed = Amount * Quantities.ResourceScale;
        long available = nation.GetResource(Resource);
        return available >= needed
            ? ValidationResult.Valid
            : ValidationResult.Invalid(
                $"Nicht genug {data.GetResource(Resource).Name} (benötigt {Quantities.FormatResource(needed)}, vorhanden {Quantities.FormatResource(available)}).");
    }

    internal override void Apply(GameState state, GameData data, NationId issuer)
    {
        var nation = state.GetNation(issuer);
        nation.Money += MarketRules.SellRevenue(nation, data, Resource, Amount);
        nation.SetMarketPrice(Resource, MarketRules.PriceAfterSell(nation, data, Resource, Amount));
        nation.AddResource(Resource, -Amount * Quantities.ResourceScale);
    }
}

internal static class TradeValidation
{
    public static ValidationResult CheckInput(GameData data, ResourceId resource, int amount)
    {
        if (!data.Contains(resource))
        {
            return ValidationResult.Invalid("Unbekannte Ressource.");
        }

        return amount is < 1 or > MarketRules.MaxTradeAmount
            ? ValidationResult.Invalid($"Die Menge muss zwischen 1 und {MarketRules.MaxTradeAmount} liegen.")
            : ValidationResult.Valid;
    }
}
