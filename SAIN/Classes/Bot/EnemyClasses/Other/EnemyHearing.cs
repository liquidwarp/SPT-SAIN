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
/// <param name="angleOffset">-1 to 1, fraction of the maximum direction error. Drifts a little with each sound.</param>
/// <param name="distanceOffset">-1 to 1, fraction of the maximum distance error. Drifts a little with each sound.</param>
/// <param name="fromSighting">The position is where the enemy was seen.</param>
public readonly struct SoundPositionEstimate(
    Vector3 position,
    Vector3 enemyPosition,
    float timeUpdated,
    float confidence,
    float angleOffset,
    float distanceOffset,
    bool fromSighting
)
{
    public readonly Vector3 Position = position;
    public readonly Vector3 EnemyPosition = enemyPosition;
    public readonly float TimeUpdated = timeUpdated;
    public readonly float Confidence = confidence;
    public readonly float AngleOffset = angleOffset;
    public readonly float DistanceOffset = distanceOffset;
    public readonly bool FromSighting = fromSighting;

    public static SoundPositionEstimate Sighting(Vector3 position, float time)
    {
        return new SoundPositionEstimate(position, position, time, 1f, Vector.RandomRange(1f), Vector.RandomRange(1f), true);
    }

    public SoundPositionEstimate WithTime(float time)
    {
        return new SoundPositionEstimate(Position, EnemyPosition, time, Confidence, AngleOffset, DistanceOffset, FromSighting);
    }
}

public class EnemyHearing(EnemyData enemyData) : EnemyBase(enemyData, enemyData.Enemy.Bot)
{
    public bool Heard { get; private set; }
    public bool EnemyHeardFromPeace { get; set; }
    public float TimeSinceHeard
    {
        get { return Heard ? Time.time - _timeLastHeard : float.MaxValue; }
    }

    public SoundPositionEstimate? PositionEstimate { get; private set; }

    private const float REPORT_HEARD_FREQUENCY = 1f;

    public override void Init()
    {
        Enemy.Events.OnFirstSeen += resetHeardFromPeace;
        Enemy.Events.OnPositionUpdated += anchorToSighting;
        base.Init();
    }

    /// <summary>
    /// Where the bot thinks the enemy is from this sound, given everything heard and seen before.
    /// </summary>
    public Vector3 EstimateSoundPosition(AISoundData sound)
    {
        SoundPositionEstimate estimate = Bot.Hearing.HearingPositionEstimator.Estimate(PositionEstimate, sound);
        PositionEstimate = estimate;
        return estimate.Position;
    }

    // Seeing the enemy, or a squadmate reporting a sighting, beats any guess: the next sounds are placed from where they were seen.
    private void anchorToSighting(Enemy enemy, EnemyPlace place)
    {
        if (place.PlaceType == EEnemyPlaceType.Vision)
        {
            PositionEstimate = SoundPositionEstimate.Sighting(place.Position, Time.time);
        }
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
        Enemy.Events.OnPositionUpdated -= anchorToSighting;
        base.Dispose();
    }

    protected override void OnEnemyKnownChanged(bool known, Enemy enemy)
    {
        if (!known)
        {
            Heard = false;
            PositionEstimate = null;
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
