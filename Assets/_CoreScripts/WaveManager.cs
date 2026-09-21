using Fusion;
using UnityEngine;
using UnityEngine.AI;

public class WaveManager : NetworkBehaviour
{
    [Networked] public int CurrentWave { get; set; }
    [Networked] public int ZombiesRemaining { get; set; }
    [Networked] public NetworkBool IsBreakTime { get; set; }
    [Networked] private TickTimer BreakTimer { get; set; }
    [Networked] private TickTimer SpawnTimer { get; set; }
    [Networked] private int ZombiesLeftToSpawn { get; set; }

    [Header("Spawn Setup")]
    public Transform[] spawnPoints;
    [SerializeField] private NetworkObject zombiePrefab;

    [Header("Wave Settings")]
    [SerializeField] private float breakDuration = 10f;
    [SerializeField] private float spawnInterval = 0.75f;
    [SerializeField] private int zombiesPerWave = 5;

    public static WaveManager Instance { get; private set; }

    public override void Spawned()
    {
        Instance = this;

        if (HasStateAuthority == false)
        {
            return;
        }

        CurrentWave = 0;
        ZombiesRemaining = 0;
        ZombiesLeftToSpawn = 0;

        IsBreakTime = true;

        BreakTimer = TickTimer.CreateFromSeconds(
            Runner,
            breakDuration
        );

        SpawnTimer = TickTimer.None;

        Debug.Log(
            $"WaveManager: Break time started ({breakDuration}s)."
        );
    }

    public override void Despawned(
        NetworkRunner runner,
        bool hasState)
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }

    public override void FixedUpdateNetwork()
    {
        if (HasStateAuthority == false)
        {
            return;
        }

        // ================================
        // BREAK TIME
        // ================================

        if (IsBreakTime)
        {
            if (BreakTimer.Expired(Runner))
            {
                StartNextWave();
            }

            return;
        }

        // ================================
        // SPAWN ZOMBIES
        // ================================

        if (ZombiesLeftToSpawn > 0 &&
            SpawnTimer.ExpiredOrNotRunning(Runner))
        {
            bool spawned = SpawnOneZombie();

            // ลดจำนวนเฉพาะตอน Spawn สำเร็จ
            if (spawned)
            {
                ZombiesLeftToSpawn--;

                SpawnTimer = TickTimer.CreateFromSeconds(
                    Runner,
                    spawnInterval
                );
            }
            else
            {
                // ถ้า Spawn ไม่สำเร็จ
                // ลองใหม่ใน Tick ถัดไป
                SpawnTimer = TickTimer.CreateFromSeconds(
                    Runner,
                    0.25f
                );
            }
        }

        // ================================
        // WAVE COMPLETE
        // ================================

        if (CurrentWave > 0 &&
            ZombiesLeftToSpawn <= 0 &&
            ZombiesRemaining <= 0)
        {
            StartBreakTime();
        }
    }

    // =========================================================
    // ZOMBIE DIED
    // =========================================================

    public void OnZombieDied()
    {
        if (HasStateAuthority)
        {
            ApplyZombieDied();
            return;
        }

        RPC_NotifyZombieDied();
    }

    [Rpc(
        RpcSources.All,
        RpcTargets.StateAuthority,
        Channel = RpcChannel.Reliable
    )]
    private void RPC_NotifyZombieDied()
    {
        if (HasStateAuthority == false)
        {
            return;
        }

        ApplyZombieDied();
    }

    private void ApplyZombieDied()
    {
        ZombiesRemaining = Mathf.Max(
            0,
            ZombiesRemaining - 1
        );

        Debug.Log(
            $"Wave {CurrentWave}: zombie died. " +
            $"Remaining: {ZombiesRemaining}"
        );
    }

    // =========================================================
    // START NEXT WAVE
    // =========================================================

    private void StartNextWave()
    {
        CurrentWave++;

        int zombieCount =
            CurrentWave * zombiesPerWave;

        ZombiesRemaining = zombieCount;
        ZombiesLeftToSpawn = zombieCount;

        IsBreakTime = false;
        SpawnTimer = TickTimer.None;

        Debug.Log(
            $"Wave {CurrentWave} started. " +
            $"Zombies: {zombieCount}"
        );
    }

    // =========================================================
    // START BREAK
    // =========================================================

    private void StartBreakTime()
    {
        IsBreakTime = true;

        BreakTimer = TickTimer.CreateFromSeconds(
            Runner,
            breakDuration
        );

        SpawnTimer = TickTimer.None;

        Debug.Log(
            $"Wave {CurrentWave} cleared. " +
            $"Break time ({breakDuration}s)."
        );
    }

    // =========================================================
    // SPAWN ZOMBIE
    // =========================================================

    private bool SpawnOneZombie()
    {
        // -----------------------------------------
        // 1. Get random spawn point
        // -----------------------------------------

        Transform spawnPoint = GetRandomSpawnPoint();

        if (spawnPoint == null)
        {
            Debug.LogError(
                "[WaveManager] No valid zombie spawn point found."
            );

            return false;
        }

        Vector3 spawnPosition = spawnPoint.position;

        // -----------------------------------------
        // 2. Find nearest NavMesh position
        // -----------------------------------------

        if (!TryGetNavMeshPosition(
            spawnPosition,
            out Vector3 validPosition))
        {
            Debug.LogError(
                $"[WaveManager] Cannot spawn Zombie. " +
                $"Spawn point '{spawnPoint.name}' " +
                $"is not near NavMesh. " +
                $"Position = {spawnPosition}"
            );

            return false;
        }

        // -----------------------------------------
        // 3. Spawn through Fusion
        // -----------------------------------------

        NetworkObject zombie = Runner.Spawn(
            zombiePrefab,
            validPosition,
            Quaternion.identity
        );

        if (zombie == null)
        {
            Debug.LogError(
                "[WaveManager] Runner.Spawn returned null. " +
                "Check that the zombie prefab is registered " +
                "as a NetworkObject."
            );

            return false;
        }

        Debug.Log(
            $"[WaveManager] Zombie spawned. " +
            $"SpawnPoint={spawnPoint.name} | " +
            $"Position={validPosition}"
        );

        return true;
    }

    // =========================================================
    // NAVMESH POSITION
    // =========================================================

    private bool TryGetNavMeshPosition(
        Vector3 sourcePosition,
        out Vector3 navMeshPosition)
    {
        if (NavMesh.SamplePosition(
            sourcePosition,
            out NavMeshHit hit,
            3f,
            NavMesh.AllAreas))
        {
            navMeshPosition = hit.position;
            return true;
        }

        navMeshPosition = sourcePosition;
        return false;
    }

    // =========================================================
    // RANDOM SPAWN POINT
    // =========================================================

    private Transform GetRandomSpawnPoint()
    {
        if (spawnPoints == null ||
            spawnPoints.Length == 0)
        {
            return null;
        }

        int validCount = 0;

        for (int i = 0; i < spawnPoints.Length; i++)
        {
            if (spawnPoints[i] != null)
            {
                validCount++;
            }
        }

        if (validCount == 0)
        {
            return null;
        }

        int targetIndex =
            Random.Range(0, validCount);

        for (int i = 0; i < spawnPoints.Length; i++)
        {
            if (spawnPoints[i] == null)
            {
                continue;
            }

            if (targetIndex == 0)
            {
                return spawnPoints[i];
            }

            targetIndex--;
        }

        return null;
    }
}