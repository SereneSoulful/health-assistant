namespace 健康助手.Models;

public sealed class Reminder
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = "";
    public string Message { get; set; } = "";
    public int IntervalMinutes { get; set; } = 20;
    public string ActionText { get; set; } = "立即开始";
    public int CountdownSeconds { get; set; } = 20;
    public bool IsEnabled { get; set; } = true;

    public Reminder Clone() => (Reminder)MemberwiseClone();
}
