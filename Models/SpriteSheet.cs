// AI-generated
using System.Text.Json;
using Silk.NET.Maths;
using Silk.NET.SDL;
using TheAdventure.Exceptions;
using Point = Silk.NET.SDL.Point;

namespace TheAdventure.Models;

/// <summary>
/// Texture + named animations container. Built from JSON via <see cref="Load"/>.
/// Animations are advanced from wall-clock time; non-looping animations clamp
/// to the last frame (the owning object decides when to remove itself).
/// </summary>
public class SpriteSheet
{
    public struct Position
    {
        public int Row { get; set; }
        public int Col { get; set; }
    }

    public struct Offset
    {
        public int OffsetX { get; set; }
        public int OffsetY { get; set; }
    }

    public class Animation
    {
        public Position StartFrame { get; set; }
        public Position EndFrame { get; set; }
        public RendererFlip Flip { get; set; } = RendererFlip.None;
        public int DurationMs { get; set; }
        public bool Loop { get; set; }
    }

    public int RowCount { get; set; }
    public int ColumnCount { get; set; }
    public int FrameWidth { get; set; }
    public int FrameHeight { get; set; }
    public Offset FrameCenter { get; set; }
    public string? FileName { get; set; }

    public Dictionary<string, Animation> Animations { get; set; } = new();
    public Animation? ActiveAnimation { get; private set; }

    private int _textureId = -1;
    private DateTimeOffset _animationStart = DateTimeOffset.MinValue;

    public static SpriteSheet Load(GameRenderer renderer, string fileName, string directory)
    {
        var path = Path.Combine(directory, fileName);
        if (!File.Exists(path))
        {
            throw new AssetLoadException($"Sprite sheet definition not found: {path}");
        }

        var json = File.ReadAllText(path);
        SpriteSheet? sheet;
        try
        {
            sheet = JsonSerializer.Deserialize<SpriteSheet>(json, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
            });
        }
        catch (JsonException ex)
        {
            throw new AssetLoadException($"Sprite sheet {fileName} is malformed: {ex.Message}", ex);
        }

        if (sheet is null)
        {
            throw new AssetLoadException($"Failed to load sprite sheet: {fileName}");
        }

        if (string.IsNullOrWhiteSpace(sheet.FileName))
        {
            throw new AssetLoadException($"Sprite sheet {fileName} has no fileName field.");
        }

        if (sheet.FrameWidth <= 0 || sheet.FrameHeight <= 0)
        {
            throw new AssetLoadException($"Sprite sheet {fileName} has invalid frame dimensions.");
        }

        if (sheet.RowCount <= 0 || sheet.ColumnCount <= 0)
        {
            throw new AssetLoadException($"Sprite sheet {fileName} has invalid row/column count.");
        }

        sheet._textureId = renderer.LoadTexture(Path.Combine(directory, sheet.FileName), out _);
        return sheet;
    }

    public void ActivateAnimation(string name)
    {
        if (!Animations.TryGetValue(name, out var animation)) return;
        ActiveAnimation = animation;
        _animationStart = DateTimeOffset.Now;
    }

    public void Render(GameRenderer renderer, (int X, int Y) dest, double angle = 0.0,
        Point rotationCenter = new())
    {
        var dst = new Rectangle<int>(
            dest.X - FrameCenter.OffsetX, dest.Y - FrameCenter.OffsetY,
            FrameWidth, FrameHeight);

        if (ActiveAnimation is null)
        {
            renderer.RenderTexture(_textureId,
                new Rectangle<int>(0, 0, FrameWidth, FrameHeight),
                dst, RendererFlip.None, angle, rotationCenter);
            return;
        }

        var totalFrames = (ActiveAnimation.EndFrame.Row - ActiveAnimation.StartFrame.Row) * ColumnCount
                          + ActiveAnimation.EndFrame.Col - ActiveAnimation.StartFrame.Col;
        if (totalFrames < 0) totalFrames = 0;

        var elapsed = (DateTimeOffset.Now - _animationStart).TotalMilliseconds;
        var perFrame = totalFrames == 0 ? ActiveAnimation.DurationMs : ActiveAnimation.DurationMs / (double)totalFrames;
        var currentFrame = perFrame > 0 ? (int)(elapsed / perFrame) : 0;

        if (currentFrame > totalFrames)
        {
            if (ActiveAnimation.Loop)
            {
                _animationStart = DateTimeOffset.Now;
                currentFrame = 0;
            }
            else
            {
                currentFrame = totalFrames;
            }
        }

        var row = ActiveAnimation.StartFrame.Row + currentFrame / ColumnCount;
        var col = ActiveAnimation.StartFrame.Col + currentFrame % ColumnCount;

        renderer.RenderTexture(_textureId,
            new Rectangle<int>(col * FrameWidth, row * FrameHeight, FrameWidth, FrameHeight),
            dst, ActiveAnimation.Flip, angle, rotationCenter);
    }
}
// end AI-generated
