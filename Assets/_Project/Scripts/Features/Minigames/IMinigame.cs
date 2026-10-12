using Fusion;

namespace ProjectZ.Minigames
{
    public enum MinigamePhase : byte
    {
        Idle = 0,
        Active = 1,
        Solved = 2
    }

    /// <summary>
    /// What the rest of the match is allowed to ask a station.
    /// Puzzle answers stay on the concrete station. This contract is only
    /// occupancy, progress, and the alarm count the scoreboard reads.
    /// </summary>
    public interface IMinigame
    {
        MinigamePhase Phase { get; }
        bool IsSolved { get; }
        bool IsFlawless { get; }
        PlayerRef Operator { get; }
        int MistakeCount { get; }
        int AlarmCount { get; }

        void RequestBegin();
        void RequestCancel();
    }
}
