using Game.Core.Data;
using Game.Core.Save;
using Godot;

namespace TechDomination.Save;

/// <summary>Spielstand-Dateien unter user://saves/. Core liefert Inhalt und Prüfungen, hier geht es nur um Dateien.</summary>
public static class SaveFiles
{
    private const string SaveDirectory = "user://saves";
    private const string Extension = ".sav";

    /// <summary>Eintrag für die Liste im Laden-Menü.</summary>
    /// <param name="Summary"><c>null</c>, wenn die Datei beschädigt ist.</param>
    public sealed record Entry(string Name, DateTimeOffset Modified, SaveGameSummary? Summary);

    public static bool Exists(string name) => Godot.FileAccess.FileExists(PathOf(name));

    /// <summary>
    /// Schreibt erst in eine temporäre Datei und benennt sie dann um,
    /// damit ein Absturz beim Schreiben keinen bestehenden Spielstand zerstört.
    /// </summary>
    /// <exception cref="IOException">Die Datei konnte nicht geschrieben werden.</exception>
    public static void Write(string name, SaveGame save)
    {
        DirAccess.MakeDirRecursiveAbsolute(SaveDirectory);

        using var buffer = new MemoryStream();
        SaveGameSerializer.Write(save, buffer);

        string path = PathOf(name);
        string tempPath = path + ".tmp";
        using (var file = Godot.FileAccess.Open(tempPath, Godot.FileAccess.ModeFlags.Write))
        {
            if (file is null)
            {
                throw new IOException($"Spielstand konnte nicht geschrieben werden ({Godot.FileAccess.GetOpenError()}).");
            }

            file.StoreBuffer(buffer.ToArray());
        }

        var error = DirAccess.RenameAbsolute(tempPath, path);
        if (error != Error.Ok)
        {
            throw new IOException($"Spielstand konnte nicht gespeichert werden ({error}).");
        }
    }

    /// <exception cref="IOException">Die Datei konnte nicht gelesen werden.</exception>
    /// <exception cref="SaveGameException">Der Spielstand ist beschädigt oder passt nicht zu Version oder Daten.</exception>
    public static SaveGame Read(string name, GameData data)
    {
        using var stream = new MemoryStream(ReadBytes(PathOf(name)));
        return SaveGameSerializer.Read(stream, data);
    }

    /// <summary>Alle Spielstände, neueste zuerst.</summary>
    public static IReadOnlyList<Entry> List()
    {
        if (!DirAccess.DirExistsAbsolute(SaveDirectory))
        {
            return [];
        }

        var entries = new List<Entry>();
        foreach (string fileName in DirAccess.GetFilesAt(SaveDirectory))
        {
            if (!fileName.EndsWith(Extension, StringComparison.Ordinal))
            {
                continue;
            }

            string path = $"{SaveDirectory}/{fileName}";
            var modified = DateTimeOffset.FromUnixTimeSeconds((long)Godot.FileAccess.GetModifiedTime(path));
            SaveGameSummary? summary;
            try
            {
                using var stream = new MemoryStream(ReadBytes(path));
                summary = SaveGameSerializer.ReadSummary(stream);
            }
            catch (IOException)
            {
                summary = null;
            }

            entries.Add(new Entry(fileName[..^Extension.Length], modified, summary));
        }

        return entries.OrderByDescending(e => e.Modified).ThenBy(e => e.Name, StringComparer.Ordinal).ToList();
    }

    private static string PathOf(string name) => $"{SaveDirectory}/{name}{Extension}";

    private static byte[] ReadBytes(string path)
    {
        byte[] bytes = Godot.FileAccess.GetFileAsBytes(path);
        if (bytes.Length == 0 && Godot.FileAccess.GetOpenError() != Error.Ok)
        {
            throw new IOException($"Spielstand konnte nicht gelesen werden ({Godot.FileAccess.GetOpenError()}).");
        }

        return bytes;
    }
}
