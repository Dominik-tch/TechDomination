namespace Game.Core;

/// <summary>Ergebnis einer Prüfung: gültig, oder ungültig mit einem Grund, den UI und KI anzeigen bzw. protokollieren können.</summary>
public readonly record struct ValidationResult
{
    private ValidationResult(string? reason)
    {
        Reason = reason;
    }

    public static ValidationResult Valid { get; } = new(null);

    /// <summary>Grund bei Ungültigkeit, sonst <c>null</c>.</summary>
    public string? Reason { get; }

    public bool IsValid => Reason is null;

    public static ValidationResult Invalid(string reason)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(reason);
        return new ValidationResult(reason);
    }

    public override string ToString() => IsValid ? "gültig" : $"ungültig: {Reason}";
}
