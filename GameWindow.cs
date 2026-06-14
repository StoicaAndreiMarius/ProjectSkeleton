// AI-generated
using Silk.NET.SDL;

namespace TheAdventure;

/// <summary>
/// Owns the SDL OS window. All raw window-handle manipulation (including the
/// unsafe pointer casts) is contained here. Implements IDisposable so the
/// native window is always destroyed, with a finalizer as a safety net.
/// </summary>
public unsafe class GameWindow : IDisposable
{
    private readonly Sdl _sdl;
    private IntPtr _window;

    public GameWindow(Sdl sdl)
    {
        _sdl = sdl;

        _window = (IntPtr)_sdl.CreateWindow(
            "The Adventure - Coin Run", Sdl.WindowposUndefined, Sdl.WindowposUndefined, 640, 400,
            (uint)WindowFlags.Resizable | (uint)WindowFlags.AllowHighdpi
        );

        if (_window == IntPtr.Zero)
        {
            var ex = _sdl.GetErrorAsException();
            if (ex != null)
            {
                throw ex;
            }

            throw new Exception("Failed to create window.");
        }
    }

    public IntPtr CreateRenderer()
    {
        var renderer = (IntPtr)_sdl.CreateRenderer((Window*)_window, -1, (uint)RendererFlags.Accelerated);
        if (renderer == IntPtr.Zero)
        {
            var ex = _sdl.GetErrorAsException();
            if (ex != null)
            {
                throw ex;
            }

            throw new Exception("Failed to create renderer.");
        }

        _sdl.RenderSetVSync((Renderer*)renderer, 1);
        return renderer;
    }

    public (int Width, int Height) Size
    {
        get
        {
            int width = 0;
            int height = 0;
            _sdl.GetWindowSize((Window*)_window, ref width, ref height);
            return (width, height);
        }
    }

    private void ReleaseUnmanagedResources()
    {
        if (_window != IntPtr.Zero)
        {
            _sdl.DestroyWindow((Window*)_window);
            _window = IntPtr.Zero;
        }
    }

    public void Dispose()
    {
        ReleaseUnmanagedResources();
        GC.SuppressFinalize(this);
    }

    ~GameWindow()
    {
        ReleaseUnmanagedResources();
    }
}
// end AI-generated
