// AI-generated
using System.Text.Json.Serialization;

namespace TheAdventure.Models.Data;

public class Level
{
    [JsonPropertyName("height")] public int? Height { get; set; }
    [JsonPropertyName("width")] public int? Width { get; set; }
    [JsonPropertyName("layers")] public List<Layer> Layers { get; set; } = new();
    [JsonPropertyName("tilesets")] public List<TileSetReference> TileSets { get; set; } = new();
    [JsonPropertyName("tileheight")] public int? TileHeight { get; set; }
    [JsonPropertyName("tilewidth")] public int? TileWidth { get; set; }
}
// end AI-generated
