using System.Collections;
using Fusion;
using UnityEngine;

public class TerminalBuffManager : NetworkBehaviour
{
    public static TerminalBuffManager Instance { get; private set; }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    public void ApplyTerminalBuff(string command)
    {
        string formattedCommand = command.Trim().ToLower();

        if (HasStateAuthority)
        {
            RPC_ExecuteBuff(formattedCommand);
        }
        else
        {
            RPC_RequestExecuteBuff(formattedCommand);
        }
    }

    [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
    private void RPC_RequestExecuteBuff(string command)
    {
        RPC_ExecuteBuff(command);
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    private void RPC_ExecuteBuff(string command)
    {
        switch (command)
        {
            case "print ammo":
                ExecuteRefillAllAmmo();
                break;

            case "print buff":
                ExecuteDamageBuffAll();
                break;

            case "print clearzombie":
                ExecuteClearAllZombies();
                break;

            case "print point":
                ExecuteAddPointsAll(2000);
                break;

            case "print immortal":
                StartCoroutine(ImmortalBuffRoutine(10f));
                break;

            default:
                Debug.LogWarning($"TerminalBuffManager: Unknown command '{command}'");
                break;
        }
    }

    // 1. เติมกระสุนกลับมาเต็มให้ทุกคน
    private void ExecuteRefillAllAmmo()
    {
        PlayerWeapon[] allWeapons = FindObjectsOfType<PlayerWeapon>();
        foreach (PlayerWeapon weapon in allWeapons)
        {
            if (weapon != null)
            {
                weapon.RefillAmmo();
            }
        }
        Debug.Log("Terminal Command: All players refilled ammo!");
    }

    // 2. บัฟเพิ่มดาเมจให้ทุกคน
    private void ExecuteDamageBuffAll()
    {
        PlayerWeapon[] allWeapons = FindObjectsOfType<PlayerWeapon>();
        foreach (PlayerWeapon weapon in allWeapons)
        {
            if (weapon != null)
            {
                // 🟢 ส่ง duration = 15 วินาที, bonusDamage = 20 ดาเมจ
                weapon.ApplyDamageBuff(15f, 20);
            }
        }
        Debug.Log("Terminal Command: All players received damage buff!");
    }

    // 3. เคลียร์ซอมบี้ทั้งหมดในแมป
    private void ExecuteClearAllZombies()
    {
        // ทำการลบวัตถุที่มี Tag หรือ Component ของซอมบี้ออกจากฉากทันที
        GameObject[] zombies = GameObject.FindGameObjectsWithTag("Zombie");
        foreach (GameObject zombie in zombies)
        {
            if (zombie != null)
            {
                // หากใช้ Photon Fusion ในการสปอว์นซอมบี้ ให้ใช้ Runner.Despawn หรือ Destroy ปกติ
                NetworkObject netObj = zombie.GetComponent<NetworkObject>();
                if (netObj != null && Runner != null && netObj.HasStateAuthority)
                {
                    Runner.Despawn(netObj);
                }
                else
                {
                    Destroy(zombie);
                }
            }
        }
        Debug.Log("Terminal Command: All zombies cleared!");
    }

    // 4. แจก 2000 Points ให้กับทุกคน
    private void ExecuteAddPointsAll(int amount)
    {
        PlayerStats[] allStats = FindObjectsOfType<PlayerStats>();
        foreach (PlayerStats stats in allStats)
        {
            if (stats != null)
            {
                // ปรับเพิ่ม Points เข้าตัวแปร Points ของ PlayerStats โดยตรง
                stats.Points += amount;
            }
        }
        Debug.Log($"Terminal Command: Added {amount} points to all players!");
    }

    // 5. ทำให้ทุกคนเป็นอมตะ 10 วินาที
    private IEnumerator ImmortalBuffRoutine(float duration)
    {
        PlayerStats[] allStats = FindObjectsOfType<PlayerStats>();

        // 🟢 เปิดโหมดอมตะให้ผู้เล่นทุกคน (ไม่โดนหัก Health ใน ApplyDamage)
        foreach (PlayerStats stats in allStats)
        {
            if (stats != null)
            {
                stats.IsImmortal = true;
                stats.Health = stats.MaxHealth; // เติมเลือดให้เต็ม
            }
        }

        Debug.Log($"Terminal Command: All players are IMMORTAL for {duration} seconds!");

        yield return new WaitForSeconds(duration);

        // ครบ 10 วินาที ปิดโหมดอมตะ
        foreach (PlayerStats stats in allStats)
        {
            if (stats != null)
            {
                stats.IsImmortal = false;
            }
        }

        Debug.Log("Terminal Command: Immortal status expired.");
    }
}