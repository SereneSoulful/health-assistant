using System.Threading;
using 健康助手.Models;

namespace 健康助手.Services;

/// <summary>
/// 基于绝对时间的单定时器调度引擎：空闲时没有任何定时器唤醒，
/// 每次触发/处理后按最新时间重算下一次触发。
/// </summary>
public sealed class ReminderScheduler : IDisposable
{
    private readonly AppConfig _config;
    private readonly Func<DateTime> _clock;
    private readonly object _lock = new();
    private readonly Dictionary<Guid, DateTime> _nextTrigger = new();
    private readonly HashSet<Guid> _pending = new();
    private System.Threading.Timer? _timer;
    private bool _disposed;

    public event Action<Reminder, bool>? ReminderDue;
    public event Action? ScheduleChanged;

    public ReminderScheduler(AppConfig config, Func<DateTime>? clock = null)
    {
        _config = config;
        _clock = clock ?? (() => DateTime.Now);
    }

    public void Start()
    {
        lock (_lock)
        {
            var now = _clock();
            foreach (var reminder in _config.Reminders)
            {
                if (reminder.IsEnabled)
                    _nextTrigger[reminder.Id] = now.AddMinutes(reminder.IntervalMinutes);
            }
            ArmLocked();
        }
    }

    public DateTime? NextTriggerTime(Reminder reminder)
    {
        lock (_lock)
            return _nextTrigger.TryGetValue(reminder.Id, out var time) ? time : null;
    }

    public void ApplyReminder(Reminder reminder)
    {
        lock (_lock)
        {
            _pending.Remove(reminder.Id);
            if (reminder.IsEnabled)
                _nextTrigger[reminder.Id] = _clock().AddMinutes(reminder.IntervalMinutes);
            else
                _nextTrigger.Remove(reminder.Id);
            ArmLocked();
        }
        ScheduleChanged?.Invoke();
    }

    public void RemoveReminder(Guid id)
    {
        lock (_lock)
        {
            _pending.Remove(id);
            _nextTrigger.Remove(id);
            ArmLocked();
        }
        ScheduleChanged?.Invoke();
    }

    public void TriggerTest(Reminder reminder)
        => RaiseDue(reminder, true);

    /// <summary>
    /// 提醒被处理（按钮确认、倒计时完成或跳过、弹窗关闭）后调用。
    /// 测试弹窗不重置真实周期。
    /// </summary>
    public void Handle(Reminder reminder, bool isTest)
    {
        if (isTest) return;

        lock (_lock)
        {
            _pending.Remove(reminder.Id);
            var current = _config.Reminders.FirstOrDefault(r => r.Id == reminder.Id);
            if (current is { IsEnabled: true })
            {
                _nextTrigger[current.Id] = _clock().AddMinutes(current.IntervalMinutes);
                ArmLocked();
            }
        }
        ScheduleChanged?.Invoke();
    }

    /// <summary>
    /// 检查并触发所有到期提醒（定时器回调入口，测试时可手动调用）。
    /// </summary>
    internal void CheckDue()
    {
        List<(Reminder Reminder, bool IsTest)> due = new();
        lock (_lock)
        {
            var now = _clock();
            foreach (var reminder in _config.Reminders)
            {
                if (!reminder.IsEnabled) continue;
                if (_pending.Contains(reminder.Id)) continue;
                if (_nextTrigger.TryGetValue(reminder.Id, out var time) && time <= now)
                {
                    _pending.Add(reminder.Id);
                    _nextTrigger.Remove(reminder.Id);
                    due.Add((reminder, false));
                }
            }
        }

        foreach (var item in due)
            RaiseDue(item.Reminder, item.IsTest);
    }

    private void OnTick(object? state) => CheckDue();

    private void ArmLocked()
    {
        if (_disposed) return;

        DateTime? next = null;
        foreach (var time in _nextTrigger.Values)
        {
            if (next == null || time < next)
                next = time;
        }

        _timer ??= new System.Threading.Timer(OnTick);
        if (next is DateTime due)
        {
            var milliseconds = (long)(due - _clock()).TotalMilliseconds;
            if (milliseconds < 0) milliseconds = 0;
            if (milliseconds > int.MaxValue) milliseconds = int.MaxValue;
            _timer.Change(milliseconds, Timeout.Infinite);
        }
        else
        {
            _timer.Change(Timeout.Infinite, Timeout.Infinite);
        }
    }

    private void RaiseDue(Reminder reminder, bool isTest)
    {
        var app = System.Windows.Application.Current;
        if (app == null)
        {
            ReminderDue?.Invoke(reminder, isTest);
            return;
        }
        if (app.Dispatcher.CheckAccess())
        {
            ReminderDue?.Invoke(reminder, isTest);
            return;
        }
        app.Dispatcher.BeginInvoke(() => ReminderDue?.Invoke(reminder, isTest));
    }

    public void Dispose()
    {
        lock (_lock)
        {
            _disposed = true;
            _timer?.Dispose();
            _timer = null;
        }
    }
}
