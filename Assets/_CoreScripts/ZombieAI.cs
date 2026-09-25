using Fusion;
using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent))]
[RequireComponent(typeof(NetworkTransform))]
public class ZombieAI : NetworkBehaviour
{
    [Networked] public int Health { get; set; }
    [Networked] private NetworkBool IsDead { get; set; }
    [Networked] private TickTimer AttackCooldown { get; set; }

    [Header("AI Movement")]
    [SerializeField] private float destinationUpdateInterval = 0.25f;
    [SerializeField] private int startingHealth = 100;
    [SerializeField] private int killPoints = 10;

    [Header("Melee Attack")]
    [SerializeField] private float attackRange = 1.8f;
    [SerializeField] private int attackDamage = 10;
    [SerializeField] private float attackInterval = 1f;

    private NavMeshAgent _agent;
    private float _nextDestinationTime;

    public override void Spawned()
    {
        _agent = GetComponent<NavMeshAgent>();

        if (HasStateAuthority)
        {
            Health = startingHealth;
            IsDead = false;
            AttackCooldown = TickTimer.None;
            EnableAgentOnNavMesh();
        }
        else if (_agent != null)
        {
            _agent.enabled = false;
        }
    }

    private void EnableAgentOnNavMesh()
    {
        if (_agent == null)
        {
            return;
        }

        if (_agent.enabled == false)
        {
            _agent.enabled = true;
        }

        Vector3 snapPosition = transform.position;
        if (NavMesh.SamplePosition(transform.position, out NavMeshHit hit, 8f, NavMesh.AllAreas))
        {
            snapPosition = hit.position;
        }

        if (_agent.enabled)
        {
            _agent.Warp(snapPosition);
        }

        if (_agent.enabled && _agent.isOnNavMesh)
        {
            _agent.updatePosition = true;
            _agent.updateRotation = true;
        }
    }

    private bool IsAgentOnNavMesh()
    {
        return _agent != null && _agent.enabled && _agent.isOnNavMesh;
    }

    [Rpc(RpcSources.All, RpcTargets.StateAuthority, Channel = RpcChannel.Reliable)]
    public void RPC_RequestDamage(int damage, PlayerRef attackerPlayerRef)
    {
        Debug.Log($"[RPC_RequestDamage] Received on {name} | HasStateAuthority={HasStateAuthority} | IsDead={IsDead} | Damage={damage}");

        if (HasStateAuthority == false)
        {
            Debug.LogWarning("[RPC_RequestDamage] Rejected - No StateAuthority");
            return;
        }

        if (IsDead)
        {
            Debug.LogWarning("[RPC_RequestDamage] Ignored - Zombie already dead");
            return;
        }

        Health -= damage;
        Debug.Log($"{name} took {damage} damage from Player{attackerPlayerRef.PlayerId}. Health: {Health}");

        if (Health <= 0)
        {
            Debug.Log("Zombie Died! Points awarded.");

            IsDead = true;

            if (HasStateAuthority)
            {
                AwardKillPoints(attackerPlayerRef);
                NotifyWaveManager();
                Runner.Despawn(Object);
            }
        }
    }

    private void AwardKillPoints(PlayerRef attackerPlayerRef)
    {
        if (attackerPlayerRef.IsRealPlayer == false)
        {
            return;
        }

        PlayerStats attackerStats = FindAttackerStats(attackerPlayerRef);

        if (attackerStats == null)
        {
            Debug.LogWarning($"ZombieAI: Could not find PlayerStats for attacker Player{attackerPlayerRef.PlayerId}");
            return;
        }

        if (attackerStats.HasStateAuthority)
        {
            attackerStats.AddPointsLocal(killPoints);
            attackerStats.RegisterKill();
        }
        else
        {
            attackerStats.RPC_AddPoints(killPoints);
            attackerStats.RPC_RegisterKill();
        }
    }

    private PlayerStats FindAttackerStats(PlayerRef attackerPlayerRef)
    {
        if (NetworkPlayerSpawner.AllPlayers != null)
        {
            for (int i = 0; i < NetworkPlayerSpawner.AllPlayers.Count; i++)
            {
                NetworkObject playerNo = NetworkPlayerSpawner.AllPlayers[i];
                if (playerNo == null)
                {
                    continue;
                }

                if (playerNo.InputAuthority == attackerPlayerRef)
                {
                    return playerNo.GetComponent<PlayerStats>();
                }
            }
        }

        PlayerStats[] allStats = FindObjectsByType<PlayerStats>(FindObjectsSortMode.None);
        for (int i = 0; i < allStats.Length; i++)
        {
            PlayerStats stats = allStats[i];
            if (stats == null || stats.Object == null || stats.Object.IsValid == false)
            {
                continue;
            }

            if (stats.Object.InputAuthority == attackerPlayerRef)
            {
                return stats;
            }
        }

        return null;
    }

