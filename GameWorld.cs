namespace TheAdventure;

public enum GameStatus { Playing, Won, Lost }

public class GameWorld
{
    public const int MaxHealth = 3;
    public const int WinScore = 500;
    public const int CoinValue = 25;
    public const int KillValue = 50;

    public int Score { get; set; }
    public int Health { get; set; } = MaxHealth;
    public int HighScore { get; set; }
    public GameStatus Status { get; set; } = GameStatus.Playing;

    public void AddScore(int amount)
    {
        Score += amount;
        if (Score > HighScore)
        {
            HighScore = Score;
        }

        if (Status == GameStatus.Playing && Score >= WinScore)
        {
            Status = GameStatus.Won;
        }
    }

    public void Damage()
    {
        if (Status != GameStatus.Playing) return;
        Health--;
        if (Health <= 0)
        {
            Health = 0;
            Status = GameStatus.Lost;
        }
    }

    public void Reset()
    {
        Score = 0;
        Health = MaxHealth;
        Status = GameStatus.Playing;
    }
}
