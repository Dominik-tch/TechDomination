using System.Text.Json.Serialization;

namespace Game.Core.State;

/// <summary>Veränderlicher Zustand einer Nation: Geld und nationaler Ressourcenpool.</summary>
public sealed class NationState
{
    private readonly long[] _resources;

    [JsonConstructor]
    internal NationState(NationId id, long money, IReadOnlyList<long> resources)
    {
        ArgumentNullException.ThrowIfNull(resources);

        Id = id;
        Money = money;
        _resources = resources.ToArray();
    }

    public NationId Id { get; }

    /// <summary>Geld in Cent (siehe <see cref="Quantities.MoneyScale"/>).</summary>
    public long Money { get; internal set; }

    /// <summary>Nationaler Pool in Tausendstel (siehe <see cref="Quantities.ResourceScale"/>), Index = <see cref="ResourceId.Value"/>.</summary>
    public IReadOnlyList<long> Resources => _resources;

    public long GetResource(ResourceId id) => _resources[id.Value];

    internal void AddResource(ResourceId id, long amount) => _resources[id.Value] += amount;
}
