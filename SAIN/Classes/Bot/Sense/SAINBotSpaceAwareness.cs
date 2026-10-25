using SAIN.Components;
using SAIN.Helpers;
using UnityEngine;
using UnityEngine.AI;

namespace SAIN.SAINComponent.Classes;

public class SAINBotSpaceAwareness : BotComponentClassBase
{
    public SAINBotSpaceAwareness(BotComponent sain)
        : base(sain)
    {
        CanEverTick = false;
    }

    public static bool ArePathsDifferent(NavMeshPath path1, NavMeshPath path2, float minRatio = 0.5f, float sqrDistCheck = 0.05f)
    {
        int path1Length = NavMeshHelpers.GetCorners(path1, ref _path1Corners);
        int path2Length = NavMeshHelpers.GetCorners(path2, ref _path2Corners);
        // A path with no corners leads nowhere, so it can't be the same as any other path.
        if (path1Length == 0)
        {
            return true;
        }
        Vector3[] path1Corners = _path1Corners;
        Vector3[] path2Corners = _path2Corners;

        int sameCount = 0;
        for (int i = 0; i < path1Length; i++)
        {
            Vector3 node = path1Corners[i];

            if (i < path2Length)
            {
                Vector3 node2 = path2Corners[i];
                if (node.IsEqual(node2, sqrDistCheck))
                {
                    sameCount++;
                }
            }
        }
        float ratio = (float)sameCount / path1Length;
        return ratio <= minRatio;
    }

    private static Vector3[] _path1Corners = new Vector3[32];
    private static Vector3[] _path2Corners = new Vector3[32];
}
