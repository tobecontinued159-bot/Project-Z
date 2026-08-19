using Fusion;
using UnityEngine;

public class PlayerCameraSetup : NetworkBehaviour
{
    [SerializeField] private Vector3 cameraOffset = new Vector3(0f, 10f, -10f);
    [SerializeField] private float smoothDampSpeed = 5f;

    private Camera _playerCamera;
    private Vector3 _velocity;

    public override void Spawned()
    {
        if (!Object.HasInputAuthority)
        {
            return;
        }

        _playerCamera = Camera.main;

        if (_playerCamera == null)
        {
            GameObject cameraObject = new GameObject("PlayerCamera");
            _playerCamera = cameraObject.AddComponent<Camera>();
            cameraObject.tag = "MainCamera";
            cameraObject.AddComponent<AudioListener>();
        }
    }

    public override void Render()
    {
        if (!Object.HasInputAuthority || _playerCamera == null)
        {
            return;
        }

        Vector3 desiredPosition = transform.position + cameraOffset;
        float smoothTime = 1f / smoothDampSpeed;
        _playerCamera.transform.position = Vector3.SmoothDamp(
            _playerCamera.transform.position,
            desiredPosition,
            ref _velocity,
            smoothTime);

        Vector3 lookDirection = transform.position - _playerCamera.transform.position;
        if (lookDirection.sqrMagnitude > 0.001f)
        {
            _playerCamera.transform.rotation = Quaternion.LookRotation(lookDirection);
        }
    }
}
