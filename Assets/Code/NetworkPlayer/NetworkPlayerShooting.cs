using Fusion;
using UnityEngine;

public class NetworkPlayerShooting : NetworkBehaviour
{
    [SerializeField] private Transform muzzlePoint;
    [SerializeField] private float fireRate = 0.2f;
    [SerializeField] private float weaponRange = 50f;
    [SerializeField] private float damage = 25f;

    [Networked]
    private TickTimer FireCooldown { get; set; }

    public override void FixedUpdateNetwork()
    {
        if (!Object.HasInputAuthority || muzzlePoint == null)
        {
            return;
        }

        if (!Input.GetMouseButton(0) || !FireCooldown.ExpiredOrNotRunning(Runner))
        {
            return;
        }

        FireCooldown = TickTimer.CreateFromSeconds(Runner, fireRate);
        Shoot();
    }

    private void Shoot()
    {
        if (Physics.Raycast(muzzlePoint.position, muzzlePoint.forward, out RaycastHit hit, weaponRange))
        {
            if (!hit.collider.CompareTag(GameConstants.ZombieTag))
            {
                return;
            }

            if (hit.collider.TryGetComponent(out ZombieHealth zombieHealth))
            {
                zombieHealth.TakeDamage(damage);
            }
        }
    }
}
