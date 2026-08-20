using System.Collections.ObjectModel;
using System.Windows;
using 健康助手.Models;
using 健康助手.Services;
using 健康助手.ViewModels;

namespace 健康助手.Windows;

public partial class MainWindow : Window
{
    private readonly ReminderStore _store;
    private readonly ReminderScheduler _scheduler;
    private readonly AppConfig _config;
    private readonly ObservableCollection<ReminderItem> _items = new();

    public bool AllowClose { get; set; }

    public MainWindow(ReminderStore store, ReminderScheduler scheduler, AppConfig config)
    {
        InitializeComponent();
        _store = store;
        _scheduler = scheduler;
        _config = config;

        ReminderList.ItemsSource = _items;
        StartupCheck.IsChecked = StartupService.IsEnabled();
        SoundCheck.IsChecked = config.SoundEnabled;

        foreach (var reminder in config.Reminders)
            _items.Add(new ReminderItem(reminder));

        _scheduler.ScheduleChanged += OnScheduleChanged;
        IsVisibleChanged += (_, _) =>
        {
            if (IsVisible) RefreshNextTimes();
        };
        Loaded += (_, _) => RefreshNextTimes();
        Closing += (_, e) =>
        {
            if (!AllowClose)
            {
                e.Cancel = true;
                Hide();
            }
        };
    }

    private void OnScheduleChanged()
    {
        if (IsVisible)
            Dispatcher.BeginInvoke(RefreshNextTimes);
    }

    public void RefreshNextTimes()
    {
        foreach (var item in _items)
            item.SetNext(_scheduler.NextTriggerTime(item.Model));
    }

    private void Minimize_Click(object sender, RoutedEventArgs e)
        => WindowState = WindowState.Minimized;

    private void Close_Click(object sender, RoutedEventArgs e)
        => Close();

    private void Add_Click(object sender, RoutedEventArgs e)
    {
        var reminder = new Reminder
        {
            Name = "新提醒",
            Message = "该休息一下啦",
            IntervalMinutes = 20,
            ActionText = "知道了",
            CountdownSeconds = 0,
            IsEnabled = true
        };
        var dialog = new ReminderEditWindow(reminder);
        if (dialog.ShowDialog() == true && dialog.Result != null)
        {
            _config.Reminders.Add(dialog.Result);
            _store.Save(_config);
            _scheduler.ApplyReminder(dialog.Result);
            _items.Add(new ReminderItem(dialog.Result));
            RefreshNextTimes();
        }
    }

    private void Pomodoro_Click(object sender, RoutedEventArgs e)
        => ((健康助手.App)System.Windows.Application.Current).ShowPomodoroWindow();

    private void CardButton_Click(object sender, RoutedEventArgs e)
    {
        if ((sender as FrameworkElement)?.Tag is not string tag) return;
        if ((sender as FrameworkElement)?.DataContext is not ReminderItem item) return;

        switch (tag)
        {
            case "Test":
                _scheduler.TriggerTest(item.Model);
                break;
            case "Edit":
                EditReminder(item);
                break;
            case "Delete":
                DeleteReminder(item);
                break;
        }
    }

    private void EditReminder(ReminderItem item)
    {
        var dialog = new ReminderEditWindow(item.Model.Clone());
        if (dialog.ShowDialog() == true && dialog.Result != null)
        {
            var model = item.Model;
            model.Name = dialog.Result.Name;
            model.Message = dialog.Result.Message;
            model.IntervalMinutes = dialog.Result.IntervalMinutes;
            model.ActionText = dialog.Result.ActionText;
            model.CountdownSeconds = dialog.Result.CountdownSeconds;
            model.IsEnabled = dialog.Result.IsEnabled;

            _store.Save(_config);
            _scheduler.ApplyReminder(model);
            item.Refresh();
            RefreshNextTimes();
        }
    }

    private void DeleteReminder(ReminderItem item)
    {
        var answer = System.Windows.MessageBox.Show(
            $"确定删除提醒「{item.Name}」吗？",
            "删除提醒",
            MessageBoxButton.YesNo,
            MessageBoxImage.Question);
        if (answer != MessageBoxResult.Yes) return;

        _config.Reminders.Remove(item.Model);
        _store.Save(_config);
        _scheduler.RemoveReminder(item.Model.Id);
        _items.Remove(item);
    }

    private void Toggle_Changed(object sender, RoutedEventArgs e)
    {
        if ((sender as FrameworkElement)?.DataContext is not ReminderItem item) return;
        _store.Save(_config);
        _scheduler.ApplyReminder(item.Model);
        RefreshNextTimes();
    }

    private void Startup_Changed(object sender, RoutedEventArgs e)
        => StartupService.SetEnabled(StartupCheck.IsChecked == true);

    private void Sound_Changed(object sender, RoutedEventArgs e)
    {
        _config.SoundEnabled = SoundCheck.IsChecked == true;
        _store.Save(_config);
    }
}
