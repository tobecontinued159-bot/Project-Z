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
            if (_agent != null)
            {
                _agent.enabled = true;
            }
        }
        else
        {
            if (_agent != null)
            {
                _agent.enabled = false;
            }
        }
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

        if (NetworkPlayerSpawner.AllPlayers == null || NetworkPlayerSpawner.AllPlayers.Count == 0)
        {
            Debug.LogWarning("ZombieAI: Cannot award points, AllPlayers list is empty.");
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
        }
        else
        {
            attackerStats.RPC_AddPoints(killPoints);
        }
    }

    private PlayerStats FindAttackerStats(PlayerRef attackerPlayerRef)
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

        if (_agent == null || _agent.isOnNavMesh == false)
        {
            return;
        }

        if (Runner.SimulationTime < _nextDestinationTime)
        {
        }
        else
        {
            _nextDestinationTime = Runner.SimulationTime + destinationUpdateInterval;

            Transform nearestPlayer = FindNearestPlayer();
            if (nearestPlayer != null)
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

        if (NetworkPlayerSpawner.AllPlayers == null || NetworkPlayerSpawner.AllPlayers.Count == 0)
        {
            return;
        }

        PlayerStats nearestPlayerStats = null;
        float nearestSqrDistance = float.MaxValue;

        for (int i = 0; i < NetworkPlayerSpawner.AllPlayers.Count; i++)
        {
            NetworkObject playerNo = NetworkPlayerSpawner.AllPlayers[i];
            if (playerNo == null)
            {
                continue;
            }

            PlayerStats stats = playerNo.GetComponent<PlayerStats>();
            if (stats == null || stats.IsDead)
            {
                continue;
            }

            float sqrDistance = (playerNo.transform.position - transform.position).sqrMagnitude;
            if (sqrDistance < nearestSqrDistance)
            {
                nearestSqrDistance = sqrDistance;
                nearestPlayerStats = stats;
            }
        }

        if (nearestPlayerStats == null)
        {
            return;
        }

        if (nearestSqrDistance > (attackRange * attackRange))
        {
            return;
        }

        AttackCooldown = TickTimer.CreateFromSeconds(Runner, attackInterval);

        if (nearestPlayerStats.HasStateAuthority)
        {
            nearestPlayerStats.TakeDamageLocal(attackDamage);
        }
        else
        {
            nearestPlayerStats.RPC_RequestTakeDamage(attackDamage);
        }

        Debug.Log($"{name} attacked player! Damage: {attackDamage}");
    }

    private Transform FindNearestPlayer()
    {
        if (NetworkPlayerSpawner.AllPlayers == null || NetworkPlayerSpawner.AllPlayers.Count == 0)
        {
            return null;
        }

        Transform nearest = null;
        float nearestDistance = float.MaxValue;

        for (int i = 0; i < NetworkPlayerSpawner.AllPlayers.Count; i++)
        {
            NetworkObject player = NetworkPlayerSpawner.AllPlayers[i];
            if (player == null)
            {
                continue;
            }

            float sqrDistance = (player.transform.position - transform.position).sqrMagnitude;
            if (sqrDistance < nearestDistance)
            {
                nearestDistance = sqrDistance;
                nearest = player.transform;
            }
        }

        return nearest;
    }
}
