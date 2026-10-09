using Game.Core.Data;
using Game.Core.Map;
using Game.Core.State;

namespace Game.Core.Military;

/// <summary>Regeln rund um Armeen, gemeinsam genutzt von Simulation, Commands und Anzeige.</summary>
public static class ArmyRules
{
    /// <summary>Tempo der Armee in Tausendstel Karteneinheiten pro Tick: das der langsamsten enthaltenen Einheit.</summary>
    public static long Speed(GameData data, ArmyState army)
    {
        ArgumentNullException.ThrowIfNull(data);
        ArgumentNullException.ThrowIfNull(army);

        long speed = long.MaxValue;
        foreach (var unitType in data.UnitTypes)
        {
            if (army.GetUnits(unitType.Id) > 0)
            {
                speed = Math.Min(speed, unitType.Speed);
            }
        }

        return speed == long.MaxValue ? 0 : speed;
    }

    /// <summary>Verbleibende Strecke des Marsches in Tausendstel Karteneinheiten.</summary>
    public static long RemainingDistance(ArmyState army)
    {
        long distance = 0;
        for (int i = 0; i < army.Legs.Count; i++)
        {
            var leg = army.Legs[i];
            bool isCurrent = i == 0 && army.Position.From == leg.From && army.Position.To == leg.To;
            distance += leg.StopAt - (isCurrent ? army.Position.Progress : 0);
        }

        return distance;
    }

    /// <summary>Ticks bis zur Ankunft (aufgerundet).</summary>
    public static long RemainingTicks(GameData data, ArmyState army)
    {
        long speed = Speed(data, army);
        return speed == 0 ? 0 : (RemainingDistance(army) + speed - 1) / speed;
    }

    /// <summary>
    /// Provinz, in der die Armee steht: in einer Stadt deren Provinz, auf einem Pfad die Provinz,
    /// in deren Fläche die Position liegt.
    /// </summary>
    public static ProvinceId ProvinceOf(GameData data, ArmyState army)
    {
        if (army.Position.City is { } city)
        {
            return city;
        }

        return MapGeometry.FindProvinceAt(data, data.Graph.PointOf(army.Position).ToMap()) ?? army.Position.From;
    }
}
