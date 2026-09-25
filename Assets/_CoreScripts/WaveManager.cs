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
    [SerializeField] private float minSpawnInterval = 0.25f;

    public static WaveManager Instance { get; private set; }

    private int _nextSpawnPointIndex;

    public float RemainingBreakSeconds
    {
        get
        {
            if (Runner == null || IsBreakTime == false)
            {
                return 0f;
            }

            return BreakTimer.RemainingTime(Runner) ?? 0f;
        }
    }

    public int GetZombieCountForWave(int wave)
    {
        return Mathf.Max(0, wave) * Mathf.Max(1, zombiesPerWave);
    }

    public override void Spawned()
    {
        Instance = this;

        if (CanRunMasterLogic() == false)
        {
            return;
        }

        CurrentWave = 0;
        ZombiesRemaining = 0;
        ZombiesLeftToSpawn = 0;
        StartBreakTime();
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
        if (CanRunMasterLogic() == false)
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
            if (SpawnOneZombie())
            {
                ZombiesLeftToSpawn--;
                ZombiesRemaining++;
                SpawnTimer = TickTimer.CreateFromSeconds(Runner, GetCurrentSpawnInterval());
            }
            else
            {
                SpawnTimer = TickTimer.CreateFromSeconds(Runner, 0.25f);
            }
        }

        if (CurrentWave > 0 && ZombiesLeftToSpawn <= 0 && ZombiesRemaining <= 0)
        {
            StartBreakTime();
        }
    }

    public void OnZombieDied()
    {
        if (Object == null || Object.IsValid == false)
        {
            return;
        }

        if (Object.HasStateAuthority)
        {
            ApplyZombieDied();
            return;
        }

        RPC_NotifyZombieDied();
    }

    [Rpc(RpcSources.All, RpcTargets.StateAuthority, Channel = RpcChannel.Reliable)]
    private void RPC_NotifyZombieDied()
    {
        ApplyZombieDied();
    }

    private void ApplyZombieDied()
    {
        if (CanRunMasterLogic() == false)
        {
            return;
        }

        ZombiesRemaining = Mathf.Max(0, ZombiesRemaining - 1);
        Debug.Log($"Wave {CurrentWave}: zombie died. Remaining: {ZombiesRemaining}");
    }

    private void StartNextWave()
    {
        CurrentWave++;

        int zombieCount = GetZombieCountForWave(CurrentWave);
        ZombiesLeftToSpawn = zombieCount;
        ZombiesRemaining = 0;
        IsBreakTime = false;
        SpawnTimer = TickTimer.None;

        Debug.Log($"Wave {CurrentWave} started. Zombies: {zombieCount}");
    }

    private void StartBreakTime()
    {
        IsBreakTime = true;
        ZombiesLeftToSpawn = 0;
        BreakTimer = TickTimer.CreateFromSeconds(Runner, breakDuration);
        SpawnTimer = TickTimer.None;

        if (CurrentWave <= 0)
        {
            Debug.Log($"WaveManager: first break ({breakDuration}s) before Wave 1.");
            return;
        }

        Debug.Log($"Wave {CurrentWave} cleared. Break time ({breakDuration}s).");
    }

    private bool SpawnOneZombie()
    {
        if (zombiePrefab == null)
        {
            Debug.LogError("WaveManager: zombiePrefab is not assigned.");
            return false;
        }

        Transform spawnPoint = GetNextSpawnPoint();
        if (spawnPoint == null)
        {
            Debug.LogError("WaveManager: no valid zombie spawn point assigned.");
            return false;
        }

        if (TryGetNavMeshPosition(spawnPoint.position, out Vector3 spawnPosition) == false)
        {
            Debug.LogError($"WaveManager: spawn point '{spawnPoint.name}' is not near a NavMesh. Position={spawnPoint.position}");
            return false;
        }

        NetworkObject zombie = Runner.Spawn(zombiePrefab, spawnPosition, spawnPoint.rotation);
        if (zombie == null)
        {
            Debug.LogError("WaveManager: Runner.Spawn returned null. Bake the zombie prefab as a NetworkObject.");
            return false;
        }

        Debug.Log($"WaveManager: spawned zombie at {spawnPoint.name} ({spawnPosition}).");
        return true;
    }

    private float GetCurrentSpawnInterval()
    {
        float fasterPerWave = 0.05f * Mathf.Max(0, CurrentWave - 1);
        return Mathf.Max(minSpawnInterval, spawnInterval - fasterPerWave);
    }

    private bool TryGetNavMeshPosition(Vector3 sourcePosition, out Vector3 navMeshPosition)
    {
        if (NavMesh.SamplePosition(sourcePosition, out NavMeshHit hit, 8f, NavMesh.AllAreas))
        {
            navMeshPosition = hit.position;
            return true;
        }

        navMeshPosition = sourcePosition;
        return false;
    }

    private Transform GetNextSpawnPoint()
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

        for (int attempt = 0; attempt < spawnPoints.Length; attempt++)
        {
            int index = _nextSpawnPointIndex % spawnPoints.Length;
            _nextSpawnPointIndex++;

            Transform spawnPoint = spawnPoints[index];
            if (spawnPoint != null)
            {
                return spawnPoint;
            }
        }

        return null;
    }

    private bool CanRunMasterLogic()
    {
        return Object != null && Object.IsValid && Object.HasStateAuthority;
    }
}
