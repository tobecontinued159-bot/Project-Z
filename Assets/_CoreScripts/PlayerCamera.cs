using Fusion;
using UnityEngine;

public class PlayerCamera : NetworkBehaviour
{
    [Header("Top-Down Camera Settings (WORLD SPACE ONLY)")]
    [SerializeField] private Vector3 worldSpaceOffset = new Vector3(0f, 15f, -10f);
    [SerializeField] private float smoothSpeed = 8f;
    [SerializeField] private float cameraTiltX = 55f;

    private Camera _cachedMainCamera;
    private PlayerStats _cachedStats;

    public override void Spawned()
    {
        if (Object.HasInputAuthority == false)
        {
            enabled = false;
            return;
        }

        _cachedStats = GetComponent<PlayerStats>();

        if (_cachedMainCamera == null)
        {
            _cachedMainCamera = Camera.main;
        }

        if (_cachedMainCamera != null)
        {
            if (_cachedMainCamera.transform.parent != null)
            {
                _cachedMainCamera.transform.SetParent(null, true);
            }

            _cachedMainCamera.transform.rotation = Quaternion.Euler(cameraTiltX, 0f, 0f);
            SnapCameraToPlayer();
        }
    }

    private void LateUpdate()
    {
        if (Object.HasInputAuthority == false)
        {
            return;
        }

        if (_cachedStats != null && _cachedStats.IsDead)
        {
            return;
        }

        if (_cachedMainCamera == null)
        {
            _cachedMainCamera = Camera.main;
            if (_cachedMainCamera != null && _cachedMainCamera.transform.parent != null)
            {
                _cachedMainCamera.transform.SetParent(null, true);
            }
        }

        if (_cachedMainCamera == null)
        {
            return;
        }

        Vector3 playerWorldPos = transform.position;
        float targetX = playerWorldPos.x + worldSpaceOffset.x;
        float targetY = playerWorldPos.y + worldSpaceOffset.y;
        float targetZ = playerWorldPos.z + worldSpaceOffset.z;
        Vector3 targetWorldPosition = new Vector3(targetX, targetY, targetZ);

        _cachedMainCamera.transform.position = Vector3.Lerp(
            _cachedMainCamera.transform.position,
            targetWorldPosition,
            smoothSpeed * Time.deltaTime);

        _cachedMainCamera.transform.rotation = Quaternion.Euler(cameraTiltX, 0f, 0f);
    }

    private void SnapCameraToPlayer()
    {
        if (_cachedMainCamera == null)
        {
            return;
        }

        Vector3 playerWorldPos = transform.position;
        float targetX = playerWorldPos.x + worldSpaceOffset.x;
        float targetY = playerWorldPos.y + worldSpaceOffset.y;
        float targetZ = playerWorldPos.z + worldSpaceOffset.z;
        _cachedMainCamera.transform.position = new Vector3(targetX, targetY, targetZ);
    }
}
