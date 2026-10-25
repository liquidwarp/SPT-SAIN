using System;
using System.Collections.Generic;
using EFT;
using EFT.Ballistics;
using EFT.InventoryLogic;
using SAIN.Components.PlayerComponentSpace;
using SAIN.Helpers;
using UnityEngine;

namespace SAIN.Components;

public class GameWorldComponent : MonoBehaviour
{
    public static bool TryGetPlayerComponent(IPlayer Player, out PlayerComponent PlayerComponent)
    {
        if (Player == null)
        {
            PlayerComponent = null;
            return false;
        }
        if (Instance == null)
        {
            PlayerComponent = null;
            return false;
        }
        PlayerSpawnTracker PlayerTracker = Instance.PlayerTracker;
        if (PlayerTracker == null)
        {
            PlayerComponent = null;
            return false;
        }
        PlayerComponent = PlayerTracker.GetPlayerComponent(Player);
        return PlayerComponent != null;
    }

    public void RegisterShot(Player Player, Shot Bullet, Item Weapon)
    {
        if (!TryGetPlayerComponent(Player, out PlayerComponent PlayerComponent))
        {
            return;
        }

        TrackedBullet tracked = _bulletPool.Count > 0 ? _bulletPool.Pop() : new TrackedBullet();
        tracked.Shooter = PlayerComponent;
        tracked.Bullet = Bullet;

        // AI players roughly in the direction this shot is going
        Vector3 PlayerLookDir = PlayerComponent.LookDirection;
        foreach (var Data in PlayerComponent.OtherPlayersData.DataDictionary)
        {
            OtherPlayerData data = Data.Value;
            PlayerComponent otherPlayerComponent = data.OtherPlayerComponent;
            if (
                otherPlayerComponent != null
                && otherPlayerComponent.IsAI
                && otherPlayerComponent.IsActive
                && Vector3.Dot(data.DistanceData.DirectionNormal, PlayerLookDir) > 0.75f
            )
            {
                tracked.PlayersToCheck.Add(data);
            }
        }

        // Checked once right away, as the bullet can finish before the next tick.
        if (TickBullet(tracked))
        {
            _trackedBullets.Add(tracked);
        }
        else
        {
            ReleaseBullet(tracked);
        }
    }

    private void TickTrackedBullets()
    {
        for (int i = _trackedBullets.Count - 1; i >= 0; i--)
        {
            TrackedBullet tracked = _trackedBullets[i];
            if (TickBullet(tracked))
            {
                continue;
            }
            int last = _trackedBullets.Count - 1;
            _trackedBullets[i] = _trackedBullets[last];
            _trackedBullets.RemoveAt(last);
            ReleaseBullet(tracked);
        }
    }

    /// <returns>True while there are still players this bullet may fly by.</returns>
    private static bool TickBullet(TrackedBullet tracked)
    {
        const float MaxFlyByDistSqr = 10 * 10;

        PlayerComponent Player = tracked.Shooter;
        Shot Bullet = tracked.Bullet;
        List<OtherPlayerData> PlayersToCheck = tracked.PlayersToCheck;
        if (PlayersToCheck.Count == 0 || Bullet.IsShotFinished || Player == null || !Player.IsActive)
        {
            return false;
        }

        Vector3 BulletPosition = Bullet.CurrentPosition;
        for (int i = PlayersToCheck.Count - 1; i >= 0; i--)
        {
            OtherPlayerData Data = PlayersToCheck[i];
            if (Data == null)
            {
                continue;
            }
            PlayerComponent otherPlayerComponent = Data.OtherPlayerComponent;
            if (otherPlayerComponent == null || !otherPlayerComponent.IsActive)
            {
                PlayersToCheck.RemoveAt(i);
                continue;
            }
            Vector3 PlayerPosition = Data.DistanceData.Position;
            float BulletDistSqr = (PlayerPosition - BulletPosition).sqrMagnitude;
            if (BulletDistSqr < MaxFlyByDistSqr)
            {
                otherPlayerComponent.RegisterFlyBy(Player, Bullet);
                PlayersToCheck.RemoveAt(i);
            }
        }
        return PlayersToCheck.Count > 0;
    }

    private void ReleaseBullet(TrackedBullet tracked)
    {
        tracked.Shooter = null;
        tracked.Bullet = null;
        tracked.PlayersToCheck.Clear();
        _bulletPool.Push(tracked);
    }

    // Pooled, so tracking a shot allocates nothing once the pool has grown to the number of bullets in flight.
    private sealed class TrackedBullet
    {
        public PlayerComponent Shooter;
        public Shot Bullet;
        public readonly List<OtherPlayerData> PlayersToCheck = [];
    }

    private readonly List<TrackedBullet> _trackedBullets = [];
    private readonly Stack<TrackedBullet> _bulletPool = new();

    public static GameWorldComponent Instance { get; private set; }
    public GameWorld GameWorld { get; private set; }
    public PlayerSpawnTracker PlayerTracker { get; private set; }
    public BotManagerComponent SAINBotController { get; private set; }
    public DoorHandler Doors { get; private set; }
    public LocationClass Location { get; private set; }
    public JobManager JobManager { get; private set; }

