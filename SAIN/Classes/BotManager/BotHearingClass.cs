using System;
using System.Collections;
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

    public event Action<SAINSoundType, Vector3, PlayerComponent, float, float> AISoundPlayed;

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
            var Range = phrase switch
            {
                EPhraseTrigger.OnBreath => 35,
                EPhraseTrigger.OnBeingHurt or EPhraseTrigger.OnAgony => 70,
                _ => (float)(mask == ETagStatus.Unaware ? 40 : 70),
            };
            SAINSoundType soundType = phrase switch
            {
                EPhraseTrigger.OnBreath => SAINSoundType.Breathing,
                EPhraseTrigger.OnBeingHurt or EPhraseTrigger.OnAgony => SAINSoundType.Pain,
                _ => SAINSoundType.Conversation,
            };
            playerComponent.PlayAISound(soundType, player.Position, Range, 1, phrase, mask);
            PlayerTalk?.Invoke(phrase, mask, player);
        }
    }

    public void PlayAISound(string profileId, SAINSoundType soundType, Vector3 position, float range, float volume)
    {
        PlayerComponent playerComponent = SAINGameWorld.PlayerTracker.GetPlayerComponent(profileId);
        PlayAISound(playerComponent, soundType, position, range, volume, true);
    }

    public void PlayAISound(
        PlayerComponent playerComponent,
        SAINSoundType soundType,
        Vector3 position,
        float range,
        float volume,
        bool limitFreq
    )
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
        AISoundPlayed?.Invoke(soundType, position, playerComponent, range, volume);

        BotController.StartCoroutine(WaitDelayThenPlayDefaultBotEvent(soundType, playerComponent, position, range, volume));
    }

    private IEnumerator WaitDelayThenPlayDefaultBotEvent(
        SAINSoundType soundType,
        PlayerComponent playerComponent,
        Vector3 position,
        float range,
        float volume,
        float delay = 0.1f
    )
    {
        yield return new WaitForSeconds(delay);
        if (
            playerComponent != null
            && playerComponent.Player != null
            && playerComponent.Player.HealthController?.IsAlive == true
            && playerComponent.IsActive
        )
        {
            playBotEvent(playerComponent.Player, position, range * volume, soundType);
        }
    }

    private void playBotEvent(Player player, Vector3 position, float range, SAINSoundType soundType)
    {
        BotController.BotEventHandler?.PlaySound(player, position, range, soundType.Convert());
    }
}
