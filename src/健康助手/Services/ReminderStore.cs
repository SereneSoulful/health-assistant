using System.IO;
using System.Text.Encodings.Web;
using System.Text.Json;
using 健康助手.Models;

namespace 健康助手.Services;

public sealed class ReminderStore
{
    private static readonly Guid WaterSeedId = Guid.Parse("7f2c9d0a-1b3e-4a5f-8c6d-9e0f1a2b3c4d");
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
    };

    public string DataDirectory { get; }
    public string ConfigPath { get; }

    public ReminderStore(string? dataDirectory = null)
    {
        DataDirectory = dataDirectory ?? Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "健康助手");
        ConfigPath = Path.Combine(DataDirectory, "reminders.json");
    }

    public AppConfig Load()
    {
        if (File.Exists(ConfigPath))
        {
            try
            {
                var config = JsonSerializer.Deserialize<AppConfig>(
                    File.ReadAllText(ConfigPath), JsonOptions);
                if (config != null)
                {
                    config.Reminders ??= new List<Reminder>();
                    config.Pomodoro ??= new PomodoroSettings();
                    if (config.Version < 3)
                    {
                        Migrate(config);
                        Save(config);
                    }
                    return config;
                }
            }
            catch (Exception ex)
            {
                BackupBrokenConfig(ex);
            }
        }

        var fresh = CreateDefault();
        Save(fresh);
        return fresh;
    }

    public void Save(AppConfig config)
    {
        Directory.CreateDirectory(DataDirectory);
        File.WriteAllText(
            ConfigPath,
            JsonSerializer.Serialize(config, JsonOptions));
    }

    public static AppConfig CreateDefault()
    {
        var config = new AppConfig { Version = 3, SoundEnabled = true };
        config.Reminders.Add(new Reminder
        {
            Name = "远眺",
            Message = "该远眺啦！离开屏幕，看看窗外远处 20 秒吧",
            IntervalMinutes = 20,
            ActionText = "立即开始",
            CountdownSeconds = 20,
            IsEnabled = true
        });
        config.Reminders.Add(new Reminder
        {
            Id = WaterSeedId,
            Name = "喝水",
            Message = "该喝水啦！起来接杯水，小口喝完再回来",
            IntervalMinutes = 60,
            ActionText = "我喝完了",
            CountdownSeconds = 0,
            IsEnabled = true
        });
        return config;
    }

    private static void Migrate(AppConfig config)
    {
        if (config.Version < 2)
        {
            var hasWater = config.Reminders.Any(
                r => r.Id == WaterSeedId || string.Equals(r.Name, "喝水", StringComparison.OrdinalIgnoreCase));
            if (!hasWater)
            {
                config.Reminders.Add(new Reminder
                {
                    Id = WaterSeedId,
                    Name = "喝水",
                    Message = "该喝水啦！起来接杯水，小口喝完再回来",
                    IntervalMinutes = 60,
                    ActionText = "我喝完了",
                    CountdownSeconds = 0,
                    IsEnabled = true
                });
            }
            config.Version = 2;
        }

        if (config.Version < 3)
        {
            config.Pomodoro ??= new PomodoroSettings();
            config.Version = 3;
        }
    }

    private void BackupBrokenConfig(Exception ex)
    {
        try
        {
            var backup = ConfigPath + ".bak-" + DateTime.Now.ToString("yyyyMMddHHmmss");
            File.Copy(ConfigPath, backup, true);
        }
        catch
        {
            // 备份失败不阻止重建
        }
        System.Diagnostics.Debug.WriteLine("配置损坏，已重建默认配置：" + ex.Message);
    }
}
