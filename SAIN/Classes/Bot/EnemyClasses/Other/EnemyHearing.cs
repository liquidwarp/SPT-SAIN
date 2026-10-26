using SAIN.Classes.Bot.Sense.Hearing;
using SAIN.Components.PlayerComponentSpace;
using SAIN.Helpers;
using SAIN.Models.Enums;
using SAIN.Models.Structs;
using SAIN.Preset.Shared.Enums;
using SAIN.SAINComponent.Classes.EnemyClasses;
using UnityEngine;

namespace SAIN.Components.BotComponentSpace.Classes.EnemyClasses;

/// <param name="position">Where the bot thinks the enemy is.</param>
/// <param name="enemyPosition">Where the enemy was when they made the last sound or were seen, to detect when they move.</param>
/// <param name="timeUpdated">When the bot last heard or saw them.</param>
/// <param name="confidence">0 to 1, grows with each sound heard. Shrinks the error toward a floor.</param>
/// <param name="confidenceBefore">Confidence before this estimate's sound was heard, so a better clue replacing it doesn't count twice.</param>
/// <param name="angleOffset">-1 to 1, fraction of the maximum direction error. Drifts a little with each sound.</param>
/// <param name="distanceOffset">-1 to 1, fraction of the maximum distance error. Drifts a little with each sound.</param>
/// <param name="soundType">The sound this estimate comes from, or null when it is where the enemy was seen.</param>
/// <param name="step">How the estimator got to this estimate, for debugging.</param>
public readonly struct SoundPositionEstimate(
    Vector3 position,
    Vector3 enemyPosition,
    float timeUpdated,
    float confidence,
    float confidenceBefore,
    float angleOffset,
    float distanceOffset,
    SAINSoundType? soundType,
    EEstimateStep step
)
{
    public readonly Vector3 Position = position;
    public readonly Vector3 EnemyPosition = enemyPosition;
    public readonly float TimeUpdated = timeUpdated;
    public readonly float Confidence = confidence;
    public readonly float ConfidenceBefore = confidenceBefore;
    public readonly float AngleOffset = angleOffset;
    public readonly float DistanceOffset = distanceOffset;
    public readonly SAINSoundType? SoundType = soundType;
    public readonly EEstimateStep Step = step;

    public bool FromSighting
    {
        get { return SoundType == null; }
    }

    public static SoundPositionEstimate Sighting(Vector3 position, float time)
    {
        return new SoundPositionEstimate(
            position,
            position,
            time,
            1f,
            1f,
            HearingPositionEstimator.RollOffset(),
            HearingPositionEstimator.RollOffset(),
            null,
            EEstimateStep.Sighting
        );
    }

    public SoundPositionEstimate With(float time, EEstimateStep step)
    {
        return new SoundPositionEstimate(
            Position,
            EnemyPosition,
            time,
            Confidence,
            ConfidenceBefore,
            AngleOffset,
            DistanceOffset,
            SoundType,
            step
        );
    }
}

public enum EEstimateStep
{
    /// <summary>The enemy is visible, or was seen more recently than heard.</summary>
    Sighting,

    /// <summary>First sound with nothing to build on.</summary>
    FirstSound,

    /// <summary>Heard where they were last seen, so the sighting stands.</summary>
    SightingHeld,

    /// <summary>A sound that gives away more replaced the last one from the same spot.</summary>
    BetterClue,

    /// <summary>Same spot heard again soon after, so the last guess stands.</summary>
    Reused,

    /// <summary>Moved after a long silence, so the error was rolled again.</summary>
    ResetAfterSilence,

    /// <summary>Moved while being heard, so the error drifted a little from the last guess.</summary>
    Drift,

    /// <summary>Same spot heard again after the reuse time, so the guess keeps its error and gains confidence.</summary>
    Refined,
}

public class EnemyHearing(EnemyData enemyData) : EnemyBase(enemyData, enemyData.Enemy.Bot)
{
    public bool Heard { get; private set; }
    public bool EnemyHeardFromPeace { get; set; }
    public float TimeSinceHeard
    {
        get { return Heard ? Time.time - _timeLastHeard : float.MaxValue; }
    }

    public SoundPositionEstimate? PositionEstimate
    {
        get { return _positionEstimate; }
    }

    private SoundPositionEstimate? _positionEstimate;

    private const float REPORT_HEARD_FREQUENCY = 1f;

    public override void Init()
    {
        Enemy.Events.OnFirstSeen += resetHeardFromPeace;
        base.Init();
    }

