using System;
using SAIN.Components;
using SAIN.Components.BotComponentSpace.Classes.EnemyClasses;
using SAIN.Components.PlayerComponentSpace;
using SAIN.Helpers;
using SAIN.SAINComponent;
using SAIN.SAINComponent.Classes.EnemyClasses;
using UnityEngine;

namespace SAIN.Classes.Bot.Sense.Hearing;

public class HearingSensor : BotComponentClassBase
{
    public event Action<AISoundData, Enemy> OnEnemySoundHeard;

    public HearingInput HearingInput { get; }
    public HearingAnalysis HearingAnalysis { get; }
    public HearingPositionEstimator HearingPositionEstimator { get; }

    public HearingSensor(BotComponent sain)
        : base(sain)
    {
        TickRequirement = ESAINTickState.OnlyNoSleep;
        HearingInput = new HearingInput(this);
        HearingAnalysis = new HearingAnalysis(this);
        HearingPositionEstimator = new HearingPositionEstimator(this);
    }

    public override void Init()
    {
        HearingInput.Init();
        HearingAnalysis.Init();
        HearingPositionEstimator.Init();
        base.Init();
    }

    public override void ManualUpdate()
    {
        HearingInput.ManualUpdate();
        HearingAnalysis.ManualUpdate();
        HearingPositionEstimator.ManualUpdate();
        base.ManualUpdate();
    }

    public override void Dispose()
    {
        HearingInput.Dispose();
        HearingAnalysis.Dispose();
        HearingPositionEstimator.Dispose();
        base.Dispose();
    }

    public void ReactToBulletFlyBy(AISoundData sound, float FlyByDistance)
    {
        bool underFire = FlyByDistance <= SAINPlugin.LoadedPreset.GlobalSettings.Mind.MaxUnderFireDistance;
        if (HearingInput.IsIgnoringSounds(underFire))
        {
            return;
        }

        AISoundData placedFrom = TakeUnchasedGunshot(sound.Enemy) ?? sound;
        SoundPositionEstimate estimate = sound.Enemy.Hearing.EstimateSoundPosition(placedFrom);
        ReactToBulletFlyBy(sound, FlyByDistance, estimate, underFire);
        OnEnemySoundHeard?.Invoke(sound, sound.Enemy);
    }

    public void ReactToHeardSound(AISoundData sound)
    {
        if (HearingAnalysis.CheckIfSoundHeard(sound))
        {
            if (sound.IsGunShot && !ShallChaseGunshot(sound.PlayerDistance) && !WasJustShotAtBy(sound.Enemy))
            {
                if (HearingAnalysis.ShallLogHearing(sound))
                {
                    HearingAnalysis.LogHearing(sound, "Not chasing: gunshot is too far away for this personality");
                }
                _unchasedGunshot = sound;
                _timeUnchasedGunshot = Time.time;
                return;
            }
            SoundPositionEstimate estimate = sound.Enemy.Hearing.EstimateSoundPosition(sound);
            Bot.Squad.SquadInfo?.AddPointToSearch(sound.Enemy, estimate, sound, Bot);
            OnEnemySoundHeard?.Invoke(sound, sound.Enemy);
        }
    }

    private AISoundData? TakeUnchasedGunshot(Enemy enemy)
    {
        if (_unchasedGunshot == null)
        {
            return null;
        }
        AISoundData gunshot = _unchasedGunshot.Value;
        if (Time.time - _timeUnchasedGunshot >= SAINPlugin.LoadedPreset.GlobalSettings.Hearing.HEAR_REPLACE_TIME)
        {
            _unchasedGunshot = null;
            return null;
        }
        if (gunshot.Enemy != enemy)
        {
            return null;
        }
        _unchasedGunshot = null;
        return gunshot;
    }

    public AISoundData? UnchasedGunshot
    {
        get { return _unchasedGunshot; }
    }

    public float TimeUnchasedGunshot
    {
        get { return _timeUnchasedGunshot; }
    }

    private AISoundData? _unchasedGunshot;
    private float _timeUnchasedGunshot;

    private bool ShallChaseGunshot(float Distance)
    {
        var searchSettings = Bot.Info.PersonalitySettings.Search;
        if (searchSettings.WillChaseDistantGunshots)
        {
            return true;
        }
        if (Distance > searchSettings.AudioStraightDistanceToIgnore)
        {
            return false;
        }
        return true;
    }

    private static bool WasJustShotAtBy(Enemy enemy)
    {
        return Time.time - enemy.Status.TimeLastShotAtMe < SAINPlugin.LoadedPreset.GlobalSettings.Hearing.HEAR_REPLACE_TIME;
    }

    private void ReactToBulletFlyBy(AISoundData sound, float ProjectionPointDistance, SoundPositionEstimate estimate, bool UnderFire)
    {
        Enemy enemy = sound.Enemy;
        Vector3 EstimatedPosition = estimate.Position;
        if (UnderFire)
        {
            if (BotOwner != null)
            {
                BotOwner.HearingSensor?.OnEnemySounHearded?.Invoke(EstimatedPosition, sound.PlayerDistance, sound.SoundType.Convert());
            }
            Bot.Memory.SetUnderFire(enemy, EstimatedPosition);
            enemy.SetEnemyAsSniper(enemy.RealDistance > Bot.Info.PersonalitySettings.General.ENEMYSNIPER_DISTANCE);
        }
        Bot.Suppression.CheckAddSuppression(enemy, ProjectionPointDistance);
        enemy.Status.RegisterEnemyFlyBy();
        Bot.Squad.SquadInfo?.AddPointToSearch(enemy, estimate, sound, Bot);
    }
}
