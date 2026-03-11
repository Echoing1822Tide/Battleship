using Microsoft.Maui.Storage;

namespace BattleshipMaui;

public static class AppAudio
{
    public const string StartupCreator = "Echo_startup.wav";
    public const string StartupVsCode = "VS_Code_startup.wav";
    public const string StartupTitle = "Title.wav";
    public const string TargetLocked = "Target_Locked.wav";
    public const string CommanderTargetHit = "Direct_Hit.wav";
    public const string CommanderTargetMiss = "Target_Missed.wav";
    public const string CommanderTargetSunk = "Enemy_Vessel_Destroyed.wav";
    public const string CommanderPlayerSunk = "User-Player_Vessel_Destroyed.wav";
    public const string VictorySting = "Victory!!.wav";
    public const string VictoryCall = "Victory.wav";
    public const string LossSting = "Lost.wav";
    public const string EnemyWonCall = "Enemy_Won.wav";
    public const string WarOverCall = "War_Over.wav";

    public static string? ResolvePath(string fileName)
    {
        try
        {
            string appBase = AppContext.BaseDirectory;
            string[] candidates =
            {
                Path.Combine(appBase, fileName),
                Path.Combine(appBase, "Resources", "Audio", fileName),
                Path.Combine(appBase, "assets", "audio", fileName),
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
