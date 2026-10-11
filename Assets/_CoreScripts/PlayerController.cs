using Fusion;
using UnityEngine;

[RequireComponent(typeof(NetworkTransform))]
public class PlayerController : NetworkBehaviour
{
    private const int StartingHp = 100;

    [Header("Movement")]
    [SerializeField] private float moveSpeed = 5f;

    [Header("Animation")]
    [SerializeField] private Animator animator;

    [Header("Shooting")]
    [SerializeField] private Transform firePoint;
    [SerializeField] private GameObject bulletPrefab;
    [SerializeField] private float fireCooldownSeconds = 0.2f;

    // พลังชีวิตที่ซิงค์ทั้งห้อง ถ้ามี PlayerStats จะตามค่า Health ของคอมโพเนนต์นั้น
    [Networked] public int HP { get; set; }
    [Networked] private TickTimer FireCooldown { get; set; }

    private PlayerStats _cachedStats;

    public override void Spawned()
    {
        CacheAnimator();
        CacheFirePoint();

        if (HasStateAuthority)
        {
            HP = StartingHp;
            FireCooldown = TickTimer.None;
        }
    }

    public override void FixedUpdateNetwork()
    {
        SyncHealthFromStats();

        // ตายแล้วล็อกเดิน หันหน้า และยิง
        if (IsPlayerDead())
        {
            UpdateAnimation(false);
            return;
        }

        if (HasInputAuthority && PlayerInputLock.IsTerminalOpen)
        {
            UpdateAnimation(false);
            return;
        }

        // อินพุต WASD และจุดที่เมาส์ชี้ ถูกเก็บใน PlayerInput แล้วส่งเข้ามาทีละ tick
        if (GetInput(out PlayerInput input) == false)
        {
            UpdateAnimation(false);
            return;
        }

        bool isWalking = MovePlayer(input.MoveInput);
        FaceMouseAim(input.LookDirection);
        UpdateAnimation(isWalking);

        if (input.FirePressed)
        {
            TryFire();
        }
    }

    private bool MovePlayer(Vector2 moveInput)
    {
        Vector3 moveDirection = new Vector3(moveInput.x, 0f, moveInput.y);
        if (moveDirection.sqrMagnitude > 1f)
        {
            moveDirection.Normalize();
        }

        if (moveDirection.sqrMagnitude <= 0.001f)
        {
            return false;
        }

        transform.position += moveDirection * moveSpeed * Runner.DeltaTime;
        return true;
    }

    private void FaceMouseAim(Vector3 lookPoint)
    {
        Vector3 flatOffset = lookPoint - transform.position;
        flatOffset.y = 0f;
        if (flatOffset.sqrMagnitude < 0.001f)
        {
            return;
        }

        transform.rotation = Quaternion.LookRotation(flatOffset, Vector3.up);
    }

    private void TryFire()
    {
        if (HasStateAuthority == false || bulletPrefab == null || firePoint == null)
        {
            return;
        }

        if (FireCooldown.ExpiredOrNotRunning(Runner) == false)
        {
            return;
        }

        NetworkObject bullet = bulletPrefab.GetComponent<NetworkObject>();
        if (bullet == null)
        {
            Debug.LogError("PlayerController: bulletPrefab must be a baked Fusion NetworkObject.");
            return;
        }

        Runner.Spawn(bullet, firePoint.position, firePoint.rotation, Object.InputAuthority);
        FireCooldown = TickTimer.CreateFromSeconds(Runner, fireCooldownSeconds);
    }

    private void UpdateAnimation(bool isWalking)
    {
        if (HasStateAuthority == false)
        {
            return;
        }

        CacheAnimator();
        if (animator == null)
        {
            return;
        }

        bool isDead = IsPlayerDead();
        animator.SetBool("isWalking", isDead == false && isWalking);
        animator.SetBool("isDead", isDead);
    }

    private void SyncHealthFromStats()
    {
        if (HasStateAuthority == false || EnsureStats() == false)
        {
            return;
        }

        HP = _cachedStats.Health;
    }

    private bool IsPlayerDead()
    {
        if (HP <= 0)
        {
            return true;
        }

        return EnsureStats() && _cachedStats.IsDead;
    }

    private bool EnsureStats()
    {
        if (_cachedStats == null)
        {
            _cachedStats = GetComponent<PlayerStats>();
        }

        return _cachedStats != null
            && _cachedStats.Object != null
            && _cachedStats.Object.IsValid;
    }

    private void CacheAnimator()
    {
        if (animator == null)
        {
            animator = GetComponentInChildren<Animator>(true);
        }
    }

    private void CacheFirePoint()
    {
        if (firePoint != null)
        {
            return;
        }

        Transform[] children = GetComponentsInChildren<Transform>(true);
        for (int i = 0; i < children.Length; i++)
        {
            if (children[i].name == "FirePoint")
            {
                firePoint = children[i];
                return;
            }
        }
    }
}
