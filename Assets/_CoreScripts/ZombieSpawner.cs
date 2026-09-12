using System.Collections.Generic;
using Fusion;
using UnityEngine;

public class ZombieSpawner : MonoBehaviour
{
    [SerializeField] private NetworkObject zombiePrefab;
    [SerializeField] private List<Transform> spawnPoints = new List<Transform>();
    [SerializeField] private float spawnInterval = 3f;
    [SerializeField] private int maxZombies = 30;

    private NetworkRunner _runner;
    private float _nextSpawnCheckTime;
    private readonly List<Transform> _cachedValidSpawnPoints = new List<Transform>();

    private void Update()
    {
        if (_runner == null)
        {
            if (NetworkPlayerSpawner.Instance != null)
            {
                _runner = NetworkPlayerSpawner.Instance.GetComponent<NetworkRunner>();
            }
            return;
        }

        if (_runner.IsRunning == false)
        {
            return;
        }

        if (IsSpawnMaster() == false)
        {
            return;
        }

        if (Time.time < _nextSpawnCheckTime)
        {
            return;
        }

        _nextSpawnCheckTime = Time.time + spawnInterval;
        TrySpawnZombie();
    }

    private bool IsSpawnMaster()
    {
        if (_runner.ActivePlayers == null)
        {
            return false;
        }

        int playerCount = 0;
        PlayerRef minPlayer = PlayerRef.None;

        foreach (PlayerRef player in _runner.ActivePlayers)
        {
            playerCount++;
            if (minPlayer.IsRealPlayer == false || player.PlayerId < minPlayer.PlayerId)
            {
                minPlayer = player;
            }
        }

        if (playerCount == 0)
        {
            return false;
        }

        return minPlayer == _runner.LocalPlayer;
    }

    private void TrySpawnZombie()
    {
        ZombieAI[] allZombies = Object.FindObjectsOfType<ZombieAI>();
        int currentZombieCount = allZombies != null ? allZombies.Length : 0;

        if (currentZombieCount >= maxZombies)
        {
            return;
        }

        SpawnZombie();
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

        NetworkObject zombie = _runner.Spawn(zombiePrefab, position, rotation);
        if (zombie == null)
        {
            Debug.LogError("ZombieSpawner: Runner.Spawn returned null. Bake the zombie prefab as a NetworkObject.");
        }
    }

    private Transform GetRandomSpawnPoint()
    {
        _cachedValidSpawnPoints.Clear();
        for (int i = 0; i < spawnPoints.Count; i++)
        {
            if (spawnPoints[i] != null)
            {
                _cachedValidSpawnPoints.Add(spawnPoints[i]);
            }
        }

        if (_cachedValidSpawnPoints.Count == 0)
        {
            return null;
        }

        int index = Random.Range(0, _cachedValidSpawnPoints.Count);
        return _cachedValidSpawnPoints[index];
    }
}
