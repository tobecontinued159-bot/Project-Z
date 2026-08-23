using System.Collections.Generic;
using Fusion;
using UnityEngine;

public class ZombieSpawner : NetworkBehaviour
{
    [SerializeField] private NetworkObject zombiePrefab;
    [SerializeField] private List<Transform> spawnPoints = new List<Transform>();
    [SerializeField] private float spawnInterval = 3f;

    [Networked] private TickTimer SpawnTimer { get; set; }

    public override void Spawned()
    {
        if (HasStateAuthority)
        {
            SpawnTimer = TickTimer.CreateFromSeconds(Runner, spawnInterval);
        }
    }

    public override void FixedUpdateNetwork()
    {
        if (HasStateAuthority == false)
        {
            return;
        }

        if (SpawnTimer.Expired(Runner) == false)
        {
            return;
        }

        SpawnZombie();
        SpawnTimer = TickTimer.CreateFromSeconds(Runner, spawnInterval);
    }

    private void SpawnZombie()
    {
        if (zombiePrefab == null)
        {
            Debug.LogError("ZombieSpawner: zombiePrefab is not assigned.");
            return;
        }

        Transform spawnPoint = GetRandomSpawnPoint();
        Vector3 position = spawnPoint != null ? spawnPoint.position : transform.position;
        Quaternion rotation = spawnPoint != null ? spawnPoint.rotation : transform.rotation;

        NetworkObject zombie = Runner.Spawn(zombiePrefab, position, rotation);
        if (zombie == null)
        {
            Debug.LogError("ZombieSpawner: Runner.Spawn returned null. Bake the zombie prefab as a NetworkObject.");
        }
    }

    private Transform GetRandomSpawnPoint()
    {
        if (spawnPoints == null || spawnPoints.Count == 0)
        {
            return null;
        }

        List<Transform> validPoints = new List<Transform>();
        for (int i = 0; i < spawnPoints.Count; i++)
        {
            if (spawnPoints[i] != null)
            {
                validPoints.Add(spawnPoints[i]);
            }
        }

        if (validPoints.Count == 0)
        {
            return null;
        }

        int index = Random.Range(0, validPoints.Count);
        return validPoints[index];
    }
}
