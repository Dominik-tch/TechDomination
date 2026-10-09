using System.Collections.Concurrent;

namespace Game.Core.Commands;

/// <summary>
/// Ordnet jedem Command-Typ einen festen Namen für die Serialisierung zu (Spielstand, Command-Log, Netzwerk).
/// Der Name darf sich nach der Veröffentlichung nicht mehr ändern, sonst lassen sich alte Spielstände nicht laden.
/// </summary>
public static class CommandTypes
{
    private static readonly ConcurrentDictionary<string, Type> TypesByName = new(StringComparer.Ordinal);
    private static readonly ConcurrentDictionary<Type, string> NamesByType = new();
    private static readonly object RegistrationLock = new();

    // Feste Namen der Spiel-Commands. Nie umbenennen, sonst lassen sich alte Spielstände nicht mehr laden.
    static CommandTypes()
    {
        Register<BuildBuildingCommand>("buildBuilding");
        Register<CancelConstructionCommand>("cancelConstruction");
    }

    /// <summary>Name des registrierten Command-Typs.</summary>
    /// <exception cref="InvalidOperationException">Der Typ ist nicht registriert.</exception>
    public static string NameOf(Type type) =>
        NamesByType.TryGetValue(type, out string? name)
            ? name
            : throw new InvalidOperationException($"Command-Typ '{type.Name}' ist nicht registriert.");

    /// <summary>Typ zum Namen, oder <c>null</c>, wenn der Name unbekannt ist.</summary>
    public static Type? Find(string name) => TypesByName.GetValueOrDefault(name);

    /// <summary>Registriert einen Command-Typ. Mehrfaches Registrieren derselben Zuordnung ist erlaubt.</summary>
    internal static void Register<T>(string name)
        where T : Command
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);

        // Erst prüfen, dann eintragen: Ein abgelehnter Versuch darf keine halbe Zuordnung hinterlassen.
        lock (RegistrationLock)
        {
            bool nameTaken = TypesByName.TryGetValue(name, out var registeredType) && registeredType != typeof(T);
            bool typeTaken = NamesByType.TryGetValue(typeof(T), out string? registeredName) && registeredName != name;
            if (nameTaken || typeTaken)
            {
                throw new InvalidOperationException(
                    $"Command-Name '{name}' oder Typ '{typeof(T).Name}' ist bereits anders registriert.");
            }

            TypesByName[name] = typeof(T);
            NamesByType[typeof(T)] = name;
        }
    }
}
