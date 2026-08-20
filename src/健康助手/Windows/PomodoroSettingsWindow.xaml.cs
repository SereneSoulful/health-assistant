using System.Windows;
using 健康助手.Models;
using 健康助手.Services;

namespace 健康助手.Windows;

public partial class PomodoroSettingsWindow : Window
{
    private readonly AppConfig _config;
    private readonly ReminderStore _store;

    public PomodoroSettingsWindow(AppConfig config, ReminderStore store)
    {
        InitializeComponent();
        _config = config;
        _store = store;

        WorkBox.Text = config.Pomodoro.WorkMinutes.ToString();
        ShortBox.Text = config.Pomodoro.ShortBreakMinutes.ToString();
        LongBox.Text = config.Pomodoro.LongBreakMinutes.ToString();
        CyclesBox.Text = config.Pomodoro.FocusCyclesBeforeLongBreak.ToString();
    }

    private void Save_Click(object sender, RoutedEventArgs e)
    {
        if (!TryParse(WorkBox.Text, 1, 120, out var work, "专注时长必须是 1–120 之间的整数（分钟）")) return;
        if (!TryParse(ShortBox.Text, 1, 30, out var shortBreak, "短休息必须是 1–30 之间的整数（分钟）")) return;
        if (!TryParse(LongBox.Text, 1, 60, out var longBreak, "长休息必须是 1–60 之间的整数（分钟）")) return;
        if (!TryParse(CyclesBox.Text, 1, 12, out var cycles, "长休周期必须是 1–12 之间的整数")) return;

        _config.Pomodoro.WorkMinutes = work;
        _config.Pomodoro.ShortBreakMinutes = shortBreak;
        _config.Pomodoro.LongBreakMinutes = longBreak;
        _config.Pomodoro.FocusCyclesBeforeLongBreak = cycles;
        _store.Save(_config);
        DialogResult = true;
    }

    private bool TryParse(string text, int min, int max, out int value, string error)
    {
        if (int.TryParse(text.Trim(), out value) && value >= min && value <= max)
            return true;
        ErrorText.Text = error;
        return false;
    }

    private void Cancel_Click(object sender, RoutedEventArgs e)
        => DialogResult = false;
}
