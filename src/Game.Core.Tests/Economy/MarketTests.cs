using Game.Core.Commands;
using Game.Core.Data;
using Game.Core.Economy;
using Game.Core.Events;
using Game.Core.State;
using static Game.Core.Tests.TestIds;

namespace Game.Core.Tests.Economy;

/// <summary>
/// Markt der Testdaten: Holz Basispreis 10, Schienen 50. Jede Einheit ändert den Preis um 10 % des Basispreises
/// (Holz: 1, Schienen: 5). Erholung 50 % je Takt (4 Ticks), Untergrenze 20 %, Verkauf zu 80 %.
/// Startbestand: 100 Geld, 10 Holz.
/// </summary>
public class MarketTests
{
    private readonly GameData _data = TestData.Load();

    [Theory]
    [InlineData(1, 10_00)]
    [InlineData(2, 10_00 + 11_00)]
    [InlineData(5, 10_00 + 11_00 + 12_00 + 13_00 + 14_00)]
    public void BuyCost_SumsRisingPricePerUnit(int amount, long expected)
    {
        Assert.Equal(expected, MarketRules.BuyCost(NewState().GetNation(Red), _data, Wood, amount));
    }

    [Fact]
    public void Buy_PaysAddsUnitsAndRaisesPrice()
    {
        var state = NewState();

        var events = Execute(state, Red, new BuyResourceCommand(Wood, 3));

        Assert.Empty(events);
        var red = state.GetNation(Red);
        Assert.Equal(100_00 - 33_00, red.Money);
        Assert.Equal(10_000 + 3000, red.GetResource(Wood));
        Assert.Equal(13_00, red.GetMarketPrice(Wood));
    }

    [Fact]
    public void Buy_OnlyAffectsOwnMarket()
    {
        var state = NewState();

        Execute(state, Red, new BuyResourceCommand(Wood, 3));

        Assert.Equal(10_00, state.GetNation(Blue).GetMarketPrice(Wood));
    }

    [Fact]
    public void SplittingAPurchase_CostsTheSame()
    {
        var once = NewState();
        var split = NewState();

        Execute(once, Red, new BuyResourceCommand(Wood, 6));
        Execute(split, Red, new BuyResourceCommand(Wood, 2), new BuyResourceCommand(Wood, 4));

        Assert.Equal(once.GetNation(Red).Money, split.GetNation(Red).Money);
        Assert.Equal(once.GetNation(Red).GetMarketPrice(Wood), split.GetNation(Red).GetMarketPrice(Wood));
    }

    [Fact]
    public void Sell_Gives80PercentOfFallingPriceAndLowersPrice()
    {
        var state = NewState();

        Execute(state, Red, new SellResourceCommand(Wood, 3));

        var red = state.GetNation(Red);
        Assert.Equal(100_00 + 8_00 + 7_20 + 6_40, red.Money);
        Assert.Equal(10_000 - 3000, red.GetResource(Wood));
        Assert.Equal(7_00, red.GetMarketPrice(Wood));
    }

    [Fact]
    public void Sell_PriceNeverFallsBelowMinimum()
    {
        var state = NewState();

        Execute(state, Red, new SellResourceCommand(Wood, 10));

        // 10 → 9 → … → 2 (Untergrenze 20 % von 10) und bleibt dort.
        Assert.Equal(2_00, state.GetNation(Red).GetMarketPrice(Wood));
        long expected = new long[] { 10, 9, 8, 7, 6, 5, 4, 3, 2, 2 }.Sum(p => p * 100 * 80 / 100);
        Assert.Equal(100_00 + expected, state.GetNation(Red).Money);
    }

    [Fact]
    public void Price_RecoversHalfTheDistanceEachInterval()
    {
        var state = NewState();
        Execute(state, Red, new BuyResourceCommand(Wood, 6));
        Assert.Equal(16_00, state.GetNation(Red).GetMarketPrice(Wood));

        Step(state, 3);
        Assert.Equal(13_00, state.GetNation(Red).GetMarketPrice(Wood));
        Step(state, 4);
        Assert.Equal(11_50, state.GetNation(Red).GetMarketPrice(Wood));
    }

    [Theory]
    [InlineData(1001, 1000, 1000)]
    [InlineData(999, 1000, 1000)]
    [InlineData(1003, 1000, 1002)]
    [InlineData(1000, 1000, 1000)]
    public void Recover_MovesAtLeastOneCentAndNeverOvershoots(long price, long basePrice, long expected)
    {
        Assert.Equal(expected, MarketRules.Recover(price, basePrice, recoveryBasisPoints: 5000));
    }

    [Fact]
    public void Buy_NotEnoughMoney_NamesAmounts()
    {
        var state = NewState();

        var reason = new BuyResourceCommand(Rails, 2).Validate(state, _data, Red).Reason;

        Assert.Equal("Nicht genug Geld (benötigt 105, vorhanden 100).", reason);
    }

    [Fact]
    public void Sell_NotEnoughStock_NamesResource()
    {
        var reason = new SellResourceCommand(Rails, 1).Validate(NewState(), _data, Red).Reason;

        Assert.Equal("Nicht genug Schienen (benötigt 1, vorhanden 0).", reason);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-5)]
    [InlineData(MarketRules.MaxTradeAmount + 1)]
    public void Trade_AmountOutOfRange_IsInvalid(int amount)
    {
        var state = NewState();

        Assert.False(new BuyResourceCommand(Wood, amount).Validate(state, _data, Red).IsValid);
        Assert.False(new SellResourceCommand(Wood, amount).Validate(state, _data, Red).IsValid);
    }

    [Fact]
    public void Trade_UnknownResource_IsInvalid()
    {
        Assert.Equal("Unbekannte Ressource.", new BuyResourceCommand(new ResourceId(9), 1).Validate(NewState(), _data, Red).Reason);
    }

    [Fact]
    public void RejectedBuy_ChangesNothing()
    {
        var state = NewState();

        var events = Execute(state, Red, new BuyResourceCommand(Rails, 50));

        Assert.IsType<CommandRejected>(Assert.Single(events));
        Assert.Equal(100_00, state.GetNation(Red).Money);
        Assert.Equal(50_00, state.GetNation(Red).GetMarketPrice(Rails));
    }

    private GameState NewState() => GameStateFactory.CreateNew(_data, seed: 1);

    private IReadOnlyList<GameEvent> Execute(GameState state, NationId issuer, params Command[] commands) =>
        Simulation.Step(state, _data, commands.Select((c, i) => new CommandEnvelope(state.Tick, issuer, i, c)).ToList());

    private void Step(GameState state, int ticks)
    {
        for (int i = 0; i < ticks; i++)
        {
            Simulation.Step(state, _data, []);
        }
    }
}
