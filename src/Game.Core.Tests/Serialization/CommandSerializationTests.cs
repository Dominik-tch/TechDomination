using System.Text.Json;
using Game.Core.Commands;
using Game.Core.Serialization;
using static Game.Core.Tests.TestIds;

namespace Game.Core.Tests.Serialization;

public class CommandSerializationTests
{
    [Fact]
    public void Envelope_RoundTripsWithConcreteCommandType()
    {
        var envelope = new CommandEnvelope(12, Red, 3, new TransferProvinceCommand(B, Blue));

        string json = JsonSerializer.Serialize(envelope, CoreJson.Options);
        var loaded = JsonSerializer.Deserialize<CommandEnvelope>(json, CoreJson.Options);

        Assert.Equal(envelope, loaded);
        Assert.IsType<TransferProvinceCommand>(loaded!.Command);
    }

    [Fact]
    public void Command_IsWrittenWithTypeName()
    {
        string json = JsonSerializer.Serialize<Command>(new TransferProvinceCommand(B, Blue), CoreJson.Options);

        Assert.Equal("""{"type":"test.transferProvince","command":{"province":1,"newOwner":1}}""", json);
    }

    [Fact]
    public void UnknownTypeName_Throws()
    {
        var error = Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<Command>(
            """{"type":"doesNotExist","command":{}}""", CoreJson.Options));

        Assert.Contains("Unbekannter Command-Typ 'doesNotExist'", error.Message);
    }

    [Theory]
    [InlineData("""{"command":{}}""")]
    [InlineData("""{"type":5,"command":{}}""")]
    [InlineData("""{"type":"test.transferProvince"}""")]
    [InlineData("""{"type":"test.transferProvince","command":null}""")]
    [InlineData("""[]""")]
    public void MalformedCommand_Throws(string json)
    {
        Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<Command>(json, CoreJson.Options));
    }

    [Fact]
    public void UnregisteredCommandType_CannotBeWritten()
    {
        Assert.Throws<InvalidOperationException>(
            () => JsonSerializer.Serialize<Command>(new UnregisteredCommand(), CoreJson.Options));
    }

    [Fact]
    public void Register_SameMappingTwice_IsAllowed()
    {
        CommandTypes.Register<TransferProvinceCommand>(TestCommandRegistration.TransferProvinceName);

        Assert.Equal(typeof(TransferProvinceCommand), CommandTypes.Find(TestCommandRegistration.TransferProvinceName));
    }

    [Fact]
    public void Register_NameForOtherType_Throws()
    {
        Assert.Throws<InvalidOperationException>(
            () => CommandTypes.Register<UnregisteredCommand>(TestCommandRegistration.TransferProvinceName));
    }

    private sealed record UnregisteredCommand : Command
    {
        public override ValidationResult Validate(Game.Core.State.GameState state, Game.Core.Data.GameData data, NationId issuer) =>
            ValidationResult.Valid;

        internal override void Apply(Game.Core.State.GameState state, Game.Core.Data.GameData data, NationId issuer)
        {
        }
    }
}
