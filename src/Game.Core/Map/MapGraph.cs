using Game.Core.Data;

namespace Game.Core.Map;

/// <summary>
/// Wegenetz: Städte als Knoten, gerade Pfade zwischen den Städten benachbarter Provinzen als Kanten.
/// Alle Längen und Positionen in Tausendstel Karteneinheiten (siehe docs/architecture.md, Abschnitt 3.4).
/// </summary>
public sealed class MapGraph
{
    private readonly IReadOnlyList<ProvinceDefinition> _provinces;
    private readonly Dictionary<(int, int), long> _lengths = [];

    internal MapGraph(IReadOnlyList<ProvinceDefinition> provinces)
    {
        _provinces = provinces;
        foreach (var province in provinces)
        {
            foreach (var neighbor in province.Neighbors)
            {
                var a = FinePoint.FromMap(province.City);
                var b = FinePoint.FromMap(provinces[neighbor.Value].City);
                _lengths[(province.Id.Value, neighbor.Value)] = Math.Max(1, IntegerMath.Sqrt(FinePoint.DistanceSquared(a, b)));
            }
        }
    }

    /// <summary>Alle Pfade, jeweils einmal (kleinere ID zuerst), in fester Reihenfolge.</summary>
    public IEnumerable<(ProvinceId A, ProvinceId B)> Edges =>
        _provinces.SelectMany(p => p.Neighbors.Where(n => n.Value > p.Id.Value).Select(n => (p.Id, n)));

    public FinePoint CityPoint(ProvinceId city) => FinePoint.FromMap(_provinces[city.Value].City);

    public bool AreConnected(ProvinceId a, ProvinceId b) => _lengths.ContainsKey((a.Value, b.Value));

    /// <summary>Länge des Pfads zwischen zwei benachbarten Städten.</summary>
    public long Length(ProvinceId a, ProvinceId b) => _lengths[(a.Value, b.Value)];

    /// <summary>Ist die Position gültig (bekannte Städte, Pfad existiert, Fortschritt im Pfad)?</summary>
    public bool IsValid(PathPosition position)
    {
        if (position.From.Value < 0 || position.From.Value >= _provinces.Count)
        {
            return false;
        }

        return position.To is not { } to
            ? position.Progress == 0
            : AreConnected(position.From, to) && position.Progress >= 0 && position.Progress <= Length(position.From, to);
    }

    /// <summary>Gleiche physische Position, aber eindeutig: Anfang oder Ende eines Pfads wird zur Stadt.</summary>
    public PathPosition Normalize(PathPosition position)
    {
        if (position.To is not { } to)
        {
            return position;
        }

        if (position.Progress <= 0)
        {
            return PathPosition.AtCity(position.From);
        }

        return position.Progress >= Length(position.From, to) ? PathPosition.AtCity(to) : position;
    }

    /// <summary>Koordinate einer Position.</summary>
    public FinePoint PointOf(PathPosition position)
    {
        var from = CityPoint(position.From);
        if (position.To is not { } to)
        {
            return from;
        }

        var target = CityPoint(to);
        long length = Length(position.From, to);
        return new FinePoint(
            from.X + (target.X - from.X) * position.Progress / length,
            from.Y + (target.Y - from.Y) * position.Progress / length);
    }

    /// <summary>
    /// Der Punkt im Wegenetz, der <paramref name="point"/> am nächsten liegt. Städte ohne Pfade (Inseln) zählen mit.
    /// Liegt eine Stadt höchstens <paramref name="citySnapDistance"/> entfernt, ist sie das Ergebnis – so trifft ein Klick
    /// neben die Stadt die Stadt und nicht den Pfad dahinter. Bei gleichem Abstand gewinnen die kleineren IDs.
    /// </summary>
    public PathPosition NearestPosition(FinePoint point, long citySnapDistance = 0)
    {
        PathPosition best = PathPosition.AtCity(_provinces[0].Id);
        long bestDistance = long.MaxValue;

        foreach (var province in _provinces)
        {
            long distance = FinePoint.DistanceSquared(point, CityPoint(province.Id));
            if (distance < bestDistance)
            {
                (best, bestDistance) = (PathPosition.AtCity(province.Id), distance);
            }
        }

        if (bestDistance <= citySnapDistance * citySnapDistance)
        {
            return best;
        }

        foreach (var (a, b) in Edges)
        {
            var start = CityPoint(a);
            var end = CityPoint(b);
            long length = Length(a, b);
            long dot = (point.X - start.X) * (end.X - start.X) + (point.Y - start.Y) * (end.Y - start.Y);
            long progress = Math.Clamp(dot / length, 0, length);
            var candidate = new PathPosition(a, b, progress);
            long distance = FinePoint.DistanceSquared(point, PointOf(candidate));
            if (distance < bestDistance)
            {
                (best, bestDistance) = (Normalize(candidate), distance);
            }
        }

        return best;
    }

