using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class Zombie : MonoBehaviour
{
    [SerializeField] private float baseMoveSpeed = 3f;
    [SerializeField] private float baseDamage = 10f;
    [SerializeField] private float attackRate = 1.5f;
    [SerializeField] private float visionRange = 8f;

    private Rigidbody _rigidbody;
    private Transform _playerTarget;
    private float _nextAttackTime;
    private bool _hasTarget = false;

    private void Awake()
    {
        _rigidbody = GetComponent<Rigidbody>();
        _rigidbody.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;
    }

    private void FixedUpdate()
    {
        if (!_hasTarget)
        {
            SearchForPlayer();
        }
        else
        {
            MoveTowardsPlayer();
        }
    }

    private void SearchForPlayer()
    {
        Collider[] colliders = Physics.OverlapSphere(transform.position, visionRange);
        foreach (Collider col in colliders)
        {
            if (col.CompareTag("Player"))
            {
                _playerTarget = col.transform;
                _hasTarget = true;
                break;
            }
        }
    }

    private void MoveTowardsPlayer()
    {
        if (_playerTarget == null)
        {
            _hasTarget = false;
            return;
        }

        Vector3 direction = _playerTarget.position - _rigidbody.position;
        direction.y = 0f;

        if (direction.sqrMagnitude > 0.1f)
        {
            Vector3 moveDirection = direction.normalized;
            Vector3 targetPosition = _rigidbody.position + moveDirection * baseMoveSpeed * Time.fixedDeltaTime;
            _rigidbody.MovePosition(targetPosition);

            Quaternion targetRotation = Quaternion.LookRotation(moveDirection);
            _rigidbody.MoveRotation(targetRotation);
        }
    }

    private void OnCollisionStay(Collision collision)
    {
        if (collision.gameObject.CompareTag("Player") && Time.time >= _nextAttackTime)
        {
            _nextAttackTime = Time.time + attackRate;
            PlayerHealth playerHealth = collision.gameObject.GetComponent<PlayerHealth>();
            if (playerHealth != null)
            {
                playerHealth.TakeDamage(CalculateFinalDamage());
            }
        }
    }

    public float CalculateFinalDamage()
    {
        DifficultyManager difficultyManager = FindObjectOfType<DifficultyManager>();
        if (difficultyManager != null)
        {
            float multiplier = difficultyManager.GetDamageMultiplier();
            return baseDamage * multiplier;
        }
        return baseDamage;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, visionRange);
    }
}