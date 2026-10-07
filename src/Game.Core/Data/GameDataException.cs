namespace Game.Core.Data;

/// <summary>Fehlerhafte oder fehlende Spieldaten. Die Meldung nennt Datei und Feld.</summary>
public sealed class GameDataException : Exception
{
    public GameDataException(string message)
        : base(message)
    {
    }

    public GameDataException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
