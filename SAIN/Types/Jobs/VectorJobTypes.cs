using UnityEngine;

namespace SAIN.Types.Jobs;

public readonly struct RandomDir
{
    public RandomDir(float magnitude, Vector3 directionNormal)
    {
        Magnitude = magnitude;
        DirectionNormal = directionNormal;
        Direction = DirectionNormal * Magnitude;
    }

    public readonly Vector3 Direction;
    public readonly Vector3 DirectionNormal;
    public readonly float Magnitude;
}
