// AI-generated
using Point = Silk.NET.SDL.Point;

namespace TheAdventure.Models;

/// <summary>
/// A renderable that auto-expires after a fixed time-to-live. Lifetime is a
/// property on the object — the engine polls <see cref="IsExpired"/> and
/// removes it; animation timing is the sprite sheet's concern.
/// </summary>
public class TemporaryGameObject : RenderableGameObject
{
    public double Ttl { get; init; }
    public bool IsExpired => (DateTimeOffset.Now - _spawnTime).TotalSeconds >= Ttl;

    private readonly DateTimeOffset _spawnTime;

    public TemporaryGameObject(SpriteSheet spriteSheet, double ttl, (int X, int Y) position,
        double angle = 0.0, Point rotationCenter = new())
        : base(spriteSheet, position, angle, rotationCenter)
    {
        Ttl = ttl;
        _spawnTime = DateTimeOffset.Now;
    }
}
// end AI-generated
