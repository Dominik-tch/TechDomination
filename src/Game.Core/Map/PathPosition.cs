namespace Game.Core.Map;

/// <summary>Punkt in Tausendstel Karteneinheiten (siehe docs/architecture.md, Abschnitt 3.4).</summary>
public readonly record struct FinePoint(long X, long Y)
{
    public const long Scale = 1000;

    public static FinePoint FromMap(Data.MapPoint point) => new(point.X * Scale, point.Y * Scale);

    /// <summary>Abgerundet auf ganze Karteneinheiten.</summary>
    public Data.MapPoint ToMap() => new((int)FloorDivide(X, Scale), (int)FloorDivide(Y, Scale));

    public static long DistanceSquared(FinePoint a, FinePoint b)
    {
        long dx = a.X - b.X;
        long dy = a.Y - b.Y;
        return dx * dx + dy * dy;
    }

    // Ganzzahlige Division, die wie Math.Floor in Richtung minus unendlich rundet.
    private static long FloorDivide(long value, long divisor) =>
        value >= 0 ? value / divisor : -((-value + divisor - 1) / divisor);
}

/// <summary>
/// Position im Wegenetz: in der Stadt von <see cref="From"/> (<see cref="To"/> ist <c>null</c>), oder auf dem Pfad
/// von der Stadt <see cref="From"/> zur Stadt <see cref="To"/>, <see cref="Progress"/> Tausendstel Karteneinheiten
/// von <see cref="From"/> entfernt.
/// </summary>
public readonly record struct PathPosition(ProvinceId From, ProvinceId? To, long Progress)
{
    public static PathPosition AtCity(ProvinceId city) => new(city, null, 0);

    public bool IsAtCity => To is null;

    /// <summary>Die Stadt, falls die Position in einer Stadt liegt.</summary>
    public ProvinceId? City => To is null ? From : null;
}

/// <summary>Ein Abschnitt eines Marsches: auf dem Pfad <see cref="From"/> → <see cref="To"/> bis <see cref="StopAt"/> (Tausendstel ab <see cref="From"/>).</summary>
public readonly record struct MoveLeg(ProvinceId From, ProvinceId To, long StopAt);

/// <summary>Ergebnis der Wegsuche: Startposition in passender Ausrichtung und die Abschnitte bis zum Ziel.</summary>
public sealed record Route(PathPosition Start, IReadOnlyList<MoveLeg> Legs, long Distance);
