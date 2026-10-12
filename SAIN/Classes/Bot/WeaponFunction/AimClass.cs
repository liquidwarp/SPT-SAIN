using System;
using EFT;
using SAIN.Classes;
using SAIN.Components;
using SAIN.SAINComponent.Classes.EnemyClasses;
using UnityEngine;

namespace SAIN.SAINComponent.Classes.WeaponFunction;

public class AimClass : BotComponentClassBase, IBotClass
{
    public AimClass(BotComponent sain)
        : base(sain)
    {
        TickRequirement = ESAINTickState.OnlyNoSleep;
    }

    public event Action<bool> OnAimAllowedOrBlocked;

    public bool CanAim { get; private set; }

    public float LastAimTime { get; set; }

    public AimStatus AimStatus
    {
        get
        {
            IBotAiming aim = BotOwner.AimingManager.CurrentAiming;
            if (aim is BotAimingData aimClass)
            {
                return aimClass._status;
            }
            if (aim != null && aim.IsReady)
            {
                return AimStatus.AimComplete;
            }
            return AimStatus.NoTarget;
        }
        set
        {
            if (BotOwner != null && BotOwner.AimingManager?.CurrentAiming is BotAimingData aimClass)
            {
                aimClass._status = value;
            }
        }
    }

    public override void ManualUpdate()
    {
        checkCanAim();
        CheckLoseTarget();
        base.ManualUpdate();
    }

    public bool AimAtTarget(Vector3 shootPoint, Enemy enemy, out bool AimComplete, IBotAiming currentAiming, BotComponent bot)
    {
        BotOwner botOwner = bot.BotOwner;
        BotWeaponManager weaponManager = botOwner.WeaponManager;
        if (!weaponManager.HaveBullets || weaponManager.Reload.Reloading)
        {
            botOwner.ShootData.EndShoot();
            AimComplete = false;
            Bot.Aim.LoseAimTarget();
            return false;
        }

        Vector3 firePort = bot.Transform.WeaponData.FirePort;
        Vector3 ballisticOffset = PlayerMovementController.Util.CalculateBallisticOffset(
            firePort,
            shootPoint,
            enemy.EnemyPlayer.Velocity,
            bot.PlayerComponent.Equipment.CurrentWeaponInfo.BulletSpeed
        );
        Vector3 aimPoint = shootPoint + ballisticOffset;

        var smoother = enemy.PositionSmoother;

        // A different enemy, or a gap where SAIN wasn't aiming (brain layer change, flashed, AI limit), is a new
        // engagement even if the aim status was never cleared, so precision built earlier doesn't carry over.
        bool newEngagement =
            AimStatus == AimStatus.NoTarget || enemy != _lastAimEnemy || Time.time - _lastAimAtTargetTime > ENGAGEMENT_GAP_TIME;
        _lastAimAtTargetTime = Time.time;
        if (newEngagement)
        {
            // Restart the aim too, so it gets a fresh aim time and offset rather than the leftover ones.
            LoseAimTarget();
            smoother.Snap(aimPoint);
            _lastAimEnemy = enemy;
        }

        // Feed desired aim point to the smoother to account for enemy movement.
        smoother.Update(aimPoint, enemy.EnemyPlayer.Velocity, Time.deltaTime);

        // Input the final aim point to EFT's bot aim system.
        currentAiming.SetTarget(smoother.Position);

        var aimingData = currentAiming as BotAimingData;
        if (newEngagement && aimingData != null)
        {
            StartEngagementPrecision(aimingData);
        }

        if (
            !bot.FriendlyFire.UpdateFriendlyFireStatus(
                currentAiming.LastDist2Target,
                bot.Transform.WeaponData.FirePort,
                bot.Transform.WeaponData.PointDirection,
                bot
            )
        )
        {
            botOwner.ShootData.EndShoot();
            AimComplete = false;
            Bot.Aim.LoseAimTarget();
            return false;
        }
        currentAiming.NodeUpdate();
        if (aimingData != null)
        {
            UpdateEngagementPrecision(aimingData);
        }
        Bot.Steering.LookToPoint(currentAiming.EndTargetPoint);
        AimComplete =
            currentAiming.IsReady
            && (botOwner.ShootData.Shooting || Bot.Steering.IsLookingAtPoint(currentAiming.EndTargetPoint, out float dot, 0.85f));
        return true;
    }

    private const float ENGAGEMENT_GAP_TIME = 0.5f;

    private Enemy _lastAimEnemy;
    private float _lastAimAtTargetTime = -1f;

    private float _engagementStartTime = -1f;
    private float _engagementStartCoef = 1f;

    // Aim precision starts at START_TIME_COEF when an engagement begins and tightens over the whole engagement,
    // instead of restarting on every mid-fight recalc (trigger count, target moved).
    private void StartEngagementPrecision(BotAimingData aimingData)
    {
        var settings = aimingData.AimingSettings;
        _engagementStartTime = Time.time;
        _engagementStartCoef = Mathf.Clamp(settings.START_TIME_COEF, settings.MAX_AIMING_UPGRADE_BY_TIME, 1f);
        aimingData._timeCoef = _engagementStartCoef;
    }

    // Replaces the coefficient vanilla NodeUpdate derives from the time since the last recalc.
    private void UpdateEngagementPrecision(BotAimingData aimingData)
    {
        if (_engagementStartTime < 0f || aimingData._status != AimStatus.AimComplete)
        {
            return;
        }
        var settings = aimingData.AimingSettings;
        float tightening = (Time.time - _engagementStartTime) * BotOwner.Settings.Current.CurrentPrecicingSpeed / settings.MAX_AIM_PRECICING;
        aimingData._timeCoef = Mathf.Clamp(_engagementStartCoef - tightening, settings.MAX_AIMING_UPGRADE_BY_TIME, 1f);
    }

    private void checkCanAim()
    {
        bool couldAim = CanAim;
        CanAim = canAim();
        if (couldAim != CanAim)
        {
            OnAimAllowedOrBlocked?.Invoke(CanAim);
        }
    }

    private bool canAim()
    {
        if (Player.IsSprintEnabled)
        {
            return false;
        }
        return true;
    }

    public void LoseAimTarget()
    {
        if (BotOwner.AimingManager.CurrentAiming is BotAimingData aimClass && aimClass._status != AimStatus.NoTarget)
        {
            aimClass._status = AimStatus.NoTarget;
        }
    }

    private void CheckLoseTarget()
    {
        var weaponManager = BotOwner.WeaponManager;
        if (!CanAim || !weaponManager.HaveBullets || weaponManager.Reload.Reloading)
        {
            LoseAimTarget();
            BotOwner.ShootData?.EndShoot();
        }
    }
}
