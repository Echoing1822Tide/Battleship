using Microsoft.Maui.ApplicationModel;
using Microsoft.Maui.Storage;
#if WINDOWS
using Windows.Media.Core;
using Windows.Media.Playback;
#endif

namespace BattleshipMaui.ViewModels;

public interface IGameFeedbackService
{
    void Play(GameFeedbackCue cue, bool soundEnabled, bool hapticsEnabled, bool reduceMotion, string? shipName = null);
}

public sealed class DefaultGameFeedbackService : IGameFeedbackService
{
    private const string SurfaceExplosionTrack = "soundreality-explosion-fx-343683.mp3";
    private const string SubmarineExplosionTrack = "daviddumaisaudio-large-underwater-explosion-190270.mp3";
    private static readonly string[] MissExplosionTracks =
    {
        "Waterside_Explosion_Water_Sound_Effects1.mp3",
        "Waterside_Explosion_Water_Sound_Effects2.mp3",
        "Waterside_Explosion_Water_Sound_Effects3.mp3",
        "Waterside_Explosion_Water_Sound_Effects4.mp3"
    };

#if WINDOWS
    private static readonly object EffectsLock = new();
    private static readonly MediaPlayer? EffectsPlayer = CreateEffectsPlayer();
#endif

    public void Play(GameFeedbackCue cue, bool soundEnabled, bool hapticsEnabled, bool reduceMotion, string? shipName = null)
    {
        if (soundEnabled)
            TryPlaySound(cue, shipName);

        if (hapticsEnabled)
            TryPlayHaptics(cue, reduceMotion);
    }

    private static void TryPlaySound(GameFeedbackCue cue, string? shipName)
    {
        string? effectTrack = cue switch
        {
            GameFeedbackCue.Miss => MissExplosionTracks[Random.Shared.Next(MissExplosionTracks.Length)],
            GameFeedbackCue.Hit or GameFeedbackCue.Sunk => ResolveShipHitTrack(shipName),
            _ => null
        };

        if (!string.IsNullOrWhiteSpace(effectTrack))
        {
            if (TryPlayAudioTrack(effectTrack))
                return;
        }

        TryPlayToneFallback(cue);
    }

    private static string ResolveShipHitTrack(string? shipName)
    {
        string normalized = NormalizeShipName(shipName);
        if (normalized.Contains("submarine", StringComparison.Ordinal))
            return SubmarineExplosionTrack;

        return SurfaceExplosionTrack;
    }

    private static string NormalizeShipName(string? shipName)
    {
        if (string.IsNullOrWhiteSpace(shipName))
            return string.Empty;

        return new string(shipName
            .Where(char.IsLetterOrDigit)
            .Select(char.ToLowerInvariant)
            .ToArray());
    }

    private static void TryPlayToneFallback(GameFeedbackCue cue)
    {
        try
        {
            var sequence = cue switch
            {
                GameFeedbackCue.Miss => new[]
                {
                    new ToneStep(520, 24, 6),
                    new ToneStep(440, 30, 4),
                    new ToneStep(390, 36)
                },
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
                    new ToneStep(680, 34, 6),
                    new ToneStep(840, 40, 8),
                    new ToneStep(1020, 52)
                },
                GameFeedbackCue.PlacementComplete => new[]
                {
                    new ToneStep(840, 46, 6),
                    new ToneStep(980, 52)
                },
                GameFeedbackCue.PlaceShip => new[]
                {
                    new ToneStep(520, 22, 4),
                    new ToneStep(610, 28, 5),
                    new ToneStep(560, 22)
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

    private static bool TryPlayAudioTrack(string fileName)
    {
#if WINDOWS
        try
        {
            if (EffectsPlayer is null)
                return false;

            string? path = ResolveAudioPath(fileName);
            if (string.IsNullOrWhiteSpace(path))
                return false;

            lock (EffectsLock)
            {
                EffectsPlayer.Source = MediaSource.CreateFromUri(new Uri(path));
                EffectsPlayer.Play();
            }

            return true;
        }
        catch
        {
            return false;
        }
#else
        _ = fileName;
        return false;
#endif
    }

#if WINDOWS
    private static MediaPlayer? CreateEffectsPlayer()
    {
        try
        {
            return new MediaPlayer
            {
                IsLoopingEnabled = false,
                AutoPlay = false,
                AudioCategory = MediaPlayerAudioCategory.GameEffects,
                Volume = 1
            };
        }
        catch
        {
            return null;
        }
    }
#endif

    private static string? ResolveAudioPath(string fileName)
    {
        try
        {
            string appBase = AppContext.BaseDirectory;
            string[] candidates =
            {
                Path.Combine(appBase, fileName),
                Path.Combine(appBase, "Resources", "Audio", fileName),
                Path.Combine(appBase, "Assets", fileName),
                Path.Combine(FileSystem.Current.AppDataDirectory, fileName)
            };

            foreach (var candidate in candidates)
            {
                if (File.Exists(candidate))
                    return candidate;
            }
        }
        catch
        {
            // Ignore and fall through to null.
        }

        return null;
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