    public override void FixedUpdateNetwork()
    {
        if (HasStateAuthority == false)
        {
            return;
        }

        if (IsDead)
        {
            return;
        }

        if (_agent == null)
        {
            _agent = GetComponent<NavMeshAgent>();
        }

        if (_agent == null)
        {
            return;
        }

        if (IsAgentOnNavMesh() == false)
        {
            EnableAgentOnNavMesh();
        }

        if (IsAgentOnNavMesh() == false)
        {
            return;
        }

        if (Runner.SimulationTime >= _nextDestinationTime)
        {
            _nextDestinationTime = Runner.SimulationTime + destinationUpdateInterval;
            Transform nearestPlayer = FindNearestPlayer();
            if (nearestPlayer != null && IsAgentOnNavMesh())
            {
                _agent.SetDestination(nearestPlayer.position);
            }
        }

        TryMeleeAttack();
    }

    private void TryMeleeAttack()
    {
        if (AttackCooldown.ExpiredOrNotRunning(Runner) == false)
        {
            return;
        }

        PlayerStats nearestPlayerStats = FindNearestLivingPlayerStats(out float nearestSqrDistance);
        if (nearestPlayerStats == null)
        {
            return;
        }

        if (nearestSqrDistance > (attackRange * attackRange))
        {
            return;
        }

        AttackCooldown = TickTimer.CreateFromSeconds(Runner, attackInterval);
        nearestPlayerStats.TakeDamage(attackDamage);
        Debug.Log($"{name} attacked {nearestPlayerStats.name} for {attackDamage}");
    }

    private PlayerStats FindNearestLivingPlayerStats(out float nearestSqrDistance)
    {
        PlayerStats nearest = null;
        nearestSqrDistance = float.MaxValue;

        if (NetworkPlayerSpawner.AllPlayers != null)
        {
            for (int i = 0; i < NetworkPlayerSpawner.AllPlayers.Count; i++)
            {
                NetworkObject playerNo = NetworkPlayerSpawner.AllPlayers[i];
                if (playerNo == null)
                {
                    continue;
                }

                ConsiderLivingPlayer(playerNo.GetComponent<PlayerStats>(), ref nearest, ref nearestSqrDistance);
            }
        }

        PlayerStats[] allStats = FindObjectsByType<PlayerStats>(FindObjectsSortMode.None);
        for (int i = 0; i < allStats.Length; i++)
        {
            ConsiderLivingPlayer(allStats[i], ref nearest, ref nearestSqrDistance);
        }

        return nearest;
    }

    private void ConsiderLivingPlayer(PlayerStats stats, ref PlayerStats nearest, ref float nearestSqrDistance)
    {
        if (stats == null || stats.Object == null || stats.Object.IsValid == false || stats.IsDead)
        {
            return;
        }

        float sqrDistance = (stats.transform.position - transform.position).sqrMagnitude;
        if (sqrDistance < nearestSqrDistance)
        {
            nearestSqrDistance = sqrDistance;
            nearest = stats;
        }
    }

    public bool ForceKill()
    {
        if (Object == null || Object.IsValid == false || HasStateAuthority == false || IsDead)
        {
            return false;
        }

        IsDead = true;
        NotifyWaveManager();
        Runner.Despawn(Object);
        return true;
    }

    private void NotifyWaveManager()
    {
        if (WaveManager.Instance != null)
        {
            WaveManager.Instance.OnZombieDied();
            return;
        }

        WaveManager waveManager = FindFirstObjectByType<WaveManager>();
        if (waveManager != null)
        {
            waveManager.OnZombieDied();
        }
    }

    private Transform FindNearestPlayer()
    {
        Transform nearest = null;
        float nearestDistance = float.MaxValue;

        if (NetworkPlayerSpawner.AllPlayers != null)
        {
            for (int i = 0; i < NetworkPlayerSpawner.AllPlayers.Count; i++)
            {
                NetworkObject player = NetworkPlayerSpawner.AllPlayers[i];
                if (player == null)
                {
                    continue;
                }

                PlayerStats stats = player.GetComponent<PlayerStats>();
                if (stats != null && stats.IsDead)
                {
                    continue;
                }

                ConsiderCandidate(player.transform.position, player.transform, ref nearest, ref nearestDistance);
            }
        }

        GameObject[] taggedPlayers = GameObject.FindGameObjectsWithTag("Player");
        for (int i = 0; i < taggedPlayers.Length; i++)
        {
            GameObject taggedPlayer = taggedPlayers[i];
            if (taggedPlayer == null || taggedPlayer.activeInHierarchy == false)
            {
                continue;
            }

            PlayerStats stats = taggedPlayer.GetComponentInParent<PlayerStats>();
            if (stats != null && stats.IsDead)
            {
                continue;
            }

            Transform candidate = stats != null ? stats.transform : taggedPlayer.transform;
            ConsiderCandidate(candidate.position, candidate, ref nearest, ref nearestDistance);
        }

        return nearest;
    }

    private void ConsiderCandidate(Vector3 position, Transform candidate, ref Transform nearest, ref float nearestDistance)
    {
        float sqrDistance = (position - transform.position).sqrMagnitude;
        if (sqrDistance < nearestDistance)
        {
            nearestDistance = sqrDistance;
            nearest = candidate;
        }
    }
}
