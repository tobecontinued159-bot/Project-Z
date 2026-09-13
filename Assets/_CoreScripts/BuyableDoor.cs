using Fusion;
using UnityEngine;
using UnityEngine.AI;

public class BuyableDoor : NetworkBehaviour
{
    [Header("Door Settings")]
    public int doorCost = 750;

    [Networked]
    [OnChangedRender(nameof(OnIsOpenChanged))]
    public NetworkBool IsOpen { get; set; }

    private Collider[] _colliders;
    private Renderer[] _renderers;
    private NavMeshObstacle _navMeshObstacle;
    private bool _localPlayerInRange;
    private PlayerNetworkData _localPlayer;

    public override void Spawned()
    {
        CacheComponents();
        EnsureNavMeshObstacle();
        ApplyOpenVisuals();
    }

    public override void Render()
    {
        if (Object == null || Object.IsValid == false)
        {
            return;
        }

        ApplyOpenVisuals();
    }

    private void Update()
    {
        if (Object == null || Object.IsValid == false || IsOpen)
        {
            return;
        }

        if (_localPlayerInRange == false || _localPlayer == null)
        {
            return;
        }

        if (Input.GetKeyDown(KeyCode.E) == false)
        {
            return;
        }

        AttemptOpenDoor(_localPlayer);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (Object == null || Object.IsValid == false || IsOpen)
        {
            return;
        }

        if (TryGetLocalPlayer(other, out PlayerNetworkData player) == false)
        {
            return;
        }

        _localPlayerInRange = true;
        _localPlayer = player;
    }

    private void OnTriggerExit(Collider other)
    {
        if (TryGetLocalPlayer(other, out _) == false)
        {
            return;
        }

        _localPlayerInRange = false;
        _localPlayer = null;
    }

    public void AttemptOpenDoor(PlayerNetworkData purchasingPlayer)
    {
        if (Object == null || Object.IsValid == false || IsOpen)
        {
            return;
        }

        if (purchasingPlayer == null)
        {
            return;
        }

        if (purchasingPlayer.HasStateAuthority == false &&
            (purchasingPlayer.Stats == null || purchasingPlayer.Stats.HasStateAuthority == false))
        {
            return;
        }

        if (purchasingPlayer.Points < doorCost)
        {
            Debug.Log($"Not enough points to open {name}. Need {doorCost}, have {purchasingPlayer.Points}.");
            return;
        }

        purchasingPlayer.Points -= doorCost;
        Debug.Log($"{purchasingPlayer.name} spent {doorCost} points. Remaining: {purchasingPlayer.Points}");
        RPC_RequestOpenDoor();
    }

    public void AttemptOpenDoor(PlayerStats purchasingPlayer)
    {
        if (purchasingPlayer == null)
        {
            return;
        }

        PlayerNetworkData networkData = purchasingPlayer.GetComponent<PlayerNetworkData>();
        if (networkData == null)
        {
            networkData = purchasingPlayer.gameObject.AddComponent<PlayerNetworkData>();
        }

        AttemptOpenDoor(networkData);
    }

    [Rpc(RpcSources.All, RpcTargets.StateAuthority, Channel = RpcChannel.Reliable)]
    private void RPC_RequestOpenDoor()
    {
        if (HasStateAuthority == false || IsOpen)
        {
            return;
        }

        IsOpen = true;
    }

    private void OnIsOpenChanged()
    {
        ApplyOpenVisuals();
    }

    private void ApplyOpenVisuals()
    {
        bool hide = IsOpen;

        if (_renderers == null || _colliders == null)
        {
            CacheComponents();
        }

        if (_renderers != null)
        {
            for (int i = 0; i < _renderers.Length; i++)
            {
                if (_renderers[i] != null)
                {
                    _renderers[i].enabled = hide == false;
                }
            }
        }

        if (_colliders != null)
        {
            for (int i = 0; i < _colliders.Length; i++)
            {
                if (_colliders[i] != null)
                {
                    _colliders[i].enabled = hide == false;
                }
            }
        }

        if (_navMeshObstacle == null)
        {
            _navMeshObstacle = GetComponent<NavMeshObstacle>();
        }

        if (_navMeshObstacle != null)
        {
            _navMeshObstacle.carving = true;
            _navMeshObstacle.enabled = hide == false;
        }
    }

    private void CacheComponents()
    {
        _renderers = GetComponentsInChildren<Renderer>(true);
        _colliders = GetComponentsInChildren<Collider>(true);
        _navMeshObstacle = GetComponent<NavMeshObstacle>();
    }

    private void EnsureNavMeshObstacle()
    {
        if (_navMeshObstacle == null)
        {
            _navMeshObstacle = gameObject.AddComponent<NavMeshObstacle>();
        }

        _navMeshObstacle.carving = true;
        _navMeshObstacle.carveOnlyStationary = true;
        BoxCollider box = GetComponent<BoxCollider>();
        if (box != null)
        {
            _navMeshObstacle.shape = NavMeshObstacleShape.Box;
            _navMeshObstacle.center = box.center;
            _navMeshObstacle.size = box.size;
        }
    }

    private bool TryGetLocalPlayer(Collider other, out PlayerNetworkData player)
    {
        player = other.GetComponentInParent<PlayerNetworkData>();
        if (player == null)
        {
            PlayerStats stats = other.GetComponentInParent<PlayerStats>();
            if (stats != null)
            {
                player = stats.GetComponent<PlayerNetworkData>();
                if (player == null)
                {
                    player = stats.gameObject.AddComponent<PlayerNetworkData>();
                }
            }
        }

        if (player == null)
        {
            return false;
        }

        return player.HasStateAuthority || player.HasInputAuthority ||
               (player.Stats != null && (player.Stats.HasStateAuthority || player.Stats.HasInputAuthority));
    }
}
