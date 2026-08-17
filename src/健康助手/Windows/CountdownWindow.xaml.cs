using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;
using 健康助手.Models;
using 健康助手.Services;

namespace 健康助手.Windows;

public partial class CountdownWindow : Window
{
    private readonly DispatcherTimer _tickTimer;
    private readonly DispatcherTimer _closeTimer = new() { Interval = TimeSpan.FromMilliseconds(1500) };
    private readonly int _total;
    private int _remaining;
    private bool _finished;

    public CountdownWindow(Reminder reminder, bool isTest, AppConfig config, ReminderScheduler scheduler)
    {
        InitializeComponent();

        MessageText.Text = reminder.Message;
        _total = Math.Max(1, reminder.CountdownSeconds);
        _remaining = _total;
        UpdateDisplay();

        _tickTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
        _tickTimer.Tick += (_, _) =>
        {
            _remaining--;
            if (_remaining <= 0)
                Finish(reminder, isTest, config, scheduler);
            else
                UpdateDisplay();
        };
        _tickTimer.Start();

        SkipButton.Click += (_, _) =>
        {
            _tickTimer.Stop();
            _closeTimer.Stop();
            scheduler.Handle(reminder, isTest);
            Close();
        };

        _closeTimer.Tick += (_, _) =>
        {
            _closeTimer.Stop();
            Close();
        };

        Closed += (_, _) =>
        {
            _tickTimer.Stop();
            _closeTimer.Stop();
        };
    }

    private void Finish(Reminder reminder, bool isTest, AppConfig config, ReminderScheduler scheduler)
    {
        if (_finished) return;
        _finished = true;

        _tickTimer.Stop();
        SoundService.PlayCountdownDone(config.SoundEnabled);
        scheduler.Handle(reminder, isTest);
        RemainingText.Text = "完成";
        UpdateDisplay();
        _closeTimer.Start();
    }

    private void UpdateDisplay()
    {
        RemainingText.Text = _remaining.ToString();

        ProgressGrid.Children.Clear();
        var filled = (int)Math.Round(_remaining * 20.0 / _total);
        for (var i = 0; i < 20; i++)
        {
            ProgressGrid.Children.Add(new Border
            {
                Margin = new Thickness(1),
                Background = i < filled
                    ? (System.Windows.Media.Brush)FindResource("AccentBrush")
                    : (System.Windows.Media.Brush)FindResource("PanelFill"),
                BorderBrush = (System.Windows.Media.Brush)FindResource("BorderDark"),
                BorderThickness = new Thickness(1)
            });
        }
    }
}
