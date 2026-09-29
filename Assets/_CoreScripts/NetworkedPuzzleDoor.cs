using Fusion;
using UnityEngine;
using UnityEngine.AI;

public class NetworkedPuzzleDoor : NetworkBehaviour
{
    [Networked]
    [OnChangedRender(nameof(OnIsOpenChanged))]
    public NetworkBool IsOpen { get; set; }

    [Header("Puzzle Interaction Reference (Optional)")]
    [SerializeField] private MonoBehaviour puzzleInteractionScript;

    private Collider[] _colliders;
    private Renderer[] _renderers;
    private NavMeshObstacle _navMeshObstacle;

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

    public void UnlockDoor()
    {
        if (Object == null || Object.IsValid == false || IsOpen)
        {
            return;
        }

        if (HasStateAuthority)
        {
            IsOpen = true;
            OnDoorUnlockedSuccess();
            return;
        }

        RPC_RequestUnlockDoor();
    }

    [Rpc(RpcSources.All, RpcTargets.StateAuthority, Channel = RpcChannel.Reliable)]
    private void RPC_RequestUnlockDoor()
    {
        if (HasStateAuthority == false || IsOpen)
        {
            return;
        }

        IsOpen = true;
        OnDoorUnlockedSuccess();
    }

    private void OnIsOpenChanged()
    {
        ApplyOpenVisuals();
    }

    private void OnDoorUnlockedSuccess()
    {
        // 🟢 ปิดสคริปต์ ServerInteract เพื่อไม่ให้ดักจับการกดปุ่มได้อีก
        if (puzzleInteractionScript != null)
        {
            puzzleInteractionScript.enabled = false;
        }
        else
        {
            // ถ้าไม่ได้ลากใส่ ให้พยายามค้นหา ServerInteract ในตัวมันเองแล้วปิดทันที
            MonoBehaviour serverInteract = GetComponent("ServerInteract") as MonoBehaviour;
            if (serverInteract != null)
            {
                serverInteract.enabled = false;
            }
        }
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

        // 🟢 หากประตูเปิดแล้ว ให้ปิดสคริปต์ ServerInteract ทันที
        if (hide)
        {
            OnDoorUnlockedSuccess();
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
}