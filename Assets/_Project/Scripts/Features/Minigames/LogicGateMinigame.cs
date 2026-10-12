using System;
using Fusion;
using UnityEngine;

namespace ProjectZ.Minigames
{
    public enum LogicOperand : byte
    {
        And = 0,
        Or = 1,
        Not = 2
    }

    [Serializable]
    public struct TruthRow
    {
        public bool inputA;
        public bool inputB;
        public bool expectedOutput;
    }

    /// <summary>
    /// Logic Gate Patch. Three sockets are filled with AND, OR, or NOT.
    /// The chain is checked against the four-row truth table on this station.
    /// </summary>
    public class LogicGateMinigame : MinigameStation
    {
        [SerializeField] private TruthRow[] truthTable = DefaultTable();

        [Networked] public LogicOperand Socket0 { get; set; }
        [Networked] public LogicOperand Socket1 { get; set; }
        [Networked] public LogicOperand Socket2 { get; set; }

        public void RequestSetSocket(int socketIndex, LogicOperand operand)
        {
            if (CanSubmit() == false || socketIndex < 0 || socketIndex > 2)
            {
                return;
            }

            if (HasStateAuthority)
            {
                AuthoritySetSocket(Runner.LocalPlayer, socketIndex, operand);
                return;
            }

            RPC_SetSocket(socketIndex, operand);
        }

        public void RequestSubmit()
        {
            if (CanSubmit() == false)
            {
                return;
            }

            if (HasStateAuthority)
            {
                AuthoritySubmit(Runner.LocalPlayer);
                return;
            }

            RPC_Submit();
        }

        public void RequestSubmit(LogicOperand socket0, LogicOperand socket1, LogicOperand socket2)
        {
            if (CanSubmit() == false)
            {
                return;
            }

            if (HasStateAuthority)
            {
                AuthoritySubmitGates(Runner.LocalPlayer, socket0, socket1, socket2);
                return;
            }

            RPC_SubmitGates(socket0, socket1, socket2);
        }

        [Rpc(RpcSources.All, RpcTargets.StateAuthority, Channel = RpcChannel.Reliable)]
        private void RPC_SetSocket(int socketIndex, LogicOperand operand, RpcInfo info = default)
        {
            AuthoritySetSocket(SourceOf(info), socketIndex, operand);
        }

        [Rpc(RpcSources.All, RpcTargets.StateAuthority, Channel = RpcChannel.Reliable)]
        private void RPC_Submit(RpcInfo info = default)
        {
            AuthoritySubmit(SourceOf(info));
        }

        [Rpc(RpcSources.All, RpcTargets.StateAuthority, Channel = RpcChannel.Reliable)]
        private void RPC_SubmitGates(LogicOperand socket0, LogicOperand socket1, LogicOperand socket2, RpcInfo info = default)
        {
            AuthoritySubmitGates(SourceOf(info), socket0, socket1, socket2);
        }

        private void AuthoritySetSocket(PlayerRef player, int socketIndex, LogicOperand operand)
        {
            if (CanAuthorityEdit(player) == false || socketIndex < 0 || socketIndex > 2)
            {
                return;
            }

            switch (socketIndex)
            {
                case 0:
                    Socket0 = operand;
                    break;
                case 1:
                    Socket1 = operand;
                    break;
                default:
                    Socket2 = operand;
                    break;
            }
        }

        private void AuthoritySubmitGates(PlayerRef player, LogicOperand socket0, LogicOperand socket1, LogicOperand socket2)
        {
            if (CanAuthorityEdit(player) == false)
            {
                return;
            }

            Socket0 = socket0;
            Socket1 = socket1;
            Socket2 = socket2;
            AuthoritySubmit(player);
        }

        private void AuthoritySubmit(PlayerRef player)
        {
            if (CanAuthorityEdit(player) == false)
            {
                return;
            }

            if (truthTable == null || truthTable.Length != 4)
            {
                Debug.LogWarning("LogicGateMinigame on " + name + " needs exactly 4 truth-table rows.");
                return;
            }

            if (TableMatches(Socket0, Socket1, Socket2))
            {
                AuthorityAccept();
                return;
            }

            AuthorityReject(true);
        }

        private bool CanAuthorityEdit(PlayerRef player)
        {
            return HasStateAuthority
                && Phase == MinigamePhase.Active
                && player == Operator;
        }

        private bool CanSubmit()
        {
            return Object != null && Object.IsValid && Runner != null && Runner.IsRunning;
        }

        private bool TableMatches(LogicOperand socket0, LogicOperand socket1, LogicOperand socket2)
        {
            for (int i = 0; i < truthTable.Length; i++)
            {
                TruthRow row = truthTable[i];
                if (Evaluate(socket0, socket1, socket2, row.inputA, row.inputB) != row.expectedOutput)
                {
                    return false;
                }
            }

            return true;
        }

        private static bool Evaluate(LogicOperand socket0, LogicOperand socket1, LogicOperand socket2, bool inputA, bool inputB)
        {
            bool value = Apply(socket0, inputA, inputB);
            value = Apply(socket1, value, inputB);
            return Apply(socket2, value, inputA);
        }

        private static bool Apply(LogicOperand operand, bool left, bool right)
        {
            switch (operand)
            {
                case LogicOperand.And:
                    return left && right;
                case LogicOperand.Or:
                    return left || right;
                default:
                    return !left;
            }
        }

        private PlayerRef SourceOf(RpcInfo info)
        {
            if (info.Source.IsRealPlayer)
            {
                return info.Source;
            }

            return Runner != null ? Runner.LocalPlayer : PlayerRef.None;
        }

        private static TruthRow[] DefaultTable()
        {
            return new[]
            {
                new TruthRow { inputA = false, inputB = false, expectedOutput = false },
                new TruthRow { inputA = false, inputB = true, expectedOutput = false },
                new TruthRow { inputA = true, inputB = false, expectedOutput = false },
                new TruthRow { inputA = true, inputB = true, expectedOutput = true }
            };
        }
    }
}

