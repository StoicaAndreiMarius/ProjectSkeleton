namespace TheAdventure.Models;

public class Coin : RenderableGameObject, ICollidable
{
    public int Radius => 10;

    public Coin(SpriteSheet sheet, int x, int y) : base(sheet, (x, y))
    {
        SpriteSheet.ActivateAnimation("Spin");
    }
}
