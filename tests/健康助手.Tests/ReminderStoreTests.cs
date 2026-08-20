using 健康助手.Models;
using 健康助手.Services;

namespace 健康助手.Tests;

public class ReminderStoreTests
{
    [Fact]
    public void Load_Version2Config_MigratesToV3WithPomodoroDefaults()
    {
        using var dir = new TempDir();
        var store = new ReminderStore(dir.Path);
        File.WriteAllText(store.ConfigPath, """
        {
          "Version": 2,
          "SoundEnabled": true,
          "Reminders": [
            {
              "Id": "7f2c9d0a-1b3e-4a5f-8c6d-9e0f1a2b3c4d",
              "Name": "喝水",
              "Message": "该喝水啦",
              "IntervalMinutes": 60,
              "ActionText": "我喝完了",
              "CountdownSeconds": 0,
              "IsEnabled": true
            }
          ]
        }
        """);

        var config = store.Load();

        Assert.Equal(3, config.Version);
        Assert.Equal(25, config.Pomodoro.WorkMinutes);
        Assert.Equal(5, config.Pomodoro.ShortBreakMinutes);
        Assert.Equal(15, config.Pomodoro.LongBreakMinutes);
        Assert.Equal(4, config.Pomodoro.FocusCyclesBeforeLongBreak);
        Assert.Single(config.Reminders);
        Assert.Equal("喝水", config.Reminders[0].Name);
    }

    [Fact]
    public void Load_AfterMigration_KeepsUserEditsAndDoesNotDuplicate()
    {
        using var dir = new TempDir();
        var store = new ReminderStore(dir.Path);
        File.WriteAllText(store.ConfigPath, """
        {
          "Version": 2,
          "SoundEnabled": true,
          "Reminders": []
        }
        """);

        var config = store.Load();
        config.Reminders.Add(new Reminder { Name = "自定", IntervalMinutes = 45 });
        config.Pomodoro.WorkMinutes = 50;
        store.Save(config);

        var reloaded = store.Load();

        Assert.Equal(3, reloaded.Version);
        Assert.Equal(50, reloaded.Pomodoro.WorkMinutes);
        Assert.Single(reloaded.Reminders);
        Assert.Equal("自定", reloaded.Reminders[0].Name);
        Assert.Equal(45, reloaded.Reminders[0].IntervalMinutes);
    }

    [Fact]
    public void CreateDefault_HasVersion3AndTwoReminders()
    {
        var config = ReminderStore.CreateDefault();

        Assert.Equal(3, config.Version);
        Assert.Equal(25, config.Pomodoro.WorkMinutes);
        Assert.Equal(2, config.Reminders.Count);
    }

    private sealed class TempDir : IDisposable
    {
        public string Path { get; } = System.IO.Path.Combine(
            System.IO.Path.GetTempPath(),
            "健康助手Tests-" + Guid.NewGuid().ToString("N"));

        public TempDir() => Directory.CreateDirectory(Path);

        public void Dispose()
        {
            try { Directory.Delete(Path, true); }
            catch { /* 临时目录清理失败不影响测试 */ }
        }
    }
}
