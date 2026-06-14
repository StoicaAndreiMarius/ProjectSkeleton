using Silk.NET.SDL;

namespace TheAdventure;

public unsafe class Input
{
    private readonly Sdl _sdl;

    public EventHandler<(int X, int Y)>? OnMouseClick;
    public EventHandler? OnBombKey;
    public EventHandler? OnRestartKey;

    public Input(Sdl sdl)
    {
        _sdl = sdl;
    }

    public bool IsLeftPressed() => IsKey(KeyCode.Left) || IsKey(KeyCode.A);
    public bool IsRightPressed() => IsKey(KeyCode.Right) || IsKey(KeyCode.D);
    public bool IsUpPressed() => IsKey(KeyCode.Up) || IsKey(KeyCode.W);
    public bool IsDownPressed() => IsKey(KeyCode.Down) || IsKey(KeyCode.S);

    private bool IsKey(KeyCode key)
    {
        ReadOnlySpan<byte> state = new(_sdl.GetKeyboardState(null), (int)KeyCode.Count);
        return state[(int)key] == 1;
    }

    public bool ProcessInput()
    {
        var ev = new Event();
        while (_sdl.PollEvent(ref ev) != 0)
        {
            if (ev.Type == (uint)EventType.Quit) return true;

            switch (ev.Type)
            {
                case (uint)EventType.Mousebuttondown:
                    if (ev.Button.Button == (byte)MouseButton.Primary)
                    {
                        OnMouseClick?.Invoke(this, (ev.Button.X, ev.Button.Y));
                    }
                    break;

                case (uint)EventType.Keydown:
                    var k = (KeyCode)ev.Key.Keysym.Scancode;
                    if (k == KeyCode.Escape) return true;
                    if (k == KeyCode.Space) OnBombKey?.Invoke(this, EventArgs.Empty);
                    if (k == KeyCode.R) OnRestartKey?.Invoke(this, EventArgs.Empty);
                    break;
            }
        }
        return false;
    }
}
