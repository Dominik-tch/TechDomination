using Game.Core.Commands;
using Game.Core.Data;
using Game.Core.State;

namespace Game.Core.Tests;

/// <summary>
/// Gibt eine eigene Provinz an eine andere Nation. Existiert nur im Testprojekt, um die Command-Pipeline
/// unabhängig von Spiel-Features zu prüfen.
/// </summary>
internal sealed record TransferProvinceCommand(ProvinceId Province, NationId NewOwner) : Command
{
    public override ValidationResult Validate(GameState state, GameData data, NationId issuer)
    {
        if (Province.Value < 0 || Province.Value >= data.Provinces.Count)
        {
            return ValidationResult.Invalid("Unbekannte Provinz.");
        }

        if (NewOwner.Value < 0 || NewOwner.Value >= data.Nations.Count)
        {
            return ValidationResult.Invalid("Unbekannte Nation.");
        }

        return state.GetProvince(Province).Owner == issuer
            ? ValidationResult.Valid
            : ValidationResult.Invalid("Die Provinz gehört nicht der eigenen Nation.");
    }

    internal override void Apply(GameState state, GameData data, NationId issuer) =>
        state.GetProvince(Province).Owner = NewOwner;
}

/// <summary>Bekannte IDs der Testkarte aus <see cref="TestData"/>.</summary>
internal static class TestIds
{
    public static readonly NationId Red = new(0);
    public static readonly NationId Blue = new(1);

    public static readonly ProvinceId A = new(0);
    public static readonly ProvinceId B = new(1);
    public static readonly ProvinceId C = new(2);
}
