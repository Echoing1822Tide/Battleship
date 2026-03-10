using Microsoft.Maui.Storage;

namespace BattleshipMaui;

public static class AppAudio
{
    public const string StartupCreator = "Echo_startup.wav";
    public const string StartupVsCode = "VS_Code_startup.wav";
    public const string StartupTitle = "Title.wav";
    public const string CommanderTargetHit = "Target_Hit.wav";
    public const string CommanderTargetMiss = "Target_miss.wav";

    public static string? ResolvePath(string fileName)
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

            foreach (string candidate in candidates)
            {
                if (File.Exists(candidate))
                    return candidate;
            }
        }
        catch
        {
            // Asset resolution should never block gameplay.
        }

        return null;
    }
}
