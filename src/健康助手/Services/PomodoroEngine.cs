using 健康助手.Models;

namespace 健康助手.Services;

public enum PomodoroPhase
{
    Idle,
    Focus,
    ShortBreak,
    LongBreak
}

public sealed class PomodoroPhaseEndedInfo
{
    public PomodoroPhase CompletedPhase { get; init; }
    public PomodoroPhase NextPhase { get; init; }
    public int CompletedFocusCount { get; init; }
}

/// <summary>
/// 番茄钟纯逻辑状态机：阶段流转、暂停/恢复、跳过、长休周期。
/// 注入时钟便于单元测试；App 负责按 PhaseEndAt 到点唤醒并调用 CheckPhaseEnd。
/// </summary>
public sealed class PomodoroEngine
{
    private readonly Func<DateTime> _clock;
    private PomodoroPhase _phase = PomodoroPhase.Idle;
    private DateTime? _phaseEndAt;
    private bool _isPaused;
    private TimeSpan _pausedRemaining;
    private int _completedFocusCount;
    private PomodoroPhase? _pendingNext;

    public AppConfig Config { get; }

    public event Action? StateChanged;
    public event Action<PomodoroPhaseEndedInfo>? PhaseEnded;

    public PomodoroPhase Phase => _phase;
    public DateTime? PhaseEndAt => _phaseEndAt;
    public bool IsPaused => _isPaused;
    public int CompletedFocusCount => _completedFocusCount;
    public PomodoroPhase? NextPhase => _pendingNext;
    public TimeSpan TotalDuration { get; private set; }

    public TimeSpan Remaining
    {
        get
        {
            if (_isPaused) return _pausedRemaining;
            if (_phase == PomodoroPhase.Idle || _phaseEndAt is not DateTime end) return TimeSpan.Zero;
            var span = end - _clock();
            return span > TimeSpan.Zero ? span : TimeSpan.Zero;
        }
    }

    public PomodoroEngine(AppConfig config, Func<DateTime>? clock = null)
    {
        Config = config;
        _clock = clock ?? (() => DateTime.Now);
    }

    public void Start()
    {
        if (_phase != PomodoroPhase.Idle || _pendingNext != null) return;
        BeginPhase(PomodoroPhase.Focus);
    }

    public void Pause()
    {
        if (_phase == PomodoroPhase.Idle || _isPaused || _phaseEndAt is not DateTime end) return;
        _pausedRemaining = end - _clock();
        if (_pausedRemaining < TimeSpan.Zero) _pausedRemaining = TimeSpan.Zero;
        _isPaused = true;
        _phaseEndAt = null;
        StateChanged?.Invoke();
    }

    public void Resume()
    {
        if (!_isPaused) return;
        _phaseEndAt = _clock() + _pausedRemaining;
        _isPaused = false;
        StateChanged?.Invoke();
    }

    /// <summary>
    /// 跳过当前进行中的阶段（不计完成数）；若正等待下一阶段确认，则等同 SkipNextPhase。
    /// </summary>
    public void Skip()
    {
        if (_phase == PomodoroPhase.Idle)
        {
            SkipNextPhase();
            return;
        }

        var next = _phase switch
        {
            PomodoroPhase.Focus => PomodoroPhase.ShortBreak,
            PomodoroPhase.ShortBreak => PomodoroPhase.Focus,
            PomodoroPhase.LongBreak => PomodoroPhase.Focus,
            _ => PomodoroPhase.Idle
        };
        BeginPhase(next);
    }

    public void StartNextPhase()
    {
        if (_phase != PomodoroPhase.Idle || _pendingNext is not PomodoroPhase next) return;
        BeginPhase(next);
    }

    public void SkipNextPhase()
    {
        if (_phase != PomodoroPhase.Idle || _pendingNext is not PomodoroPhase pending) return;
        var next = pending is PomodoroPhase.ShortBreak or PomodoroPhase.LongBreak
            ? PomodoroPhase.Focus
            : PomodoroPhase.ShortBreak;
        BeginPhase(next);
    }

    public void Stop()
    {
        if (_phase == PomodoroPhase.Idle && _pendingNext == null && !_isPaused) return;

        _phase = PomodoroPhase.Idle;
        _phaseEndAt = null;
        _isPaused = false;
        _pausedRemaining = TimeSpan.Zero;
        _completedFocusCount = 0;
        _pendingNext = null;
        StateChanged?.Invoke();
    }

    /// <summary>
    /// 检查当前阶段是否到点（定时器唤醒入口，测试可手动调用）。
    /// 阶段到点后不自动开始下一阶段，等待弹窗确认。
    /// </summary>
    public void CheckPhaseEnd()
    {
        if (_phase == PomodoroPhase.Idle || _isPaused || _phaseEndAt is not DateTime end) return;
        if (_clock() < end) return;

        var completed = _phase;
        var focusCount = _completedFocusCount;

        if (completed == PomodoroPhase.Focus)
        {
            focusCount = ++_completedFocusCount;
            _pendingNext = focusCount >= Config.Pomodoro.FocusCyclesBeforeLongBreak
                ? PomodoroPhase.LongBreak
                : PomodoroPhase.ShortBreak;
        }
        else
        {
            if (completed == PomodoroPhase.LongBreak)
                _completedFocusCount = 0;
            _pendingNext = PomodoroPhase.Focus;
        }

        _phase = PomodoroPhase.Idle;
        _phaseEndAt = null;
        StateChanged?.Invoke();
        PhaseEnded?.Invoke(new PomodoroPhaseEndedInfo
        {
            CompletedPhase = completed,
            NextPhase = _pendingNext.Value,
            CompletedFocusCount = focusCount
        });
    }

    private void BeginPhase(PomodoroPhase phase)
    {
        if (phase == PomodoroPhase.Idle) return;

        _phase = phase;
        _phaseEndAt = _clock().AddMinutes(GetDurationMinutes(phase));
        _isPaused = false;
        _pausedRemaining = TimeSpan.Zero;
        _pendingNext = null;
        TotalDuration = TimeSpan.FromMinutes(GetDurationMinutes(phase));
        StateChanged?.Invoke();
    }

    private int GetDurationMinutes(PomodoroPhase phase) => phase switch
    {
        PomodoroPhase.Focus => Math.Max(1, Config.Pomodoro.WorkMinutes),
        PomodoroPhase.ShortBreak => Math.Max(1, Config.Pomodoro.ShortBreakMinutes),
        PomodoroPhase.LongBreak => Math.Max(1, Config.Pomodoro.LongBreakMinutes),
        _ => 1
    };
}
