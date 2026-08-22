using Fusion;
using UnityEngine;

public class PlayerController : NetworkBehaviour
{
    [SerializeField] private float moveSpeed = 5f;

    public override void FixedUpdateNetwork()
    {
        if (HasStateAuthority == false)
        {
            return;
        }

        MovePlayer();
        FaceMouseCursor();
    }

    private void MovePlayer()
    {
        float horizontal = Input.GetAxisRaw("Horizontal");
        float vertical = Input.GetAxisRaw("Vertical");

        Vector3 moveDirection = new Vector3(horizontal, 0f, vertical);

        if (moveDirection.sqrMagnitude > 1f)
        {
            moveDirection.Normalize();
        }

        transform.position += moveDirection * moveSpeed * Runner.DeltaTime;
    }

    private void FaceMouseCursor()
    {
        Camera gameplayCamera = Camera.main;
        if (gameplayCamera == null)
        {
            return;
        }

        Ray ray = gameplayCamera.ScreenPointToRay(Input.mousePosition);
        Vector3 lookPoint;

        if (Physics.Raycast(ray, out RaycastHit hit, 500f))
        {
            lookPoint = hit.point;
        }
        else
        {
            Plane groundPlane = new Plane(Vector3.up, new Vector3(0f, transform.position.y, 0f));
            if (groundPlane.Raycast(ray, out float enterDistance) == false)
            {
                return;
            }

            lookPoint = ray.GetPoint(enterDistance);
        }

        lookPoint.y = transform.position.y;
        Vector3 lookDirection = lookPoint - transform.position;
        if (lookDirection.sqrMagnitude < 0.001f)
        {
            return;
        }

        transform.LookAt(lookPoint);
    }
}
