using System.Text.Json.Serialization;

namespace Game.Core.State;

/// <summary>Veränderlicher Zustand einer Nation: Geld, nationaler Ressourcenpool, eigener Markt und Fabrik-Steuerung.</summary>
public sealed class NationState
{
    private readonly long[] _resources;
    private readonly long[] _marketPrices;
    private readonly int?[] _factoryLimits;
    private readonly int[] _lastFactoryRuns;

    [JsonConstructor]
    internal NationState(
        NationId id,
        long money,
        IReadOnlyList<long> resources,
        IReadOnlyList<long> marketPrices,
        IReadOnlyList<int?> factoryLimits,
        IReadOnlyList<int> lastFactoryRuns)
    {
        ArgumentNullException.ThrowIfNull(resources);
        ArgumentNullException.ThrowIfNull(marketPrices);
        ArgumentNullException.ThrowIfNull(factoryLimits);
        ArgumentNullException.ThrowIfNull(lastFactoryRuns);

        Id = id;
        Money = money;
        _resources = resources.ToArray();
        _marketPrices = marketPrices.ToArray();
        _factoryLimits = factoryLimits.ToArray();
        _lastFactoryRuns = lastFactoryRuns.ToArray();
    }

    public NationId Id { get; }

    /// <summary>Geld in Cent (siehe <see cref="Quantities.MoneyScale"/>).</summary>
    public long Money { get; internal set; }

    /// <summary>Nationaler Pool in Tausendstel (siehe <see cref="Quantities.ResourceScale"/>), Index = <see cref="ResourceId.Value"/>.</summary>
    public IReadOnlyList<long> Resources => _resources;

    /// <summary>Aktueller Marktpreis pro Einheit in Cent, Index = <see cref="ResourceId.Value"/>. Jede Nation hat ihren eigenen Markt.</summary>
    public IReadOnlyList<long> MarketPrices => _marketPrices;

    /// <summary>
    /// Wie viele Fabriken je Fabriktyp höchstens laufen sollen (Schieberegler), Index = <see cref="BuildingId.Value"/>.
    /// <c>null</c> = alle, auch neu gebaute.
    /// </summary>
    public IReadOnlyList<int?> FactoryLimits => _factoryLimits;

    /// <summary>Wie viele Rezept-Durchläufe je Fabriktyp im letzten Fabrikzyklus tatsächlich stattfanden.</summary>
    public IReadOnlyList<int> LastFactoryRuns => _lastFactoryRuns;

    public long GetResource(ResourceId id) => _resources[id.Value];

    public long GetMarketPrice(ResourceId id) => _marketPrices[id.Value];

    public int? GetFactoryLimit(BuildingId id) => _factoryLimits[id.Value];

    internal void AddResource(ResourceId id, long amount) => _resources[id.Value] += amount;

    internal void SetMarketPrice(ResourceId id, long price) => _marketPrices[id.Value] = price;

    internal void SetFactoryLimit(BuildingId id, int? limit) => _factoryLimits[id.Value] = limit;

    internal void SetLastFactoryRuns(BuildingId id, int runs) => _lastFactoryRuns[id.Value] = runs;
}
