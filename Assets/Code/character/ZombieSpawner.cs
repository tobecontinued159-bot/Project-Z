using UnityEngine;

public class ZombieSpawner : MonoBehaviour
{
    [SerializeField] private GameObject zombiePrefab;
    [SerializeField] private float spawnInterval = 3f;
    [SerializeField] private int maxZombiesFromThisSpawner = 5;

    private float _nextSpawnTime;
    private int _spawnedCount = 0;

    private void Update()
    {
        if (Time.time >= _nextSpawnTime && _spawnedCount < maxZombiesFromThisSpawner)
        {
            _nextSpawnTime = Time.time + spawnInterval;
            SpawnZombie();
        }
    }

    private void SpawnZombie()
    {
        if (zombiePrefab == null) return;

        Instantiate(zombiePrefab, transform.position, transform.rotation);
        _spawnedCount++;
    }
}