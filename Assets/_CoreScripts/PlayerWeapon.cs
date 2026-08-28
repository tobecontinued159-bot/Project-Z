using Fusion;
using UnityEngine;

public class PlayerWeapon : NetworkBehaviour
{
    [SerializeField] private float weaponRange = 50f;
    [SerializeField] private float muzzleHeight = 0.5f;
    [SerializeField] private int damage = 25;

    private PlayerStats _cachedPlayerStats;

    public override void Spawned()
    {
        _cachedPlayerStats = GetComponent<PlayerStats>();
    }

    public override void FixedUpdateNetwork()
    {
        if (GetInput(out PlayerInput input) == false)
        {
            return;
        }

        if (input.FirePressed)
        {
            ProcessFire();
        }
    }

    private void ProcessFire()
    {
        Vector3 muzzlePosition = transform.position + Vector3.up * muzzleHeight;
        Vector3 fireDirection = transform.forward;

        RPC_RenderShotEffect(muzzlePosition, fireDirection);

        if (Physics.Raycast(muzzlePosition, fireDirection, out RaycastHit hit, weaponRange))
        {
            ZombieAI zombie = hit.collider.GetComponentInParent<ZombieAI>();
            if (zombie != null)
            {
                zombie.RPC_RequestDamage(damage, _cachedPlayerStats);
            }
        }
    }

    [Rpc(RpcSources.InputAuthority, RpcTargets.All)]
    private void RPC_RenderShotEffect(Vector3 muzzlePosition, Vector3 fireDirection)
    {
        Debug.DrawRay(muzzlePosition, fireDirection * weaponRange, Color.red, 0.2f);
    }
}
