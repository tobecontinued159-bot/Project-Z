using Fusion;
using UnityEngine;

public class PlayerWeapon : NetworkBehaviour
{
    [SerializeField] private float weaponRange = 50f;
    [SerializeField] private float muzzleHeight = 0.5f;

    private void Update()
    {
        if (HasStateAuthority == false)
        {
            return;
        }

        if (Input.GetButtonDown("Fire1"))
        {
            RPC_Fire();
        }
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    private void RPC_Fire()
    {
        Vector3 muzzlePosition = transform.position + Vector3.up * muzzleHeight;
        Vector3 fireDirection = transform.forward;

        Debug.DrawRay(muzzlePosition, fireDirection * weaponRange, Color.red, 1f);

        if (Physics.Raycast(muzzlePosition, fireDirection, out RaycastHit hit, weaponRange))
        {
            Debug.Log($"Hit: {hit.collider.gameObject.name}");

            ZombieAI zombie = hit.collider.GetComponentInParent<ZombieAI>();
            if (zombie != null)
            {
                zombie.TakeDamage(25);
            }
        }
    }
}
