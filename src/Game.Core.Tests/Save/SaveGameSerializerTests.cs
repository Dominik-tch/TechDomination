using System.IO.Compression;
using System.Text;
using System.Text.Json.Nodes;
using Game.Core.Data;
using Game.Core.Determinism;
using Game.Core.Save;
using Game.Core.Session;
using Game.Core.State;
using static Game.Core.Tests.TestIds;

namespace Game.Core.Tests.Save;

public class SaveGameSerializerTests
{
    private static readonly PlayerId Host = new(0);

    private readonly GameData _data = TestData.Load();

    [Fact]
    public void WriteAndRead_PreservesStateHash()
    {
        var session = RunningSession(ticks: 9);

        var loaded = SaveGameSerializer.Read(Write(session.CreateSaveGame()), _data);

        Assert.Equal(StateHasher.ComputeHash(session.State), StateHasher.ComputeHash(loaded.State));
        Assert.Equal(SaveGame.CurrentFormatVersion, loaded.FormatVersion);
        Assert.Equal(_data.ContentHash, loaded.DataHash);
    }

    [Fact]
    public void WriteAndRead_PreservesSessionPart()
    {
        var session = RunningSession(ticks: 2);
        session.SetSpeed(Host, "fast");
        session.Pause(Host);
        var pending = session.Submit(Red, new TransferProvinceCommand(A, Blue));

        var loaded = SaveGameSerializer.Read(Write(session.CreateSaveGame()), _data);

        Assert.Equal("fast", loaded.Session.SpeedLevel);
        Assert.Equal(pending.Sequence + 1, loaded.Session.NextSequence);
        Assert.Equal([pending], loaded.Session.PendingCommands);
    }

    [Fact]
    public void Write_ProducesGzip()
    {
        byte[] bytes = Write(RunningSession(ticks: 1).CreateSaveGame()).ToArray();

        Assert.Equal(new byte[] { 0x1F, 0x8B }, bytes[..2]);
    }

    [Fact]
    public void Read_OtherFormatVersion_IsRejected()
    {
        var stream = WriteModified(json => json["formatVersion"] = SaveGame.CurrentFormatVersion + 1);

        var error = Assert.Throws<SaveGameException>(() => SaveGameSerializer.Read(stream, _data));

        Assert.Contains("anderen Spielversion", error.Message);
    }

    [Fact]
    public void Read_OtherData_IsRejected()
    {
        var stream = Write(RunningSession(ticks: 1).CreateSaveGame());
        var changedData = TestData.Economy();
        changedData["taxPerProvince"] = 3;

        var error = Assert.Throws<SaveGameException>(
            () => SaveGameSerializer.Read(stream, GameDataLoader.Load(TestData.Files(economy: changedData))));

        Assert.Contains("anderen Spieldaten", error.Message);
    }

    [Fact]
    public void Read_NotGzip_IsRejected()
    {
        var stream = new MemoryStream(Encoding.UTF8.GetBytes("""{"formatVersion":1}"""));

        var error = Assert.Throws<SaveGameException>(() => SaveGameSerializer.Read(stream, _data));

        Assert.Contains("beschädigt", error.Message);
    }

    [Fact]
    public void Read_GzipWithInvalidJson_IsRejected()
    {
        var error = Assert.Throws<SaveGameException>(() => SaveGameSerializer.Read(Gzip("{ kaputt"), _data));

        Assert.Contains("beschädigt", error.Message);
    }

    [Fact]
    public void Read_MissingState_IsRejected()
    {
        var stream = WriteModified(json => json.Remove("state"));

        Assert.Throws<SaveGameException>(() => SaveGameSerializer.Read(stream, _data));
    }

    [Fact]
    public void Read_UnknownCommandType_IsRejected()
    {
        var session = RunningSession(ticks: 1);
        session.Pause(Host);
        session.Submit(Red, new TransferProvinceCommand(A, Blue));
        var stream = WriteModified(session, json => json["session"]!["pendingCommands"]![0]!["command"]!["type"] = "gone");

        var error = Assert.Throws<SaveGameException>(() => SaveGameSerializer.Read(stream, _data));

        Assert.Contains("Unbekannter Command-Typ 'gone'", error.Message);
    }

    [Fact]
    public void Read_ProvinceCountNotMatchingData_IsRejected()
    {
        var stream = WriteModified(json => json["state"]!["provinces"]!.AsArray().RemoveAt(2));

        Assert.Throws<SaveGameException>(() => SaveGameSerializer.Read(stream, _data));
    }

    [Fact]
    public void Read_UnknownOwner_IsRejected()
    {
        var stream = WriteModified(json => json["state"]!["provinces"]![0]!["owner"] = 7);

        Assert.Throws<SaveGameException>(() => SaveGameSerializer.Read(stream, _data));
    }

    [Fact]
    public void Read_NullPendingCommands_IsRejected()
    {
        var stream = WriteModified(json => json["session"]!["pendingCommands"] = null);

        Assert.Throws<SaveGameException>(() => SaveGameSerializer.Read(stream, _data));
    }

    [Fact]
    public void ReadSummary_ReturnsVersionAndTick()
    {
        var summary = SaveGameSerializer.ReadSummary(Write(RunningSession(ticks: 17).CreateSaveGame()));

        Assert.Equal(new SaveGameSummary(SaveGame.CurrentFormatVersion, 17), summary);
    }

    [Fact]
    public void ReadSummary_OfBrokenFile_IsNull()
    {
        Assert.Null(SaveGameSerializer.ReadSummary(new MemoryStream([1, 2, 3])));
        Assert.Null(SaveGameSerializer.ReadSummary(Gzip("""{"formatVersion":1}""")));
    }

    private GameSession RunningSession(int ticks)
    {
        var session = new GameSession(_data, GameStateFactory.CreateNew(_data, seed: 3), Host);
        for (int i = 0; i < ticks; i++)
        {
            session.Advance();
        }

        return session;
    }

    private static MemoryStream Write(SaveGame save)
    {
        var stream = new MemoryStream();
        SaveGameSerializer.Write(save, stream);
        stream.Position = 0;
        return stream;
    }

    private MemoryStream WriteModified(Action<JsonObject> modify) => WriteModified(RunningSession(ticks: 1), modify);

    // Schreibt einen echten Spielstand, verändert das JSON und komprimiert es wieder.
    private static MemoryStream WriteModified(GameSession session, Action<JsonObject> modify)
    {
        using var gzip = new GZipStream(Write(session.CreateSaveGame()), CompressionMode.Decompress);
        var json = JsonNode.Parse(gzip)!.AsObject();
        modify(json);
        return Gzip(json.ToJsonString());
    }

    private static MemoryStream Gzip(string content)
    {
        var stream = new MemoryStream();
        using (var gzip = new GZipStream(stream, CompressionLevel.Fastest, leaveOpen: true))
        {
            gzip.Write(Encoding.UTF8.GetBytes(content));
        }

        stream.Position = 0;
        return stream;
    }
}
