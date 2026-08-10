using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class Survivor : MonoBehaviour
{
    [SerializeField] private float followSpeed = 4f;
    [SerializeField] private float stopDistance = 2f;
    [SerializeField] private float detectionRadius = 3f;

    private Rigidbody _rigidbody;
    private Transform _playerTarget;
    private bool _isRescued = false;

    private void Awake()
    {
        _rigidbody = GetComponent<Rigidbody>();
        _rigidbody.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;
    }

    private void Update()
    {
        if (!_isRescued)
        {
            CheckForPlayer();
        }
    }

    private void FixedUpdate()
    {
        if (_isRescued && _playerTarget != null)
        {
            MoveTowardsPlayer();
        }
    }

    private void CheckForPlayer()
    {
        Collider[] colliders = Physics.OverlapSphere(transform.position, detectionRadius);
        foreach (Collider col in colliders)
        {
            if (col.CompareTag("Player"))
            {
                _playerTarget = col.transform;
                _isRescued = true;
                Debug.Log("Survivor Rescued! Following player.");
                break;
            }
        }
    }

    private void MoveTowardsPlayer()
    {
        Vector3 direction = _playerTarget.position - _rigidbody.position;
        direction.y = 0f;

        if (direction.magnitude > stopDistance)
        {
            Vector3 moveDirection = direction.normalized;
            Vector3 targetPosition = _rigidbody.position + moveDirection * followSpeed * Time.fixedDeltaTime;
            _rigidbody.MovePosition(targetPosition);

            Quaternion targetRotation = Quaternion.LookRotation(moveDirection);
            _rigidbody.MoveRotation(targetRotation);
        }
    }
}