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
            var sequence = cue switch
            {
                GameFeedbackCue.Hit => new[]
                {
                    new ToneStep(930, 32, 8),
                    new ToneStep(1180, 55)
                },
                GameFeedbackCue.Sunk => new[]
                {
                    new ToneStep(860, 46, 12),
                    new ToneStep(990, 56, 8),
                    new ToneStep(1160, 72)
                },
                GameFeedbackCue.Win => new[]
                {
                    new ToneStep(760, 52, 10),
                    new ToneStep(940, 52, 10),
                    new ToneStep(1160, 86, 18),
                    new ToneStep(1320, 94)
                },
                GameFeedbackCue.Loss => new[]
                {
                    new ToneStep(520, 76, 12),
                    new ToneStep(420, 90, 10),
                    new ToneStep(320, 122)
                },
                GameFeedbackCue.Draw => new[]
                {
                    new ToneStep(650, 62, 10),
                    new ToneStep(650, 62)
                },
                GameFeedbackCue.NewGame => new[]
                {
                    new ToneStep(700, 44, 8),
                    new ToneStep(880, 58)
                },
                GameFeedbackCue.PlacementComplete => new[]
                {
                    new ToneStep(840, 46, 6),
                    new ToneStep(980, 52)
                },
                GameFeedbackCue.PlaceShip => new[]
                {
                    new ToneStep(560, 28, 4),
                    new ToneStep(620, 34)
                },
                _ => new[]
                {
                    new ToneStep(500, 34)
                }
            };

            _ = Task.Run(async () =>
            {
                foreach (var tone in sequence)
                {
                    try
                    {
                        Console.Beep(tone.Frequency, tone.DurationMs);
                    }
                    catch
                    {
                    }

                    if (tone.GapMs > 0)
                    {
                        try
                        {
                            await Task.Delay(tone.GapMs).ConfigureAwait(false);
                        }
                        catch
                        {
                        }
                    }
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

            if (cue is GameFeedbackCue.Win)
            {
                Vibration.Default.Vibrate(TimeSpan.FromMilliseconds(95));
                HapticFeedback.Default.Perform(HapticFeedbackType.LongPress);
                return;
            }

            if (cue is GameFeedbackCue.Sunk)
            {
                Vibration.Default.Vibrate(TimeSpan.FromMilliseconds(72));
                HapticFeedback.Default.Perform(HapticFeedbackType.LongPress);
                return;
            }

            if (cue is GameFeedbackCue.Loss or GameFeedbackCue.Draw)
            {
                Vibration.Default.Vibrate(TimeSpan.FromMilliseconds(70));
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

file readonly record struct ToneStep(int Frequency, int DurationMs, int GapMs = 0);
