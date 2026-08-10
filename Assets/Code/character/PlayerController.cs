using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class PlayerController : MonoBehaviour
{
    [SerializeField] private float moveSpeed = 5f;
    [SerializeField] private Camera playerCamera;

    private Rigidbody _rigidbody;
    private Vector3 _moveDirection;

    private void Awake()
    {
        _rigidbody = GetComponent<Rigidbody>();
        _rigidbody.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;

        if (playerCamera == null)
        {
            playerCamera = Camera.main;
        }
    }

    private void Update()
    {
        float horizontal = Input.GetAxisRaw("Horizontal");
        float vertical = Input.GetAxisRaw("Vertical");

        _moveDirection = new Vector3(horizontal, 0f, vertical);

        if (_moveDirection.sqrMagnitude > 1f)
        {
            _moveDirection.Normalize();
        }

        FaceMouseCursor();
    }

    private void FixedUpdate()
    {
        MovePlayer();
    }

    private void MovePlayer()
    {
        Vector3 targetPosition = _rigidbody.position + _moveDirection * moveSpeed * Time.fixedDeltaTime;
        _rigidbody.MovePosition(targetPosition);
    }

    private void FaceMouseCursor()
    {
        if (playerCamera == null)
        {
            return;
        }

        Plane groundPlane = new Plane(Vector3.up, Vector3.zero);
        Ray ray = playerCamera.ScreenPointToRay(Input.mousePosition);

        if (!groundPlane.Raycast(ray, out float enterDistance))
        {
            return;
        }

        Vector3 hitPoint = ray.GetPoint(enterDistance);
        Vector3 lookDirection = hitPoint - transform.position;
        lookDirection.y = 0f;

        if (lookDirection.sqrMagnitude < 0.001f)
        {
            return;
        }

        transform.rotation = Quaternion.LookRotation(lookDirection);
    }
}