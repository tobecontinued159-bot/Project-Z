using Fusion;
using UnityEngine;

public class PlayerStats : NetworkBehaviour
{
    [Networked] public int Kills { get; set; }

    [Networked]
    [OnChangedRender(nameof(OnHealthChanged))]
    public int Health { get; set; }

    [Networked]
    [OnChangedRender(nameof(OnHealthChanged))]
    public NetworkBool IsDead { get; set; }
    [Networked] private TickTimer RespawnTimer { get; set; }

    [Header("Survival Settings")]
    [SerializeField] private int startingHealth = 100;
    [SerializeField] private float respawnSeconds = 5f;

    [Header("Respawn Settings")]
    [SerializeField] private Vector3 fallbackRespawnPosition = new Vector3(0f, 1f, 0f);

    private Renderer[] _allRenderers;
    private Collider[] _allColliders;
    private bool _visualsHidden;
    private PlayerPoints _playerPoints;

    public int Points
    {
        get
        {
            return PlayerPointsComponent != null ? PlayerPointsComponent.TotalPoints : 0;
        }
        set
        {
            if (PlayerPointsComponent != null && PlayerPointsComponent.HasStateAuthority)
            {
                PlayerPointsComponent.TotalPoints = Mathf.Max(0, value);
            }
        }
    }

    private PlayerPoints PlayerPointsComponent
    {
        get
        {
            if (_playerPoints == null)
            {
                _playerPoints = GetComponent<PlayerPoints>();
            }

            return _playerPoints;
        }
    }

    public int MaxHealth
    {
        get
        {
            return Mathf.Max(1, startingHealth);
        }
    }

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
            Kills = 0;
            Health = startingHealth;
            IsDead = false;
            RespawnTimer = TickTimer.None;
        }

        RefreshVisuals();
        OnHealthChanged();
    }

    private void OnHealthChanged()
    {
        PlayerUI playerUI = GetComponent<PlayerUI>();
        if (playerUI != null)
        {
            playerUI.RefreshHealthUI();
        }
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
        if (PlayerPointsComponent != null)
        {
            PlayerPointsComponent.AddPoints(amount);
            Debug.Log($"{Object.name} Points: {PlayerPointsComponent.TotalPoints}");
            return;
        }

        if (HasStateAuthority == false)
        {
            RPC_AddPoints(amount);
        }
    }

    [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
    public void RPC_AddPoints(int amount)
    {
        if (PlayerPointsComponent != null)
        {
            PlayerPointsComponent.AddPoints(amount);
            Debug.Log($"{Object.name} Points (RPC): {PlayerPointsComponent.TotalPoints}");
        }
    }

    public bool TrySpendPoints(int amount)
    {
        if (PlayerPointsComponent != null)
        {
            return PlayerPointsComponent.TrySpendPoints(amount);
        }

        return false;
    }

    public void RegisterKill()
    {
        if (Object == null || Object.IsValid == false)
        {
            return;
        }

        if (HasStateAuthority == false)
        {
            RPC_RegisterKill();
            return;
        }

        Kills++;
        Debug.Log($"{Object.name} Kills: {Kills}");
    }

    [Rpc(RpcSources.All, RpcTargets.StateAuthority, Channel = RpcChannel.Reliable)]
    public void RPC_RegisterKill()
    {
        if (HasStateAuthority == false)
        {
            return;
        }

        Kills++;
        Debug.Log($"{Object.name} Kills (RPC): {Kills}");
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

        GetRespawnPose(out Vector3 spawnPosition, out Quaternion spawnRotation);
        transform.position = spawnPosition;
        transform.rotation = spawnRotation;

        Debug.Log($"{Object.name} RESPAWNED at {spawnPosition}");
    }

    private void GetRespawnPose(out Vector3 spawnPosition, out Quaternion spawnRotation)
    {
        spawnPosition = fallbackRespawnPosition;
        spawnRotation = Quaternion.identity;

        NetworkPlayerSpawner spawner = NetworkPlayerSpawner.Instance;
        if (spawner == null)
        {
            spawner = FindFirstObjectByType<NetworkPlayerSpawner>();
        }

        if (spawner == null || spawner.spawnPoint == null)
        {
            return;
        }

        spawnPosition = spawner.spawnPoint.position;
        spawnRotation = spawner.spawnPoint.rotation;
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
