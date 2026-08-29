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

    [Header("Laser Sight")]
    [SerializeField] private float laserWidth = 0.03f;
    [SerializeField] private Color laserColor = new Color(1f, 0f, 0f, 0.7f);
    [SerializeField] private Gradient laserGradient;
    [SerializeField] private Material laserMaterial;

    [Networked] private TickTimer FireCooldown { get; set; }

    private LineRenderer _laserLine;

    public override void Spawned()
    {
        SetupLaserSight();
    }

    private void LateUpdate()
    {
        UpdateLaserSight();
    }

    public override void FixedUpdateNetwork()
    {
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
        FireCooldown = TickTimer.CreateFromSeconds(Runner, fireRate);
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

        if (Object.HasInputAuthority)
        {
            _laserLine.enabled = true;
        }
        else
        {
            _laserLine.enabled = true;
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
                PlayerRef shooterPlayerRef = Object.InputAuthority;
                zombie.RPC_RequestDamage(damage, shooterPlayerRef);
            }
        }
    }

    [Rpc(RpcSources.InputAuthority, RpcTargets.All, Channel = RpcChannel.Unreliable)]
    private void RPC_RenderShotEffect(Vector3 muzzlePosition, Vector3 fireDirection)
    {
        Debug.DrawRay(muzzlePosition, fireDirection * weaponRange, Color.yellow, 0.1f);
    }
}
