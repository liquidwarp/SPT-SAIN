using EFT;
using SAIN.Components;
using SAIN.SAINComponent.Classes.EnemyClasses;
using UnityEngine;

namespace SAIN.SAINComponent.Classes;

public class BotSurgery : BotBase
{
    public BotSurgery(BotComponent bot)
        : base(bot) { }

    public float SurgeryStartTime { get; private set; }
    public float LastSurgeryActivityTime { get; private set; }

    public bool AreaClearForSurgery { get; private set; }

    public bool CheckAreaClearForSurgery()
    {
        AreaClearForSurgery = (IsSurgeryInProgress() || IsWaitingForNextSurgery() || CheckCanStartUsingKit()) && CheckEnemies();
        return AreaClearForSurgery;
    }

    public void OnSurgeryStarted()
    {
        SurgeryStartTime = Time.time;
        LastSurgeryActivityTime = Time.time;
    }

    public void OnSurgeryDone()
    {
        LastSurgeryActivityTime = Time.time;
        BotOwner botOwner = BotOwner;
        var surgicalKit = botOwner == null ? null : botOwner.Medecine?.SurgicalKit;
        _waitForNextSurgeryUntil = surgicalKit?.HaveWork == true ? surgicalKit._nextPosibleUseTime + NEXT_SURGERY_GRACE : 0f;
    }

    private bool IsWaitingForNextSurgery()
    {
        if (_waitForNextSurgeryUntil < Time.time)
        {
            return false;
        }
        BotOwner botOwner = BotOwner;
        if (botOwner == null)
        {
            return false;
        }
        var surgicalKit = botOwner.Medecine?.SurgicalKit;
        return surgicalKit?.HaveWork == true && !surgicalKit.Using;
    }

    private bool IsSurgeryInProgress()
    {
        BotOwner botOwner = BotOwner;
        if (botOwner == null)
        {
            return false;
        }
        return botOwner.Medecine?.SurgicalKit?.Using == true;
    }

    public void CancelSurgery()
    {
        BotOwner botOwner = BotOwner;
        if (botOwner == null)
        {
            return;
        }
        botOwner.Medecine?.SurgicalKit?.CancelCurrent();
    }

    public bool CheckCanStartUsingKit()
    {
        BotOwner botOwner = BotOwner;
        if (botOwner == null)
        {
            return false;
        }
        if (_nextAllowedStartTime > Time.time)
        {
            return false;
        }
        var medecine = botOwner.Medecine;
        return medecine?.FirstAid?.IsBleeding == false && medecine.SurgicalKit?.ShallStartUse() == true;
    }

    public void OnSurgeryFailedToStart()
    {
        _nextAllowedStartTime = Time.time + FAILED_START_COOLDOWN;
    }

    private const float FAILED_START_COOLDOWN = 10f;

    private const float NEXT_SURGERY_GRACE = 1f;
    private float _nextAllowedStartTime;
    private float _waitForNextSurgeryUntil;

    private bool CheckEnemies()
    {
        if (_nextCheckEnemiesTime < Time.time)
        {
            _nextCheckEnemiesTime = Time.time + 0.1f;
            const float minPathDist = 80f;
            const float minTimeSinceLastKnown = 60f;
            const float minTimeSinceSeen = 60f;

            _allClear = true;
            EnemyList enemies = Bot.EnemyController.KnownEnemies;
            foreach (Enemy enemy in enemies)
            {
                if (enemy.IsVisible)
                {
                    _allClear = false;
                    break;
                }
                if (enemy.Seen && enemy.TimeSinceSeen < minTimeSinceSeen)
                {
                    _allClear = false;
                    break;
                }
                if (enemy.TimeSinceLastKnownUpdated < minTimeSinceLastKnown)
                {
                    _allClear = false;
                    break;
                }
                if (enemy.Path.PathLength < minPathDist)
                {
                    _allClear = false;
                    break;
                }
            }
        }
        return _allClear;
    }

    private bool _allClear;
    private float _nextCheckEnemiesTime;
}
