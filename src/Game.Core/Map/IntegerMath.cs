namespace Game.Core.Map;

/// <summary>Ganzzahlige Rechenhilfen, damit Geometrie ohne Gleitkomma und damit deterministisch bleibt.</summary>
public static class IntegerMath
{
    /// <summary>Ganzzahlige Quadratwurzel: die größte Zahl r mit r * r ≤ <paramref name="value"/>.</summary>
    public static long Sqrt(long value)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(value);
        if (value < 2)
        {
            return value;
        }

        // Newton-Verfahren, startet oberhalb der Wurzel und fällt monoton auf sie herab.
        // (x + 1) / 2 ohne Überlauf bei long.MaxValue.
        long x = value;
        long y = x / 2 + (x & 1);
        while (y < x)
        {
            x = y;
            y = (x + value / x) / 2;
        }

        return x;
    }
}
