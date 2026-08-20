using 健康助手.Models;
using 健康助手.Services;

namespace 健康助手.Tests;

public class PomodoroEngineTests
{
    private static readonly DateTime BaseTime = new(2026, 1, 1, 10, 0, 0);

    [Fact]
    public void Start_BeginsFocusWithCorrectEndTime()
    {
        var (engine, clock) = CreateEngine();

        engine.Start();

        Assert.Equal(PomodoroPhase.Focus, engine.Phase);
        Assert.Equal(BaseTime.AddMinutes(25), engine.PhaseEndAt);
        Assert.Equal(TimeSpan.FromMinutes(25), engine.TotalDuration);
        Assert.Equal(TimeSpan.FromMinutes(25), engine.Remaining);
    }

    [Fact]
    public void Start_WhileActive_IsIgnored()
    {
        var (engine, clock) = CreateEngine();
        engine.Start();
        var end = engine.PhaseEndAt;

        engine.Start();

        Assert.Equal(end, engine.PhaseEndAt);
        Assert.Equal(PomodoroPhase.Focus, engine.Phase);
    }

    [Fact]
    public void Start_WhileAwaitingNextPhase_IsIgnored()
    {
        var (engine, clock) = CreateEngine();
        engine.Start();
        clock.Advance(TimeSpan.FromMinutes(25));
        engine.CheckPhaseEnd();

        engine.Start();

        Assert.Equal(PomodoroPhase.Idle, engine.Phase);
        Assert.Equal(PomodoroPhase.ShortBreak, engine.NextPhase);
    }

    [Fact]
    public void CheckPhaseEnd_FocusCompletesOnceAndAdvancesCount()
    {
        var (engine, clock) = CreateEngine();
        engine.Start();
        var ended = 0;
        PomodoroPhaseEndedInfo? lastInfo = null;
        engine.PhaseEnded += info => { ended++; lastInfo = info; };

        clock.Advance(TimeSpan.FromMinutes(25));
        engine.CheckPhaseEnd();
        engine.CheckPhaseEnd();

        Assert.Equal(1, ended);
        Assert.Equal(PomodoroPhase.Idle, engine.Phase);
        Assert.Equal(1, engine.CompletedFocusCount);
        Assert.Equal(PomodoroPhase.ShortBreak, engine.NextPhase);
        Assert.Equal(PomodoroPhase.Focus, lastInfo!.CompletedPhase);
        Assert.Equal(PomodoroPhase.ShortBreak, lastInfo.NextPhase);
        Assert.Equal(1, lastInfo.CompletedFocusCount);
        Assert.Null(engine.PhaseEndAt);
    }

    [Fact]
    public void FourthFocusCompletion_PendingNextIsLongBreak()
    {
        var (engine, clock) = CreateEngine();

        for (var i = 0; i < 3; i++)
        {
            engine.Start();
            clock.Advance(TimeSpan.FromMinutes(25));
            engine.CheckPhaseEnd();
            engine.StartNextPhase();
            clock.Advance(TimeSpan.FromMinutes(5));
            engine.CheckPhaseEnd();
            engine.StartNextPhase();
        }

        engine.Start();
        clock.Advance(TimeSpan.FromMinutes(25));
        engine.CheckPhaseEnd();

        Assert.Equal(4, engine.CompletedFocusCount);
        Assert.Equal(PomodoroPhase.LongBreak, engine.NextPhase);
    }

    [Fact]
    public void LongBreakCompletion_ResetsCountAndNextIsFocus()
    {
        var (engine, clock) = CreateEngine();

        for (var i = 0; i < 3; i++)
        {
            engine.Start();
            clock.Advance(TimeSpan.FromMinutes(25));
            engine.CheckPhaseEnd();
            engine.StartNextPhase();
            clock.Advance(TimeSpan.FromMinutes(5));
            engine.CheckPhaseEnd();
            engine.StartNextPhase();
        }

        engine.Start();
        clock.Advance(TimeSpan.FromMinutes(25));
        engine.CheckPhaseEnd();
        engine.StartNextPhase();
        Assert.Equal(PomodoroPhase.LongBreak, engine.Phase);

        clock.Advance(TimeSpan.FromMinutes(15));
        engine.CheckPhaseEnd();

        Assert.Equal(0, engine.CompletedFocusCount);
        Assert.Equal(PomodoroPhase.Focus, engine.NextPhase);
    }

    [Fact]
    public void Pause_FreezesRemaining_ResumeRecomputesEnd()
    {
        var (engine, clock) = CreateEngine();
        engine.Start();
        clock.Advance(TimeSpan.FromMinutes(10));

        engine.Pause();

        Assert.True(engine.IsPaused);
        Assert.Equal(TimeSpan.FromMinutes(15), engine.Remaining);
        Assert.Null(engine.PhaseEndAt);

        clock.Advance(TimeSpan.FromMinutes(20));
        Assert.Equal(TimeSpan.FromMinutes(15), engine.Remaining);

        engine.Resume();

        Assert.False(engine.IsPaused);
        Assert.Equal(clock.Now.AddMinutes(15), engine.PhaseEndAt);
        Assert.Equal(PomodoroPhase.Focus, engine.Phase);
    }

