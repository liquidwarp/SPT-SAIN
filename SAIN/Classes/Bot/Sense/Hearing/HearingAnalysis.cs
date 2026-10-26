using EFT;
using EFT.InventoryLogic;
using SAIN.Components.PlayerComponentSpace;
using SAIN.Preset.Shared.Enums;
using SAIN.Preset.Shared.GlobalSettings;
using SAIN.Preset.Shared.GlobalSettings.Categories;
using SAIN.SAINComponent;
using SAIN.SAINComponent.Classes.EnemyClasses;
using SAIN.SAINComponent.Classes.Info;
using UnityEngine;

namespace SAIN.Classes.Bot.Sense.Hearing;

public class HearingAnalysis(HearingSensor hearingSensor) : BotSubClass<HearingSensor>(hearingSensor), IBotClass
{
    public bool CheckIfSoundHeard(AISoundData sound)
    {
        if (ShallLimitAI(sound))
        {
            return false;
        }

        bool log = ShallLogHearing(sound);
        if (!sound.IsGunShot && !DoIDetectFootsteps(sound, out float chanceToHear, out float maxDistance))
        {
            if (log)
            {
                LogHearing(
                    sound,
                    sound.PlayerDistance > maxDistance
                        ? $"Not heard: beyond max footstep distance [{maxDistance:0.0}m]"
                        : $"Not heard: failed {chanceToHear:0}% chance roll"
                );
            }
            return false;
        }

        if ((sound.Sound.Position - sound.Sound.PlayerComponent.Position).sqrMagnitude > 5f * 5f)
        {
            if (log)
            {
                LogHearing(sound, "Not heard: sound is more than 5m from its source");
            }
            return false;
        }

        float EnvironmentModifier = CalcEnvironmentMod(sound);
        float ConditionModifier = CalcConditionMod(sound.SoundType);
        float OcclusionModifier = CalcOcclusionMod(sound.Enemy, sound.SoundType);
        float DifficultyModifier = Bot.Info.Difficulty.HearingDistanceModifier;
        float FinalModifier = Mathf.Clamp(
            1.0f * EnvironmentModifier * ConditionModifier * OcclusionModifier * DifficultyModifier,
            _settings.HEAR_MODIFIER_MIN_CLAMP,
            _settings.HEAR_MODIFIER_MAX_CLAMP
        );
        float FinalRange = sound.Sound.Range * sound.Sound.Volume * FinalModifier;
        bool heard = sound.PlayerDistance <= FinalRange;
        if (log)
        {
            LogHearing(
                sound,
                $"{(heard ? "Heard" : "Not heard: out of range")} Range [{FinalRange:0.0}m] = Base [{sound.Sound.Range:0.0}m] x Volume [{sound.Sound.Volume:0.00}] "
                    + $"x Modifier [{FinalModifier:0.00}] (Environment [{EnvironmentModifier:0.00}] Condition [{ConditionModifier:0.00}] "
                    + $"Occlusion [{OcclusionModifier:0.00}] Difficulty [{DifficultyModifier:0.00}])"
            );
        }
        return heard;
    }

    public static bool ShallLogHearing(AISoundData sound)
    {
        return SAINPlugin.DebugSettings.Logs.DebugHearingChecks && IsFromMainPlayer(sound);
    }

    public static bool IsFromMainPlayer(AISoundData sound)
    {
        PlayerComponent source = sound.HeardPlayerComponent;
        if (source == null)
        {
            return false;
        }
        return source.IsMainPlayer;
    }

    public static void LogHearing(AISoundData sound, string result)
    {
        Logger.LogDebug($"[{sound.Bot.PlayerComponent.Name}] {sound.SoundType} at {sound.PlayerDistance:0.0}m: {result}");
    }

    private static HearingSettings _settings
    {
        get { return GlobalSettingsClass.Instance.Hearing; }
    }

    private float CalcBunkerVolumeReduction(AISoundData sound)
    {
        var botLocation = Bot.PlayerComponent.AIData.PlayerLocation;
        var enemyLocation = sound.HeardPlayerComponent.AIData.PlayerLocation;

        bool botinBunker = botLocation.InBunker;
        bool playerinBunker = enemyLocation.InBunker;
        if (botinBunker != playerinBunker)
        {
            return _settings.BUNKER_REDUCTION_COEF;
        }
        if (botinBunker)
        {
            float diff = Mathf.Abs(botLocation.BunkerDepth - enemyLocation.BunkerDepth);
            if (diff > 0)
            {
                return _settings.BUNKER_ELEV_DIFF_COEF;
            }
        }
        return 1f;
    }

