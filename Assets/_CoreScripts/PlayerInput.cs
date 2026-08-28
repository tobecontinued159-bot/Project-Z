using Fusion;
using UnityEngine;

public struct PlayerInput : INetworkInput
{
    public Vector2 MoveInput;
    public Vector3 LookDirection;
    public NetworkBool FirePressed;
}
