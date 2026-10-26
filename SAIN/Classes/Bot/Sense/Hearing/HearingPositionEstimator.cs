using System.Collections.Generic;
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

public class HearingPositionEstimator(HearingSensor hearingSensor) : BotSubClass<HearingSensor>(hearingSensor), IBotClass
{
    private static HearingSettings _settings
    {
        get { return GlobalSettingsClass.Instance.Hearing; }
    }

    public SoundPositionEstimate Estimate(SoundPositionEstimate? estimate, AISoundData Sound)
    {
        SoundPositionEstimate result = GetEstimate(estimate, Sound);
        if (SAINPlugin.DebugSettings.Gizmos.DrawHearingEstimates && IsNewPlacement(result.Step) && HearingAnalysis.IsFromMainPlayer(Sound))
        {
            DrawEstimate(result, Sound);
        }
        return result;
    }

    private static bool IsNewPlacement(EEstimateStep step)
    {
        return step != EEstimateStep.Reused && step != EEstimateStep.SightingHeld;
    }

    private SoundPositionEstimate GetEstimate(SoundPositionEstimate? estimate, AISoundData Sound)
    {
        if (estimate == null)
        {
            return Place(Sound, 0f, RollOffset(), RollOffset(), EEstimateStep.FirstSound);
        }
        SoundPositionEstimate last = estimate.Value;

        Vector3 enemyPosition = Sound.Sound.PlayerPosition;
        float moveDistance = GetMoveDistance(FlatDistance(enemyPosition, Bot.Position));
        bool moved = (enemyPosition - last.EnemyPosition).sqrMagnitude >= moveDistance * moveDistance;
        if (last.FromSighting && !moved)
        {
            return last.With(Time.time, EEstimateStep.SightingHeld);
        }

        float sinceUpdated = Time.time - last.TimeUpdated;
        HearingSettings settings = _settings;

        if (!moved && sinceUpdated < settings.HEAR_REPLACE_TIME && IsBetterClue(Sound.SoundType, last.SoundType))
        {
            return Place(Sound, last.ConfidenceBefore, last.AngleOffset, last.DistanceOffset, EEstimateStep.BetterClue);
        }

        if (!moved && sinceUpdated < settings.HEAR_REUSE_TIME)
        {
            return last.With(last.TimeUpdated, EEstimateStep.Reused);
        }

        if (moved && sinceUpdated > settings.HEAR_RESET_SILENCE_TIME)
        {
            float confidence = GetRememberedConfidence(last, enemyPosition, sinceUpdated, moveDistance);
            return Place(Sound, confidence, RollOffset(), RollOffset(), EEstimateStep.ResetAfterSilence);
        }
        return Place(Sound, last.Confidence, Drift(last.AngleOffset), Drift(last.DistanceOffset), EEstimateStep.Drift);
    }

    private void DrawEstimate(SoundPositionEstimate estimate, AISoundData Sound)
    {
        const float expireTime = 10f;
        Vector3 enemyPosition = Sound.Sound.PlayerPosition;
        float error = (estimate.Position - enemyPosition).magnitude;
        Color color = Color.Lerp(Color.red, Color.green, estimate.Confidence);
        string label =
            $"[{Bot.PlayerComponent.Name}] {Sound.SoundType} {estimate.Step}\n"
            + $"Confidence {estimate.Confidence:0.00} Error {error:0.0}m";
        DebugGizmos.DrawSphere(estimate.Position, 0.25f, color, expireTime, label);
        DebugGizmos.DrawLine(enemyPosition, estimate.Position, color, 0.05f, expireTime);
    }

    private SoundPositionEstimate Place(AISoundData Sound, float confidence, float angleOffset, float distanceOffset, EEstimateStep step)
    {
        Vector3 enemyPosition = Sound.Sound.PlayerPosition;
        Vector3 botPosition = Bot.Position;
        Vector3 toEnemy = enemyPosition - botPosition;
        toEnemy.y = 0f;
        float distance = toEnemy.magnitude;

        HearingSettings settings = _settings;
        float errorCoef = Mathf.Lerp(1f, settings.HEAR_MIN_ERROR_COEF, confidence);
        float angleError = angleOffset * GetMaxAngleError(Sound) * errorCoef;
        float distanceError = distanceOffset * settings.HEAR_DISTANCE_ERROR * errorCoef;

        Vector3 result = enemyPosition;
        if (distance > 0.1f)
        {
            Vector3 direction = Vector.Rotate(toEnemy / distance, 0f, angleError, 0f);
            result = botPosition + direction * (distance * (1f + distanceError));
            result.y = enemyPosition.y;
            result = SnapToNavMesh(result, enemyPosition);
        }

        float confidenceAfter = Mathf.Min(1f, confidence + GetConfidenceGain(Sound.SoundType));
        return new SoundPositionEstimate(
            result,
            enemyPosition,
            Time.time,
            confidenceAfter,
            confidence,
            angleOffset,
            distanceOffset,
            Sound.SoundType,
            step
        );
    }

