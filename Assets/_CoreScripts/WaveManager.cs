using Fusion;
using UnityEngine;

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
        BreakTimer = TickTimer.CreateFromSeconds(Runner, breakDuration);
        SpawnTimer = TickTimer.None;
        Debug.Log($"WaveManager: Break time started ({breakDuration}s).");
    }

    public override void Despawned(NetworkRunner runner, bool hasState)
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

        if (IsBreakTime)
        {
            if (BreakTimer.Expired(Runner))
            {
                StartNextWave();
            }
            return;
        }

        if (ZombiesLeftToSpawn > 0 && SpawnTimer.ExpiredOrNotRunning(Runner))
        {
            SpawnOneZombie();
            ZombiesLeftToSpawn--;
            SpawnTimer = TickTimer.CreateFromSeconds(Runner, spawnInterval);
        }

        if (CurrentWave > 0 && ZombiesLeftToSpawn <= 0 && ZombiesRemaining <= 0)
        {
            StartBreakTime();
        }
    }

    public void OnZombieDied()
    {
        if (HasStateAuthority)
        {
            ApplyZombieDied();
            return;
        }

        RPC_NotifyZombieDied();
    }

    [Rpc(RpcSources.All, RpcTargets.StateAuthority, Channel = RpcChannel.Reliable)]
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
        ZombiesRemaining = Mathf.Max(0, ZombiesRemaining - 1);
        Debug.Log($"Wave {CurrentWave}: zombie died. Remaining: {ZombiesRemaining}");
    }

    private void StartNextWave()
    {
        CurrentWave++;
        int zombieCount = CurrentWave * zombiesPerWave;
        ZombiesRemaining = zombieCount;
        ZombiesLeftToSpawn = zombieCount;
        IsBreakTime = false;
        SpawnTimer = TickTimer.None;
        Debug.Log($"Wave {CurrentWave} started. Zombies: {zombieCount}");
    }

    private void StartBreakTime()
    {
        IsBreakTime = true;
        BreakTimer = TickTimer.CreateFromSeconds(Runner, breakDuration);
        SpawnTimer = TickTimer.None;
        Debug.Log($"Wave {CurrentWave} cleared. Break time ({breakDuration}s).");
    }

    private void SpawnOneZombie()
    {
        if (zombiePrefab == null)
        {
            Debug.LogError("WaveManager: zombiePrefab is not assigned.");
            return;
        }

        Transform spawnPoint = GetRandomSpawnPoint();
        Vector3 position = spawnPoint != null ? spawnPoint.position : transform.position;
        Quaternion rotation = spawnPoint != null ? spawnPoint.rotation : Quaternion.identity;

        NetworkObject zombie = Runner.Spawn(zombiePrefab, position, rotation);
        if (zombie == null)
        {
            Debug.LogError("WaveManager: Runner.Spawn returned null. Bake the zombie prefab as a NetworkObject.");
        }
    }

    private Transform GetRandomSpawnPoint()
    {
        if (spawnPoints == null || spawnPoints.Length == 0)
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

        int targetIndex = Random.Range(0, validCount);
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
