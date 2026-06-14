using System.Text.Json;

namespace TheAdventure;

public class SaveManager
{
    private readonly string _path;

    public SaveManager(string path)
    {
        _path = path;
    }

    public record SaveData(int HighScore);

    public async Task<int> LoadHighScoreAsync()
    {
        if (!File.Exists(_path)) return 0;

        try
        {
            var text = await File.ReadAllTextAsync(_path);
            var data = JsonSerializer.Deserialize<SaveData>(text);
            return data?.HighScore ?? 0;
        }
        catch
        {
            return 0;
        }
    }

    public async Task SaveHighScoreAsync(int highScore)
    {
        var data = new SaveData(highScore);
        var text = JsonSerializer.Serialize(data);
        await File.WriteAllTextAsync(_path, text);
    }
}
