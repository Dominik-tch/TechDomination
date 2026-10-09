using Game.Core.Data;
using Game.Core.Map;
using Game.Core.State;

namespace Game.Core.Military;

/// <summary>Neue Einheiten erscheinen in der Stadt und werden einer dort stehenden eigenen Armee zugeschlagen.</summary>
internal static class ArmySpawning
{
    public static void Spawn(GameState state, GameData data, NationId owner, ProvinceId province, UnitTypeId type, int count)
    {
        var units = new int[data.UnitTypes.Count];
        units[type.Value] = count;

        var city = PathPosition.AtCity(province);
        var standing = state.Armies.FirstOrDefault(army =>
            army.Owner == owner && !army.IsMoving && data.Graph.IsSamePosition(army.Position, city));

        if (standing is not null)
        {
            standing.AddUnits(units);
        }
        else
        {
            state.CreateArmy(owner, city, units);
        }
    }
}
