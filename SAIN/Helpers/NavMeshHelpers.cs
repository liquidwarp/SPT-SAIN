using UnityEngine;
using UnityEngine.AI;

namespace SAIN.Helpers;

public static class NavMeshHelpers
{
    public static Vector3? GetNearbyNavMeshPoint(Vector3 testPoint, float radius)
    {
        if (NavMesh.SamplePosition(testPoint, out var hit, radius, -1))
        {
            return hit.position;
        }

        return null;
    }
}
