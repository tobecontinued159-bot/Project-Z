using Fusion;
using UnityEngine;

public class PlayerCamera : NetworkBehaviour
{
    [Header("Top-Down Camera Settings")]
    [SerializeField] private Vector3 cameraOffset = new Vector3(0f, 15f, -10f);
    [SerializeField] private float smoothSpeed = 8f;
    [SerializeField] private float cameraTiltX = 55f;

    private Vector3 _targetPosition;
    private Camera _cachedMainCamera;

    public override void Spawned()
    {
        if (Object.HasInputAuthority == false)
        {
            enabled = false;
            return;
        }

        if (_cachedMainCamera == null)
        {
            _cachedMainCamera = Camera.main;
        }

        if (_cachedMainCamera != null)
        {
            _cachedMainCamera.transform.eulerAngles = new Vector3(cameraTiltX, 0f, 0f);
            SnapCameraToPlayer();
        }
    }

    private void LateUpdate()
    {
        if (Object.HasInputAuthority == false)
        {
            return;
        }

        if (_cachedMainCamera == null)
        {
            _cachedMainCamera = Camera.main;
            if (_cachedMainCamera != null)
            {
                _cachedMainCamera.transform.eulerAngles = new Vector3(cameraTiltX, 0f, 0f);
            }
        }

        if (_cachedMainCamera == null)
        {
            return;
        }

        _targetPosition = transform.position + cameraOffset;

        _cachedMainCamera.transform.position = Vector3.Lerp(
            _cachedMainCamera.transform.position,
            _targetPosition,
            smoothSpeed * Time.deltaTime);
    }

    private void SnapCameraToPlayer()
    {
        if (_cachedMainCamera == null)
        {
            return;
        }

        _cachedMainCamera.transform.position = transform.position + cameraOffset;
    }
}
