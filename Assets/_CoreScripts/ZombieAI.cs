using Fusion;
using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent))]
[RequireComponent(typeof(NetworkTransform))]
public class ZombieAI : NetworkBehaviour
{
    [Networked] public int Health { get; set; }

    [SerializeField] private float destinationUpdateInterval = 0.25f;
    [SerializeField] private int startingHealth = 100;

    private NavMeshAgent _agent;
    private float _nextDestinationTime;

    public override void Spawned()
    {
        _agent = GetComponent<NavMeshAgent>();

        if (HasStateAuthority)
        {
            Health = startingHealth;
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

    [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
    public void RPC_RequestDamage(int damage, PlayerStats attacker)
    {
        if (HasStateAuthority == false)
        {
            return;
        }

        ApplyDamage(damage, attacker);
    }

    private void ApplyDamage(int damage, PlayerStats attacker)
    {
        Health -= damage;
        Debug.Log($"{name} took {damage} damage. Health: {Health}");

        if (Health <= 0)
        {
            if (attacker != null && attacker.HasStateAuthority)
            {
                attacker.AddPointsLocal(10);
            }
            else if (attacker != null)
            {
                attacker.RPC_AddPoints(10);
            }

            Runner.Despawn(Object);
        }
    }

    public override void FixedUpdateNetwork()
    {
        if (HasStateAuthority == false)
        {
            return;
        }

        if (_agent == null || _agent.isOnNavMesh == false)
        {
            return;
        }

        if (Runner.SimulationTime < _nextDestinationTime)
        {
            return;
        }

        _nextDestinationTime = Runner.SimulationTime + destinationUpdateInterval;

        Transform nearestPlayer = FindNearestPlayer();
        if (nearestPlayer == null)
        {
            return;
        }

        _agent.SetDestination(nearestPlayer.position);
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
