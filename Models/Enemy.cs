namespace TheAdventure.Models;

public class Enemy : RenderableGameObject, ICollidable, IUpdatable
{
    private const int Speed = 60;
    public int Radius => 14;

    public Enemy(SpriteSheet sheet, int x, int y) : base(sheet, (x, y))
    {
        SpriteSheet.ActivateAnimation("Move");
    }

    public (int X, int Y) Target { get; set; }

    public void Update(double ms)
    {
        int dx = Target.X - Position.X;
        int dy = Target.Y - Position.Y;
        double dist = Math.Sqrt(dx * dx + dy * dy);
        if (dist < 1) return;

        var step = Speed * (ms / 1000.0);
        double nx = Position.X + (dx / dist) * step;
        double ny = Position.Y + (dy / dist) * step;
        Position = ((int)nx, (int)ny);
    }
}
