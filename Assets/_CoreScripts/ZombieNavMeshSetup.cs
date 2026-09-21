using Fusion;
using UnityEngine;
using UnityEngine.AI;

public class ZombieNavMeshSetup : NetworkBehaviour
{
    private NavMeshAgent agent;

    public override void Spawned()
    {
        agent = GetComponent<NavMeshAgent>();

        if (agent == null)
        {
            Debug.LogError(
                $"[ZombieNavMeshSetup] {gameObject.name} " +
                "does not have a NavMeshAgent."
            );

            return;
        }

        // หา NavMesh ที่ใกล้ตำแหน่งที่ Fusion spawn มา
        if (!NavMesh.SamplePosition(
            transform.position,
            out NavMeshHit hit,
            3f,
            NavMesh.AllAreas))
        {
            Debug.LogError(
                $"[ZombieNavMeshSetup] Cannot find NavMesh near " +
                $"{transform.position}"
            );

            return;
        }

        // Agent ยังปิดอยู่
        // จึงสามารถย้าย Transform ไปยัง NavMesh ได้ก่อน
        transform.position = hit.position;

        // เปิด Agent หลังจากอยู่บน NavMesh แล้ว
        agent.enabled = true;

        Debug.Log(
            $"[ZombieNavMeshSetup] NavMeshAgent enabled at " +
            $"{hit.position}"
        );
    }
}