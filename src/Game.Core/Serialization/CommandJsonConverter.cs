using System.Text.Json;
using System.Text.Json.Serialization;
using Game.Core.Commands;

namespace Game.Core.Serialization;

/// <summary>
/// Serialisiert Commands polymorph als <c>{ "type": "name", "command": { ... } }</c>.
/// Der Name kommt aus <see cref="CommandTypes"/>.
/// </summary>
internal sealed class CommandJsonConverter : JsonConverter<Command>
{
    private const string TypeProperty = "type";
    private const string CommandProperty = "command";

    public override Command Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        using var document = JsonDocument.ParseValue(ref reader);
        var root = document.RootElement;

        if (root.ValueKind != JsonValueKind.Object
            || !root.TryGetProperty(TypeProperty, out var typeElement)
            || typeElement.ValueKind != JsonValueKind.String)
        {
            throw new JsonException($"Command ohne '{TypeProperty}'.");
        }

        string name = typeElement.GetString()!;
        var type = CommandTypes.Find(name) ?? throw new JsonException($"Unbekannter Command-Typ '{name}'.");

        if (!root.TryGetProperty(CommandProperty, out var payload))
        {
            throw new JsonException($"Command '{name}' ohne '{CommandProperty}'.");
        }

        return (Command?)payload.Deserialize(type, options)
            ?? throw new JsonException($"Command '{name}' ist leer (null).");
    }

    public override void Write(Utf8JsonWriter writer, Command value, JsonSerializerOptions options)
    {
        writer.WriteStartObject();
        writer.WriteString(TypeProperty, CommandTypes.NameOf(value.GetType()));
        writer.WritePropertyName(CommandProperty);

        // Mit dem konkreten Typ serialisieren; dieser Converter gilt nur für den Basistyp, daher keine Rekursion.
        JsonSerializer.Serialize(writer, value, value.GetType(), options);
        writer.WriteEndObject();
    }
}
