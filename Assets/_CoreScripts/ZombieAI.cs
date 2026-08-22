using Fusion;
using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent))]
public class ZombieAI : NetworkBehaviour
{
    [Networked] public int Health { get; set; }

    [SerializeField] private float destinationUpdateInterval = 0.25f;

    private NavMeshAgent _agent;
    private float _nextDestinationTime;

    public override void Spawned()
    {
        _agent = GetComponent<NavMeshAgent>();
        if (_agent == null)
        {
            _agent = gameObject.AddComponent<NavMeshAgent>();
        }

        if (HasStateAuthority)
        {
            Health = 100;
            _agent.enabled = true;
        }
        else
        {
            _agent.enabled = false;
        }
    }

    public void TakeDamage(int damage)
    {
        if (HasStateAuthority == false)
        {
            return;
        }

        Health -= damage;
        Debug.Log($"{name} took {damage} damage. Health: {Health}");

        if (Health <= 0)
        {
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
        GameObject[] players = GameObject.FindGameObjectsWithTag("Player");
        Transform nearest = null;
        float nearestDistance = float.MaxValue;

        for (int i = 0; i < players.Length; i++)
        {
            GameObject player = players[i];
            if (player == null)
            {
                continue;
            }

            float distance = (player.transform.position - transform.position).sqrMagnitude;
            if (distance < nearestDistance)
            {
                nearestDistance = distance;
                nearest = player.transform;
            }
        }

        return nearest;
    }
}
