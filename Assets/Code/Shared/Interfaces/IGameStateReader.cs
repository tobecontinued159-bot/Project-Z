public interface IGameStateReader
{
    int CurrentWave { get; }
    bool IsWaveActive { get; }
    bool IsGameOver { get; }
}
