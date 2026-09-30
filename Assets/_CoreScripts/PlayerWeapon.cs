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
    [SerializeField] private int damage = 15;
    [SerializeField] private float fireRate = 0.2f;

    [Header("Upgrade Settings")]
    [SerializeField] private int upgradeDamageBonus = 15;
    [SerializeField] private float upgradeFireRateMultiplier = 0.7f;
    [SerializeField] private float minFireRate = 0.08f;

    [Header("Ammo & Reload Settings")]
    [SerializeField] private int maxAmmo = 15;
    [SerializeField] private int maxReserveAmmo = 120;
    [SerializeField] private float reloadTime = 2f;

    [Header("Infinite Ammo Settings")]
    [SerializeField] private bool hasInfiniteReserve = false;

    // 🟢 หมวดหมู่ตั้งค่าปืนลูกซอง (Shotgun Settings)
    [Header("Shotgun Settings")]
    [SerializeField] private bool defaultIsShotgun = false; // ติ๊กถูกถ้าปืนนี้คือลูกซอง
    [SerializeField] private int pelletsCount = 8; // จำนวนเม็ดลูกปืนต่อการยิง 1 ครั้ง
    [SerializeField] private float spreadAngle = 10f; // องศาความบานของกระสุน
    [SerializeField] private float optimalDistance = 4f; // ระยะที่ทำดาเมจเต็ม 100%
    [SerializeField] private float maxDistance = 15f; // ระยะยิงสูงสุดของลูกซอง
    [SerializeField] private float minDamageMultiplier = 0.3f; // ตัวคูณดาเมจถ้าซอมบี้อยู่ไกล (เช่น 0.3 = 30%)

    [Networked] public int Damage { get; set; }
    [Networked] public float FireRate { get; set; }
    [Networked] public int CurrentAmmo { get; set; }
    [Networked] public int ReserveAmmo { get; set; }
    [Networked] public NetworkBool HasInfiniteReserve { get; set; }
    [Networked] public NetworkBool IsShotgun { get; set; } // 🟢 ตรวจสอบว่าเป็นลูกซองหรือไม่ผ่านระบบ Network
    [Networked] public NetworkBool IsReloading { get; set; }
    [Networked] private TickTimer ReloadTimer { get; set; }
    [Networked] private NetworkBool DamageBuffActive { get; set; }
    [Networked] private TickTimer DamageBuffTimer { get; set; }
    [Networked] private int UnbuffedDamage { get; set; }

    [Networked] public int DefinitionWeaponId { get; set; }
    [Networked] public int DefinitionDamage { get; set; }
    [Networked] public float DefinitionFireRate { get; set; }
    [Networked] public int DefinitionMaxAmmo { get; set; }
    [Networked] public int DefinitionMaxReserveAmmo { get; set; }

    public int MaxAmmo => maxAmmo;
    public int MaxReserveAmmo => maxReserveAmmo;

    public bool IsAmmoFull()
    {
        return CurrentAmmo >= maxAmmo && ReserveAmmo >= maxReserveAmmo;
    }

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
            CurrentAmmo = maxAmmo;

            HasInfiniteReserve = hasInfiniteReserve;
            IsShotgun = defaultIsShotgun; // 🟢 โหลดค่าว่าเป็นลูกซองหรือไม่ตอนเกิด
            ReserveAmmo = HasInfiniteReserve ? 9999 : maxReserveAmmo;

            IsReloading = false;
            ReloadTimer = TickTimer.None;
            DamageBuffActive = false;
            DamageBuffTimer = TickTimer.None;
            UnbuffedDamage = damage;

            DefinitionWeaponId = 0;
            DefinitionDamage = damage;
            DefinitionFireRate = fireRate;
            DefinitionMaxAmmo = maxAmmo;
            DefinitionMaxReserveAmmo = maxReserveAmmo;
        }
    }

    public bool HasWeapon(int weaponId)
    {
        return DefinitionWeaponId == weaponId;
    }

    // 🟢 อัปเดตพารามิเตอร์ให้รองรับปืนลูกซอง (เพิ่ม isShotgunWeapon ด้านหลังสุด)
    public void EquipWeapon(int weaponId, int newDamage, float newFireRate, int newMaxAmmo, int newMaxReserveAmmo, bool infiniteReserve = false, bool isShotgunWeapon = false)
    {
        if (HasStateAuthority)
        {
            ApplyEquipWeapon(weaponId, newDamage, newFireRate, newMaxAmmo, newMaxReserveAmmo, infiniteReserve, isShotgunWeapon);
        }
        else
        {
            RPC_RequestEquipWeapon(weaponId, newDamage, newFireRate, newMaxAmmo, newMaxReserveAmmo, infiniteReserve, isShotgunWeapon);
        }
    }

    [Rpc(RpcSources.All, RpcTargets.StateAuthority, Channel = RpcChannel.Reliable)]
    private void RPC_RequestEquipWeapon(int weaponId, int newDamage, float newFireRate, int newMaxAmmo, int newMaxReserveAmmo, bool infiniteReserve, bool isShotgunWeapon)
    {
        ApplyEquipWeapon(weaponId, newDamage, newFireRate, newMaxAmmo, newMaxReserveAmmo, infiniteReserve, isShotgunWeapon);
    }

    private void ApplyEquipWeapon(int weaponId, int newDamage, float newFireRate, int newMaxAmmo, int newMaxReserveAmmo, bool infiniteReserve, bool isShotgunWeapon)
    {
        DefinitionWeaponId = weaponId;
        Damage = newDamage;
        FireRate = newFireRate;
        maxAmmo = newMaxAmmo;
        maxReserveAmmo = newMaxReserveAmmo;

        hasInfiniteReserve = infiniteReserve;
        HasInfiniteReserve = infiniteReserve;
        IsShotgun = isShotgunWeapon; // 🟢 อัปเดตสถานะลูกซองเมื่อเปลี่ยนปืน

        RefillAmmo();
    }

    public void RefillAmmo()
    {
        if (HasStateAuthority)
        {
            CurrentAmmo = maxAmmo;
            ReserveAmmo = HasInfiniteReserve ? 9999 : maxReserveAmmo;
            IsReloading = false;
        }
        else
        {
            RPC_RequestRefillAmmo();
        }
    }

    [Rpc(RpcSources.All, RpcTargets.StateAuthority, Channel = RpcChannel.Reliable)]
    private void RPC_RequestRefillAmmo()
    {
        CurrentAmmo = maxAmmo;
        ReserveAmmo = HasInfiniteReserve ? 9999 : maxReserveAmmo;
        IsReloading = false;
    }

    public void BuyReserveAmmo()
    {
        if (HasStateAuthority)
        {
            ReserveAmmo = maxReserveAmmo;
        }
        else
        {
            RPC_RequestBuyReserveAmmo();
        }
    }

    [Rpc(RpcSources.All, RpcTargets.StateAuthority, Channel = RpcChannel.Reliable)]
    private void RPC_RequestBuyReserveAmmo()
    {
        ReserveAmmo = maxReserveAmmo;
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
        if (HasStateAuthority == false) return;
        ApplyUpgrade();
    }

    private void ApplyUpgrade()
    {
        Damage += upgradeDamageBonus;
        FireRate = Mathf.Max(minFireRate, FireRate * upgradeFireRateMultiplier);
    }

    public void StartReload()
    {
        if (HasStateAuthority == false)
        {
            RPC_RequestStartReload();
            return;
        }

        bool canReload = HasInfiniteReserve ? (CurrentAmmo < maxAmmo) : (CurrentAmmo < maxAmmo && ReserveAmmo > 0);
        if (IsReloading || !canReload) return;

        IsReloading = true;
        ReloadTimer = TickTimer.CreateFromSeconds(Runner, reloadTime);
    }

    [Rpc(RpcSources.All, RpcTargets.StateAuthority, Channel = RpcChannel.Reliable)]
    private void RPC_RequestStartReload()
    {
        if (HasStateAuthority == false) return;

        bool canReload = HasInfiniteReserve ? (CurrentAmmo < maxAmmo) : (CurrentAmmo < maxAmmo && ReserveAmmo > 0);
        if (IsReloading || !canReload) return;

        IsReloading = true;
        ReloadTimer = TickTimer.CreateFromSeconds(Runner, reloadTime);
    }

    private void CompleteReload()
    {
        int neededAmmo = maxAmmo - CurrentAmmo;
        if (HasInfiniteReserve)
        {
            CurrentAmmo = maxAmmo;
        }
        else
        {
            int ammoToDeduct = Mathf.Min(neededAmmo, ReserveAmmo);
            CurrentAmmo += ammoToDeduct;
            ReserveAmmo -= ammoToDeduct;
        }

        IsReloading = false;
        ReloadTimer = TickTimer.None;
    }

    public void ApplyDamageBuff(float duration, int multiplier)
    {
        if (HasStateAuthority == false)
        {
            RPC_RequestDamageBuff(duration, multiplier);
            return;
        }
        ApplyDamageBuffLocal(duration, multiplier);
    }

    [Rpc(RpcSources.All, RpcTargets.StateAuthority, Channel = RpcChannel.Reliable)]
    private void RPC_RequestDamageBuff(float duration, int multiplier)
    {
        if (HasStateAuthority == false) return;
        ApplyDamageBuffLocal(duration, multiplier);
    }

    private void ApplyDamageBuffLocal(float duration, int multiplier)
    {
        int safeMultiplier = Mathf.Max(1, multiplier);
        if (DamageBuffActive == false)
        {
            UnbuffedDamage = Damage > 0 ? Damage : damage;
            Damage = UnbuffedDamage * safeMultiplier;
            DamageBuffActive = true;
        }
        DamageBuffTimer = TickTimer.CreateFromSeconds(Runner, duration);
    }

    private void TickDamageBuff()
    {
        if (HasStateAuthority == false || DamageBuffActive == false) return;
        if (DamageBuffTimer.Expired(Runner) == false) return;

        Damage = UnbuffedDamage > 0 ? UnbuffedDamage : damage;
        DamageBuffActive = false;
        DamageBuffTimer = TickTimer.None;
    }

    private void LateUpdate()
    {
        if (Object == null || Object.IsValid == false) return;

        if (EnsureStats() && _cachedStats.IsDead)
        {
            if (_laserLine != null) _laserLine.enabled = false;
            return;
        }

        if (HasInputAuthority && PlayerInputLock.IsTerminalOpen) return;

        if (HasInputAuthority)
        {
            // 🟢 ปิดการใช้งานเส้นเลเซอร์ยาวสีขาว ถ้าปืนนี้คือปืนลูกซอง
            if (IsShotgun)
            {
                if (_laserLine != null) _laserLine.enabled = false;
            }
            else
            {
                Fire(applyDamage: false); // ลากเส้นเลเซอร์ให้ปืนปกติ
            }
        }
    }

    public override void FixedUpdateNetwork()
    {
        if (EnsureStats() && _cachedStats.IsDead) return;

        TickDamageBuff();

        if (HasStateAuthority && IsReloading)
        {
            if (ReloadTimer.Expired(Runner)) CompleteReload();
        }

        if (GetInput(out PlayerInput input) == false) return;

        if (input.ReloadPressed || (input.FirePressed && CurrentAmmo <= 0))
        {
            if (IsReloading == false && CurrentAmmo < maxAmmo && (HasInfiniteReserve || ReserveAmmo > 0))
            {
                StartReload();
            }
        }

        if (PlayerInputLock.IsTerminalOpen) return;
        if (IsReloading || CurrentAmmo <= 0) return;
        if (input.FirePressed == false) return;
        if (FireCooldown.ExpiredOrNotRunning(Runner) == false) return;

        if (HasStateAuthority)
        {
            Fire(applyDamage: true);
            CurrentAmmo = Mathf.Max(0, CurrentAmmo - 1);

            if (CurrentAmmo <= 0 && (HasInfiniteReserve || ReserveAmmo > 0))
            {
                StartReload();
            }

            float currentFireRate = FireRate > 0f ? FireRate : fireRate;
            FireCooldown = TickTimer.CreateFromSeconds(Runner, currentFireRate);
        }
    }

    private bool EnsureStats()
    {
        if (_cachedStats == null) _cachedStats = GetComponent<PlayerStats>();
        return _cachedStats != null && _cachedStats.Object != null && _cachedStats.Object.IsValid;
    }

    private void SetupLaserSight()
    {
        _laserLine = GetComponent<LineRenderer>();
        if (_laserLine == null) _laserLine = gameObject.AddComponent<LineRenderer>();

        _laserLine.positionCount = 2;
        _laserLine.startWidth = laserWidth;
        _laserLine.endWidth = laserWidth;

        if (laserGradient != null) _laserLine.colorGradient = laserGradient;
        else
        {
            _laserLine.startColor = laserColor;
            _laserLine.endColor = laserColor;
        }

        if (laserMaterial != null) _laserLine.material = laserMaterial;
        else _laserLine.material = new Material(Shader.Find("Sprites/Default"));

        _laserLine.numCapVertices = 2;
        _laserLine.useWorldSpace = true;
    }

    private void EnsureFirePoint()
    {
        if (firePoint != null) return;
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

    // 🟢 ฟังก์ชันช่วยสุ่มทิศทางให้กระสุนลูกซองบานออก
    private Vector3 GetSpreadDirection(Vector3 forward, float angle)
    {
        Quaternion randomRot = Quaternion.Euler(Random.Range(-angle, angle), Random.Range(-angle, angle), 0f);
        return Quaternion.LookRotation(forward) * randomRot * Vector3.forward;
    }

    private void Fire(bool applyDamage)
    {
        EnsureFirePoint();
        if (firePoint == null) return;

        Vector3 fireOrigin = firePoint.position;
        Vector3 targetPoint = fireOrigin + FlattenHorizontal(transform.forward);
        Camera gameplayCamera = Camera.main;

        if (HasInputAuthority && gameplayCamera != null)
        {
            Ray ray = gameplayCamera.ScreenPointToRay(Input.mousePosition);
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

        targetPoint.y = fireOrigin.y;
        Vector3 direction = targetPoint - fireOrigin;
        direction.y = 0f;

        if (direction.sqrMagnitude < 0.001f) direction = FlattenHorizontal(transform.forward);
        direction.Normalize();

        if (IsShotgun)
        {
            // ==========================================
            // 🟢 ระบบยิงแบบปืนลูกซอง (Shotgun)
            // ==========================================
            for (int i = 0; i < pelletsCount; i++)
            {
                Vector3 spreadDir = GetSpreadDirection(direction, spreadAngle);
                Vector3 endPos = fireOrigin + (spreadDir * maxDistance);
                float hitDistance = maxDistance;

                // ใช้ Raycast ยิงออกไปตามจำนวนกระสุน (Pellets)
                if (Physics.Raycast(fireOrigin, spreadDir, out RaycastHit hit, maxDistance, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore))
                {
                    endPos = fireOrigin + (spreadDir * hit.distance);
                    hitDistance = hit.distance;

                    if (applyDamage && HasStateAuthority)
                    {
                        ZombieAI zombie = hit.collider.GetComponentInParent<ZombieAI>();
                        if (zombie != null && zombie.Object != null && zombie.Object.IsValid)
                        {
                            // 🟢 คำนวณความแรงตามระยะ (ยิ่งไกลยิ่งเบา)
                            float falloffMultiplier = 1f;
                            if (hitDistance > optimalDistance)
                            {
                                float t = Mathf.Clamp01((hitDistance - optimalDistance) / (maxDistance - optimalDistance));
                                falloffMultiplier = Mathf.Lerp(1f, minDamageMultiplier, t);
                            }

                            int currentBaseDamage = Damage > 0 ? Damage : damage;
                            int finalDamage = Mathf.RoundToInt(currentBaseDamage * falloffMultiplier);

                            PlayerRef shooterPlayerRef = Object.InputAuthority;
                            zombie.RPC_RequestDamage(finalDamage, shooterPlayerRef);
                        }
                    }
                }

                if (applyDamage)
                {
                    RPC_RenderShotEffect(fireOrigin, spreadDir, hitDistance);
                }
            }
        }
        else
        {
            // ==========================================
            // 🟢 ระบบยิงแบบปืนปกติ (Assault Rifle/Pistol)
            // ==========================================
            Vector3 endPosition = fireOrigin + (direction * weaponRange);
            float hitDistance = weaponRange;

            if (Physics.SphereCast(fireOrigin, shotRadius, direction, out RaycastHit hit, weaponRange, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore))
            {
                endPosition = fireOrigin + (direction * hit.distance);
                hitDistance = hit.distance;

                if (applyDamage && HasStateAuthority)
                {
                    ZombieAI zombie = hit.collider.GetComponentInParent<ZombieAI>();
                    if (zombie != null && zombie.Object != null && zombie.Object.IsValid)
                    {
                        PlayerRef shooterPlayerRef = Object.InputAuthority;
                        int currentDamage = Damage > 0 ? Damage : damage;
                        zombie.RPC_RequestDamage(currentDamage, shooterPlayerRef);
                    }
                }
            }

            if (_laserLine != null && HasInputAuthority)
            {
                _laserLine.useWorldSpace = true;
                _laserLine.SetPosition(0, fireOrigin);
                _laserLine.SetPosition(1, endPosition);
                _laserLine.enabled = true;
            }

            if (applyDamage)
            {
                RPC_RenderShotEffect(fireOrigin, direction, hitDistance);
            }
        }
    }

    private static Vector3 FlattenHorizontal(Vector3 value)
    {
        value.y = 0f;
        if (value.sqrMagnitude < 0.001f) return Vector3.forward;
        return value.normalized;
    }

    // 🟢 อัปเดต Effect ให้รองรับระยะทาง (เวลาทิ้ง Debug Ray)
    [Rpc(RpcSources.StateAuthority, RpcTargets.All, Channel = RpcChannel.Unreliable)]
    private void RPC_RenderShotEffect(Vector3 muzzlePosition, Vector3 fireDirection, float distance)
    {
        float drawDist = distance > 0 ? distance : weaponRange;
        Debug.DrawRay(muzzlePosition, fireDirection * drawDist, Color.yellow, 0.1f);
    }
}