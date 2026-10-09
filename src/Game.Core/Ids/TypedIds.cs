using System.Text.Json;
using System.Text.Json.Serialization;

namespace Game.Core;

/// <summary>
/// Gemeinsame Schnittstelle der typisierten Ganzzahl-IDs. Der Wert ist der Index in den
/// nach ID sortierten Listen von <see cref="Data.GameData"/> und <see cref="State.GameState"/>.
/// </summary>
public interface ITypedId<TSelf>
    where TSelf : struct, ITypedId<TSelf>
{
    int Value { get; }

    static abstract TSelf FromValue(int value);
}

[JsonConverter(typeof(TypedIdJsonConverter<ProvinceId>))]
public readonly record struct ProvinceId(int Value) : ITypedId<ProvinceId>
{
    public static ProvinceId FromValue(int value) => new(value);

    public override string ToString() => $"Province#{Value}";
}

[JsonConverter(typeof(TypedIdJsonConverter<NationId>))]
public readonly record struct NationId(int Value) : ITypedId<NationId>
{
    public static NationId FromValue(int value) => new(value);

    public override string ToString() => $"Nation#{Value}";
}

[JsonConverter(typeof(TypedIdJsonConverter<ResourceId>))]
public readonly record struct ResourceId(int Value) : ITypedId<ResourceId>
{
    public static ResourceId FromValue(int value) => new(value);

    public override string ToString() => $"Resource#{Value}";
}

[JsonConverter(typeof(TypedIdJsonConverter<BuildingId>))]
public readonly record struct BuildingId(int Value) : ITypedId<BuildingId>
{
    public static BuildingId FromValue(int value) => new(value);

    public override string ToString() => $"Building#{Value}";
}

[JsonConverter(typeof(TypedIdJsonConverter<UnitTypeId>))]
public readonly record struct UnitTypeId(int Value) : ITypedId<UnitTypeId>
{
    public static UnitTypeId FromValue(int value) => new(value);

    public override string ToString() => $"UnitType#{Value}";
}

/// <summary>Eine Armee. IDs werden fortlaufend vergeben und nach Auflösung nicht wiederverwendet.</summary>
[JsonConverter(typeof(TypedIdJsonConverter<ArmyId>))]
public readonly record struct ArmyId(int Value) : ITypedId<ArmyId>
{
    public static ArmyId FromValue(int value) => new(value);

    public override string ToString() => $"Army#{Value}";
}

/// <summary>Ein Spieler in der Session (Mensch am Host oder Client). Nicht dasselbe wie die Nation, die er steuert.</summary>
[JsonConverter(typeof(TypedIdJsonConverter<PlayerId>))]
public readonly record struct PlayerId(int Value) : ITypedId<PlayerId>
{
    public static PlayerId FromValue(int value) => new(value);

    public override string ToString() => $"Player#{Value}";
}

/// <summary>Serialisiert typisierte IDs als einfache Zahl statt als Objekt.</summary>
internal sealed class TypedIdJsonConverter<T> : JsonConverter<T>
    where T : struct, ITypedId<T>
{
    public override T Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) =>
        T.FromValue(reader.GetInt32());

    public override void Write(Utf8JsonWriter writer, T value, JsonSerializerOptions options) =>
        writer.WriteNumberValue(value.Value);
}
