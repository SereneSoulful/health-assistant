namespace 健康助手.Models;

public sealed class PomodoroSettings
{
    public int WorkMinutes { get; set; } = 25;
    public int ShortBreakMinutes { get; set; } = 5;
    public int LongBreakMinutes { get; set; } = 15;
    public int FocusCyclesBeforeLongBreak { get; set; } = 4;
}
