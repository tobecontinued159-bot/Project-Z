using Fusion;
using UnityEngine;

public class PlayerStats : NetworkBehaviour
{
    [Networked] public int Points { get; set; }
    [Networked] public int Kills { get; set; }
    [Networked] public int Health { get; set; }
    [Networked] public NetworkBool IsDead { get; set; }
    [Networked] private TickTimer RespawnTimer { get; set; }

    [Header("Survival Settings")]
    [SerializeField] private int startingHealth = 100;
    [SerializeField] private float respawnSeconds = 5f;

    [Header("Respawn Settings")]
    [SerializeField] private Vector3 respawnPosition = new Vector3(0f, 1f, 0f);

    private Renderer[] _allRenderers;
    private Collider[] _allColliders;
    private bool _visualsHidden;

    public float RemainingRespawnSeconds
    {
        get
        {
            if (Runner == null)
            {
                return 0f;
            }
            return RespawnTimer.RemainingTime(Runner) ?? 0f;
        }
    }

    public override void Spawned()
    {
        _allRenderers = GetComponentsInChildren<Renderer>(true);
        _allColliders = GetComponentsInChildren<Collider>(true);

        if (HasStateAuthority)
        {
            Points = 0;
            Kills = 0;
            Health = startingHealth;
            IsDead = false;
            RespawnTimer = TickTimer.None;
        }

        RefreshVisuals();
    }

    public override void FixedUpdateNetwork()
    {
        if (HasStateAuthority == false)
        {
            return;
        }

        if (IsDead && RespawnTimer.Expired(Runner))
        {
            Respawn();
        }
    }

    public override void Render()
    {
        RefreshVisuals();
    }

    public void AddPointsLocal(int amount)
    {
        if (HasStateAuthority == false)
        {
            RPC_AddPoints(amount);
            return;
        }

        Points += amount;
        Debug.Log($"{Object.name} Points (Local): {Points}");
    }

    [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
    public void RPC_AddPoints(int amount)
    {
        Points += amount;
        Debug.Log($"{Object.name} Points (RPC): {Points}");
    }

    public void TakeDamageLocal(int damage)
    {
        if (HasStateAuthority == false)
        {
            RPC_RequestTakeDamage(damage);
            return;
        }

        ApplyDamage(damage);
    }

    [Rpc(RpcSources.All, RpcTargets.StateAuthority, Channel = RpcChannel.Reliable)]
    public void RPC_RequestTakeDamage(int damage)
    {
        if (HasStateAuthority == false)
        {
            return;
        }

        ApplyDamage(damage);
    }

    private void ApplyDamage(int damage)
    {
        if (IsDead)
        {
            return;
        }

        Health -= damage;
        Debug.Log($"{Object.name} took {damage} damage. Health: {Health}");

        if (Health <= 0)
        {
            Health = 0;
            IsDead = true;
            RespawnTimer = TickTimer.CreateFromSeconds(Runner, respawnSeconds);
            Debug.Log($"!!! {Object.name} DIED !!! Respawn in {respawnSeconds}s");
        }
    }

    private void Respawn()
    {
        if (HasStateAuthority == false)
        {
            return;
        }

        Health = startingHealth;
        IsDead = false;
        RespawnTimer = TickTimer.None;
        transform.position = respawnPosition;
        transform.rotation = Quaternion.identity;

        Debug.Log($"{Object.name} RESPAWNED at {respawnPosition}");
    }

    public void Heal(int amount)
    {
        if (HasStateAuthority == false)
        {
            return;
        }

        if (IsDead)
        {
            return;
        }

        Health = Mathf.Min(Health + amount, startingHealth);
    }

    private void RefreshVisuals()
    {
        bool shouldHide = IsDead;

        if (shouldHide == _visualsHidden)
        {
            return;
        }

        _visualsHidden = shouldHide;

        if (_allRenderers != null)
        {
            for (int i = 0; i < _allRenderers.Length; i++)
            {
                if (_allRenderers[i] != null)
                {
                    _allRenderers[i].enabled = !shouldHide;
                }
            }
        }

        if (_allColliders != null)
        {
            for (int i = 0; i < _allColliders.Length; i++)
            {
                if (_allColliders[i] != null)
                {
                    _allColliders[i].enabled = !shouldHide;
                }
            }
        }
    }
}
