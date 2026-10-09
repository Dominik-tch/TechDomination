using Godot;

namespace TechDomination.UI;

/// <summary>
/// Lädt Symbole. Rohstoff-Symbole liegen unter res://assets/icons/resources/ und heißen wie die
/// Ressourcen-ID aus resources.json (z. B. wood.svg).
/// </summary>
public static class Icons
{
    private const string ResourceIconDirectory = "res://assets/icons/resources";
    private const string MoneyIconPath = "res://assets/icons/money.svg";

    private static readonly Dictionary<string, Texture2D?> Cache = [];

    public static Texture2D? Money => Load(MoneyIconPath);

    public static Texture2D? Resource(string resourceKey) => Load($"{ResourceIconDirectory}/{resourceKey}.svg");

    private static Texture2D? Load(string path)
    {
        if (!Cache.TryGetValue(path, out var texture))
        {
            texture = ResourceLoader.Exists(path) ? GD.Load<Texture2D>(path) : null;
            if (texture is null)
            {
                GD.PushWarning($"Symbol fehlt: {path}");
            }

            Cache[path] = texture;
        }

        return texture;
    }
}
