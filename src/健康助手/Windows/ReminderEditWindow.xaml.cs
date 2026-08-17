using System.Windows;
using 健康助手.Models;

namespace 健康助手.Windows;

public partial class ReminderEditWindow : Window
{
    private readonly Reminder _working;

    public Reminder? Result { get; private set; }

    public ReminderEditWindow(Reminder reminder)
    {
        InitializeComponent();
        _working = reminder;

        TitleText.Text = string.IsNullOrEmpty(reminder.Name) || reminder.Name == "新提醒"
            ? "添加提醒"
            : "编辑提醒";
        NameBox.Text = reminder.Name;
        MessageBox.Text = reminder.Message;
        IntervalBox.Text = reminder.IntervalMinutes.ToString();
        ActionBox.Text = reminder.ActionText;
        CountdownBox.Text = reminder.CountdownSeconds.ToString();
        EnabledCheck.IsChecked = reminder.IsEnabled;
    }

    private void Save_Click(object sender, RoutedEventArgs e)
    {
        var name = NameBox.Text.Trim();
        var message = MessageBox.Text.Trim();
        var actionText = ActionBox.Text.Trim();

        if (name.Length == 0) { ShowError("名称不能为空"); return; }
        if (message.Length == 0) { ShowError("提醒内容不能为空"); return; }
        if (actionText.Length == 0) { ShowError("按钮文字不能为空"); return; }

        if (!int.TryParse(IntervalBox.Text.Trim(), out var interval) || interval is < 1 or > 1440)
        {
            ShowError("间隔必须是 1–1440 之间的整数（分钟）");
            return;
        }

        if (!int.TryParse(CountdownBox.Text.Trim(), out var countdown) || countdown is < 0 or > 600)
        {
            ShowError("倒计时必须是 0–600 之间的整数（秒），0 表示无倒计时");
            return;
        }

        _working.Name = name;
        _working.Message = message;
        _working.IntervalMinutes = interval;
        _working.ActionText = actionText;
        _working.CountdownSeconds = countdown;
        _working.IsEnabled = EnabledCheck.IsChecked == true;

        Result = _working;
        DialogResult = true;
    }

    private void Cancel_Click(object sender, RoutedEventArgs e)
        => DialogResult = false;

    private void ShowError(string text)
    {
        ErrorText.Text = text;
    }
}
