using SAIN.Preset.Shared.GlobalSettings.Categories.Look;
using SAIN.SAINComponent.Classes.EnemyClasses;
using UnityEngine;

namespace SAIN.SAINComponent.Classes;

public static class SAINNotLooking
{
    public static float GetVisionSpeedDecrease(Enemy enemy)
    {
        NotLookingSettings settings = SAINPlugin.LoadedPreset.GlobalSettings.Look.NotLooking;
        if (settings.NotLookingToggle && CheckIfPlayerNotLooking(enemy, settings))
        {
            return settings.NotLookingVisionSpeedModifier;
        }
        return 1f;
    }

    private static bool CheckIfPlayerNotLooking(Enemy enemy, NotLookingSettings settings)
    {
        if (
            enemy.InLineOfSight
            && Time.time - enemy.Events.OnEnemyLineOfSightChanged.TimeLastTrue > settings.NotLookingTimeLimit
        )
        {
            return false;
        }

        // Eye to chest, so the player's look pitch is compared against a matching vector.
        Vector3 botDir = enemy.Bot.Transform.BodyPosition - enemy.EnemyTransform.EyePosition;
        float angle = Vector3.Angle(botDir, enemy.EnemyTransform.LookDirection);
        return angle >= settings.NotLookingAngle;
    }
}
