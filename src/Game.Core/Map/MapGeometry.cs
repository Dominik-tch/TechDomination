using Game.Core.Data;

namespace Game.Core.Map;

/// <summary>Geometrische Abfragen auf der Karte, mit Ganzzahl-Rechnung und damit deterministisch.</summary>
public static class MapGeometry
{
    /// <summary>
    /// Findet die Provinz an einem Kartenpunkt, oder <c>null</c>, wenn dort keine liegt.
    /// Liegt der Punkt auf einer gemeinsamen Grenze, gewinnt die Provinz mit der kleineren ID.
    /// </summary>
    public static ProvinceId? FindProvinceAt(GameData data, MapPoint point)
    {
        ArgumentNullException.ThrowIfNull(data);

        foreach (var province in data.Provinces)
        {
            if (Contains(province.Outline, point))
            {
                return province.Id;
            }
        }

        return null;
    }

    /// <summary>Liegt der Punkt im Polygon? Punkte auf dem Rand zählen als innen. Funktioniert auch für nicht-konvexe Polygone.</summary>
    public static bool Contains(IReadOnlyList<MapPoint> polygon, MapPoint point)
    {
        ArgumentNullException.ThrowIfNull(polygon);

        bool inside = false;
        for (int i = 0, j = polygon.Count - 1; i < polygon.Count; j = i++)
        {
            var a = polygon[i];
            var b = polygon[j];

            if (IsOnSegment(a, b, point))
            {
                return true;
            }

            // Strahl von point nach rechts: Kante zählt, wenn sie die Höhe von point kreuzt
            // und der Schnittpunkt rechts von point liegt (ohne Division umgestellt).
            if ((a.Y > point.Y) != (b.Y > point.Y))
            {
                long left = (long)(point.X - a.X) * (b.Y - a.Y);
                long right = (long)(point.Y - a.Y) * (b.X - a.X);
                if (b.Y > a.Y ? left < right : left > right)
                {
                    inside = !inside;
                }
            }
        }

        return inside;
    }

    private static bool IsOnSegment(MapPoint a, MapPoint b, MapPoint p)
    {
        long cross = (long)(b.X - a.X) * (p.Y - a.Y) - (long)(b.Y - a.Y) * (p.X - a.X);
        return cross == 0
            && p.X >= Math.Min(a.X, b.X) && p.X <= Math.Max(a.X, b.X)
            && p.Y >= Math.Min(a.Y, b.Y) && p.Y <= Math.Max(a.Y, b.Y);
    }
}
