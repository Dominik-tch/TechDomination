namespace Game.Core.Tests;

/// <summary>Findet Verzeichnisse im Repository, unabhängig davon, wo die Tests ausgeführt werden.</summary>
internal static class RepositoryPaths
{
    public static string GodotDataDirectory => Path.Combine(FindRoot(), "godot", "data");

    private static string FindRoot()
    {
        for (var dir = new DirectoryInfo(AppContext.BaseDirectory); dir is not null; dir = dir.Parent)
        {
            if (File.Exists(Path.Combine(dir.FullName, "godot", "project.godot")))
            {
                return dir.FullName;
            }
        }

        throw new DirectoryNotFoundException("Repository-Wurzel (mit godot/project.godot) nicht gefunden.");
    }
}