    private bool DoIDetectFootsteps(AISoundData sound, out float chanceToHear, out float farhearing)
    {
        bool hasheadPhones = Bot.PlayerComponent.Equipment.GearInfo.HasEarPiece;
        float closehearing = hasheadPhones ? _settings.HEAR_CHANCE_MIN_DIST_HEADPHONES : _settings.HEAR_CHANCE_MIN_DIST;
        farhearing = hasheadPhones
            ? SAINPlugin.LoadedPreset.GlobalSettings.Hearing.MaxFootstepAudioDistance
            : SAINPlugin.LoadedPreset.GlobalSettings.Hearing.MaxFootstepAudioDistanceNoHeadphones;
        float distance = sound.PlayerDistance;
        if (distance <= closehearing)
        {
            chanceToHear = 100f;
            return true;
        }

        if (distance > farhearing)
        {
            chanceToHear = 0f;
            return false;
        }

        float minimumChance = 0f;
        if (hasheadPhones)
        {
            if (distance < farhearing * _settings.HEAR_CHANCE_MIDRANGE_COEF)
            {
                minimumChance += _settings.HEAR_CHANCE_MIDRANGE_MINCHANCE_HEADPHONES;
            }
            else
            {
                minimumChance += _settings.HEAR_CHANCE_LONGRANGE_MINCHANCE_HEADPHONES;
            }
            if (sound.SoundType != SAINSoundType.FootStep)
            {
                minimumChance += _settings.HEAR_CHANCE_HEADPHONES_OTHERSOUNDS;
            }
        }

        if (Bot.PlayerComponent.Transform.VelocityData.VelocityMagnitudeNormal < _settings.HEAR_CHANCE_NOTMOVING_VELOCITY)
        {
            minimumChance += hasheadPhones
                ? _settings.HEAR_CHANCE_NOTMOVING_MINCHANCE_HEADPHONES
                : _settings.HEAR_CHANCE_NOTMOVING_MINCHANCE;
        }

        if (Bot.HasEnemy && Bot.GoalEnemy.EnemyProfileId == sound.Sound.PlayerComponent.ProfileId)
        {
            minimumChance += hasheadPhones
                ? _settings.HEAR_CHANCE_CURRENTENEMY_MINCHANCE_HEADPHONES
                : _settings.HEAR_CHANCE_CURRENTENEMY_MINCHANCE;
        }

        float num = farhearing - closehearing;
        float num2 = distance - closehearing;
        chanceToHear = 1f - num2 / num;
        chanceToHear *= 100f;

        chanceToHear = Mathf.Clamp(chanceToHear, minimumChance, 100f);
        return EFTMath.RandomBool(chanceToHear);
    }

    private float CalcOcclusionMod(Enemy Enemy, SAINSoundType SoundType)
    {
        if (IsInLineOfSight(Enemy))
        {
            return 1f;
        }
        switch (SoundType)
        {
            case SAINSoundType.Shot:
                return _settings.GUNSHOT_OCCLUSION_MOD;

            case SAINSoundType.SuppressedShot:
                return _settings.GUNSHOT_OCCLUSION_MOD_SUPP;

            case SAINSoundType.Sprint:
                return _settings.FOOTSTEP_OCCLUSION_MOD_SPRINT;

            case SAINSoundType.FootStep:
                if (Enemy.EnemyPlayer != null && Enemy.EnemyPlayer.IsSprintEnabled)
                {
                    return _settings.FOOTSTEP_OCCLUSION_MOD_SPRINT;
                }
                return _settings.FOOTSTEP_OCCLUSION_MOD;

            default:
                return _settings.OTHER_OCCLUSION_MOD;
        }
    }

