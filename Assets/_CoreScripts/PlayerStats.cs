using Fusion;
using UnityEngine;

public class PlayerStats : NetworkBehaviour
{
    [Networked] public int Points { get; set; }

    public override void Spawned()
    {
        if (HasStateAuthority)
        {
            Points = 0;
        }
    }

    [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
    public void RPC_AddPoints(int amount)
    {
        Points += amount;
        Debug.Log($"{Object.name} Points: {Points}");
    }
}
