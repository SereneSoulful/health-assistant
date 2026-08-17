using System.ComponentModel;
using System.Runtime.CompilerServices;
using 健康助手.Models;

namespace 健康助手.ViewModels;

public sealed class ReminderItem : INotifyPropertyChanged
{
    public Reminder Model { get; }

    public ReminderItem(Reminder model) => Model = model;

    public string Name => Model.Name;
    public string Message => Model.Message;
    public string IntervalText => $"每 {Model.IntervalMinutes} 分钟";
    public string ActionText => Model.ActionText;
    public string CountdownText => Model.CountdownSeconds > 0
        ? $"倒计时 {Model.CountdownSeconds} 秒"
        : "无倒计时";

    public string NextText { get; private set; } = "—";

    public bool IsEnabled
    {
        get => Model.IsEnabled;
        set
        {
            if (Model.IsEnabled == value) return;
            Model.IsEnabled = value;
            OnPropertyChanged();
        }
    }

    public void SetNext(DateTime? time)
    {
        NextText = time is DateTime t ? $"下次提醒 {t:HH:mm:ss}" : "已暂停";
        OnPropertyChanged(nameof(NextText));
    }

    public void Refresh()
    {
        OnPropertyChanged(nameof(Name));
        OnPropertyChanged(nameof(Message));
        OnPropertyChanged(nameof(IntervalText));
        OnPropertyChanged(nameof(ActionText));
        OnPropertyChanged(nameof(CountdownText));
        OnPropertyChanged(nameof(IsEnabled));
        OnPropertyChanged(nameof(NextText));
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    private void OnPropertyChanged([CallerMemberName] string? name = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}
