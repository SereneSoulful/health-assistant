namespace 健康助手.Models;

public sealed class AppConfig
{
    public int Version { get; set; } = 3;
    public bool SoundEnabled { get; set; } = true;
    public PomodoroSettings Pomodoro { get; set; } = new();
    public List<Reminder> Reminders { get; set; } = new();
}
