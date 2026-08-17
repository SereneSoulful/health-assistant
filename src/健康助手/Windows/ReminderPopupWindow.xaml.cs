using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Animation;
using 健康助手.Models;

namespace 健康助手.Windows;

public partial class ReminderPopupWindow : Window
{
    private readonly Action _onAction;
    private readonly Action _onDismiss;

    public ReminderPopupWindow(Reminder reminder, bool isTest, Action onAction, Action onDismiss)
    {
        InitializeComponent();
        _onAction = onAction;
        _onDismiss = onDismiss;

        NameText.Text = isTest ? reminder.Name + "（测试）" : reminder.Name;
        MessageText.Text = reminder.Message;
        ActionButton.Content = reminder.ActionText;

        Opacity = 0;
        Root.RenderTransform = new TranslateTransform(56, 0);
        Loaded += (_, _) => PlayEntrance();
    }

    private void ActionButton_Click(object sender, RoutedEventArgs e)
        => _onAction();

    private void CloseButton_Click(object sender, RoutedEventArgs e)
        => _onDismiss();

    private void PlayEntrance()
    {
        var duration = TimeSpan.FromMilliseconds(300);
        var opacity = new DoubleAnimationUsingKeyFrames { Duration = duration };
        var offsetX = new DoubleAnimationUsingKeyFrames { Duration = duration };

        AddHardKeyFrames(opacity, 0, 1, duration);
        AddHardKeyFrames(offsetX, 56, 0, duration);

        BeginAnimation(OpacityProperty, opacity);
        Root.RenderTransform.BeginAnimation(TranslateTransform.XProperty, offsetX);
    }

    private static void AddHardKeyFrames(
        DoubleAnimationUsingKeyFrames animation,
        double from,
        double to,
        TimeSpan duration)
    {
        animation.KeyFrames.Add(new DiscreteDoubleKeyFrame(from, TimeSpan.Zero));
        animation.KeyFrames.Add(new DiscreteDoubleKeyFrame(
            from + (to - from) * 0.35, TimeSpan.FromMilliseconds(100)));
        animation.KeyFrames.Add(new DiscreteDoubleKeyFrame(
            from + (to - from) * 0.72, TimeSpan.FromMilliseconds(200)));
        animation.KeyFrames.Add(new DiscreteDoubleKeyFrame(to, duration));
    }
}
