using System.Text.Json;

namespace BattleshipMaui.ViewModels;

public interface IGameStatsStore
{
    GameStatsSnapshot Load();
    void Save(GameStatsSnapshot snapshot);
}

public readonly record struct GameStatsSnapshot(
    int Wins,
    int Losses,
    int Draws,
    int TotalTurns,
    int TotalShots,
    int TotalHits);

public sealed class JsonFileGameStatsStore : IGameStatsStore
{
    private readonly string _filePath;

    public JsonFileGameStatsStore(string? filePath = null)
    {
        _filePath = string.IsNullOrWhiteSpace(filePath)
            ? Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "BattleshipMaui",
                "game-stats.json")
            : filePath;
    }

    public GameStatsSnapshot Load()
    {
        try
        {
            if (!File.Exists(_filePath))
                return default;

            string json = File.ReadAllText(_filePath);
            if (string.IsNullOrWhiteSpace(json))
                return default;

            return JsonSerializer.Deserialize<GameStatsSnapshot>(json);
        }
        catch
        {
            return default;
        }
    }

    public void Save(GameStatsSnapshot snapshot)
    {
        try
        {
            var directory = Path.GetDirectoryName(_filePath);
            if (!string.IsNullOrWhiteSpace(directory))
                Directory.CreateDirectory(directory);

            string json = JsonSerializer.Serialize(snapshot);
            File.WriteAllText(_filePath, json);
        }
        catch
        {
            // Best-effort persistence: skip failures to avoid gameplay interruption.
        }
    }
}
