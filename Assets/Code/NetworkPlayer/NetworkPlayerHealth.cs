using Fusion;
using UnityEngine;

public class NetworkPlayerHealth : NetworkBehaviour, IDamageable
{
    [SerializeField] private float maxHealth = 100f;

    [Networked, OnChangedRender(nameof(OnHealthChanged))]
    public float CurrentHealth { get; private set; }

    public bool IsAlive => CurrentHealth > 0f;

    public override void Spawned()
    {
        if (Object.HasStateAuthority)
        {
            CurrentHealth = maxHealth;
        }
    }

    public void TakeDamage(float amount)
    {
        if (!Object.HasStateAuthority || !IsAlive)
        {
            return;
        }

        CurrentHealth = Mathf.Max(0f, CurrentHealth - amount);

        if (CurrentHealth <= 0f)
        {
            HandleDeath();
        }
    }

    private void HandleDeath()
    {
        Debug.Log($"Player {Object.InputAuthority} died.");

        if (Object.HasInputAuthority)
        {
            gameObject.SetActive(false);
        }
    }

    private void OnHealthChanged()
    {
        // Hook for UI / VFX team via events later.
    }
}
