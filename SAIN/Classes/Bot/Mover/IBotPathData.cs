using System;
using SAIN.Models.Enums;
using UnityEngine.AI;

namespace SAIN.SAINComponent.Classes.Mover;

public interface IBotPathData : IDisposable
{
    public bool Moving { get; }
    public bool Running { get; }

    /// <summary>
    /// Reasons for sprinting or not if the bot wants to sprint
    /// </summary>
    public EBotSprintStatus CurrentSprintStatus { get; }

    /// <summary>
    /// Whether the path finder is currently trying to sprint or not.
    /// </summary>
    public bool WantToSprint { get; }

    public void RequestStartSprint(ESprintUrgency urgency, string reason);
    public void RequestEndSprint(ESprintUrgency urgency, string reason);

    public string SprintReason { get; }

    public bool Crawling { get; }
    public CornerMoveData CurrentCornerMoveData { get; }
    public EBotMoveStatus Status { get; }
    public float CurrentCornerDistanceSqr { get; }

    public NavMeshPathStatus PathStatus { get; }
    public void Pause(float duration);
    public void UnPause();
    public void Cancel(float delay = 0.25f);

    public BotPathCorner GetCurrentCorner();
    public BotPathCorner GetLastCorner();
}
