using Game.Core.Events;
using Game.Core.Session;
using Game.Core.State;
using static Game.Core.Tests.TestIds;

namespace Game.Core.Tests.Session;

public class GameSessionTests
{
    private static readonly PlayerId Host = new(0);
    private static readonly PlayerId Guest = new(1);

    [Fact]
    public void NewSession_RunsAtDefaultSpeed()
    {
        var session = NewSession();

        Assert.False(session.IsPaused);
        Assert.Null(session.PausedBy);
        Assert.Equal("normal", session.Speed.Key);
        Assert.Equal(Host, session.Host);
    }

    [Fact]
    public void Submit_SchedulesForCurrentTickWithIncreasingSequence()
    {
        var session = NewSession();

        var first = session.Submit(Red, new TransferProvinceCommand(A, Blue));
        var second = session.Submit(Blue, new TransferProvinceCommand(B, Red));

        Assert.Equal(0, first.ExecuteAtTick);
        Assert.Equal(Red, first.Issuer);
        Assert.True(second.Sequence > first.Sequence);
        Assert.Equal([first, second], session.PendingCommands);
    }

    [Fact]
    public void Submit_DoesNotChangeStateBeforeAdvance()
    {
        var session = NewSession();

        session.Submit(Red, new TransferProvinceCommand(A, Blue));

        Assert.Equal(Red, session.State.GetProvince(A).Owner);
    }

    [Fact]
    public void Advance_ExecutesPendingCommandsAndClearsQueue()
    {
        var session = NewSession();
        session.Submit(Red, new TransferProvinceCommand(A, Blue));

        var events = session.Advance();

        Assert.Empty(events);
        Assert.Equal(Blue, session.State.GetProvince(A).Owner);
        Assert.Equal(1, session.State.Tick);
        Assert.Empty(session.PendingCommands);
    }

    [Fact]
    public void Advance_ReturnsRejections()
    {
        var session = NewSession();
        var envelope = session.Submit(Red, new TransferProvinceCommand(B, Red));

        var events = session.Advance();

        Assert.Same(envelope, Assert.IsType<CommandRejected>(Assert.Single(events)).Envelope);
    }

    [Fact]
    public void CommandSubmittedAfterAdvance_IsScheduledForFollowingTick()
    {
        var session = NewSession();
        session.Advance();

        var envelope = session.Submit(Red, new TransferProvinceCommand(A, Blue));

        Assert.Equal(1, envelope.ExecuteAtTick);
    }

    [Fact]
    public void Advance_WhilePaused_DoesNothingAndKeepsQueue()
    {
        var session = NewSession();
        session.Submit(Red, new TransferProvinceCommand(A, Blue));
        session.Pause(Guest);

        var events = session.Advance();

        Assert.Empty(events);
        Assert.Equal(0, session.State.Tick);
        Assert.Equal(Red, session.State.GetProvince(A).Owner);
        Assert.Single(session.PendingCommands);
    }

    [Fact]
    public void QueuedCommand_RunsAfterResume()
    {
        var session = NewSession();
        session.Submit(Red, new TransferProvinceCommand(A, Blue));
        session.Pause(Host);
        session.Advance();

        session.Resume(Host);
        session.Advance();

        Assert.Equal(Blue, session.State.GetProvince(A).Owner);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    public void AnyPlayer_CanPause_AndIsRecorded(int player)
    {
        var session = NewSession();

        var result = session.Pause(new PlayerId(player));

        Assert.True(result.IsValid);
        Assert.True(session.IsPaused);
        Assert.Equal(new PlayerId(player), session.PausedBy);
    }

    [Fact]
    public void Pause_WhenAlreadyPaused_IsRejectedAndKeepsFirstPauser()
    {
        var session = NewSession();
        session.Pause(Guest);

        var result = session.Pause(Host);

        Assert.False(result.IsValid);
        Assert.Equal(Guest, session.PausedBy);
    }

    [Theory]
    [InlineData(0, 1)]
    [InlineData(1, 0)]
    [InlineData(1, 1)]
    public void AnyPlayer_CanResume_RegardlessOfWhoPaused(int pausedBy, int resumedBy)
    {
        var session = NewSession();
        session.Pause(new PlayerId(pausedBy));

        var result = session.Resume(new PlayerId(resumedBy));

        Assert.True(result.IsValid);
        Assert.False(session.IsPaused);
        Assert.Null(session.PausedBy);
    }

    [Fact]
    public void Resume_WhenNotPaused_IsRejected()
    {
        Assert.False(NewSession().Resume(Host).IsValid);
    }

    [Fact]
    public void SetSpeed_ByHost_ChangesSpeed()
    {
        var session = NewSession();

        var result = session.SetSpeed(Host, "fast");

        Assert.True(result.IsValid);
        Assert.Equal(20, session.Speed.TicksPerSecond);
    }

    [Fact]
    public void SetSpeed_ByGuest_IsRejectedAndKeepsSpeed()
    {
        var session = NewSession();

        var result = session.SetSpeed(Guest, "fast");

        Assert.Equal("Nur der Host darf die Geschwindigkeit ändern.", result.Reason);
        Assert.Equal("normal", session.Speed.Key);
    }

    [Fact]
    public void SetSpeed_UnknownLevel_IsRejected()
    {
        var session = NewSession();

        var result = session.SetSpeed(Host, "turbo");

        Assert.False(result.IsValid);
        Assert.Equal("normal", session.Speed.Key);
    }

    [Fact]
    public void SetSpeed_WhilePaused_IsAllowedAndKeepsPause()
    {
        var session = NewSession();
        session.Pause(Guest);

        Assert.True(session.SetSpeed(Host, "slow").IsValid);
        Assert.True(session.IsPaused);
    }

    private static GameSession NewSession()
    {
        var data = TestData.Load();
        return new GameSession(data, GameStateFactory.CreateNew(data, seed: 1), Host);
    }
}
