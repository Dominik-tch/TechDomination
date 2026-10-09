using Game.Core.Data;
using Game.Core.State;

namespace Game.Core.Economy;

/// <summary>
/// Preisregeln des nationalen Markts. Gehandelt wird in ganzen Einheiten. Jede gekaufte Einheit erhöht den Preis
/// um einen festen Anteil des Basispreises, jede verkaufte senkt ihn; die Kosten mehrerer Einheiten werden
/// Einheit für Einheit aufsummiert, sodass sich Aufteilen nicht lohnt.
/// </summary>
public static class MarketRules
{
    /// <summary>Höchstens so viele Einheiten pro Kauf oder Verkauf.</summary>
    public const int MaxTradeAmount = 10_000;

    private const long BasisPointsPerWhole = 10_000;

    /// <summary>Preisänderung je Einheit in Cent.</summary>
    public static long PriceStep(GameData data, ResourceId resource) =>
        data.GetResource(resource).BasePrice * data.Market.PriceChangePerUnit / BasisPointsPerWhole;

    /// <summary>Untergrenze des Preises in Cent.</summary>
    public static long MinPrice(GameData data, ResourceId resource) =>
        data.GetResource(resource).BasePrice * data.Market.MinPrice / BasisPointsPerWhole;

    /// <summary>Gesamtkosten in Cent, um <paramref name="amount"/> Einheiten zu kaufen.</summary>
    public static long BuyCost(NationState nation, GameData data, ResourceId resource, int amount)
    {
        long price = nation.GetMarketPrice(resource);
        long step = PriceStep(data, resource);
        return amount * price + step * amount * (amount - 1L) / 2;
    }

    /// <summary>Erlös in Cent für <paramref name="amount"/> verkaufte Einheiten (anteilig vom jeweils aktuellen Preis).</summary>
    public static long SellRevenue(NationState nation, GameData data, ResourceId resource, int amount)
    {
        long price = nation.GetMarketPrice(resource);
        long step = PriceStep(data, resource);
        long minPrice = MinPrice(data, resource);

        long revenue = 0;
        for (int i = 0; i < amount; i++)
        {
            revenue += price * data.Market.SellPrice / BasisPointsPerWhole;
            price = Math.Max(minPrice, price - step);
        }

        return revenue;
    }

    /// <summary>Preis nach dem Kauf von <paramref name="amount"/> Einheiten.</summary>
    public static long PriceAfterBuy(NationState nation, GameData data, ResourceId resource, int amount) =>
        nation.GetMarketPrice(resource) + PriceStep(data, resource) * amount;

    /// <summary>Preis nach dem Verkauf von <paramref name="amount"/> Einheiten, nie unter der Untergrenze.</summary>
    public static long PriceAfterSell(NationState nation, GameData data, ResourceId resource, int amount) =>
        Math.Max(MinPrice(data, resource), nation.GetMarketPrice(resource) - PriceStep(data, resource) * amount);

    /// <summary>
    /// Preis nach einem Wirtschaftstakt Erholung: Er bewegt sich um einen Anteil des Abstands zum Basispreis zurück,
    /// mindestens um 1 Cent, und schießt nie über den Basispreis hinaus.
    /// </summary>
    public static long Recover(long price, long basePrice, long recoveryBasisPoints)
    {
        long distance = basePrice - price;
        if (distance == 0 || recoveryBasisPoints == 0)
        {
            return price;
        }

        long step = distance * recoveryBasisPoints / BasisPointsPerWhole;
        if (step == 0)
        {
            step = Math.Sign(distance);
        }

        return price + step;
    }
}
