using System.Collections.Generic;
using EFT;
using SAIN.Models.PlayerData;
using Unity.Collections;
using UnityEngine;

namespace SAIN.Components.PlayerComponentSpace;

public class OtherPlayerData(string id, PlayerComponent Component)
{
    public readonly string ProfileId = id;
    public readonly PlayerComponent OtherPlayerComponent = Component;
    public PlayerDistanceData DistanceData { get; } = new PlayerDistanceData(Component);
    public OtherPlayerLoSData LoSData { get; } = new OtherPlayerLoSData(Component.Player);

    public bool IsInHearingRadius_Footsteps
    {
        get { return DistanceData.Distance <= 100.0f; }
    }

    public bool IsInHearingRadius_GunFire
    {
        get { return DistanceData.Distance <= 500.0f; }
    }

    public void Dispose()
    {
        LoSData.Dispose();
    }
}

public class OtherPlayerLoSData
{
    public NativeArray<RaycastCommand> RaycastCommands { get; private set; }
    public NativeArray<RaycastHit> RaycastHits { get; private set; }
    public BodyColliderLoSData[] RaycastArray { get; private set; }
    public BodyPartCollider[] BodyPartColliders { get; }

    public OtherPlayerLoSData(Player player)
    {
        var parts = player.PlayerBones.BodyPartCollidersDictionary;
        foreach (var part in parts)
        {
            if (part.Value != null)
            {
                _bodyPartColliders.Add(part.Value);
                _bodyPartColliderTypes.Add(part.Key);
            }
            else
            {
                Logger.LogWarning($"Player {player.name} has a null BodyPartCollider for {part.Key}");
            }
        }

        int count = _bodyPartColliders.Count;
        RaycastCommands = new NativeArray<RaycastCommand>(count, Allocator.Persistent);
        RaycastHits = new NativeArray<RaycastHit>(count, Allocator.Persistent);

        RaycastArray = new BodyColliderLoSData[count];
        for (int i = 0; i < count; i++)
        {
            RaycastArray[i] = new BodyColliderLoSData
            {
                BodyPartColliderType = _bodyPartColliders[i].BodyPartColliderType,
                PartType = _bodyPartColliders[i].BodyPartType,
                TimeLastLineofSightSuccess = -1f,
                TimeLastCanShootSuccess = -1f,
            };
        }

        BodyPartColliders = [.. _bodyPartColliders];

        Logger.LogDebug($"Found [{BodyPartColliders.Length}] Parts for [{player.name}]");

        _bodyPartColliders.Clear();
        _bodyPartColliderTypes.Clear();
    }

    public void Dispose()
    {
        if (RaycastCommands.IsCreated)
        {
            RaycastCommands.Dispose();
        }
        if (RaycastHits.IsCreated)
        {
            RaycastHits.Dispose();
        }
    }

    private static List<BodyPartCollider> _bodyPartColliders = [];
    private static List<EBodyPartColliderType> _bodyPartColliderTypes = [];
}

public struct BodyColliderLoSData
{
    public EBodyPartColliderType BodyPartColliderType;
    public EBodyPart PartType;
    public float TimeLastLineofSightSuccess;
    public float TimeLastVisibleSuccess;
    public float TimeLastCanShootSuccess;
    public Vector3 LastSuccessCastPoint;
}
