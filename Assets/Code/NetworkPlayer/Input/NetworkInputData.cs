using Fusion;
using UnityEngine;

public struct NetworkInputData : INetworkInput
{
    public Vector2 MoveDirection;
    public Vector3 AimWorldPoint;
    public NetworkBool HasAimPoint;
}
