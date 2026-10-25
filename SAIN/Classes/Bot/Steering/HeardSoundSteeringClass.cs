using SAIN.Preset.Shared.Enums;
using SAIN.SAINComponent.Classes.EnemyClasses;
using UnityEngine;

namespace SAIN.SAINComponent.Classes.Mover;

public struct SoundStruct
{
    private const float TIME_TO_LOOK = 3f;
    private const float TIME_TO_CLEAR = 10f;

    public SoundStruct(Enemy enemy, EnemyPlace place)
    {
        Enemy = enemy;
        Place = place;
    }

    public readonly Enemy Enemy;

    public readonly EnemyPlace Place;

    public float TimeSinceHeard
    {
        get { return Place.TimeSincePositionUpdated; }
    }

    public Vector3 Position
    {
        get { return Place.Position; }
    }

    public bool ShallLook
    {
        get { return clearForLook && TimeSinceHeard < TIME_TO_LOOK && Place.Visible; }
    }

    private bool clearForLook
    {
        get { return Place != null && Enemy != null && Enemy.WasValid; }
    }

    public bool ShallClear
    {
        get { return !clearForLook || TimeSinceHeard >= TIME_TO_CLEAR; }
    }
}

public class HeardSoundSteeringClass : BotSubClass<SAINSteeringClass>, IBotClass
{
    public SoundStruct? LastHeardDanger { get; private set; }

    public HeardSoundSteeringClass(SAINSteeringClass steering)
        : base(steering) { }

    public override void Init()
    {
        Bot.EnemyController.Events.OnEnemyHeard += enemyHeard;
        base.Init();
    }

    public override void ManualUpdate()
    {
        checkClearPlaces();
        base.ManualUpdate();
    }

    private void checkClearPlaces()
    {
        if (_nextCheckClearTime < Time.time)
        {
            _nextCheckClearTime = Time.time + CHECK_CLEAR_FREQ;
            if (LastHeardDanger?.ShallClear == true)
            {
                clearPlace(LastHeardDanger.Value.Place);
            }
        }
    }

    private float _nextCheckClearTime;
    private const float CHECK_CLEAR_FREQ = 1f;

    private void clearPlace(EnemyPlace place)
    {
        if (place == null)
        {
            return;
        }
        if (LastHeardDanger?.Place == place)
        {
            LastHeardDanger.Value.Place.OnDispose -= clearPlace;
            LastHeardDanger = null;
        }
    }

    public override void Dispose()
    {
        Bot.EnemyController.Events.OnEnemyHeard -= enemyHeard;
        base.Dispose();
    }

    public void LookToHeardPosition()
    {
        var heardSound = LastHeardDanger;
        if (heardSound?.ShallLook == true)
        {
            BaseClass.LookToFloorPoint(heardSound.Value.Position);
            return;
        }
        BaseClass.LookToRandomPosition();
    }

    private void enemyHeard(Enemy enemy, SAINSoundType soundType, bool isDanger, EnemyPlace place)
    {
        if (place == null || !isDanger || place.PlaceData.Owner != Bot)
        {
            return;
        }
        LastHeardDanger = new SoundStruct(enemy, place);
    }
}
