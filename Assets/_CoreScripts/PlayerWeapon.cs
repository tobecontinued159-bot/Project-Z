using Fusion;
using UnityEngine;

[RequireComponent(typeof(LineRenderer))]
public class PlayerWeapon : NetworkBehaviour
{
    [Header("Weapon Settings")]
    [SerializeField] private float weaponRange = 50f;
    [SerializeField] private float muzzleHeight = 0.5f;
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

        UpdateLaserSight();
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

        ProcessFire();
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
        _laserLine.useWorldSpace = true;
    }

    private void UpdateLaserSight()
    {
        if (_laserLine == null)
        {
            return;
        }

        Vector3 muzzlePosition = transform.position + Vector3.up * muzzleHeight;
        Vector3 direction = transform.forward;
        Vector3 endPoint = muzzlePosition + direction * weaponRange;

        if (Physics.Raycast(muzzlePosition, direction, out RaycastHit hit, weaponRange))
        {
            endPoint = hit.point;
        }

        _laserLine.SetPosition(0, muzzlePosition);
        _laserLine.SetPosition(1, endPoint);
        _laserLine.enabled = true;
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
                PlayerRef shooterPlayerRef = Object.InputAuthority;
                int currentDamage = Damage > 0 ? Damage : damage;
                zombie.RPC_RequestDamage(currentDamage, shooterPlayerRef);
            }
        }
    }

    [Rpc(RpcSources.InputAuthority, RpcTargets.All, Channel = RpcChannel.Unreliable)]
    private void RPC_RenderShotEffect(Vector3 muzzlePosition, Vector3 fireDirection)
    {
        Debug.DrawRay(muzzlePosition, fireDirection * weaponRange, Color.yellow, 0.1f);
    }
}
