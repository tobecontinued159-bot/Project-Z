using Fusion;
using UnityEngine;

public class TerminalBuffManager : NetworkBehaviour
{
    public const string CommandGiveAmmo = "sudo give_ammo_all";
    public const string CommandKillZombies = "rm -rf zombies";
    public const string CommandDoubleDamage = "chmod 777 damage";

    [SerializeField] private float damageBuffDuration = 15f;
    [SerializeField] private int damageBuffMultiplier = 2;

    public static TerminalBuffManager Instance { get; private set; }

    public override void Spawned()
    {
        Instance = this;
    }

    public override void Despawned(NetworkRunner runner, bool hasState)
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }

    public void ApplyTerminalBuff(string command)
    {
        if (Object == null || Object.IsValid == false)
        {
            Debug.LogWarning("TerminalBuffManager: NetworkObject is not spawned.");
            return;
        }

        if (string.IsNullOrEmpty(command))
        {
            return;
        }

        if (command == CommandGiveAmmo)
        {
            Rpc_GiveMaxAmmo();
            return;
        }

        if (command == CommandKillZombies)
        {
            Rpc_InstaKillZombies();
            return;
        }

        if (command == CommandDoubleDamage)
        {
            Rpc_DoubleDamage();
        }
    }

    [Rpc(RpcSources.All, RpcTargets.All, Channel = RpcChannel.Reliable)]
    private void Rpc_GiveMaxAmmo()
    {
        PlayerWeapon[] weapons = FindObjectsByType<PlayerWeapon>(FindObjectsSortMode.None);
        for (int i = 0; i < weapons.Length; i++)
        {
            PlayerWeapon weapon = weapons[i];
            if (IsUsableWeapon(weapon) == false)
            {
                continue;
            }

            // RpcTargets.All: each peer only writes ammo on weapons it owns.
            if (weapon.HasStateAuthority)
            {
                weapon.RefillAmmo();
            }
        }

        Debug.Log("Network Buff: Max Ammo given to all players!");
    }

    [Rpc(RpcSources.All, RpcTargets.All, Channel = RpcChannel.Reliable)]
    private void Rpc_InstaKillZombies()
    {
        ZombieAI[] zombies = FindObjectsByType<ZombieAI>(FindObjectsSortMode.None);
        int killed = 0;

        for (int i = 0; i < zombies.Length; i++)
        {
            ZombieAI zombie = zombies[i];
            if (zombie == null || zombie.Object == null || zombie.Object.IsValid == false)
            {
                continue;
            }

            if (zombie.ForceKill())
            {
                killed++;
            }
        }

        Debug.Log($"Network Buff: All zombies destroyed! ({killed} killed on this peer)");
    }

    [Rpc(RpcSources.All, RpcTargets.All, Channel = RpcChannel.Reliable)]
    private void Rpc_DoubleDamage()
    {
        PlayerWeapon[] weapons = FindObjectsByType<PlayerWeapon>(FindObjectsSortMode.None);
        for (int i = 0; i < weapons.Length; i++)
        {
            PlayerWeapon weapon = weapons[i];
            if (IsUsableWeapon(weapon) == false)
            {
                continue;
            }

            if (weapon.HasStateAuthority)
            {
                weapon.ApplyDamageBuff(damageBuffDuration, damageBuffMultiplier);
            }
        }

        Debug.Log("Network Buff: Double damage active!");
    }

    private static bool IsUsableWeapon(PlayerWeapon weapon)
    {
        return weapon != null && weapon.Object != null && weapon.Object.IsValid;
    }
}
