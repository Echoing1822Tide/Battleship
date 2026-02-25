using Microsoft.Maui.ApplicationModel;

namespace BattleshipMaui.ViewModels;

public interface IGameFeedbackService
{
    void Play(GameFeedbackCue cue, bool soundEnabled, bool hapticsEnabled, bool reduceMotion);
}

public sealed class DefaultGameFeedbackService : IGameFeedbackService
{
    public void Play(GameFeedbackCue cue, bool soundEnabled, bool hapticsEnabled, bool reduceMotion)
    {
        if (soundEnabled)
            TryPlaySound(cue);

        if (hapticsEnabled)
            TryPlayHaptics(cue, reduceMotion);
    }

    private static void TryPlaySound(GameFeedbackCue cue)
    {
        try
        {
            (int frequency, int duration) = cue switch
            {
                GameFeedbackCue.Hit => (980, 55),
                GameFeedbackCue.Sunk => (720, 90),
                GameFeedbackCue.Win => (1040, 140),
                GameFeedbackCue.Loss => (340, 150),
                GameFeedbackCue.Draw => (520, 90),
                GameFeedbackCue.NewGame => (760, 65),
                GameFeedbackCue.PlacementComplete => (820, 65),
                _ => (560, 45)
            };

            _ = Task.Run(() =>
            {
                try
                {
                    Console.Beep(frequency, duration);
                }
                catch
                {
                }
            });
        }
        catch
        {
            // Audio feedback is non-critical.
        }
    }

    private static void TryPlayHaptics(GameFeedbackCue cue, bool reduceMotion)
    {
        try
        {
            if (reduceMotion)
            {
                HapticFeedback.Default.Perform(HapticFeedbackType.Click);
                return;
            }

            if (cue is GameFeedbackCue.Sunk or GameFeedbackCue.Win or GameFeedbackCue.Loss)
            {
                Vibration.Default.Vibrate(TimeSpan.FromMilliseconds(60));
                return;
            }

            HapticFeedback.Default.Perform(HapticFeedbackType.Click);
        }
        catch
        {
            // Haptics may not be available on this device.
        }
    }
}
