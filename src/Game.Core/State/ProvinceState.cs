using System.Text.Json.Serialization;

namespace Game.Core.State;

/// <summary>Veränderlicher Zustand einer Provinz. Die festen Eigenschaften stehen in <see cref="Data.ProvinceDefinition"/>.</summary>
public sealed class ProvinceState
{
    [JsonConstructor]
    internal ProvinceState(ProvinceId id, NationId owner)
    {
        Id = id;
        Owner = owner;
    }

    public ProvinceId Id { get; }

    /// <summary>Aktueller Besitzer. Jede Provinz hat immer einen Besitzer.</summary>
    public NationId Owner { get; internal set; }
}
