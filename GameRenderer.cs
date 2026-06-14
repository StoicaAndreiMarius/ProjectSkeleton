// AI-generated
using Silk.NET.Maths;
using Silk.NET.SDL;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using TheAdventure.Exceptions;
using TheAdventure.Models;
using Point = Silk.NET.SDL.Point;

namespace TheAdventure;

/// <summary>
/// Owns the SDL renderer and a per-frame Camera. Provides texture loading via
/// ImageSharp + SDL_CreateTextureFromSurface, world-space drawing
/// (camera-translated) and screen-space drawing (HUD).
/// </summary>
public unsafe class GameRenderer
{
    private readonly Sdl _sdl;
    private readonly Renderer* _renderer;
    private readonly Camera _camera;

    private readonly Dictionary<int, IntPtr> _texturePointers = new();
    private readonly Dictionary<int, TextureData> _textureData = new();
    private int _nextTextureId;

    public GameRenderer(Sdl sdl, GameWindow window)
    {
        _sdl = sdl;
        _renderer = (Renderer*)window.CreateRenderer();
        _sdl.SetRenderDrawBlendMode(_renderer, BlendMode.Blend);

        var size = window.Size;
        _camera = new Camera(size.Width, size.Height);
    }

    public (int Width, int Height) ViewportSize => (_camera.Width, _camera.Height);

    public int LoadTexture(string fileName, out TextureData textureInfo)
    {
        if (!File.Exists(fileName))
        {
            throw new AssetLoadException($"Texture file not found: {fileName}");
        }

        using var fStream = new FileStream(fileName, FileMode.Open, FileAccess.Read, FileShare.Read);
        using var image = Image.Load<Rgba32>(fStream);

        textureInfo = new TextureData
        {
            Width = image.Width,
            Height = image.Height,
        };

        var rawData = new byte[textureInfo.Width * textureInfo.Height * 4];
        image.CopyPixelDataTo(rawData.AsSpan());

        fixed (byte* data = rawData)
        {
            var surface = _sdl.CreateRGBSurfaceWithFormatFrom(
                data, textureInfo.Width, textureInfo.Height,
                8, textureInfo.Width * 4, (uint)PixelFormatEnum.Rgba32);

            if (surface == null)
            {
                throw new AssetLoadException($"Failed to create surface for: {fileName}");
            }

            var texture = _sdl.CreateTextureFromSurface(_renderer, surface);
            _sdl.FreeSurface(surface);

            if (texture == null)
            {
                throw new AssetLoadException($"Failed to create texture for: {fileName}");
            }

            var id = _nextTextureId++;
            _texturePointers[id] = (IntPtr)texture;
            _textureData[id] = textureInfo;
            return id;
        }
    }

    public void RenderTexture(int textureId, Rectangle<int> src, Rectangle<int> dst,
        RendererFlip flip = RendererFlip.None, double angle = 0.0, Point center = default)
    {
        if (!_texturePointers.TryGetValue(textureId, out var ptr)) return;
        var translatedDst = _camera.ToScreenCoordinates(dst);
        _sdl.RenderCopyEx(_renderer, (Texture*)ptr, in src, in translatedDst, angle, in center, flip);
    }

    public void RenderTextureScreen(int textureId, Rectangle<int> src, Rectangle<int> dst,
        RendererFlip flip = RendererFlip.None)
    {
        if (!_texturePointers.TryGetValue(textureId, out var ptr)) return;
        Point zero = default;
        _sdl.RenderCopyEx(_renderer, (Texture*)ptr, in src, in dst, 0.0, in zero, flip);
    }

    public void FillRectScreen(int x, int y, int w, int h, byte r, byte g, byte b, byte a)
    {
        _sdl.SetRenderDrawColor(_renderer, r, g, b, a);
        var rect = new Rectangle<int>(x, y, w, h);
        _sdl.RenderFillRect(_renderer, in rect);
    }

    public void SetDrawColor(byte r, byte g, byte b, byte a) =>
        _sdl.SetRenderDrawColor(_renderer, r, g, b, a);

    public void ClearScreen() => _sdl.RenderClear(_renderer);

    public void PresentFrame() => _sdl.RenderPresent(_renderer);

    public void SetWorldBounds(Rectangle<int> bounds) => _camera.SetWorldBounds(bounds);

    public void CameraLookAt(int x, int y) => _camera.LookAt(x, y);

    public Vector2D<int> ToWorldCoordinates(int x, int y) =>
        _camera.ToWorldCoordinates(new Vector2D<int>(x, y));
}
// end AI-generated