    /// <summary>
    /// Kürzester Weg von <paramref name="start"/> nach <paramref name="target"/> über das Wegenetz,
    /// oder <c>null</c>, wenn das Ziel nicht erreichbar ist oder Start und Ziel gleich sind.
    /// Bei gleich langen Wegen entscheidet eine feste Reihenfolge, damit das Ergebnis deterministisch ist.
    /// </summary>
    public Route? FindRoute(PathPosition start, PathPosition target)
    {
        start = Normalize(start);
        target = Normalize(target);
        if (start == target || IsSamePoint(start, target))
        {
            return null;
        }

        if (DirectOnSameEdge(start, target) is { } direct)
        {
            return direct;
        }

        // Mögliche Einstiege ins Netz: die Stadt selbst oder beide Enden des aktuellen Pfads.
        var sources = new List<(ProvinceId Node, long Offset, PathPosition Start, MoveLeg? FirstLeg)>();
        if (start.To is not { } startTo)
        {
            sources.Add((start.From, 0, start, null));
        }
        else
        {
            long length = Length(start.From, startTo);
            sources.Add((startTo, length - start.Progress, start, new MoveLeg(start.From, startTo, length)));
            var reversed = new PathPosition(startTo, start.From, length - start.Progress);
            sources.Add((start.From, start.Progress, reversed, new MoveLeg(startTo, start.From, length)));
        }

        // Mögliche Ausstiege: die Zielstadt oder beide Enden des Zielpfads.
        var sinks = new List<(ProvinceId Node, long Offset, MoveLeg? LastLeg)>();
        if (target.To is not { } targetTo)
        {
            sinks.Add((target.From, 0, null));
        }
        else
        {
            long length = Length(target.From, targetTo);
            sinks.Add((target.From, target.Progress, new MoveLeg(target.From, targetTo, target.Progress)));
            sinks.Add((targetTo, length - target.Progress, new MoveLeg(targetTo, target.From, length - target.Progress)));
        }

        var (distances, previous, origin) = Dijkstra(sources.Select(s => (s.Node, s.Offset)).ToList());

        long bestDistance = long.MaxValue;
        (ProvinceId Node, long Offset, MoveLeg? LastLeg)? bestSink = null;
        foreach (var sink in sinks)
        {
            if (distances.TryGetValue(sink.Node.Value, out long distance) && distance + sink.Offset < bestDistance)
            {
                bestDistance = distance + sink.Offset;
                bestSink = sink;
            }
        }

        if (bestSink is not { } chosen)
        {
            return null;
        }

        // Knotenfolge rückwärts aufbauen.
        var nodes = new List<int> { chosen.Node.Value };
        while (previous.TryGetValue(nodes[^1], out int before))
        {
            nodes.Add(before);
        }

        nodes.Reverse();
        var source = sources[origin[nodes[0]]];

        var legs = new List<MoveLeg>();
        if (source.FirstLeg is { } first)
        {
            legs.Add(first);
        }

        for (int i = 0; i + 1 < nodes.Count; i++)
        {
            var from = new ProvinceId(nodes[i]);
            var to = new ProvinceId(nodes[i + 1]);
            legs.Add(new MoveLeg(from, to, Length(from, to)));
        }

        if (chosen.LastLeg is { } last)
        {
            legs.Add(last);
        }

        return new Route(source.Start, legs, bestDistance);
    }

    private Route? DirectOnSameEdge(PathPosition start, PathPosition target)
    {
        if (start.To is not { } startTo || target.To is not { } targetTo)
        {
            return null;
        }

        long length = Length(start.From, startTo);
        long targetProgress;
        if (target.From == start.From && targetTo == startTo)
        {
            targetProgress = target.Progress;
        }
        else if (target.From == startTo && targetTo == start.From)
        {
            targetProgress = length - target.Progress;
        }
        else
        {
            return null;
        }

        if (targetProgress > start.Progress)
        {
            return new Route(start, [new MoveLeg(start.From, startTo, targetProgress)], targetProgress - start.Progress);
        }

        // Umkehren: gleiche Position, Pfad in Gegenrichtung betrachtet.
        var reversed = new PathPosition(startTo, start.From, length - start.Progress);
        return new Route(reversed, [new MoveLeg(startTo, start.From, length - targetProgress)], start.Progress - targetProgress);
    }

    /// <summary>Beschreiben beide Positionen denselben Punkt im Wegenetz (auch in entgegengesetzter Pfadrichtung)?</summary>
    public bool IsSamePosition(PathPosition a, PathPosition b)
    {
        a = Normalize(a);
        b = Normalize(b);
        return a == b || IsSamePoint(a, b);
    }

    private bool IsSamePoint(PathPosition a, PathPosition b) =>
        a.To is { } aTo && b.To is { } bTo && a.From == bTo && aTo == b.From && a.Progress == Length(a.From, aTo) - b.Progress;

    // Dijkstra mit mehreren Startknoten (je mit Anfangsabstand). Gleichstand: kleinere Knoten-ID zuerst, erster Fund gewinnt.
    private (Dictionary<int, long> Distances, Dictionary<int, int> Previous, Dictionary<int, int> Origin) Dijkstra(
        IReadOnlyList<(ProvinceId Node, long Offset)> sources)
    {
        var distances = new Dictionary<int, long>();
        var previous = new Dictionary<int, int>();
        var origin = new Dictionary<int, int>();
        var queue = new SortedSet<(long Distance, int Node)>();

        for (int i = 0; i < sources.Count; i++)
        {
            int node = sources[i].Node.Value;
            if (!distances.TryGetValue(node, out long known) || sources[i].Offset < known)
            {
                if (distances.ContainsKey(node))
                {
                    queue.Remove((known, node));
                }

                distances[node] = sources[i].Offset;
                origin[node] = i;
                queue.Add((sources[i].Offset, node));
            }
        }

        while (queue.Count > 0)
        {
            var (distance, node) = queue.Min;
            queue.Remove(queue.Min);

            foreach (var neighbor in _provinces[node].Neighbors)
            {
                long candidate = distance + Length(new ProvinceId(node), neighbor);
                if (distances.TryGetValue(neighbor.Value, out long known) && known <= candidate)
                {
                    continue;
                }

                if (distances.ContainsKey(neighbor.Value))
                {
                    queue.Remove((known, neighbor.Value));
                }

                distances[neighbor.Value] = candidate;
                previous[neighbor.Value] = node;
                origin[neighbor.Value] = origin[node];
                queue.Add((candidate, neighbor.Value));
            }
        }

        return (distances, previous, origin);
    }
}
