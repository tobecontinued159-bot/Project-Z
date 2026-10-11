using Fusion;
using UnityEngine;

[RequireComponent(typeof(NetworkTransform))]
public class PlayerController : NetworkBehaviour
{
    private static readonly int IsWalkingHash = Animator.StringToHash("isWalking");

    [SerializeField] private float moveSpeed = 5f;
    [SerializeField] private Animator animator;

    private PlayerStats _cachedStats;

    public override void Spawned()
    {
        if (animator == null)
        {
            animator = GetComponentInChildren<Animator>(true);
        }
    }

    public override void FixedUpdateNetwork()
    {
        if (EnsureStats() && _cachedStats.IsDead)
        {
            SetWalking(false);
            return;
        }

        if (HasInputAuthority && PlayerInputLock.IsTerminalOpen)
        {
            SetWalking(false);
            return;
        }

        if (GetInput(out PlayerInput input) == false)
        {
            SetWalking(false);
            return;
        }

        MovePlayer(input.MoveInput);
        FaceLookPoint(input.LookDirection);
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

    private void MovePlayer(Vector2 moveInput)
    {
        Vector3 moveDirection = new Vector3(moveInput.x, 0f, moveInput.y);

        if (moveDirection.sqrMagnitude > 1f)
        {
            moveDirection.Normalize();
        }

        transform.position += moveDirection * moveSpeed * Runner.DeltaTime;
        SetWalking(moveDirection.sqrMagnitude > 0.001f);
    }

    private void SetWalking(bool isWalking)
    {
        if (HasStateAuthority == false)
        {
            return;
        }

        if (animator == null)
        {
            animator = GetComponentInChildren<Animator>(true);
        }

        if (animator == null)
        {
            return;
        }

        animator.SetBool(IsWalkingHash, isWalking);
    }

    private void FaceLookPoint(Vector3 lookPoint)
    {
        if ((lookPoint - transform.position).sqrMagnitude < 0.001f)
        {
            return;
        }

        lookPoint.y = transform.position.y;
        transform.LookAt(lookPoint);
    }
}
