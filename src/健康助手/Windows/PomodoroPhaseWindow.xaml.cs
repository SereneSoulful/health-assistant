using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Animation;

namespace 健康助手.Windows;

public partial class PomodoroPhaseWindow : Window
{
    private readonly Action _onStart;
    private readonly Action _onSkip;
    private readonly Action _onStop;

    public PomodoroPhaseWindow(
        string title,
        string message,
        string startText,
        bool showSkip,
        Action onStart,
        Action onSkip,
        Action onStop)
    {
        InitializeComponent();
        _onStart = onStart;
        _onSkip = onSkip;
        _onStop = onStop;

        NameText.Text = title;
        MessageText.Text = message;
        StartButton.Content = startText;
        SkipButton.Visibility = showSkip ? Visibility.Visible : Visibility.Collapsed;

        Opacity = 0;
        Root.RenderTransform = new TranslateTransform(56, 0);
        Loaded += (_, _) => PlayEntrance();
    }

    private void StartButton_Click(object sender, RoutedEventArgs e)
    {
        _onStart();
        Close();
    }

    private void SkipButton_Click(object sender, RoutedEventArgs e)
    {
        _onSkip();
        Close();
    }

    private void StopButton_Click(object sender, RoutedEventArgs e)
    {
        _onStop();
        Close();
    }

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
