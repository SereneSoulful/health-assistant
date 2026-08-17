namespace 健康助手.Models;

public sealed class AppConfig
{
    public int Version { get; set; } = 1;
    public bool SoundEnabled { get; set; } = true;
    public List<Reminder> Reminders { get; set; } = new();
}