    public void WorldTick(float deltaTime)
    {
        float currentTime = Time.time;
        TickTrackedBullets();
        ManualUpdate(currentTime, deltaTime);
        SAINBotController.ManualUpdate(currentTime, deltaTime);
    }

    protected void ManualUpdate(float CurrentTime, float DeltaTime)
    {
        if (_activated)
        {
            Doors.ManualUpdate(CurrentTime, DeltaTime);
            Location.ManualUpdate(CurrentTime, DeltaTime);
            HashSet<PlayerComponent> players = PlayerTracker?.AlivePlayerArray;
            if (players != null && players.Count > 0)
            {
                foreach (PlayerComponent Player in players)
                {
                    if (Player != null)
                    {
                        Player.ManualUpdate(CurrentTime, DeltaTime);
                    }
                }

                TickSoundCaches(players, CurrentTime);
            }
        }
    }

    private const float _Sounds_PlayerCache_Interval = 1f / 30f;
    private const float _Sounds_BotCache_Interval = 1f / 15f;

    protected void TickSoundCaches(HashSet<PlayerComponent> PlayerComponents, float CurrentTime)
    {
        if (_Sounds_PlayerCache_Time < CurrentTime)
        {
            _Sounds_PlayerCache_Time = CurrentTime + _Sounds_PlayerCache_Interval;

            foreach (PlayerComponent Player in PlayerComponents)
            {
                if (Player != null)
                {
                    UpdatePlayerSoundCache(Player);
                }
            }
        }

        if (_Sounds_BotCache_Time < CurrentTime)
        {
            _Sounds_BotCache_Time = CurrentTime + _Sounds_BotCache_Interval;

            foreach (var playerComponent in PlayerComponents)
            {
                if (playerComponent == null)
                {
                    continue;
                }
                BotComponent Bot = playerComponent.BotComponent;
                if (Bot != null && Bot.BotOwner != null && Bot.BotOwner.BotState == EBotState.Active)
                {
                    Bot.Hearing.HearingInput.ProcessAISoundCache();
                }
            }
        }
    }

    protected static void UpdatePlayerSoundCache(PlayerComponent Player)
    {
        List<SoundEvent> events = Player.AISoundCachedEvents;
        if (Player.IsActive)
        {
            foreach (OtherPlayerData OtherPlayerData in Player.OtherPlayersData.DataHashSet)
            {
                PlayerComponent OtherPlayer = OtherPlayerData?.OtherPlayerComponent;
                if (OtherPlayer != null && OtherPlayer.IsActive && OtherPlayer.IsSAINBot)
                {
                    BotComponent Bot = OtherPlayer.BotComponent;
                    if (Bot != null && Bot.BotOwner != null && Bot.BotOwner.BotState == EBotState.Active)
                    {
                        bool InFootstepRadius = OtherPlayerData.IsInHearingRadius_Footsteps;
                        bool InGunfireRadius = OtherPlayerData.IsInHearingRadius_GunFire;
                        float Distance = OtherPlayerData.DistanceData.Distance;
                        foreach (var soundEvent in events)
                        {
                            bool isGunshot = soundEvent.IsGunShot;
                            if (isGunshot && !InGunfireRadius)
                            {
                                continue;
                            }

                            if (!isGunshot && !InFootstepRadius)
                            {
                                continue;
                            }

                            Bot.Hearing.HearingInput.CheckAddSoundToCache(soundEvent, Distance);
                        }
                    }
                }
            }
        }
        events.Clear();
    }

    public void Activate(BotsController botsController)
    {
        SAINBotController.DefaultController = botsController;
        SAINBotController.BotSpawner = botsController.BotSpawner;
        _activated = true;
        JobManager.Start();
    }

    private bool _activated = false;

    public void Init(GameWorld gameWorld, BotManagerComponent sainBotController)
    {
        Instance = this;
        GameWorld = gameWorld;
        if (GameWorld == null)
        {
#if DEBUG
            Logger.LogWarning("GameWorld Null, cannot Init SAIN Gameworld! Check 2. Disposing Component...");
#endif
            DestroyComponent();
            return;
        }

        SAINBotController = sainBotController;
        PlayerTracker = new PlayerSpawnTracker(this);
        Doors = new DoorHandler(this);
        Location = new LocationClass(this);
        JobManager = new JobManager(this);
        GameWorld.OnDispose += DestroyComponent;

        try
        {
            EFTCoreSettings.UpdateCoreSettings();
        }
        catch (Exception e)
        {
            Logger.LogError(e);
        }

        Doors.Init();
        Location.Init();
    }

    public void DestroyComponent()
    {
        Instance = null;
        try
        {
            PlayerTracker?.Dispose();
            Doors?.Dispose();
            Location?.Dispose();
            JobManager?.Dispose();
        }
        catch (Exception e)
        {
            Logger.LogError($"Dispose GameWorld Component Class Error: {e}");
        }

        try
        {
            ComponentHelpers.DestroyComponent(SAINBotController);
        }
        catch (Exception e)
        {
            Logger.LogError($"Dispose GameWorld SubComponent Error: {e}");
        }

        StopAllCoroutines();
        _trackedBullets.Clear();
        _bulletPool.Clear();
        Instance = null;
        GameWorld.OnDispose -= DestroyComponent;
        Destroy(this);
    }

    private float _Sounds_PlayerCache_Time;
    private float _Sounds_BotCache_Time;
}
