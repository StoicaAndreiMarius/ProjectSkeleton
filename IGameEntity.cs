namespace TheAdventure;

public interface IGameEntity
{
    int Id { get; }
    (int X, int Y) Position { get; }
}

public interface IUpdatable
{
    void Update(double msSinceLastFrame);
}

public interface ICollidable : IGameEntity
{
    int Radius { get; }
}
