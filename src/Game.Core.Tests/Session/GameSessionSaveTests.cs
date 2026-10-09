using Game.Core.Data;
using Game.Core.Determinism;
using Game.Core.Save;
using Game.Core.Session;
using Game.Core.State;
using static Game.Core.Tests.TestIds;

namespace Game.Core.Tests.Session;

public class GameSessionSaveTests
{
    private static readonly PlayerId Host = new(0);
    private static readonly PlayerId Guest = new(1);

    private readonly GameData _data = TestData.Load();

    [Fact]
    public void LoadedSession_IsPausedByHost()
    {
        var session = NewSession();

        var loaded = SaveAndLoad(session);

        Assert.True(loaded.IsPaused);
        Assert.Equal(Host, loaded.PausedBy);
    }

    [Fact]
    public void LoadedSession_KeepsSpeed()
    {
        var session = NewSession();
        session.SetSpeed(Host, "slow");

        Assert.Equal("slow", SaveAndLoad(session).Speed.Key);
    }

    [Fact]
    public void LoadedSession_ContinuesSequenceNumbers()
    {
        var session = NewSession();
        var before = session.Submit(Red, new TransferProvinceCommand(A, Blue));
        session.Advance();

        var loaded = SaveAndLoad(session);
        var after = loaded.Submit(Red, new TransferProvinceCommand(A, Blue));

        Assert.True(after.Sequence > before.Sequence);
    }

    [Fact]
    public void PendingCommands_SurviveSaveAndLoad()
    {
        var session = NewSession();
        session.Pause(Guest);
        session.Submit(Red, new TransferProvinceCommand(A, Blue));

        var loaded = SaveAndLoad(session);
        loaded.Resume(Host);
        loaded.Advance();

        Assert.Equal(Blue, loaded.State.GetProvince(A).Owner);
    }

    [Fact]
    public void Construction_SurvivesSaveAndLoad()
    {
        var session = NewSession();
        session.Submit(Red, new Game.Core.Commands.BuildBuildingCommand(A, Mine));
        session.Advance();

        var loaded = SaveAndLoad(session);
        loaded.Resume(Host);
        loaded.Advance();
        loaded.Advance();

        Assert.Equal(1, loaded.State.GetProvince(A).GetBuildingLevel(Mine));
    }

    [Fact]
    public void FromSaveGame_WithOtherData_IsRejected()
    {
        var save = NewSession().CreateSaveGame();
        var otherEconomy = TestData.Economy();
        otherEconomy["startMoney"] = 1;
        var otherData = GameDataLoader.Load(TestData.Files(economy: otherEconomy));

        Assert.Throws<SaveGameException>(() => GameSession.FromSaveGame(otherData, save, Host));
    }

    [Fact]
    public void SaveFileMidGame_ProducesSameHashAsContinuousGame()
    {
        // Kerntest aus docs/architecture.md, Abschnitt 8, diesmal über die echte (komprimierte) Spielstanddatei,
        // inklusive eines Commands, der während der Pause eingereicht wurde und beim Speichern noch wartet.
        const int ticksBeforeSave = 37;
        const int ticksAfterLoad = 50;

        var continuous = NewSession();
        Play(continuous, ticksBeforeSave);
        continuous.Pause(Guest);
        continuous.Submit(Blue, new TransferProvinceCommand(C, Red));
        continuous.Submit(Blue, new Game.Core.Commands.BuildBuildingCommand(B, Mine));
        continuous.Resume(Guest);
        Play(continuous, ticksAfterLoad);

        var interrupted = NewSession();
        Play(interrupted, ticksBeforeSave);
        interrupted.Pause(Guest);
        interrupted.Submit(Blue, new TransferProvinceCommand(C, Red));
        interrupted.Submit(Blue, new Game.Core.Commands.BuildBuildingCommand(B, Mine));
        var resumed = SaveAndLoad(interrupted);
        resumed.Resume(Host);
        Play(resumed, ticksAfterLoad);

        Assert.Equal(continuous.State.Tick, resumed.State.Tick);
        Assert.Equal(StateHasher.ComputeHash(continuous.State), StateHasher.ComputeHash(resumed.State));
    }

    private GameSession NewSession() => new(_data, GameStateFactory.CreateNew(_data, seed: 5), Host);

    private GameSession SaveAndLoad(GameSession session)
    {
        using var stream = new MemoryStream();
        SaveGameSerializer.Write(session.CreateSaveGame(), stream);
        stream.Position = 0;
        return GameSession.FromSaveGame(_data, SaveGameSerializer.Read(stream, _data), Host);
    }

    // Spielt mit einem festen Muster von Commands, damit Zustand und Sequenznummern sich laufend ändern.
    private static void Play(GameSession session, int ticks)
    {
        for (int i = 0; i < ticks; i++)
        {
            if (session.State.Tick % 5 == 0)
            {
                var owner = session.State.GetProvince(A).Owner;
                session.Submit(owner, new TransferProvinceCommand(A, owner == Red ? Blue : Red));
            }

            session.Advance();
        }
    }
}
