using Game.Core.Data;

namespace Game.Core.Save;

/// <summary>Regeln für Spielstandnamen und automatisches Speichern. Kennt nur Namen, keine Pfade oder Dateiendungen.</summary>
public static class SaveNames
{
    public const int MaxLength = 64;

    /// <summary>Anzahl der Dateien, in die rotierend automatisch gespeichert wird.</summary>
    public const int AutosaveSlots = 3;

    /// <summary>Automatisch gespeichert wird alle so viele Minuten Spielzeit bei Standardgeschwindigkeit.</summary>
    public const int AutosaveIntervalMinutes = 10;

    // Unter Windows als Dateiname nicht erlaubt; abgelehnt, damit Spielstände plattformübergreifend funktionieren.
    private static readonly HashSet<string> ReservedNames = new(StringComparer.OrdinalIgnoreCase)
    {
        "CON", "PRN", "AUX", "NUL",
        "COM1", "COM2", "COM3", "COM4", "COM5", "COM6", "COM7", "COM8", "COM9",
        "LPT1", "LPT2", "LPT3", "LPT4", "LPT5", "LPT6", "LPT7", "LPT8", "LPT9",
    };

    /// <summary>Erlaubt sind Buchstaben, Ziffern, Leerzeichen, '-' und '_', höchstens <see cref="MaxLength"/> Zeichen.</summary>
    public static ValidationResult Validate(string? name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return ValidationResult.Invalid("Bitte einen Namen eingeben.");
        }

        if (name.Length > MaxLength)
        {
            return ValidationResult.Invalid($"Der Name darf höchstens {MaxLength} Zeichen lang sein.");
        }

        if (name != name.Trim())
        {
            return ValidationResult.Invalid("Der Name darf nicht mit einem Leerzeichen beginnen oder enden.");
        }

        if (!name.All(c => char.IsLetterOrDigit(c) || c is ' ' or '-' or '_'))
        {
            return ValidationResult.Invalid("Erlaubt sind nur Buchstaben, Ziffern, Leerzeichen, '-' und '_'.");
        }

        if (ReservedNames.Contains(name))
        {
            return ValidationResult.Invalid($"Der Name '{name}' ist nicht erlaubt.");
        }

        return ValidationResult.Valid;
    }

    public static string AutosaveName(int slot) => $"Autosave {slot}";

    /// <summary>10 Minuten Spielzeit bei Standardgeschwindigkeit, in Ticks.</summary>
    public static long AutosaveIntervalTicks(GameData data)
    {
        ArgumentNullException.ThrowIfNull(data);
        return (long)data.DefaultSpeedLevel.TicksPerSecond * 60 * AutosaveIntervalMinutes;
    }

    /// <summary>Soll nach diesem Tick automatisch gespeichert werden? Nicht bei Tick 0.</summary>
    public static bool IsAutosaveTick(long tick, long intervalTicks) => tick > 0 && tick % intervalTicks == 0;

    /// <summary>Datei für das automatische Speichern bei diesem Tick: 1, 2, 3, 1, 2, …</summary>
    public static int AutosaveSlot(long tick, long intervalTicks) =>
        (int)((tick / intervalTicks - 1) % AutosaveSlots) + 1;
}
