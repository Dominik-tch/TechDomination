namespace Game.Core.Save;

/// <summary>Ein Spielstand kann nicht geladen werden. Die Meldung ist für den Spieler bestimmt.</summary>
public sealed class SaveGameException : Exception
{
    public SaveGameException(string message)
        : base(message)
    {
    }

    public SaveGameException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
