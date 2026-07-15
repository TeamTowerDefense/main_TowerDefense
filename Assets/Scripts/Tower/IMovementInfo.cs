using UnityEngine;

public interface IMovementInfo
{
    Vector3 MoveDirection { get; }
    float CurrentMoveSpeed { get; }
}
