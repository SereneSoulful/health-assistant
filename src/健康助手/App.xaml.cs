using System.Threading;
using System.Windows;
using 健康助手.Models;
using 健康助手.Services;
using 健康助手.Windows;
using WinForms = System.Windows.Forms;

namespace 健康助手;

public partial class App : System.Windows.Application
{
    private Mutex? _mutex;
    private EventWaitHandle? _showMainEvent;
    private Thread? _listenerThread;
    private WinForms.NotifyIcon? _tray;
    private ReminderStore? _store;
    private ReminderScheduler? _scheduler;
    private AppConfig? _config;
    private MainWindow? _mainWindow;
    private PomodoroEngine? _pomodoro;
    private System.Threading.Timer? _pomodoroTimer;
    private PomodoroWindow? _pomodoroWindow;
    private readonly List<ReminderPopupWindow> _popupStack = new();
    private readonly List<PomodoroPhaseWindow> _pomodoroPopups = new();
    private readonly List<Window> _activeWindows = new();
    private bool _ownsMutex;

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        _mutex = new Mutex(true, @"Local\健康助手.SingleInstance", out var createdNew);
        _ownsMutex = createdNew;
        _showMainEvent = new EventWaitHandle(false, EventResetMode.AutoReset, @"Local\健康助手.ShowMain");
        if (!createdNew)
        {
            try { _showMainEvent.Set(); } catch { /* 另一实例已退出 */ }
            Shutdown();
            return;
        }

        _listenerThread = new Thread(() =>
        {
            try
            {
                while (_showMainEvent.WaitOne())
                    Dispatcher.BeginInvoke(ShowMainWindow);
            }
            catch { /* 应用退出时忽略 */ }
        })
        {
            IsBackground = true
        };
        _listenerThread.Start();

        _store = new ReminderStore();
        _config = _store.Load();
        _scheduler = new ReminderScheduler(_config);
        _scheduler.ReminderDue += (reminder, isTest) => ShowPopup(reminder, isTest);
        _scheduler.ScheduleChanged += () =>
        {
            if (_mainWindow is { IsVisible: true })
                _mainWindow.RefreshNextTimes();
        };
        _scheduler.Start();

        _pomodoro = new PomodoroEngine(_config);
        _pomodoro.StateChanged += OnPomodoroStateChanged;
        _pomodoro.PhaseEnded += OnPomodoroPhaseEnded;
        _pomodoroTimer = new System.Threading.Timer(OnPomodoroTimerTick);
        ArmPomodoroTimer();

