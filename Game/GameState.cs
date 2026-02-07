namespace AVAGunner.Game;

public enum GamePhase
{
    Title,
    Playing,
    Paused,
    Warping,
    GameOver
}

public class GameState
{
    public GamePhase Phase { get; set; } = GamePhase.Title;
    public int Score { get; set; }
    public int Lives { get; set; } = 3;
    public int Wave { get; set; } = 1;
    public int EnemiesDestroyedThisWave { get; set; }
    public int EnemiesPerWave => 4 + Wave; // Slower increase: 5, 6, 7, 8...
    public float DifficultyMultiplier => 1f + (Wave - 1) * 0.1f; // Slower difficulty ramp
    public int ShieldsRemaining { get; set; } = 3;

    public void Reset()
    {
        Phase = GamePhase.Playing;
        Score = 0;
        Lives = 3;
        Wave = 1;
        EnemiesDestroyedThisWave = 0;
        ShieldsRemaining = 3;
    }

    public void NextWave()
    {
        Wave++;
        EnemiesDestroyedThisWave = 0;
        ShieldsRemaining = 3;
    }

    public void LoseLife()
    {
        Lives--;
        if (Lives <= 0)
        {
            Phase = GamePhase.GameOver;
        }
    }

    public void GainLife()
    {
        Lives++;
    }

    public void AddScore(int points)
    {
        Score += points;
    }

    public bool UseShield()
    {
        if (ShieldsRemaining <= 0) return false;
        ShieldsRemaining--;
        return true;
    }
}
