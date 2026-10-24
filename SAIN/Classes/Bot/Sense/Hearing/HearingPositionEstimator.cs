using SAIN.Components.BotComponentSpace.Classes.EnemyClasses;
using SAIN.Components.PlayerComponentSpace;
using SAIN.Helpers;
using SAIN.Preset.Shared.Enums;
using SAIN.Preset.Shared.GlobalSettings;
using SAIN.Preset.Shared.GlobalSettings.Categories;
using SAIN.SAINComponent;
using SAIN.SAINComponent.Classes.EnemyClasses;
using UnityEngine;

namespace SAIN.Classes.Bot.Sense.Hearing;

/// <summary>
/// Turns a heard sound into a guess of where the enemy is: a direction error and a distance error, like a person localizing a sound.
/// Both shrink the more the bot hears the same enemy, down to a floor.
/// </summary>
public class HearingPositionEstimator(HearingSensor hearingSensor) : BotSubClass<HearingSensor>(hearingSensor), IBotClass
{
    private const float MOVE_DISTANCE = 3f;
    private const float NEW_TRACK_SILENCE_TIME = 3f;
    private const float OFFSET_DRIFT = 0.2f;
    private const float NAV_SAMPLE_RANGE = 4f;
    private const float CONFIDENCE_GAIN_INTERVAL = 0.5f;

    private static HearingSettings _settings
    {
        get { return GlobalSettingsClass.Instance.Hearing; }
    }

    /// <summary>
    /// The next estimate after hearing this sound. The previous one is the last sound heard or the last sighting, which EnemyHearing keeps.
    /// </summary>
    public SoundPositionEstimate Estimate(SoundPositionEstimate? estimate, AISoundData Sound)
    {
        // Where the enemy was when they made the sound, not where they are now, so sound travel and reaction time leave the bot a step behind.
        Vector3 enemyPosition = Sound.Sound.PlayerPosition;

        bool moved = estimate != null && (enemyPosition - estimate.Value.EnemyPosition).sqrMagnitude >= MOVE_DISTANCE * MOVE_DISTANCE;
        if (estimate != null && estimate.Value.FromSighting && !moved)
        {
            // Hearing them counts as not silent, but keep the sighting as the anchor so creeping away a step at a time still adds up.
            return estimate.Value.WithTime(Time.time);
        }

        // Sounds right after the last guess reuse it, so a burst of shots and flybys can't max confidence in an instant or sample the NavMesh every round.
        if (estimate != null && !moved && Time.time - estimate.Value.TimeUpdated < CONFIDENCE_GAIN_INTERVAL)
        {
            return estimate.Value;
        }

        // Silence means they stayed put, so confidence is kept. Heard again somewhere else after a silence means they moved quietly: start over.
        float confidence;
        float angleOffset;
        float distanceOffset;
        if (estimate == null || (moved && Time.time - estimate.Value.TimeUpdated > NEW_TRACK_SILENCE_TIME))
        {
            confidence = 0f;
            angleOffset = Vector.RandomRange(1f);
            distanceOffset = Vector.RandomRange(1f);
        }
        else
        {
            confidence = estimate.Value.Confidence;
            angleOffset = Drift(estimate.Value.AngleOffset);
            distanceOffset = Drift(estimate.Value.DistanceOffset);
        }

        float errorCoef = Mathf.Lerp(1f, _settings.HEAR_MIN_ERROR_COEF, confidence);
        float angleError = angleOffset * GetMaxAngleError(Sound) * errorCoef;
        float distanceError = distanceOffset * _settings.HEAR_DISTANCE_ERROR * errorCoef;

        // Keep the enemy's height, so the bot looks at them rather than at the floor or the sky.
        Vector3 botPosition = Bot.Position;
        Vector3 toEnemy = enemyPosition - botPosition;
        toEnemy.y = 0f;
        float distance = toEnemy.magnitude;
        Vector3 result = enemyPosition;
        if (distance > 0.1f)
        {
            Vector3 direction = Vector.Rotate(toEnemy / distance, 0f, angleError, 0f);
            result = botPosition + direction * (distance * (1f + distanceError));
            result.y = enemyPosition.y;
            result = SnapToNavMesh(result, enemyPosition);
        }
#if DEBUG
        if (Sound.HeardPlayer.IsYourPlayer)
        {
            DebugGizmos.DrawLine(enemyPosition, result, Color.magenta, 0.1f, 10f, false);
        }
#endif

        confidence = Mathf.Min(1f, confidence + GetConfidenceGain(Sound.SoundType));
        return new SoundPositionEstimate(result, enemyPosition, Time.time, confidence, angleOffset, distanceOffset, false);
    }

    private static Vector3 SnapToNavMesh(Vector3 guess, Vector3 enemyPosition)
    {
        return NavMeshHelpers.GetNearbyNavMeshPoint(guess, NAV_SAMPLE_RANGE)
            ?? NavMeshHelpers.GetNearbyNavMeshPoint(Vector3.Lerp(enemyPosition, guess, 0.5f), NAV_SAMPLE_RANGE)
            ?? enemyPosition;
    }

    private static float Drift(float offset)
    {
        return Mathf.Clamp(offset + Vector.RandomRange(OFFSET_DRIFT), -1f, 1f);
    }

    /// <summary>
    /// Degrees either side of the true direction. Worse from behind, for sounds that are hard to place, and indoors where sound echoes.
    /// </summary>
    private float GetMaxAngleError(AISoundData sound)
    {
        if (!_settings.HEAR_DIRECTION_ERROR_VALUES.TryGetValue(sound.SoundType, out float angle))
        {
            angle = HearingSettings.HEAR_DIRECTION_ERROR_VALUES_Defaults[sound.SoundType];
        }
        angle *= GetFacingModifier(sound.Enemy);
        if (sound.EnvironmentId != 0)
        {
            angle *= _settings.HEAR_DIRECTION_ERROR_INDOOR_MULTI;
        }
        return angle;
    }

    private static float GetConfidenceGain(SAINSoundType soundType)
    {
        if (!_settings.HEAR_CONFIDENCE_GAIN_VALUES.TryGetValue(soundType, out float gain))
        {
            gain = HearingSettings.HEAR_CONFIDENCE_GAIN_VALUES_Defaults[soundType];
        }
        return gain;
    }

    private float GetFacingModifier(Enemy Enemy)
    {
        float dotProduct = Vector3.Dot(Bot.LookDirection.normalized, Enemy.EnemyDirectionNormal);
        float scaled = (dotProduct + 1) / 2;

        return Mathf.Lerp(_settings.HEAR_DIRECTION_ERROR_BEHIND_MULTI, _settings.HEAR_DIRECTION_ERROR_FRONT_MULTI, scaled);
    }
}
