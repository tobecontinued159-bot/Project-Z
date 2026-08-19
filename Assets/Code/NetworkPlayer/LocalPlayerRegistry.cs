using Fusion;
using UnityEngine;

public static class LocalPlayerRegistry
{
    public static NetworkObject LocalPlayer { get; private set; }

    public static event System.Action<NetworkObject> OnLocalPlayerSpawned;
    public static event System.Action OnLocalPlayerDespawned;

    public static void Register(NetworkObject player)
    {
        if (player == null || !player.HasInputAuthority)
        {
            return;
        }

        LocalPlayer = player;
        OnLocalPlayerSpawned?.Invoke(player);
    }

    public static void Unregister(NetworkObject player)
    {
        if (LocalPlayer != player)
        {
            return;
        }

        LocalPlayer = null;
        OnLocalPlayerDespawned?.Invoke();
    }
}
