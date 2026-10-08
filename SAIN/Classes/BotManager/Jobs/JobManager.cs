using System;
using System.Collections.Generic;
using SAIN.Types.Jobs;
using UnityEngine;

namespace SAIN.Components;

public class JobManager : IDisposable
{
    public JobManager(MonoBehaviour Owner)
    {
        Jobs.Add(new FlashlightRaycastJob(Owner));
        Jobs.Add(new EnemyPathVisibilityRaycastJob(Owner));
    }

    public void Start()
    {
        foreach (var job in Jobs)
        {
            job?.Start();
        }
    }

    public readonly List<ISainJob> Jobs = [];

    public void Dispose()
    {
        foreach (var job in Jobs)
        {
            job?.Stop();
        }
    }
}
