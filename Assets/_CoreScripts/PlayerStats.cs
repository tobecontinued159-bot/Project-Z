using Fusion;
using UnityEngine;

public class PlayerStats : NetworkBehaviour
{
    [Networked] public int Points { get; set; }
    [Networked] public int Kills { get; set; }

    public override void Spawned()
    {
        if (HasStateAuthority)
        {
            Points = 0;
            Kills = 0;
        }
    }

    public void AddPointsLocal(int amount)
    {
        if (HasStateAuthority == false)
        {
            Debug.LogWarning("AddPointsLocal called without StateAuthority, using RPC instead.");
            RPC_AddPoints(amount);
            return;
        }

        Points += amount;
        Debug.Log($"{Object.name} Points (Local): {Points}");
    }

    [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
    public void RPC_AddPoints(int amount)
    {
        Points += amount;
        Debug.Log($"{Object.name} Points (RPC): {Points}");
    }
}
