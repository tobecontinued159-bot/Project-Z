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

    [Header("Ammo & Reload Settings")]
    [SerializeField] private int weaponId = 0;
    [SerializeField] private int maxAmmo = 30;
    [SerializeField] private int maxReserveAmmo = 90;
    [SerializeField] private float reloadDuration = 2f;

    [Networked] public int Damage { get; set; }
    [Networked] public float FireRate { get; set; }
    [Networked] public int CurrentAmmo { get; set; }
    [Networked] public int ReserveAmmo { get; set; }
    [Networked] public int EquippedWeaponId { get; set; }
    [Networked] public NetworkBool IsReloading { get; set; }
    [Networked] private int MagazineSize { get; set; }
    [Networked] private int ReserveSize { get; set; }
    [Networked] private TickTimer ReloadTimer { get; set; }
    [Networked] private NetworkBool DamageBuffActive { get; set; }
    [Networked] private TickTimer DamageBuffTimer { get; set; }
    [Networked] private int UnbuffedDamage { get; set; }

    public int MaxAmmo => MagazineSize > 0 ? MagazineSize : Mathf.Max(1, maxAmmo);
    public int MaxReserveAmmo => ReserveSize > 0 ? ReserveSize : Mathf.Max(0, maxReserveAmmo);
    public int DefinitionWeaponId => weaponId;
    public int DefinitionDamage => damage;
    public float DefinitionFireRate => fireRate;
    public int DefinitionMaxAmmo => maxAmmo;
    public int DefinitionMaxReserveAmmo => maxReserveAmmo;

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
            ReserveAmmo = maxReserveAmmo;
            EquippedWeaponId = weaponId;
            MagazineSize = maxAmmo;
            ReserveSize = maxReserveAmmo;
            IsReloading = false;
            ReloadTimer = TickTimer.None;
            DamageBuffActive = false;
            DamageBuffTimer = TickTimer.None;
            UnbuffedDamage = damage;
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
        if (HasStateAuthority == false) return;
        ApplyUpgrade();
    }

    private void ApplyUpgrade()
    {
        Damage += upgradeDamageBonus;
        FireRate = Mathf.Max(minFireRate, FireRate * upgradeFireRateMultiplier);
        Debug.Log($"{name} upgraded weapon. Damage: {Damage}, FireRate: {FireRate:0.00}");
    }

    public void StartReload()
    {
        if (HasStateAuthority == false)
        {
            RPC_RequestStartReload();
            return;
        }

        BeginReload();
    }

    [Rpc(RpcSources.All, RpcTargets.StateAuthority, Channel = RpcChannel.Reliable)]
    private void RPC_RequestStartReload()
    {
        BeginReload();
    }

    private void BeginReload()
    {
        if (HasStateAuthority == false)
        {
            return;
        }

        if (IsReloading || CurrentAmmo >= MaxAmmo || ReserveAmmo <= 0)
        {
            return;
        }

        IsReloading = true;
        ReloadTimer = TickTimer.CreateFromSeconds(Runner, Mathf.Max(0.1f, reloadDuration));
        Debug.Log($"{name} started reload ({reloadDuration:0.00}s).");
    }

    private void CompleteReload()
    {
        if (HasStateAuthority == false)
        {
            return;
        }

        int missingAmmo = Mathf.Max(0, MaxAmmo - CurrentAmmo);
        int ammoToLoad = Mathf.Min(missingAmmo, Mathf.Max(0, ReserveAmmo));
        CurrentAmmo += ammoToLoad;
        ReserveAmmo = Mathf.Max(0, ReserveAmmo - ammoToLoad);

        IsReloading = false;
        ReloadTimer = TickTimer.None;
        Debug.Log($"{name} reload complete: {CurrentAmmo}/{ReserveAmmo}");
    }

    public void RefillAmmo()
    {
        if (HasStateAuthority == false)
        {
            RPC_RequestRefillAmmo();
            return;
        }

        FillAmmoToMax();
    }

    [Rpc(RpcSources.All, RpcTargets.StateAuthority, Channel = RpcChannel.Reliable)]
    private void RPC_RequestRefillAmmo()
    {
        FillAmmoToMax();
    }

    private void FillAmmoToMax()
    {
        if (HasStateAuthority == false)
        {
            return;
        }

        CurrentAmmo = Mathf.Max(1, MaxAmmo);
        ReserveAmmo = Mathf.Max(0, MaxReserveAmmo);
        IsReloading = false;
        ReloadTimer = TickTimer.None;
        Debug.Log($"{name} ammo refilled: {CurrentAmmo}/{ReserveAmmo}");
    }

    public bool HasWeapon(int targetWeaponId)
    {
        return EquippedWeaponId == targetWeaponId;
    }

    public bool IsAmmoFull()
    {
        return CurrentAmmo >= MaxAmmo && ReserveAmmo >= MaxReserveAmmo;
    }

    public void EquipWeapon(int newWeaponId, int newDamage, float newFireRate, int newMagazineSize, int newReserveSize)
    {
        if (HasStateAuthority == false)
        {
            RPC_RequestEquipWeapon(newWeaponId, newDamage, newFireRate, newMagazineSize, newReserveSize);
            return;
        }

        ApplyEquipWeapon(newWeaponId, newDamage, newFireRate, newMagazineSize, newReserveSize);
    }

    [Rpc(RpcSources.All, RpcTargets.StateAuthority, Channel = RpcChannel.Reliable)]
    private void RPC_RequestEquipWeapon(int newWeaponId, int newDamage, float newFireRate, int newMagazineSize, int newReserveSize)
    {
        ApplyEquipWeapon(newWeaponId, newDamage, newFireRate, newMagazineSize, newReserveSize);
    }

    private void ApplyEquipWeapon(int newWeaponId, int newDamage, float newFireRate, int newMagazineSize, int newReserveSize)
    {
        if (HasStateAuthority == false)
        {
            return;
        }

        EquippedWeaponId = newWeaponId;
        Damage = Mathf.Max(1, newDamage);
        FireRate = Mathf.Max(minFireRate, newFireRate);
        MagazineSize = Mathf.Max(1, newMagazineSize);
        ReserveSize = Mathf.Max(0, newReserveSize);
        UnbuffedDamage = Damage;
        DamageBuffActive = false;
        DamageBuffTimer = TickTimer.None;
        FillAmmoToMax();
        Debug.Log($"{name} equipped weapon id {EquippedWeaponId}. Damage: {Damage}, Mag: {MaxAmmo}");
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
        Debug.Log($"{name} damage buff x{safeMultiplier} for {duration:0}s. Damage: {Damage}");
    }

    private void TickDamageBuff()
    {
        if (HasStateAuthority == false || DamageBuffActive == false) return;

        if (DamageBuffTimer.Expired(Runner) == false) return;

        Damage = UnbuffedDamage > 0 ? UnbuffedDamage : damage;
        DamageBuffActive = false;
        DamageBuffTimer = TickTimer.None;
        Debug.Log($"{name} damage buff ended. Damage: {Damage}");
    }

    private void LateUpdate()
    {
        if (EnsureStats() && _cachedStats.IsDead)
        {
            if (_laserLine != null) _laserLine.enabled = false;
            return;
        }

        if (HasInputAuthority && PlayerInputLock.IsTerminalOpen) return;

        if (HasInputAuthority)
        {
            Fire(applyDamage: false);
        }
    }

    public override void FixedUpdateNetwork()
    {
        if (EnsureStats() && _cachedStats.IsDead) return;

        TickDamageBuff();

        // [แก้ไขจุดที่ 5]: เช็กว่าครบรอบ 2 วินาทีของการ Reload หรือยัง (ฝั่ง State Authority)
        if (HasStateAuthority && IsReloading)
        {
            if (ReloadTimer.Expired(Runner))
            {
                CompleteReload();
            }
        }

        if (GetInput(out PlayerInput input) == false) return;

        // [แก้ไขจุดที่ 6]: หากกดปุ่ม R หรือกระสุนหมด ให้สั่งเริ่ม StartReload() 2 วินาที
        if (input.ReloadPressed || (input.FirePressed && CurrentAmmo <= 0))
        {
            if (IsReloading == false && CurrentAmmo < MaxAmmo)
            {
                StartReload();
            }
        }

        if (PlayerInputLock.IsTerminalOpen) return;

        // ห้ามยิงถ้าระหว่างกำลัง Reload อยู่ หรือกระสุนหมด
        if (IsReloading || CurrentAmmo <= 0) return;

        if (input.FirePressed == false) return;

        if (FireCooldown.ExpiredOrNotRunning(Runner) == false) return;

        if (HasStateAuthority)
        {
            Fire(applyDamage: true);
            CurrentAmmo = Mathf.Max(0, CurrentAmmo - 1);

            // กระสุนหมดนัดสุดท้าย ให้สั่งเริ่ม Reload ทันทีอัตโนมัติ
            if (CurrentAmmo <= 0)
            {
                StartReload();
            }

            float currentFireRate = FireRate > 0f ? FireRate : fireRate;
            FireCooldown = TickTimer.CreateFromSeconds(Runner, currentFireRate);
        }
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

        if (direction.sqrMagnitude < 0.001f)
        {
            direction = FlattenHorizontal(transform.forward);
        }
        direction.Normalize();

        Vector3 endPosition = fireOrigin + (direction * weaponRange);

        if (Physics.SphereCast(fireOrigin, shotRadius, direction, out RaycastHit hit, weaponRange, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore))
        {
            endPosition = fireOrigin + (direction * hit.distance);
            endPosition.y = fireOrigin.y;

            if (applyDamage && HasStateAuthority)
            {
                ZombieAI zombie = hit.collider.GetComponentInParent<ZombieAI>();
                if (zombie != null)
                {
                    // [แก้ไขเพิ่มบรรทัดนี้]: ตรวจสอบว่า Zombie มี NetworkObject และ Spawn สมบูรณ์บน Network แล้วหรือยัง
                    if (zombie.Object != null && zombie.Object.IsValid)
                    {
                        PlayerRef shooterPlayerRef = Object.InputAuthority;
                        int currentDamage = Damage > 0 ? Damage : damage;
                        zombie.RPC_RequestDamage(currentDamage, shooterPlayerRef);
                    }
                    else
                    {
                        Debug.LogWarning($"[PlayerWeapon] Shot hit Zombie '{zombie.name}', but it's not spawned on Fusion Network!");
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
                RPC_RenderShotEffect(fireOrigin, direction);
            }
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

    [Rpc(RpcSources.StateAuthority, RpcTargets.All, Channel = RpcChannel.Unreliable)]
    private void RPC_RenderShotEffect(Vector3 muzzlePosition, Vector3 fireDirection)
    {
        Debug.DrawRay(muzzlePosition, fireDirection * weaponRange, Color.yellow, 0.1f);
    }
}