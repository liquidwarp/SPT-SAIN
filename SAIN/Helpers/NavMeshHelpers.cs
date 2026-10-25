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

    public static int GetCorners(NavMeshPath path, ref Vector3[] buffer, int maxLength = int.MaxValue)
    {
        int count = path.GetCornersNonAlloc(buffer);
        while (count == buffer.Length && buffer.Length < maxLength)
        {
            // An empty buffer would otherwise never grow
            buffer = new Vector3[Mathf.Max(buffer.Length * 2, 16)];
            count = path.GetCornersNonAlloc(buffer);
        }
        return count;
    }

    public static float GetPathLength(Vector3[] corners, int count)
    {
        float length = 0f;
        for (int i = 1; i < count; i++)
        {
            length += (corners[i] - corners[i - 1]).magnitude;
        }
        return length;
    }
}