    private static float FlatDistance(Vector3 a, Vector3 b)
    {
        Vector3 offset = a - b;
        offset.y = 0f;
        return offset.magnitude;
    }

    private static float GetRememberedConfidence(SoundPositionEstimate last, Vector3 enemyPosition, float sinceLast, float moveDistance)
    {
        float distanceFromLast = (enemyPosition - last.EnemyPosition).magnitude;
        HearingSettings settings = _settings;
        float timeCoef = Mathf.InverseLerp(settings.HEAR_MEMORY_TIME, settings.HEAR_RESET_SILENCE_TIME, sinceLast);
        float distanceCoef = Mathf.InverseLerp(moveDistance * settings.HEAR_MEMORY_MOVES, moveDistance, distanceFromLast);
        return last.Confidence * timeCoef * distanceCoef;
    }

    private static float GetMoveDistance(float distance)
    {
        HearingSettings settings = _settings;
        return Mathf.Max(settings.HEAR_MOVE_DISTANCE, distance * settings.HEAR_MOVE_DISTANCE_FRACTION);
    }

    private static bool IsBetterClue(SAINSoundType soundType, SAINSoundType? previous)
    {
        if (previous == null)
        {
            return false;
        }
        float gain = GetConfidenceGain(soundType);
        float previousGain = GetConfidenceGain(previous.Value);
        if (gain != previousGain)
        {
            return gain > previousGain;
        }
        return GetBaseAngleError(soundType) < GetBaseAngleError(previous.Value);
    }

    private static Vector3 SnapToNavMesh(Vector3 guess, Vector3 enemyPosition)
    {
        return SampleSameFloor(guess, enemyPosition.y) ?? SampleSameFloor(Vector3.Lerp(enemyPosition, guess, 0.5f), enemyPosition.y) ?? guess;
    }

    private static Vector3? SampleSameFloor(Vector3 point, float floorHeight)
    {
        HearingSettings settings = _settings;
        Vector3? sampled = NavMeshHelpers.GetNearbyNavMeshPoint(point, settings.HEAR_NAV_SAMPLE_RANGE);
        if (sampled == null || Mathf.Abs(sampled.Value.y - floorHeight) > settings.HEAR_NAV_MAX_HEIGHT_DIFF)
        {
            return null;
        }
        return sampled;
    }

    private static float Drift(float offset)
    {
        return Mathf.Clamp(offset + RollOffset() * _settings.HEAR_OFFSET_DRIFT, -1f, 1f);
    }

    public static float RollOffset()
    {
        int rolls = Mathf.Max(1, Mathf.RoundToInt(_settings.HEAR_ERROR_ROLLS));
        float sum = 0f;
        for (int i = 0; i < rolls; i++)
        {
            sum += Vector.RandomRange(1f);
        }
        return sum / rolls;
    }

    private float GetMaxAngleError(AISoundData sound)
    {
        return GetBaseAngleError(sound.SoundType) * GetFacingModifier(sound.Enemy);
    }

    private static float GetBaseAngleError(SAINSoundType soundType)
    {
        return GetPerSound(_settings.HEAR_DIRECTION_ERROR_VALUES, HearingSettings.HEAR_DIRECTION_ERROR_VALUES_Defaults, soundType);
    }

    private static float GetConfidenceGain(SAINSoundType soundType)
    {
        return GetPerSound(_settings.HEAR_CONFIDENCE_GAIN_VALUES, HearingSettings.HEAR_CONFIDENCE_GAIN_VALUES_Defaults, soundType);
    }

    private static float GetPerSound(Dictionary<SAINSoundType, float> values, Dictionary<SAINSoundType, float> defaults, SAINSoundType soundType)
    {
        return values.TryGetValue(soundType, out float value) ? value : defaults[soundType];
    }

    private float GetFacingModifier(Enemy Enemy)
    {
        float dotProduct = Vector3.Dot(Bot.LookDirection.normalized, Enemy.EnemyDirectionNormal);
        float scaled = (dotProduct + 1) / 2;

        return Mathf.Lerp(_settings.HEAR_DIRECTION_ERROR_BEHIND_MULTI, _settings.HEAR_DIRECTION_ERROR_FRONT_MULTI, scaled);
    }
}
