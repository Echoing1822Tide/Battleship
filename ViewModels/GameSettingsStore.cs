using System.Text.Json;

namespace BattleshipMaui.ViewModels;

public interface IGameSettingsStore
{
    GameSettingsSnapshot Load();
    void Save(GameSettingsSnapshot settings);
}

public readonly record struct GameSettingsSnapshot(
    CpuDifficulty Difficulty,
    AnimationSpeed AnimationSpeed,
    bool SoundEnabled,
    bool HapticsEnabled,
    bool HighContrastMode,
    bool LargeTextMode,
    bool ReduceMotionMode,
    bool SettingsPanelOpen,
    bool HasSeenCommandBriefing,
    GameThemePreset Theme = GameThemePreset.RetroWave80s,
    bool MusicEnabled = true,
    double MusicVolume = 0.10,
    bool HasConfiguredMusicPreference = false,
    double SoundFxVolume = 0.10,
    bool TurnCinematicsEnabled = false,
    bool HasConfiguredTurnCinematicsPreference = false,
    bool CommanderVoiceEnabled = true,
    bool HasConfiguredCommanderVoicePreference = false)
{
    public static GameSettingsSnapshot Default => new(
        Difficulty: CpuDifficulty.Standard,
        AnimationSpeed: AnimationSpeed.Normal,
        SoundEnabled: true,
        HapticsEnabled: true,
        HighContrastMode: false,
        LargeTextMode: false,
        ReduceMotionMode: false,
        SettingsPanelOpen: false,
        HasSeenCommandBriefing: false,
        Theme: GameThemePreset.RetroWave80s,
        MusicEnabled: true,
        MusicVolume: 0.10,
        HasConfiguredMusicPreference: false,
        SoundFxVolume: 0.10,
        TurnCinematicsEnabled: false,
        HasConfiguredTurnCinematicsPreference: false,
        CommanderVoiceEnabled: true,
        HasConfiguredCommanderVoicePreference: false);
}

public sealed class JsonFileGameSettingsStore : IGameSettingsStore
{
    private readonly string _filePath;

    public JsonFileGameSettingsStore(string? filePath = null)
    {
        _filePath = string.IsNullOrWhiteSpace(filePath)
            ? Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "BattleshipMaui",
                "game-settings.json")
            : filePath;
    }

    public GameSettingsSnapshot Load()
    {
        try
        {
            if (!File.Exists(_filePath))
                return GameSettingsSnapshot.Default;

            string json = File.ReadAllText(_filePath);
            if (string.IsNullOrWhiteSpace(json))
                return GameSettingsSnapshot.Default;

            var snapshot = JsonSerializer.Deserialize<GameSettingsSnapshot>(json);
            return snapshot with
            {
                Difficulty = Enum.IsDefined(snapshot.Difficulty) ? snapshot.Difficulty : CpuDifficulty.Standard,
                AnimationSpeed = Enum.IsDefined(snapshot.AnimationSpeed) ? snapshot.AnimationSpeed : AnimationSpeed.Normal,
                Theme = Enum.IsDefined(snapshot.Theme) ? snapshot.Theme : GameThemePreset.RetroWave80s,
                SoundFxVolume = snapshot.SoundFxVolume is > 1 or < 0 ? 0.10 : snapshot.SoundFxVolume,
                MusicVolume = snapshot.MusicVolume is > 1 or < 0 ? 0.10 : snapshot.MusicVolume
            };
        }
        catch
        {
            return GameSettingsSnapshot.Default;
        }
    }

    public void Save(GameSettingsSnapshot settings)
    {
        try
        {
            var directory = Path.GetDirectoryName(_filePath);
            if (!string.IsNullOrWhiteSpace(directory))
                Directory.CreateDirectory(directory);

            string json = JsonSerializer.Serialize(settings);
            File.WriteAllText(_filePath, json);
        }
        catch
        {
            // Best-effort persistence: settings save failures should not block gameplay.
        }
    }
}
