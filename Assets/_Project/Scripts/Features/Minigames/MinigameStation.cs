using System;
using Fusion;
using UnityEngine;

namespace ProjectZ.Minigames
{
    /// <summary>
    /// Networked shell for every station. Fusion can replicate properties on a
    /// NetworkBehaviour, so the shared state lives here. Each puzzle adds its
    /// own answer RPC, then calls <see cref="AuthorityAccept"/> or
    /// <see cref="AuthorityReject"/> from the state authority.
    /// </summary>
    [RequireComponent(typeof(NetworkObject))]
    public abstract class MinigameStation : NetworkBehaviour, IMinigame
    {
        [SerializeField] private string stationId = "station";

        [Networked, OnChangedRender(nameof(HandlePhaseChanged))]
        public MinigamePhase Phase { get; set; }

        [Networked]
        public PlayerRef Operator { get; set; }

        [Networked]
        public int MistakeCount { get; set; }

        [Networked, OnChangedRender(nameof(HandleAlarmChanged))]
        public int AlarmCount { get; set; }

        public string StationId => stationId;
        public bool IsSolved => Phase == MinigamePhase.Solved;
        public bool IsFlawless => IsSolved && AlarmCount == 0;

        public event Action<MinigamePhase> PhaseChanged;
        public event Action AlarmRaised;

        private int _renderedAlarmCount;

        public override void Spawned()
        {
            _renderedAlarmCount = AlarmCount;
        }

        public void RequestBegin()
        {
            if (CanSendRequest() == false)
            {
                return;
            }

            if (HasStateAuthority)
            {
                AuthorityBegin(Runner.LocalPlayer);
                return;
            }

            RPC_RequestBegin();
        }

        public void RequestCancel()
        {
            if (CanSendRequest() == false)
            {
                return;
            }

            if (HasStateAuthority)
            {
                AuthorityCancel(Runner.LocalPlayer);
                return;
            }

            RPC_RequestCancel();
        }

        /// <summary>
        /// State authority only. The solving submission already passed the puzzle check.
        /// </summary>
        protected void AuthorityAccept()
        {
            if (HasStateAuthority == false || Phase != MinigamePhase.Active)
            {
                return;
            }

            Operator = PlayerRef.None;
            Phase = MinigamePhase.Solved;
        }

        /// <summary>
        /// State authority only. Partial puzzle progress stays on the subclass.
        /// Raising the alarm increments <see cref="AlarmCount"/>, which is what a flawless clear reads.
        /// </summary>
        protected void AuthorityReject(bool raiseAlarm)
        {
            if (HasStateAuthority == false || Phase != MinigamePhase.Active)
            {
                return;
            }

            MistakeCount += 1;
            if (raiseAlarm)
            {
                AlarmCount += 1;
            }
        }

        protected virtual void OnPhaseChanged()
        {
        }

        [Rpc(RpcSources.All, RpcTargets.StateAuthority, Channel = RpcChannel.Reliable)]
        private void RPC_RequestBegin(RpcInfo info = default)
        {
            AuthorityBegin(SourceOf(info));
        }

        [Rpc(RpcSources.All, RpcTargets.StateAuthority, Channel = RpcChannel.Reliable)]
        private void RPC_RequestCancel(RpcInfo info = default)
        {
            AuthorityCancel(SourceOf(info));
        }

        private void AuthorityBegin(PlayerRef player)
        {
            if (HasStateAuthority == false || player.IsRealPlayer == false)
            {
                return;
            }

            if (Phase != MinigamePhase.Idle)
            {
                return;
            }

            Operator = player;
            Phase = MinigamePhase.Active;
        }

        private void AuthorityCancel(PlayerRef player)
        {
            if (HasStateAuthority == false || Phase != MinigamePhase.Active)
            {
                return;
            }

            if (player != Operator)
            {
                return;
            }

            Operator = PlayerRef.None;
            Phase = MinigamePhase.Idle;
        }

        private bool CanSendRequest()
        {
            return Object != null
                && Object.IsValid
                && Runner != null
                && Runner.IsRunning;
        }

        private PlayerRef SourceOf(RpcInfo info)
        {
            if (info.Source.IsRealPlayer)
            {
                return info.Source;
            }

            return Runner != null ? Runner.LocalPlayer : PlayerRef.None;
        }

        private void HandlePhaseChanged()
        {
            OnPhaseChanged();
            PhaseChanged?.Invoke(Phase);
        }

        private void HandleAlarmChanged()
        {
            if (AlarmCount > _renderedAlarmCount)
            {
                AlarmRaised?.Invoke();
            }

            _renderedAlarmCount = AlarmCount;
        }
    }
}
