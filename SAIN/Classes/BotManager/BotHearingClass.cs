using System;
using System.Collections.Generic;
using EFT;
using EFT.Ballistics;
using SAIN.Components.PlayerComponentSpace;
using SAIN.Helpers;
using SAIN.Preset.Shared.Enums;
using UnityEngine;

namespace SAIN.Components.BotControllerSpace.Classes;

public class BotHearingClass : BotManagerBase
{
    public event Action<EPhraseTrigger, ETagStatus, Player> PlayerTalk;

    public event Action<Shot> BulletImpact;

    public BotHearingClass(BotManagerComponent botController)
        : base(botController) { }

    public void BulletImpacted(Shot bullet)
    {
        BulletImpact?.Invoke(bullet);
    }

    public void PlayerTalked(EPhraseTrigger phrase, ETagStatus mask, Player player)
    {
        if (phrase == EPhraseTrigger.OnDeath)
        {
            return;
        }
        if (player == null || !player.HealthController.IsAlive)
        {
            return;
        }

        PlayerComponent playerComponent = SAINGameWorld.PlayerTracker.GetPlayerComponent(player);
        if (playerComponent != null)
        {
            (SAINSoundType soundType, float range) = phrase switch
            {
                EPhraseTrigger.OnBreath => (SAINSoundType.Breathing, 35f),
                EPhraseTrigger.OnBeingHurt or EPhraseTrigger.OnAgony => (SAINSoundType.Pain, 70f),
                _ => (SAINSoundType.Conversation, mask == ETagStatus.Unaware ? 40f : 70f),
            };
            playerComponent.PlayAISound(soundType, player.Position, range, 1, phrase, mask);
            PlayerTalk?.Invoke(phrase, mask, player);
        }
    }

    public void PlayAISound(string profileId, SAINSoundType soundType, Vector3 position, float range, float volume)
    {
        PlayerComponent playerComponent = SAINGameWorld.PlayerTracker.GetPlayerComponent(profileId);
        PlayAISound(playerComponent, soundType, position, range, volume);
    }

    public void PlayAISound(PlayerComponent playerComponent, SAINSoundType soundType, Vector3 position, float range, float volume)
    {
        if (playerComponent == null)
        {
#if DEBUG
            Logger.LogError("Player Component Null");
#endif
            return;
        }
        if (!playerComponent.IsActive)
        {
            return;
        }
        if (!playerComponent.AIData.AISoundPlayer.ShallPlayAISound())
        {
            return;
        }

        playerComponent.PlayAISound(soundType, position, range, volume);

        _pendingBotEvents.Enqueue(
            new PendingBotEvent(playerComponent, position, range * volume, soundType.Convert(), Time.time + BOT_EVENT_DELAY)
        );
    }

    public void Update(float currentTime)
    {
        while (_pendingBotEvents.Count > 0 && _pendingBotEvents.Peek().PlayTime <= currentTime)
        {
            PendingBotEvent botEvent = _pendingBotEvents.Dequeue();
            PlayerComponent playerComponent = botEvent.PlayerComponent;
            if (
                playerComponent != null
                && playerComponent.Player != null
                && playerComponent.Player.HealthController?.IsAlive == true
                && playerComponent.IsActive
            )
            {
                BotController.BotEventHandler?.PlaySound(playerComponent.Player, botEvent.Position, botEvent.Range, botEvent.SoundType);
            }
        }
    }

    private const float BOT_EVENT_DELAY = 0.1f;

    private readonly Queue<PendingBotEvent> _pendingBotEvents = new();

    private readonly struct PendingBotEvent(
        PlayerComponent playerComponent,
        Vector3 position,
        float range,
        AISoundType soundType,
        float playTime
    )
    {
        public readonly PlayerComponent PlayerComponent = playerComponent;
        public readonly Vector3 Position = position;
        public readonly float Range = range;
        public readonly AISoundType SoundType = soundType;
        public readonly float PlayTime = playTime;
    }
}
