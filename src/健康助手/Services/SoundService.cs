using System.Media;

namespace 健康助手.Services;

public static class SoundService
{
    public static void PlayReminder(bool enabled)
    {
        if (enabled)
            SystemSounds.Asterisk.Play();
    }

    public static void PlayCountdownDone(bool enabled)
    {
        if (enabled)
            SystemSounds.Exclamation.Play();
    }
}
