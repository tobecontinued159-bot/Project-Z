using Fusion;
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class NetworkPlayerController : NetworkBehaviour
{
    [SerializeField] private float moveSpeed = 5f;

    private Rigidbody _rigidbody;

    public override void Spawned()
    {
        _rigidbody = GetComponent<Rigidbody>();
        _rigidbody.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;

        if (Object.HasInputAuthority)
        {
            LocalPlayerRegistry.Register(Object);
        }
    }

    public override void Despawned(NetworkRunner runner, bool hasState)
    {
        if (Object.HasInputAuthority)
        {
            LocalPlayerRegistry.Unregister(Object);
        }
    }

    public override void FixedUpdateNetwork()
    {
        if (!GetInput(out NetworkInputData input))
        {
            return;
        }

        ApplyMovement(input);
        ApplyFacing(input);
    }

    private void ApplyMovement(NetworkInputData input)
    {
        Vector3 moveDirection = new Vector3(input.MoveDirection.x, 0f, input.MoveDirection.y);

        if (moveDirection.sqrMagnitude > 1f)
        {
            moveDirection.Normalize();
        }

        Vector3 targetPosition = _rigidbody.position + moveDirection * moveSpeed * Runner.DeltaTime;
        _rigidbody.MovePosition(targetPosition);
    }

    private void ApplyFacing(NetworkInputData input)
    {
        if (!input.HasAimPoint)
        {
            return;
        }

        Vector3 lookDirection = input.AimWorldPoint - transform.position;
        lookDirection.y = 0f;

        if (lookDirection.sqrMagnitude < 0.001f)
        {
            return;
        }

        transform.rotation = Quaternion.LookRotation(lookDirection);
    }
}
