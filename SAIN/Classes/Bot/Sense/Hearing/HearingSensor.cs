using System;
using SAIN.Components;
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

        Vector3 EstimatedPosition = sound.Enemy.Hearing.EstimateSoundPosition(sound);
        ReactToBulletFlyBy(sound, FlyByDistance, EstimatedPosition, underFire);
        OnEnemySoundHeard?.Invoke(sound, sound.Enemy);
    }

    public void ReactToHeardSound(AISoundData sound)
    {
        Vector3 EstimatedPosition;
        if (HearingAnalysis.CheckIfSoundHeard(sound))
        {
            if (sound.IsGunShot && !ShallChaseGunshot(sound.PlayerDistance))
            {
                return;
            }
            // The sound may have become ignored since it was cached. SetHeard would drop it, so it must not feed the estimate either.
            if (HearingInput.IsIgnoringSounds(sound.IsGunShot))
            {
                return;
            }
            EstimatedPosition = sound.Enemy.Hearing.EstimateSoundPosition(sound);
            Bot.Squad.SquadInfo?.AddPointToSearch(sound.Enemy, EstimatedPosition, sound, Bot);
            OnEnemySoundHeard?.Invoke(sound, sound.Enemy);
        }
    }

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

    private void ReactToBulletFlyBy(AISoundData sound, float ProjectionPointDistance, Vector3 EstimatedPosition, bool UnderFire)
    {
        Enemy enemy = sound.Enemy;
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
        Bot.Squad.SquadInfo?.AddPointToSearch(enemy, EstimatedPosition, sound, Bot);
    }
}
