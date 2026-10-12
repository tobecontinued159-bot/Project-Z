using Fusion;
using ProjectZ.Minigames;
using UnityEngine;

namespace ProjectZ.Levels
{
    /// <summary>
    /// Level 1, The Gateway. Watches the two Logic Gate stations.
    /// Completion is both stations reaching the solved phase.
    /// </summary>
    [RequireComponent(typeof(NetworkObject))]
    public class LevelManager_Gateway : NetworkBehaviour
    {
        [SerializeField] private LogicGateMinigame stationOne;
        [SerializeField] private LogicGateMinigame stationTwo;

        private bool _completionLogged;

        public MinigamePhase StationOnePhase { get; private set; }
        public MinigamePhase StationTwoPhase { get; private set; }
        public int StationOneAlarmCount { get; private set; }
        public int StationTwoAlarmCount { get; private set; }

        public int TotalAlarmCount => StationOneAlarmCount + StationTwoAlarmCount;

        public override void Render()
        {
            if (TryReadStation(stationOne, out MinigamePhase phaseOne, out int alarmsOne) == false
                || TryReadStation(stationTwo, out MinigamePhase phaseTwo, out int alarmsTwo) == false)
            {
                return;
            }

            StationOnePhase = phaseOne;
            StationTwoPhase = phaseTwo;
            StationOneAlarmCount = alarmsOne;
            StationTwoAlarmCount = alarmsTwo;

            if (_completionLogged || phaseOne != MinigamePhase.Solved || phaseTwo != MinigamePhase.Solved)
            {
                return;
            }

            _completionLogged = true;
            Debug.Log("Level 1 Complete");
        }

        private static bool TryReadStation(LogicGateMinigame station, out MinigamePhase phase, out int alarmCount)
        {
            phase = MinigamePhase.Idle;
            alarmCount = 0;
            if (station == null || station.Object == null || station.Object.IsValid == false)
            {
                return false;
            }

            phase = station.Phase;
            alarmCount = station.AlarmCount;
            return true;
        }
    }
}
