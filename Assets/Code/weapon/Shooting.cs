using UnityEngine;

public class Shooting : MonoBehaviour
{
    [SerializeField] private Transform muzzlePoint;
    [SerializeField] private float fireRate = 0.2f;
    [SerializeField] private float weaponRange = 50f;
    [SerializeField] private float damage = 25f;

    private float _nextFireTime;

    private void Update()
    {
        if (Input.GetMouseButton(0) && Time.time >= _nextFireTime)
        {
            _nextFireTime = Time.time + fireRate;
            Shoot();
        }
    }

    private void Shoot()
    {
        if (muzzlePoint == null)
        {
            return;
        }

        RaycastHit hit;
        
        if (Physics.Raycast(muzzlePoint.position, muzzlePoint.forward, out hit, weaponRange))
        {
            if (hit.collider.CompareTag("Zombie"))
            {
                ZombieHealth zombieHealth = hit.collider.GetComponent<ZombieHealth>();
                if (zombieHealth != null)
                {
                    zombieHealth.TakeDamage(damage);
                }
            }
        }
    }
}