    public SoundPositionEstimate EstimateSoundPosition(AISoundData sound)
    {
        if (Enemy.IsVisible)
        {
            return SoundPositionEstimate.Sighting(Enemy.EnemyPosition, Time.time);
        }
        return Bot.Hearing.HearingPositionEstimator.Estimate(GetLastEstimate(), sound);
    }

    private SoundPositionEstimate? GetLastEstimate()
    {
        EnemyPlace seen = Enemy.KnownPlaces.LastSeenPlace;
        EnemyPlace squadSeen = Enemy.KnownPlaces.LastSquadSeenPlace;
        if (seen == null || (squadSeen != null && squadSeen._timeLastUpdated > seen._timeLastUpdated))
        {
            seen = squadSeen;
        }
        if (seen == null || (_positionEstimate != null && _positionEstimate.Value.TimeUpdated >= seen._timeLastUpdated))
        {
            return _positionEstimate;
        }
        return SoundPositionEstimate.Sighting(seen.Position, seen._timeLastUpdated);
    }

    public void TickEnemy(float currentTime)
    {
        if (Enemy.Seen && EnemyHeardFromPeace)
        {
            EnemyHeardFromPeace = false;
        }
    }

    private void resetHeardFromPeace(Enemy enemy)
    {
        EnemyHeardFromPeace = false;
    }

    public override void Dispose()
    {
        Enemy.Events.OnFirstSeen -= resetHeardFromPeace;
        base.Dispose();
    }

    protected override void OnEnemyKnownChanged(bool known, Enemy enemy)
    {
        if (!known)
        {
            Heard = false;
            _positionEstimate = null;
            _timeLastHeard = 0f;
            EnemyHeardFromPeace = false;
        }
    }

    public EnemyPlace SetHeard(SAINHearingReport report, float currentTime)
    {
        if (Bot.Hearing.HearingInput.IsIgnoringSounds(report.soundType.IsGunShot()))
        {
            return null;
        }

        if (report.estimate != null)
        {
            _positionEstimate = report.estimate;
        }

        if (Enemy.IsVisible)
        {
            report.position = Enemy.EnemyPosition;
        }

        Heard = true;
        bool wasGunfire = report.soundType.IsGunShot();
        Enemy.Status.HeardRecently = true;
        _timeLastHeard = Time.time;

        EnemyPlace place = UpdateHeardPosition(report, currentTime);

        if (!Bot.HasEnemy)
        {
            EnemyHeardFromPeace = true;
        }

        if (place != null)
        {
            Enemy.Events.EnemyHeard(report.soundType, report.isDanger, place);
        }

        if (wasGunfire || !report.shallReportToSquad)
        {
            return place;
        }
        updateEnemyAction(report.soundType, report.position, currentTime);
        return place;
    }

    private void updateEnemyAction(SAINSoundType soundType, Vector3 soundPosition, float currentTime)
    {
        EEnemyAction action;
        switch (soundType)
        {
            case SAINSoundType.GrenadeDraw:
            case SAINSoundType.GrenadePin:
                action = EEnemyAction.HasGrenade;
                break;

            case SAINSoundType.Reload:
            case SAINSoundType.DryFire:
                action = EEnemyAction.Reloading;
                break;

            case SAINSoundType.Looting:
                action = EEnemyAction.Looting;
                break;

            case SAINSoundType.Heal:
                action = EEnemyAction.Healing;
                break;

            case SAINSoundType.Surgery:
                action = EEnemyAction.UsingSurgery;
                break;

            default:
                action = EEnemyAction.None;
                break;
        }

        if (action != EEnemyAction.None)
        {
            Enemy.Status.SetVulnerableAction(action);
            Bot.Squad.SquadInfo.UpdateSharedEnemyStatus(EnemyPlayer, action, Bot, soundType, soundPosition, currentTime);
        }
    }

    public EnemyPlace UpdateHeardPosition(SAINHearingReport report, float currentTime)
    {
        EnemyPlace place = Enemy.KnownPlaces.UpdatePersonalHeardPosition(report, currentTime);
        if (report.shallReportToSquad && place != null && _nextReportHeardTime < currentTime)
        {
            _nextReportHeardTime = currentTime + REPORT_HEARD_FREQUENCY;
            Bot.Squad?.SquadInfo?.ReportEnemyPosition(Enemy, place, false, currentTime);
        }
        return place;
    }

    private float _nextReportHeardTime;
    private float _timeLastHeard;
}