    private bool IsInLineOfSight(Enemy enemy)
    {
        if (!enemy.EnemyPlayerComponent.HiddenFromBots)
        {
            return enemy.InLineOfSight;
        }

        // Hiding stops the vision raycasts that keep InLineOfSight current, but walls should still muffle sounds.
        Vector3 eyePosition = Bot.Transform.EyePosition;
        PlayerTransformClass target = enemy.EnemyTransform;
        LayerMask mask = LayersMaskController.HighPolyWithTerrainNoGrassMask;
        return !Physics.Linecast(eyePosition, target.EyePosition, mask) || !Physics.Linecast(eyePosition, target.BodyPosition, mask);
    }

    private float CalcEnvironmentMod(AISoundData sound)
    {
        if (Player.AIData.EnvironmentId == sound.EnvironmentId)
        {
            return 1f;
        }
        float envMod = sound.IsGunShot ? _settings.GUNSHOT_ENVIR_MOD : _settings.FOOTSTEP_ENVIR_MOD;
        float bunkerMod = CalcBunkerVolumeReduction(sound);
        float result = envMod * bunkerMod;
        result = Mathf.Clamp(result, _settings.MIN_ENVIRONMENT_MOD, 1f);
        return result;
    }

    private float CalcConditionMod(SAINSoundType SoundType)
    {
        float modifier = 1f;
        float? currentHearSense = null;
        if (BotOwner != null)
        {
            currentHearSense = BotOwner.Settings?.Current?.CurrentHearingSense;
        }

        if (currentHearSense != null)
        {
            modifier *= currentHearSense.Value;
        }

        modifier *= Bot.Info.FileSettings.Core.HearingDistanceMulti;

        if (SoundType != SAINSoundType.Shot)
        {
            GearInfo gearInfo = Bot.PlayerComponent.Equipment.GearInfo;
            if (!gearInfo.HasEarPiece)
            {
                modifier *= _settings.HEAR_MODIFIER_NO_EARS;
            }

            if (gearInfo.Deafness == EDeafStrength.High)
            {
                modifier *= _settings.HEAR_MODIFIER_HEAVY_HELMET;
            }
            else if (gearInfo.Deafness == EDeafStrength.Low)
            {
                modifier *= _settings.HEAR_MODIFIER_LIGHT_HELMET;
            }
            if (Bot.Memory.Health.Dying && !Bot.Memory.Health.OnPainKillers)
            {
                modifier *= _settings.HEAR_MODIFIER_DYING;
            }
            if (Player.IsSprintEnabled)
            {
                modifier *= _settings.HEAR_MODIFIER_SPRINT;
            }
            if (Player.HeavyBreath)
            {
                modifier *= _settings.HEAR_MODIFIER_HEAVYBREATH;
            }
        }

        return modifier;
    }

    private bool ShallLimitAI(AISoundData sound)
    {
        if (!sound.Enemy.IsAI)
        {
            return false;
        }

        var aiLimit = GlobalSettingsClass.Instance.General.AILimit;
        if (!aiLimit.LimitAIvsAIGlobal)
        {
            return false;
        }

        if (!aiLimit.LimitAIvsAIHearing)
        {
            return false;
        }

        var enemyPlayer = sound.Sound.PlayerComponent;
        if (Bot.GoalEnemy?.EnemyProfileId == enemyPlayer.ProfileId)
        {
            return false;
        }

        var enemyBot = enemyPlayer.BotComponent;
        float maxRange;
        if (enemyBot == null)
        {
            BotOwner enemyBotOwner = enemyPlayer.BotOwner;
            if (enemyBotOwner != null && enemyBotOwner.Memory.GoalEnemy?.ProfileId == Bot.ProfileId)
            {
                return false;
            }
            maxRange = GetMaxRange(Bot.CurrentAILimit);
        }
        else
        {
            if (enemyBot.GoalEnemy?.EnemyProfileId == Bot.ProfileId)
            {
                return false;
            }
            maxRange = GetMaxRange(enemyBot.CurrentAILimit);
        }

        if (sound.PlayerDistance <= maxRange)
        {
            return false;
        }
        return true;
    }

    private static float GetMaxRange(AILimitSetting aiLimit)
    {
        var maxHearingRanges = GlobalSettingsClass.Instance.General.AILimit.MaxHearingRanges;

        if (maxHearingRanges.TryGetValue(aiLimit, out float maxRange))
        {
            return maxRange;
        }

        return float.MaxValue;
    }
}
