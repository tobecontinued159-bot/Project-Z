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

    public void ExecuteCommand(string command)
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
            RPC_GiveAmmoAll();
            return;
        }

        if (command == CommandKillZombies)
        {
            RPC_KillAllZombies();
            return;
        }

        if (command == CommandDoubleDamage)
        {
            RPC_DoubleDamage(damageBuffDuration, damageBuffMultiplier);
        }
    }

    [Rpc(RpcSources.All, RpcTargets.All, Channel = RpcChannel.Reliable)]
    private void RPC_GiveAmmoAll()
    {
        PlayerWeapon[] weapons = FindObjectsByType<PlayerWeapon>(FindObjectsSortMode.None);
        for (int i = 0; i < weapons.Length; i++)
        {
            PlayerWeapon weapon = weapons[i];
            if (weapon == null)
            {
                continue;
            }

            // แก้ไขจุดนี้: เรียกฟังก์ชันเติมกระสุนผ่าน StartReload หรือสั่งเติมกระสุนโดยตรง
            if (weapon.HasStateAuthority || weapon.HasInputAuthority)
            {
                weapon.StartReload(); // สั่งให้ผู้เล่นทุกคนเริ่มรีโหลดกระสุนทันที
            }
        }

        Debug.Log("TerminalBuff: Ammo refilled/reloading for all players.");
    }

    [Rpc(RpcSources.All, RpcTargets.All, Channel = RpcChannel.Reliable)]
    private void RPC_KillAllZombies()
    {
        ZombieAI[] zombies = FindObjectsByType<ZombieAI>(FindObjectsSortMode.None);
        int killed = 0;

        for (int i = 0; i < zombies.Length; i++)
        {
            ZombieAI zombie = zombies[i];
            if (zombie == null)
            {
                continue;
            }

            if (zombie.ForceKill())
            {
                killed++;
            }
        }

        Debug.Log($"TerminalBuff: Insta-killed {killed} zombies.");
    }

    [Rpc(RpcSources.All, RpcTargets.All, Channel = RpcChannel.Reliable)]
    private void RPC_DoubleDamage(float duration, int multiplier)
    {
        PlayerWeapon[] weapons = FindObjectsByType<PlayerWeapon>(FindObjectsSortMode.None);
        for (int i = 0; i < weapons.Length; i++)
        {
            PlayerWeapon weapon = weapons[i];
            if (weapon == null)
            {
                continue;
            }

            if (weapon.HasStateAuthority || weapon.HasInputAuthority)
            {
                weapon.ApplyDamageBuff(duration, multiplier);
            }
        }

        Debug.Log($"TerminalBuff: x{multiplier} damage for {duration:0}s.");
    }
}