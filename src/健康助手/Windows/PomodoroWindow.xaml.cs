using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using 健康助手.Models;
using 健康助手.Services;

namespace 健康助手.Windows;

public partial class PomodoroWindow : Window
{
    private readonly AppConfig _config;
    private readonly ReminderStore _store;
    private readonly PomodoroEngine _engine;
    private readonly DispatcherTimer _tickTimer = new() { Interval = TimeSpan.FromSeconds(1) };

    /// <summary>每次刷新显示后触发（App 用于同步托盘提示文字）。</summary>
    public event Action? DisplayRefreshed;

    public bool AllowClose { get; set; }

    public PomodoroWindow(AppConfig config, ReminderStore store, PomodoroEngine engine)
    {
        InitializeComponent();
        _config = config;
        _store = store;
        _engine = engine;

        _engine.StateChanged += OnEngineStateChanged;
        IsVisibleChanged += (_, _) =>
        {
            if (IsVisible)
            {
                _tickTimer.Start();
                Refresh();
            }
            else
            {
                _tickTimer.Stop();
            }
        };
        _tickTimer.Tick += (_, _) => Refresh();
        Closing += (_, e) =>
        {
            if (!AllowClose)
            {
                e.Cancel = true;
                Hide();
            }
        };
        Refresh();
    }

    public void Refresh()
    {
        var phase = _engine.Phase;
        var paused = _engine.IsPaused;
        var remaining = _engine.Remaining;

        PhaseText.Text = phase switch
        {
            PomodoroPhase.Focus => paused ? "专注中（已暂停）" : "专注中",
            PomodoroPhase.ShortBreak => paused ? "短休息（已暂停）" : "短休息",
            PomodoroPhase.LongBreak => paused ? "长休息（已暂停）" : "长休息",
            _ => _engine.NextPhase != null ? "阶段结束，请选择下一步" : "准备开始"
        };

        var brush = phase switch
        {
            PomodoroPhase.Focus => (System.Windows.Media.Brush)FindResource("AccentBrush"),
            PomodoroPhase.ShortBreak => (System.Windows.Media.Brush)FindResource("GreenBrush"),
            PomodoroPhase.LongBreak => (System.Windows.Media.Brush)FindResource("DeepGreenBrush"),
            _ => (System.Windows.Media.Brush)FindResource("AccentBrush")
        };

        RemainingText.Text = phase == PomodoroPhase.Idle
            ? FormatTime(TimeSpan.FromMinutes(_config.Pomodoro.WorkMinutes))
            : FormatTime(remaining);
        RemainingText.Foreground = brush;

        ProgressGrid.Children.Clear();
        var total = _engine.TotalDuration;
        var filled = phase == PomodoroPhase.Idle || total <= TimeSpan.Zero
            ? 20
            : (int)Math.Round(remaining.TotalSeconds * 20.0 / total.TotalSeconds);
        filled = Math.Clamp(filled, 0, 20);
        for (var i = 0; i < 20; i++)
        {
            ProgressGrid.Children.Add(new Border
            {
                Margin = new Thickness(1),
                Background = i < filled ? brush : (System.Windows.Media.Brush)FindResource("PanelFill"),
                BorderBrush = (System.Windows.Media.Brush)FindResource("BorderDark"),
                BorderThickness = new Thickness(1)
            });
        }

        StatusText.Text = phase == PomodoroPhase.Focus
            ? $"正在第 {_engine.CompletedFocusCount + 1} 个番茄 · 已完成 {_engine.CompletedFocusCount} 个"
            : _engine.CompletedFocusCount > 0
                ? $"已完成 {_engine.CompletedFocusCount} 个番茄"
                : "空闲，点击「开始」进入专注";

        StartPauseButton.Content = phase == PomodoroPhase.Idle
            ? (_engine.NextPhase != null ? "开始下一阶段" : "开始")
            : paused ? "继续" : "暂停";
        SkipButton.Content = phase == PomodoroPhase.Idle && _engine.NextPhase != null
            ? "跳过休息"
            : "跳过";
        var hasActive = phase != PomodoroPhase.Idle || _engine.NextPhase != null;
        SkipButton.IsEnabled = hasActive;
        StopButton.IsEnabled = hasActive;

        DisplayRefreshed?.Invoke();
    }

    private void OnEngineStateChanged()
    {
        if (IsVisible)
            Dispatcher.BeginInvoke(Refresh);
    }

    private void StartPause_Click(object sender, RoutedEventArgs e)
    {
        if (_engine.Phase == PomodoroPhase.Idle)
        {
            if (_engine.NextPhase != null)
                _engine.StartNextPhase();
            else
                _engine.Start();
        }
        else if (_engine.IsPaused)
        {
            _engine.Resume();
        }
        else
        {
            _engine.Pause();
        }
        Refresh();
    }

    private void Skip_Click(object sender, RoutedEventArgs e)
    {
        _engine.Skip();
        Refresh();
    }

    private void Stop_Click(object sender, RoutedEventArgs e)
    {
        _engine.Stop();
        Refresh();
    }

    private void Settings_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new PomodoroSettingsWindow(_config, _store)
        {
            Owner = this
        };
        dialog.ShowDialog();
        Refresh();
    }

    private void CloseButton_Click(object sender, RoutedEventArgs e)
        => Close();

    private void Header_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        => DragMove();

    private static string FormatTime(TimeSpan time)
    {
        var seconds = Math.Max(0, (int)time.TotalSeconds);
        return $"{(seconds / 60):00}:{seconds % 60:00}";
    }
}
