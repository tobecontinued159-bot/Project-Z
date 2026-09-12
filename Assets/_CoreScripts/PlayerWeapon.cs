using Fusion;
using UnityEngine;

[RequireComponent(typeof(LineRenderer))]
public class PlayerWeapon : NetworkBehaviour
{
    [Header("Weapon Settings")]
    [SerializeField] private Transform firePoint;
    [SerializeField] private float weaponRange = 50f;
    [SerializeField] private float chestHeight = 1f;
    [SerializeField] private float shotRadius = 0.5f;
    [SerializeField] private int damage = 25;
    [SerializeField] private float fireRate = 0.2f;

    [Header("Upgrade Settings")]
    [SerializeField] private int upgradeDamageBonus = 15;
    [SerializeField] private float upgradeFireRateMultiplier = 0.7f;
    [SerializeField] private float minFireRate = 0.08f;

    [Networked] public int Damage { get; set; }
    [Networked] public float FireRate { get; set; }

    [Header("Laser Sight")]
    [SerializeField] private float laserWidth = 0.03f;
    [SerializeField] private Color laserColor = new Color(1f, 0f, 0f, 0.7f);
    [SerializeField] private Gradient laserGradient;
    [SerializeField] private Material laserMaterial;

    [Networked] private TickTimer FireCooldown { get; set; }

    private LineRenderer _laserLine;
    private PlayerStats _cachedStats;

    public override void Spawned()
    {
        EnsureFirePoint();
        SetupLaserSight();

        if (HasStateAuthority)
        {
            Damage = damage;
            FireRate = fireRate;
        }
    }

    public void UpgradeWeapon()
    {
        if (HasStateAuthority == false)
        {
            RPC_RequestUpgrade();
            return;
        }

        ApplyUpgrade();
    }

    [Rpc(RpcSources.All, RpcTargets.StateAuthority, Channel = RpcChannel.Reliable)]
    private void RPC_RequestUpgrade()
    {
        if (HasStateAuthority == false)
        {
            return;
        }

        ApplyUpgrade();
    }

    private void ApplyUpgrade()
    {
        Damage += upgradeDamageBonus;
        FireRate = Mathf.Max(minFireRate, FireRate * upgradeFireRateMultiplier);
        Debug.Log($"{name} upgraded weapon. Damage: {Damage}, FireRate: {FireRate:0.00}");
    }

    private void LateUpdate()
    {
        if (EnsureStats() && _cachedStats.IsDead)
        {
            if (_laserLine != null)
            {
                _laserLine.enabled = false;
            }
            return;
        }

        Fire(applyDamage: false);
    }

    public override void FixedUpdateNetwork()
    {
        if (EnsureStats() && _cachedStats.IsDead)
        {
            return;
        }

        if (GetInput(out PlayerInput input) == false)
        {
            return;
        }

        if (input.FirePressed == false)
        {
            return;
        }

        if (FireCooldown.ExpiredOrNotRunning(Runner) == false)
        {
            return;
        }

        Fire(applyDamage: true);
        float currentFireRate = FireRate > 0f ? FireRate : fireRate;
        FireCooldown = TickTimer.CreateFromSeconds(Runner, currentFireRate);
    }

    private bool EnsureStats()
    {
        if (_cachedStats == null)
        {
            _cachedStats = GetComponent<PlayerStats>();
        }

        return _cachedStats != null;
    }

    private void SetupLaserSight()
    {
        _laserLine = GetComponent<LineRenderer>();
        if (_laserLine == null)
        {
            _laserLine = gameObject.AddComponent<LineRenderer>();
        }

        _laserLine.positionCount = 2;
        _laserLine.startWidth = laserWidth;
        _laserLine.endWidth = laserWidth;

        if (laserGradient != null)
        {
            _laserLine.colorGradient = laserGradient;
        }
        else
        {
            _laserLine.startColor = laserColor;
            _laserLine.endColor = laserColor;
        }

        if (laserMaterial != null)
        {
            _laserLine.material = laserMaterial;
        }
        else
        {
            _laserLine.material = new Material(Shader.Find("Sprites/Default"));
        }

        _laserLine.numCapVertices = 2;
        _laserLine.useWorldSpace = true; // บังคับ Use World Space ในโค้ด
    }

