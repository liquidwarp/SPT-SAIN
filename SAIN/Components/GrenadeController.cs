using System;
using System.Collections.Generic;
using System.Reflection;
using Comfort.Common;
using EFT;
using HarmonyLib;
using SAIN.Components.PlayerComponentSpace;
using SAIN.Helpers;
using SAIN.Preset.Shared.Enums;
using UnityEngine;

namespace SAIN.Components;

public class GrenadeController(BotManagerComponent controller) : BotManagerBase(controller)
{
    public event Action<Grenade, float> OnGrenadeCollision;

    public event Action<Grenade, Vector3, string> OnGrenadeThrown;

    public event Action<Grenade, Vector3> OnGrenadeDangerUpdated;

    public void Subscribe(GlobalEventDispatcher eventHandler)
    {
        eventHandler.OnGrenadeThrow += GrenadeThrown;
        eventHandler.OnGrenadeExplosive += GrenadeExplosion;
    }

    public void UnSubscribe(GlobalEventDispatcher eventHandler)
    {
        eventHandler.OnGrenadeThrow -= GrenadeThrown;
        eventHandler.OnGrenadeExplosive -= GrenadeExplosion;
    }

    public void GrenadeCollided(Grenade grenade, float maxRange)
    {
        OnGrenadeCollision?.Invoke(grenade, maxRange);
    }

    private void GrenadeExplosion(
        Vector3 explosionPosition,
        string playerProfileID,
        bool isSmoke,
        float smokeRadius,
        float smokeLifeTime,
        int throwableId
    )
    {
        if (!Singleton<GlobalEventDispatcher>.Instantiated || playerProfileID == null)
        {
            return;
        }

        Player player = GameWorld.GetAlivePlayerByProfileID(playerProfileID);
        if (player != null)
        {
            Singleton<GlobalEventDispatcher>.Instance.PlaySound(player, explosionPosition, isSmoke ? 50f : 200f, AISoundType.gun);
            if (isSmoke)
            {
                float radius = smokeRadius * HelpersGClass.SMOKE_GRENADE_RADIUS_COEF;
                Vector3 position = player.Position;

                if (BotController.DefaultController != null)
                {
                    foreach (var keyValuePair in BotController.DefaultController.Groups())
                    {
                        foreach (BotsGroup botGroupClass in keyValuePair.Value.GetGroups(true))
                        {
                            botGroupClass.AddSmokePlace(explosionPosition, smokeLifeTime, radius, position);
                        }
                    }

                    if (BotController.DefaultController.BotSmokesVisionSystem != null)
                    {
                        BotController.DefaultController.BotSmokesVisionSystem.TryAddGrenade(throwableId, explosionPosition);
                    }
                }
            }
        }
    }

    private void GrenadeThrown(Grenade grenade, Vector3 position, Vector3 force, float mass)
    {
        if (grenade == null || ActiveGrenades.ContainsKey(grenade))
        {
            return;
        }

        Player player = GameWorld.GetAlivePlayerByProfileID(grenade.ProfileId);
        if (player == null)
        {
            Logger.LogError($"Player Null from ID {grenade.ProfileId}");
            return;
        }
        if (!player.HealthController.IsAlive)
        {
            return;
        }

        Vector3 dangerPoint = Vector.DangerPoint(position, force, mass);
        OnGrenadeThrown?.Invoke(grenade, dangerPoint, grenade.ProfileId);
        if (GameWorldComponent.TryGetPlayerComponent(player, out PlayerComponent playerComponent))
        {
            BotController.BotHearing.PlayAISound(playerComponent, SAINSoundType.GrenadeThrow, grenade.transform.position, 20f, 1f);

            List<PlayerComponent> relevantPlayers = _playerListPool.Count > 0 ? _playerListPool.Pop() : [];
            foreach (var otherPlayer in playerComponent.OtherPlayersData.DataDictionary.Values)
            {
                PlayerComponent otherPlayerComponent = otherPlayer.OtherPlayerComponent;
                if (otherPlayer.DistanceData.Distance < 125f && otherPlayerComponent != null && otherPlayerComponent.IsSAINBot)
                {
                    relevantPlayers.Add(otherPlayerComponent);
                }
            }
            ActiveGrenades[grenade] = relevantPlayers;

            Rigidbody rigidbody = (Rigidbody)_rigidBodyField.GetValue(grenade);
#if DEBUG
            if (rigidbody == null)
            {
                Logger.LogError("RigidBody Null");
            }
#endif
            _onGrenadeDestroyed ??= grenadeDestroyed;
            grenade.DestroyEvent += _onGrenadeDestroyed;
            _trackedGrenades.Add(new TrackedGrenade(grenade, rigidbody));
        }
    }

    public void Update()
    {
        for (int i = _trackedGrenades.Count - 1; i >= 0; i--)
        {
            TrackedGrenade tracked = _trackedGrenades[i];
            Grenade grenade = tracked.Grenade;
            if (grenade == null)
            {
                RemoveTrackedGrenade(i);
                continue;
            }
            Rigidbody rigidbody = tracked.Rigidbody;
            if (rigidbody == null)
            {
                continue;
            }

            Vector3 Velocity = rigidbody.velocity;
            if (Velocity.magnitude < 0.1f)
            {
                OnGrenadeDangerUpdated?.Invoke(grenade, grenade.transform.position);
            }
            else if (Velocity.y < 0)
            {
                Vector3 VelocityNormal = Velocity.normalized;
                if (
                    Vector3.Dot(VelocityNormal, Vector3.down) > 0.5f
                    && Physics.Raycast(
                        grenade.transform.position,
                        VelocityNormal,
                        out RaycastHit Hit,
                        5,
                        LayersMaskController.HighPolyWithTerrainMask
                    )
                )
                {
                    OnGrenadeDangerUpdated?.Invoke(grenade, Hit.point);
                }
            }
        }
    }

    /// <summary>
    /// SAIN bots within 125 meters of each grenade's thrower, by grenade, until the grenade is destroyed.
    /// The lists are pooled and reused for later grenades, so don't keep one after its grenade is gone.
    /// </summary>
    public readonly Dictionary<Throwable, List<PlayerComponent>> ActiveGrenades = [];

    private readonly Stack<List<PlayerComponent>> _playerListPool = new();

    private void grenadeDestroyed(Throwable grenade)
    {
        if (ActiveGrenades.TryGetValue(grenade, out List<PlayerComponent> relevantPlayers))
        {
            ActiveGrenades.Remove(grenade);
            relevantPlayers.Clear();
            _playerListPool.Push(relevantPlayers);
        }
        for (int i = 0; i < _trackedGrenades.Count; i++)
        {
            if (_trackedGrenades[i].Grenade == grenade)
            {
                RemoveTrackedGrenade(i);
                break;
            }
        }
    }

    private void RemoveTrackedGrenade(int index)
    {
        int last = _trackedGrenades.Count - 1;
        _trackedGrenades[index] = _trackedGrenades[last];
        _trackedGrenades.RemoveAt(last);
    }

    private readonly struct TrackedGrenade(Grenade grenade, Rigidbody rigidbody)
    {
        public readonly Grenade Grenade = grenade;
        public readonly Rigidbody Rigidbody = rigidbody;
    }

    private readonly List<TrackedGrenade> _trackedGrenades = [];

    private Action<Throwable> _onGrenadeDestroyed;

    static GrenadeController()
    {
        _rigidBodyField = AccessTools.Field(typeof(Throwable), "Rigidbody");
    }

    private static FieldInfo _rigidBodyField;
}