        CreateTray();
        ShowMainWindow();
    }

    private void CreateTray()
    {
        _tray = new WinForms.NotifyIcon
        {
            Icon = System.Drawing.Icon.ExtractAssociatedIcon(Environment.ProcessPath!),
            Text = "健康助手",
            Visible = true
        };

        var menu = new WinForms.ContextMenuStrip();
        menu.Items.Add("打开主界面", null, (_, _) => Dispatcher.BeginInvoke(ShowMainWindow));
        menu.Items.Add("番茄钟", null, (_, _) => Dispatcher.BeginInvoke(ShowPomodoroWindow));
        menu.Items.Add(new WinForms.ToolStripSeparator());
        menu.Items.Add("退出", null, (_, _) => Dispatcher.BeginInvoke(ExitApp));
        _tray.ContextMenuStrip = menu;
        _tray.DoubleClick += (_, _) => Dispatcher.BeginInvoke(ShowMainWindow);
    }

    private void ShowMainWindow()
    {
        if (_mainWindow == null)
        {
            _mainWindow = new MainWindow(_store!, _scheduler!, _config!);
            _mainWindow.Show();
        }
        else if (!_mainWindow.IsVisible)
        {
            _mainWindow.Show();
        }

        if (_mainWindow.WindowState == WindowState.Minimized)
            _mainWindow.WindowState = WindowState.Normal;
        _mainWindow.Activate();
    }

    public void ShowPomodoroWindow()
    {
        if (_pomodoroWindow == null)
        {
            _pomodoroWindow = new PomodoroWindow(_config!, _store!, _pomodoro!);
            _pomodoroWindow.DisplayRefreshed += UpdatePomodoroTrayTooltip;
        }

        if (!_pomodoroWindow.IsVisible)
            _pomodoroWindow.Show();
        if (_pomodoroWindow.WindowState == WindowState.Minimized)
            _pomodoroWindow.WindowState = WindowState.Normal;
        _pomodoroWindow.Activate();
    }

    private void OnPomodoroTimerTick(object? state)
        => Dispatcher.BeginInvoke(() => _pomodoro?.CheckPhaseEnd());

    private void OnPomodoroStateChanged()
    {
        Dispatcher.BeginInvoke(() =>
        {
            if (_pomodoroWindow is { IsVisible: true })
                _pomodoroWindow.Refresh();
            UpdatePomodoroTrayTooltip();
            ArmPomodoroTimer();
        });
    }

    private void OnPomodoroPhaseEnded(PomodoroPhaseEndedInfo info)
    {
        SoundService.PlayCountdownDone(_config!.SoundEnabled);

        var settings = _config.Pomodoro;
        var minutes = info.NextPhase switch
        {
            PomodoroPhase.Focus => settings.WorkMinutes,
            PomodoroPhase.ShortBreak => settings.ShortBreakMinutes,
            PomodoroPhase.LongBreak => settings.LongBreakMinutes,
            _ => 0
        };
        var title = info.CompletedPhase == PomodoroPhase.Focus ? "专注结束！" : "休息结束！";
        var message = info.NextPhase == PomodoroPhase.Focus
            ? $"休息结束，开始专注 {minutes} 分钟吧"
            : $"专注结束，休息 {minutes} 分钟吧";
        var startText = info.NextPhase == PomodoroPhase.Focus ? "开始专注" : "开始休息";
        var showSkip = info.CompletedPhase == PomodoroPhase.Focus;

        PomodoroPhaseWindow popup = null!;
        popup = new PomodoroPhaseWindow(
            title,
            message,
            startText,
            showSkip,
            () => _pomodoro!.StartNextPhase(),
            () => _pomodoro!.SkipNextPhase(),
            () => _pomodoro!.Stop());

        _pomodoroPopups.Add(popup);
        popup.Closed += (_, _) => _pomodoroPopups.Remove(popup);

        var workArea = SystemParameters.WorkArea;
        popup.Left = workArea.Right - popup.Width - 24;
        popup.Top = workArea.Bottom - 220 - 24 - (_pomodoroPopups.Count - 1) * 160;
        popup.Show();
        popup.Top = Math.Max(
            workArea.Top + 12,
            workArea.Bottom - popup.ActualHeight - 24 - (_pomodoroPopups.Count - 1) * 160);
    }

    public void UpdatePomodoroTrayTooltip()
    {
        if (_tray == null || _pomodoro == null) return;

        var text = _pomodoro.Phase switch
        {
            PomodoroPhase.Focus => $"健康助手 · 专注中 {FormatRemaining(_pomodoro.Remaining)}",
            PomodoroPhase.ShortBreak => $"健康助手 · 短休息 {FormatRemaining(_pomodoro.Remaining)}",
            PomodoroPhase.LongBreak => $"健康助手 · 长休息 {FormatRemaining(_pomodoro.Remaining)}",
            _ => "健康助手"
        };
        if (text.Length > 63) text = text[..63];
        _tray.Text = text;
    }

    private static string FormatRemaining(TimeSpan time)
    {
        var seconds = Math.Max(0, (int)time.TotalSeconds);
        return $"{(seconds / 60):00}:{seconds % 60:00}";
    }

    private void ArmPomodoroTimer()
    {
        if (_pomodoro?.PhaseEndAt is not DateTime due)
        {
            _pomodoroTimer?.Change(Timeout.Infinite, Timeout.Infinite);
            return;
        }

        var milliseconds = (long)(due - DateTime.Now).TotalMilliseconds;
        if (milliseconds < 0) milliseconds = 0;
        if (milliseconds > int.MaxValue) milliseconds = int.MaxValue;
        _pomodoroTimer?.Change(milliseconds, Timeout.Infinite);
    }

    private void ShowPopup(Reminder reminder, bool isTest)
    {
        SoundService.PlayReminder(_config!.SoundEnabled);

        ReminderPopupWindow popup = null!;
        popup = new ReminderPopupWindow(
            reminder,
            isTest,
            () => OnPopupAction(popup, reminder, isTest),
            () => OnPopupDismiss(popup, reminder, isTest));

        _popupStack.Add(popup);
        _activeWindows.Add(popup);
        popup.Closed += (_, _) =>
        {
            _popupStack.Remove(popup);
            _activeWindows.Remove(popup);
        };

        var workArea = SystemParameters.WorkArea;
        popup.Left = workArea.Right - popup.Width - 24;
        popup.Top = workArea.Bottom - 220 - 24 - (_popupStack.Count - 1) * 160;
        popup.Show();
        popup.Top = Math.Max(workArea.Top + 12, workArea.Bottom - popup.ActualHeight - 24 - (_popupStack.Count - 1) * 160);
    }

    private void OnPopupAction(ReminderPopupWindow popup, Reminder reminder, bool isTest)
    {
        popup.Close();
        if (reminder.CountdownSeconds > 0)
            ShowCountdown(reminder, isTest);
        else
            _scheduler!.Handle(reminder, isTest);
    }

    private void OnPopupDismiss(ReminderPopupWindow popup, Reminder reminder, bool isTest)
    {
        _scheduler!.Handle(reminder, isTest);
        popup.Close();
    }

    private void ShowCountdown(Reminder reminder, bool isTest)
    {
        var window = new CountdownWindow(reminder, isTest, _config!, _scheduler!);
        _activeWindows.Add(window);
        window.Closed += (_, _) => _activeWindows.Remove(window);
        window.Show();
    }

    public void ExitApp()
    {
        if (_pomodoroWindow != null)
        {
            _pomodoroWindow.AllowClose = true;
            _pomodoroWindow.Close();
        }
        if (_mainWindow != null)
        {
            _mainWindow.AllowClose = true;
            _mainWindow.Close();
        }
        Shutdown();
    }

    protected override void OnExit(ExitEventArgs e)
    {
        _scheduler?.Dispose();
        _pomodoroTimer?.Dispose();
        _tray?.Dispose();
        _showMainEvent?.Dispose();
        if (_ownsMutex)
        {
            try { _mutex?.ReleaseMutex(); } catch { /* 已释放则忽略 */ }
        }
        _mutex?.Dispose();
        base.OnExit(e);
    }
}
