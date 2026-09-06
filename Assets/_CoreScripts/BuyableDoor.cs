using Fusion;
using UnityEngine;

public class BuyableDoor : NetworkBehaviour
{
    public int doorCost = 100;

    [Networked] public NetworkBool IsOpen { get; set; }

    public override void Spawned()
    {
        ApplyOpenState();
    }

    public override void FixedUpdateNetwork()
    {
        ApplyOpenState();
    }

    private void OnTriggerEnter(Collider other)
    {
        if (IsOpen)
        {
            return;
        }

        if (other.CompareTag("Player") == false)
        {
            return;
        }

        PlayerStats playerStats = other.GetComponentInParent<PlayerStats>();
        if (playerStats == null)
        {
            return;
        }

        if (playerStats.HasStateAuthority == false)
        {
            return;
        }

        if (playerStats.IsDead)
        {
            return;
        }

        if (playerStats.Points < doorCost)
        {
            Debug.Log($"Not enough points. Need {doorCost}, have {playerStats.Points}.");
            return;
        }

        playerStats.Points -= doorCost;
        Debug.Log($"{playerStats.name} spent {doorCost} points. Remaining: {playerStats.Points}");
        RPC_RequestOpenDoor();
    }

    [Rpc(RpcSources.All, RpcTargets.StateAuthority, Channel = RpcChannel.Reliable)]
    private void RPC_RequestOpenDoor()
    {
        if (HasStateAuthority == false || IsOpen)
        {
            return;
        }

        IsOpen = true;
    }

    private void ApplyOpenState()
    {
        if (IsOpen && gameObject.activeSelf)
        {
            gameObject.SetActive(false);
        }
    }
}
