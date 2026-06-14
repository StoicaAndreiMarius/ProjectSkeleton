namespace TheAdventure.Models;

public class PlayerObject : RenderableGameObject, ICollidable
{
    private const int Speed = 140;
    private const double InvulnSeconds = 1.0;

    public int Radius => 14;

    private string _currentAnim = "IdleDown";
    private DateTimeOffset _lastHit = DateTimeOffset.MinValue;

    public PlayerObject(SpriteSheet sheet, int x, int y) : base(sheet, (x, y))
    {
        SpriteSheet.ActivateAnimation(_currentAnim);
    }

    public bool IsInvulnerable => (DateTimeOffset.Now - _lastHit).TotalSeconds < InvulnSeconds;

    public void TakeHit()
    {
        _lastHit = DateTimeOffset.Now;
    }

    public void UpdatePosition(double up, double down, double left, double right,
        int worldWidth, int worldHeight, double ms)
    {
        if (up + down + left + right == 0)
        {
            if (_currentAnim != "IdleDown")
            {
                _currentAnim = "IdleDown";
                SpriteSheet.ActivateAnimation(_currentAnim);
            }
            return;
        }

        var step = Speed * (ms / 1000.0);
        int x = Position.X + (int)((right - left) * step);
        int y = Position.Y + (int)((down - up) * step);

        // clamp to world
        if (x < 24) x = 24;
        if (y < 42) y = 42;
        if (x > worldWidth - 24) x = worldWidth - 24;
        if (y > worldHeight - 6) y = worldHeight - 6;

        string next = _currentAnim;
        if (y < Position.Y) next = "MoveUp";
        if (y > Position.Y) next = "MoveDown";
        if (x < Position.X) next = "MoveLeft";
        if (x > Position.X) next = "MoveRight";

        if (next != _currentAnim)
        {
            _currentAnim = next;
            SpriteSheet.ActivateAnimation(_currentAnim);
        }

        Position = (x, y);
    }

    public override void Render(GameRenderer renderer)
    {
        // flicker while invulnerable
        if (IsInvulnerable)
        {
            var ms = (int)(DateTimeOffset.Now - _lastHit).TotalMilliseconds;
            if ((ms / 80) % 2 == 0) return;
        }
        base.Render(renderer);
    }
}
