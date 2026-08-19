using Fusion;

public static class GameEvents
{
    public static event System.Action<PlayerRef, int> OnZombieKilled;
    public static event System.Action<int> OnWaveStarted;
    public static event System.Action<int> OnWaveCleared;
    public static event System.Action<PlayerRef, int> OnMoneyChanged;
    public static event System.Action<string> OnInteractPromptShow;
    public static event System.Action OnInteractPromptHide;
    public static event System.Action<string> OnZoneUnlocked;

    public static void RaiseZombieKilled(PlayerRef killer, int reward)
    {
        OnZombieKilled?.Invoke(killer, reward);
    }

    public static void RaiseWaveStarted(int waveIndex)
    {
        OnWaveStarted?.Invoke(waveIndex);
    }

    public static void RaiseWaveCleared(int waveIndex)
    {
        OnWaveCleared?.Invoke(waveIndex);
    }

    public static void RaiseMoneyChanged(PlayerRef player, int newBalance)
    {
        OnMoneyChanged?.Invoke(player, newBalance);
    }

    public static void RaiseInteractPromptShow(string message)
    {
        OnInteractPromptShow?.Invoke(message);
    }

    public static void RaiseInteractPromptHide()
    {
        OnInteractPromptHide?.Invoke();
    }

    public static void RaiseZoneUnlocked(string zoneId)
    {
        OnZoneUnlocked?.Invoke(zoneId);
    }
}
