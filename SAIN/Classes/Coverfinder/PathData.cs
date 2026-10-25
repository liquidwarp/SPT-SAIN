using SAIN.Helpers;
using UnityEngine;
using UnityEngine.AI;

namespace SAIN.SAINComponent.SubComponents.CoverFinder;

public class PathData
{
    public NavMeshPath Path { get; private set; }
    public float PathLength { get; private set; }
    public int RoundedPathLength { get; private set; }
    public float TimeSinceLastUpdated
    {
        get { return Time.time - TimeLastUpdated; }
    }

    public float TimeLastUpdated { get; private set; }

    public void CalcPath(Vector3 origin, Vector3 destination)
    {
        Path ??= new NavMeshPath();
        Path.ClearCorners();
        NavMesh.CalculatePath(origin, destination, NavMesh.AllAreas, Path);
        if (Path.status == NavMeshPathStatus.PathComplete)
        {
            LastCornerCount = NavMeshHelpers.GetCorners(Path, ref _cornerBuffer);
            PathLength = NavMeshHelpers.GetPathLength(_cornerBuffer, LastCornerCount);
            RoundedPathLength = Mathf.FloorToInt(PathLength);
        }
        else
        {
            LastCornerCount = 0;
            PathLength = float.MaxValue;
            RoundedPathLength = int.MaxValue;
        }
        TimeLastUpdated = Time.time;
    }

    // Corners of the path the last CalcPath call found, on any PathData, valid until the next call. Empty unless the path was complete.
    public static Vector3[] LastCorners
    {
        get { return _cornerBuffer; }
    }

    public static int LastCornerCount { get; private set; }

    private static Vector3[] _cornerBuffer = new Vector3[32];
}
