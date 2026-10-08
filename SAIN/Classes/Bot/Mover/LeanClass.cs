using System;
using System.Linq;
using EFT;
using SAIN.Components;
using SAIN.Preset.Shared.Enums;
using SAIN.Preset.Shared.GlobalSettings;
using SAIN.SAINComponent.Classes.EnemyClasses;
using UnityEngine;

namespace SAIN.SAINComponent.Classes.Mover;

public class LeanClass : BotBase
{
    private const float LEAN_UPDATE_FOUND_FREQ = 0.33f;
    private const float LEAN_UPDATE_NOT_FOUND_FREQ = 0.25f;
    private const float LEAN_RAYCAST_OFFSET_DIST = 0.66f;
    private const float LEAN_MAX_RAYCAST_DIST = 16f;
    private const float RESET_LEAN_AFTER_TIME = 0.66f;
    private const float MAX_CORNER_DISTANCE_LEAN = 12f;
    private const float MAX_CORNER_DISTANCE_LEAN_SQR = MAX_CORNER_DISTANCE_LEAN * MAX_CORNER_DISTANCE_LEAN;

    public LeanSetting LeanDirection { get; private set; }
    public LeanSetting LastLeanDirection { get; private set; }
    public bool CanLeanByState { get; private set; }
    public bool IsRaycastLeaning { get; private set; }
    public SmoothDampenedFloat LeanAngleValue { get; } = new(0.2f);

    public LeanClass(BotComponent sain)
        : base(sain)
    {
        TickInterval = 1f / 20f;
    }

    private static readonly ECombatDecision[] DontLean = [ECombatDecision.Retreat, ECombatDecision.RunAway, ECombatDecision.MeleeAttack];

    public override void ManualUpdate()
    {
        float time = Time.time;
        UpdateLeanSetting(time);
        SetTilt();
    }

    private void SetTilt()
    {
        if (Player.IsSprintEnabled)
        {
            Player.MovementContext.SetTilt(0);
            LeanAngleValue.Set(0);
            LeanAngleValue.Get(Time.fixedDeltaTime);
        }
        else
        {
            var num = LeanDirection switch
            {
                LeanSetting.Left => -5f,
                LeanSetting.Right => 5f,
                _ => 0f,
            };
            LeanAngleValue.Set(num);
            float tiltValue = LeanAngleValue.Get(Time.fixedDeltaTime);
            Player.MovementContext.SetTilt(tiltValue);
        }
    }

    public void FastLean(LeanSetting value)
    {
        if (value != LeanSetting.None)
        {
            _timeLastLeaned = Time.time;
        }

        if (LeanDirection != value)
        {
            LastLeanDirection = LeanDirection;
            LeanDirection = value;
        }
    }

    private void UpdateLeanSetting(float time)
    {
        if (ShallTick(time))
        {
            CanLeanByState = CheckCanLeanByState(out bool resetLean);
            if (CanLeanByState)
            {
                if (_leanTimer < Time.time)
                {
                    var enemy = Bot.GoalEnemy;
                    FindLean(enemy);
                    float timeAdd = LeanDirection == LeanSetting.None ? LEAN_UPDATE_NOT_FOUND_FREQ : LEAN_UPDATE_FOUND_FREQ;
                    _leanTimer = Time.time + timeAdd;
                }
                return;
            }
            if (resetLean)
            {
                ResetLean();
            }
        }
    }

    private bool CheckCanLeanByState(out bool resetLean)
    {
        resetLean = true;
        if (!Bot.Info.FileSettings.Move.LEAN_TOGGLE || !GlobalSettingsClass.Instance.Move.LEAN_TOGGLE)
        {
            return false;
        }
        if (!Bot.SAINLayersActive && !Bot.HasEnemy)
        {
            return false;
        }
        if (Bot.Mover.Running)
        {
            return false;
        }
        var CurrentDecision = Bot.Decision.CurrentCombatDecision;
        var enemy = Bot.GoalEnemy;
        if (enemy == null || DontLean.Contains(CurrentDecision) || Bot.Suppression.IsHeavySuppressed)
        {
            return false;
        }
        if (IsHoldingLean)
        {
            resetLean = false;
            return false;
        }
        if (enemy.IsVisible && Bot.Decision.CurrentSelfDecision != ESelfActionType.None)
        {
            return false;
        }
        if (GlobalSettingsClass.Instance.General.AILimit.LimitAIvsAIGlobal && enemy.IsAI && Bot.CurrentAILimit != AILimitSetting.None)
        {
            return false;
        }
        if (Bot.Cover.CoverInUse != null)
        {
            resetLean = false;
            return false;
        }
        return true;
    }

    private void FindLean(Enemy enemy)
    {
        IsRaycastLeaning = false;
        DirectLineOfSight = false;

        var lastKnownPlace = enemy.KnownPlaces.LastKnownPlace;
        if (lastKnownPlace == null)
        {
            FastLean(LeanSetting.None);
            return;
        }

        var blindCornerLean = FindLeanFromBlindCornerAngle(enemy, 1);
        FastLean(blindCornerLean);
    }

    public LeanSetting FindLeanFromBlindCornerAngle(Enemy enemy, float minAngle = -1f)
    {
        if (enemy == null)
        {
            return LeanSetting.None;
        }
        var blindCorner = enemy.VisiblePathPoint;
        if (blindCorner == null)
        {
            return LeanSetting.None;
        }
        float? signedAngle = enemy.VisiblePathPointSignedAngle;
        if (signedAngle == null)
        {
            return LeanSetting.None;
        }
        if (minAngle > 0f && Mathf.Abs(signedAngle.Value) < minAngle)
        {
            return LeanSetting.None;
        }
        Vector3 direction = blindCorner.Value - Bot.Position;
        if (direction.sqrMagnitude > MAX_CORNER_DISTANCE_LEAN_SQR)
        {
            return LeanSetting.None;
        }

        LeanSetting result = signedAngle > 0 ? LeanSetting.Left : LeanSetting.Right;
        return result;
    }

    private float _stopHoldLeanTime;

    public bool IsHoldingLean
    {
        get { return _stopHoldLeanTime > Time.time; }
    }

    public void HoldLean(float duration)
    {
        if (LeanDirection != LeanSetting.None)
        {
            _stopHoldLeanTime = Time.time + duration;
        }
    }

    public void ResetHoldLean()
    {
        _stopHoldLeanTime = 0;
    }

    private float _leanTimer = 0f;

    public void ResetLean()
    {
        FastLean(LeanSetting.None);
    }

    public bool DirectLineOfSight { get; set; }

    private float _timeLastLeaned;
}
