using EFT;
using SAIN.Components;
using SAIN.Preset.Shared.Enums;
using SAIN.Preset.Shared.GlobalSettings;
using SAIN.SAINComponent.Classes.EnemyClasses;
using UnityEngine;

namespace SAIN.SAINComponent.Classes.Mover;

public class ProneClass : BotBase
{
    private float _nextChangeProneTime { get; set; }

    public ProneClass(BotComponent sain)
        : base(sain) { }

    public void SetProne(bool value)
    {
        BotOwner.BotLay.IsLay = value;
    }

    public bool ShallProneHide(Enemy enemy, float mindist = 10f)
    {
        if (enemy == null)
        {
            return false;
        }
        if (!Bot.Info.FileSettings.Move.PRONE_TOGGLE || !GlobalSettingsClass.Instance.Move.PRONE_TOGGLE)
        {
            return false;
        }
        if (_nextChangeProneTime > Time.time)
        {
            return Player.IsInPronePose;
        }

        if (Bot.Decision.CurrentSelfDecision == ESelfActionType.None)
        {
            return false;
        }

        if (!Player.MovementContext.CanProne)
        {
            return false;
        }
        if (enemy.KnownPlaces.BotDistanceFromLastKnown < mindist)
        {
            return false;
        }
        if (enemy.IsVisible)
        {
            return true;
        }
        return Player.IsInPronePose;

        //// bool isUnderDuress = Bot.Decision.CurrentSelfDecision != ESelfActionType.None || Bot.Suppression.IsHeavySuppressed;
    }
}
