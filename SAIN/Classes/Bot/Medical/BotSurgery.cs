using EFT;
using SAIN.Components;
using SAIN.SAINComponent.Classes.EnemyClasses;
using UnityEngine;

namespace SAIN.SAINComponent.Classes;

public class BotSurgery : BotBase
{
    public BotSurgery(BotComponent bot)
        : base(bot) { }

    public bool SurgeryStarted
    {
        get { return _surgeryStarted; }
        set
        {
            if (_surgeryStarted != value && value)
            {
                SurgeryStartTime = Time.time;
            }
            _surgeryStarted = value;
        }
    }

    public float SurgeryStartTime { get; private set; }

    private bool _surgeryStarted;

    public bool AreaClearForSurgery { get; private set; }

    public bool CheckAreaClearForSurgery()
    {
        return CheckCanStartUsingKit() && CheckEnemies();
    }

    public bool CheckCanStartUsingKit()
    {
        BotOwner botOwner = BotOwner;
        if (botOwner == null)
        {
            return false;
        }
        var medecine = botOwner.Medecine;
        return medecine?.FirstAid?.IsBleeding == false && medecine.SurgicalKit?.ShallStartUse() == true;
    }

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
