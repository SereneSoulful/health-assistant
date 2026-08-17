using Microsoft.Win32;

namespace 健康助手.Services;

public static class StartupService
{
    private const string RunKeyPath = @"Software\Microsoft\Windows\CurrentVersion\Run";
    private const string ValueName = "健康助手";

    public static bool IsEnabled()
    {
        using var key = Registry.CurrentUser.OpenSubKey(RunKeyPath);
        var value = key?.GetValue(ValueName) as string;
        return !string.IsNullOrEmpty(value) &&
               value.Contains("\"" + Environment.ProcessPath + "\"", StringComparison.OrdinalIgnoreCase);
    }

    public static void SetEnabled(bool enabled)
    {
        using var key = Registry.CurrentUser.CreateSubKey(RunKeyPath);
        if (enabled)
            key.SetValue(ValueName, "\"" + Environment.ProcessPath + "\"");
        else
            key.DeleteValue(ValueName, false);
    }
}
