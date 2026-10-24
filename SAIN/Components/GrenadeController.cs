using System;
using System.Collections;
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
        grenade.DestroyEvent += grenadeDestroyed;
        OnGrenadeThrown?.Invoke(grenade, dangerPoint, grenade.ProfileId);
        if (GameWorldComponent.TryGetPlayerComponent(player, out PlayerComponent playerComponent))
        {
            // Bots hear sounds from a cache processed later, so playing it after the event doesn't delay anything.
            BotController.BotHearing.PlayAISound(playerComponent, SAINSoundType.GrenadeThrow, grenade.transform.position, 20f, 1f, true);
            List<PlayerComponent> RelevantPlayers = [];
            foreach (var otherPlayer in playerComponent.OtherPlayersData.DataDictionary.Values)
            {
                if (otherPlayer.DistanceData.Distance < 125f && otherPlayer.OtherPlayerComponent.IsSAINBot)
                {
                    RelevantPlayers.Add(otherPlayer.OtherPlayerComponent);
                }
            }
            ActiveGrenades[grenade] = RelevantPlayers;
            BotController.StartCoroutine(GrenadeTracker(grenade, playerComponent, RelevantPlayers, dangerPoint));
        }
    }

    public readonly Dictionary<Throwable, List<PlayerComponent>> ActiveGrenades = [];

    private void grenadeDestroyed(Throwable Grenade)
    {
        ActiveGrenades.Remove(Grenade);
    }

    private IEnumerator GrenadeTracker(Grenade Grenade, PlayerComponent Thrower, List<PlayerComponent> RelevantPlayers, Vector3 DangerPoint)
    {
        Rigidbody Rigidbody = (Rigidbody)_rigidBodyField.GetValue(Grenade);

        if (Rigidbody == null)
        {
#if DEBUG
            Logger.LogError("RigidBody Null");
#endif
            yield break;
        }
        while (Grenade != null && BotController != null && Rigidbody != null)
        {
            Vector3 Velocity = Rigidbody.velocity;
            if (Velocity.magnitude < 0.1f)
            {
                OnGrenadeDangerUpdated?.Invoke(Grenade, Grenade.transform.position);
            }
            else if (Velocity.y < 0)
            {
                Vector3 VelocityNormal = Velocity.normalized;
                if (
                    Vector3.Dot(VelocityNormal, Vector3.down) > 0.5f
                    && Physics.Raycast(
                        Grenade.transform.position,
                        VelocityNormal,
                        out RaycastHit Hit,
                        5,
                        LayersMaskController.HighPolyWithTerrainMask
                    )
                )
                {
                    OnGrenadeDangerUpdated?.Invoke(Grenade, Hit.point);
                }
            }
            yield return null;
        }
    }

    static GrenadeController()
    {
        _rigidBodyField = AccessTools.Field(typeof(Throwable), "Rigidbody");
    }

    private static FieldInfo _rigidBodyField;
}
