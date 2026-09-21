using Fusion;
using UnityEngine;

public class PlayerPoints : NetworkBehaviour
{
    private const int StartingPoints = 500;

    [Networked]
    public int TotalPoints { get; set; } = StartingPoints;

    public override void Spawned()
    {
        if (HasStateAuthority == false)
        {
            return;
        }

        TotalPoints = StartingPoints;
    }

    public void AddPoints(int amount)
    {
        if (Object == null || Object.IsValid == false || amount <= 0)
        {
            return;
        }

        if (HasStateAuthority == false)
        {
            RPC_AddPoints(amount);
            return;
        }

        TotalPoints += amount;

        Debug.Log($"{Object.name} Points: {TotalPoints}");
    }

    public bool TrySpendPoints(int amount)
    {
        if (Object == null || Object.IsValid == false || amount < 0)
        {
            return false;
        }

        if (amount == 0)
        {
            return true;
        }

        if (HasStateAuthority == false)
        {
            return false;
        }

        if (TotalPoints < amount)
        {
            return false;
        }

        TotalPoints -= amount;

        Debug.Log($"{Object.name} spent {amount}. Remaining: {TotalPoints}");

        return true;
    }

    [Rpc(RpcSources.All, RpcTargets.StateAuthority, Channel = RpcChannel.Reliable)]
    private void RPC_AddPoints(int amount)
    {
        if (HasStateAuthority == false || amount <= 0)
        {
            return;
        }

        TotalPoints += amount;

        Debug.Log($"{Object.name} Points (RPC): {TotalPoints}");
    }
}