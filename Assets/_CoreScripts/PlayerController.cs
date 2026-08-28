using Fusion;
using UnityEngine;

[RequireComponent(typeof(NetworkTransform))]
public class PlayerController : NetworkBehaviour
{
    [SerializeField] private float moveSpeed = 5f;

    public override void FixedUpdateNetwork()
    {
        if (GetInput(out PlayerInput input) == false)
        {
            return;
        }

        MovePlayer(input.MoveInput);
        FaceLookPoint(input.LookDirection);
    }

    private void MovePlayer(Vector2 moveInput)
    {
        Vector3 moveDirection = new Vector3(moveInput.x, 0f, moveInput.y);

        if (moveDirection.sqrMagnitude > 1f)
        {
            moveDirection.Normalize();
        }

        transform.position += moveDirection * moveSpeed * Runner.DeltaTime;
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