    [Fact]
    public void CheckPhaseEnd_WhilePaused_DoesNotFire()
    {
        var (engine, clock) = CreateEngine();
        engine.Start();
        engine.Pause();
        var ended = 0;
        engine.PhaseEnded += _ => ended++;

        clock.Advance(TimeSpan.FromHours(1));
        engine.CheckPhaseEnd();

        Assert.Equal(0, ended);
        Assert.True(engine.IsPaused);
    }

    [Fact]
    public void Skip_WhileFocusRunning_StartsShortBreakWithoutCounting()
    {
        var (engine, clock) = CreateEngine();
        engine.Start();

        engine.Skip();

        Assert.Equal(PomodoroPhase.ShortBreak, engine.Phase);
        Assert.Equal(0, engine.CompletedFocusCount);
        Assert.Equal(clock.Now.AddMinutes(5), engine.PhaseEndAt);
    }

    [Fact]
    public void Skip_WhileShortBreak_StartsFocus()
    {
        var (engine, clock) = CreateEngine();
        engine.Start();
        engine.Skip();

        engine.Skip();

        Assert.Equal(PomodoroPhase.Focus, engine.Phase);
        Assert.Equal(clock.Now.AddMinutes(25), engine.PhaseEndAt);
    }

    [Fact]
    public void Skip_WhileAwaitingNextPhase_StartsFocusWithoutCountChange()
    {
        var (engine, clock) = CreateEngine();
        engine.Start();
        clock.Advance(TimeSpan.FromMinutes(25));
        engine.CheckPhaseEnd();

        engine.Skip();

        Assert.Equal(PomodoroPhase.Focus, engine.Phase);
        Assert.Equal(1, engine.CompletedFocusCount);
    }

    [Fact]
    public void StartNextPhase_WithoutPending_IsNoOp()
    {
        var (engine, clock) = CreateEngine();

        engine.StartNextPhase();

        Assert.Equal(PomodoroPhase.Idle, engine.Phase);
        Assert.Null(engine.NextPhase);
    }

    [Fact]
    public void SkipNextPhase_FromPendingBreak_StartsFocus()
    {
        var (engine, clock) = CreateEngine();
        engine.Start();
        clock.Advance(TimeSpan.FromMinutes(25));
        engine.CheckPhaseEnd();

        engine.SkipNextPhase();

        Assert.Equal(PomodoroPhase.Focus, engine.Phase);
        Assert.Equal(clock.Now.AddMinutes(25), engine.PhaseEndAt);
        Assert.Equal(1, engine.CompletedFocusCount);
    }

    [Fact]
    public void SkipLongBreak_DoesNotResetCount()
    {
        var (engine, clock) = CreateEngine();
        for (var i = 0; i < 3; i++)
        {
            engine.Start();
            clock.Advance(TimeSpan.FromMinutes(25));
            engine.CheckPhaseEnd();
            engine.StartNextPhase();
            clock.Advance(TimeSpan.FromMinutes(5));
            engine.CheckPhaseEnd();
            engine.StartNextPhase();
        }
        engine.Start();
        clock.Advance(TimeSpan.FromMinutes(25));
        engine.CheckPhaseEnd();

        engine.SkipNextPhase();

        Assert.Equal(PomodoroPhase.Focus, engine.Phase);
        Assert.Equal(4, engine.CompletedFocusCount);
    }

    [Fact]
    public void Stop_ResetsSessionToIdle()
    {
        var (engine, clock) = CreateEngine();
        engine.Start();
        clock.Advance(TimeSpan.FromMinutes(10));

        engine.Stop();

        Assert.Equal(PomodoroPhase.Idle, engine.Phase);
        Assert.Equal(0, engine.CompletedFocusCount);
        Assert.Null(engine.NextPhase);
        Assert.Null(engine.PhaseEndAt);
        Assert.False(engine.IsPaused);
        Assert.Equal(TimeSpan.Zero, engine.Remaining);
    }

    [Fact]
    public void SettingsChange_AffectsOnlyPhasesStartedAfterwards()
    {
        var (engine, clock) = CreateEngine();
        engine.Start();
        var originalEnd = engine.PhaseEndAt;

        engine.Config.Pomodoro.WorkMinutes = 50;

        Assert.Equal(originalEnd, engine.PhaseEndAt);

        clock.Advance(TimeSpan.FromMinutes(25));
        engine.CheckPhaseEnd();
        engine.StartNextPhase();
        clock.Advance(TimeSpan.FromMinutes(5));
        engine.CheckPhaseEnd();
        engine.StartNextPhase();

        Assert.Equal(clock.Now.AddMinutes(50), engine.PhaseEndAt);
    }

    [Fact]
    public void StateChanged_FiresOnStartAndStop()
    {
        var (engine, clock) = CreateEngine();
        var changed = 0;
        engine.StateChanged += () => changed++;

        engine.Start();
        engine.Stop();

        Assert.Equal(2, changed);
    }

    private static (PomodoroEngine Engine, FakeClock Clock) CreateEngine()
    {
        var clock = new FakeClock();
        var config = new AppConfig();
        return (new PomodoroEngine(config, () => clock.Now), clock);
    }

    private sealed class FakeClock
    {
        public DateTime Now { get; set; } = BaseTime;

        public void Advance(TimeSpan span) => Now = Now.Add(span);
    }
}