    private void EnsureFirePoint()
    {
        if (firePoint != null)
        {
            return;
        }

        Transform existing = transform.Find("FirePoint");
        if (existing != null)
        {
            firePoint = existing;
            return;
        }

        GameObject firePointGo = new GameObject("FirePoint");
        firePointGo.transform.SetParent(transform, false);
        firePointGo.transform.localPosition = new Vector3(0f, chestHeight, 0.6f);
        firePoint = firePointGo.transform;
    }

    private void Fire(bool applyDamage)
    {
        EnsureFirePoint();
        if (firePoint == null)
        {
            return;
        }

        // ใช้ตำแหน่ง firePoint ตรงๆ เลย ไม่ต้องไปบังคับแก้แกน Y แบบโค้ดเก่า
        Vector3 fireOrigin = firePoint.position;

        Vector3 targetPoint = fireOrigin + FlattenHorizontal(transform.forward);
        Camera gameplayCamera = Camera.main;
        
        if (HasInputAuthority && gameplayCamera != null)
        {
            Ray ray = gameplayCamera.ScreenPointToRay(Input.mousePosition);
            
            // สร้าง Plane จำลองให้อยู่ระดับเดียวกับ firePoint แบบเป๊ะๆ
            Plane aimPlane = new Plane(Vector3.up, fireOrigin);
            
            if (aimPlane.Raycast(ray, out float enter))
            {
                targetPoint = ray.GetPoint(enter);
            }
        }
        else if (GetInput(out PlayerInput input))
        {
            targetPoint = input.LookDirection;
        }

        // บังคับให้เป้าหมายมีความสูงเท่ากับจุดปล่อยกระสุน
        targetPoint.y = fireOrigin.y;

        Vector3 direction = targetPoint - fireOrigin;
        direction.y = 0f; // ล็อกไม่ให้กระสุนเฉียงขึ้น/ลง
        
        if (direction.sqrMagnitude < 0.001f)
        {
            direction = FlattenHorizontal(transform.forward);
        }
        direction.Normalize();

        Vector3 endPosition = fireOrigin + (direction * weaponRange);

        if (Physics.SphereCast(fireOrigin, shotRadius, direction, out RaycastHit hit, weaponRange, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore))
        {
            endPosition = fireOrigin + (direction * hit.distance);
            endPosition.y = fireOrigin.y; // ล็อกจุดตกกระทบให้ขนานพื้น

            if (applyDamage)
            {
                ZombieAI zombie = hit.collider.GetComponentInParent<ZombieAI>();
                if (zombie != null)
                {
                    PlayerRef shooterPlayerRef = Object.InputAuthority;
                    int currentDamage = Damage > 0 ? Damage : damage;
                    zombie.RPC_RequestDamage(currentDamage, shooterPlayerRef);
                }
            }
        }

        if (_laserLine != null)
        {
            _laserLine.useWorldSpace = true; // แถมให้อีกรอบกันเหนียว
            _laserLine.SetPosition(0, fireOrigin);
            _laserLine.SetPosition(1, endPosition);
            _laserLine.enabled = true;
        }

        if (applyDamage)
        {
            RPC_RenderShotEffect(fireOrigin, direction);
        }
    }

    private static Vector3 FlattenHorizontal(Vector3 value)
    {
        value.y = 0f;
        if (value.sqrMagnitude < 0.001f)
        {
            return Vector3.forward;
        }

        return value.normalized;
    }

    [Rpc(RpcSources.InputAuthority, RpcTargets.All, Channel = RpcChannel.Unreliable)]
    private void RPC_RenderShotEffect(Vector3 muzzlePosition, Vector3 fireDirection)
    {
        Debug.DrawRay(muzzlePosition, fireDirection * weaponRange, Color.yellow, 0.1f);
    }
}