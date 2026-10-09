using System.Runtime.CompilerServices;
using Game.Core.Commands;

namespace Game.Core.Tests;

/// <summary>Registriert die Test-Commands für die Serialisierung, bevor irgendein Test läuft.</summary>
internal static class TestCommandRegistration
{
    public const string TransferProvinceName = "test.transferProvince";

    [ModuleInitializer]
    internal static void Register() => CommandTypes.Register<TransferProvinceCommand>(TransferProvinceName);
}
