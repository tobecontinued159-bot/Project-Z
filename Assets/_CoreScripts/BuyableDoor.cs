using Fusion;
using UnityEngine;

public class BuyableDoor : NetworkBehaviour
{
    [Header("Door Settings")]
    public int doorCost = 100;

    [Networked] public NetworkBool IsOpen { get; set; }

    public override void Spawned()
    {
        if (IsOpen)
        {
            HideDoor();
        }
    }

    public override void FixedUpdateNetwork()
    {
        if (IsOpen)
        {
            HideDoor();
        }
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
            Debug.Log($"Not enough points to open {name}. Need {doorCost}, have {playerStats.Points}.");
            return;
        }

        playerStats.Points -= doorCost;
        Debug.Log($"{playerStats.Object.name} spent {doorCost} points to open {name}. Remaining: {playerStats.Points}");
        RPC_RequestOpenDoor();
    }

    [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
    private void RPC_RequestOpenDoor()
    {
        if (HasStateAuthority == false)
        {
            return;
        }

        if (IsOpen)
        {
            return;
        }

        IsOpen = true;
        Debug.Log($"{name} opened.");
    }

    private void HideDoor()
    {
        if (gameObject.activeSelf)
        {
            gameObject.SetActive(false);
        }
    }
}
