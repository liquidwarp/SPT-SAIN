using System;
using System.Collections.Generic;
using System.Reflection;
using BepInEx.Logging;
using Comfort.Common;
using EFT;
using HarmonyLib;
using SAIN.Classes.Bot.Sense.Hearing;
using SAIN.Components;
using SAIN.Helpers;
using SAIN.Extensions;
using SAIN.Models.Enums;
using SAIN.SAINComponent.Classes.EnemyClasses;
using SPT.Reflection.Patching;
using UnityEngine;

namespace SAIN.Patches.Generic.Fixes;

internal class RunToEnemyUpdatePatch : ModulePatch
{
    protected override MethodBase GetTargetMethod()
    {
        return AccessTools.Method(typeof(BotMeleeWeaponData), nameof(BotMeleeWeaponData.RunToEnemyUpdate));
    }

    [PatchPrefix]
    public static bool Patch(BotMeleeWeaponData __instance)
    {
        if (SAINEnableClass.GetSAIN(__instance._owner.ProfileId, out BotComponent bot) && bot.SAINLayersActive)
        {
            Enemy enemy = bot.GoalEnemy;
            if (enemy == null)
            {
                return false;
            }
            __instance.ShallEndRun = false;
            if (!__instance._owner.WeaponManager.IsMelee)
            {
                if (!__instance._owner.WeaponManager.Selector.CanChangeToMeleeWeapons)
                {
                    __instance.ShallEndRun = true;
                    return false;
                }
                __instance._owner.WeaponManager.Selector.ChangeToMelee();
            }
            if (__instance._owner.BotLay.IsLay)
            {
                __instance._owner.BotLay.GetUp(false);
            }
            bot.Mover.SetTargetPose(1f);
            EnemyInfo goalEnemy = enemy.EnemyInfo;
            bool flag;
            if (flag = (goalEnemy.Distance < __instance.DIST_TO_HIT))
            {
                bot.Steering.LookToPoint(goalEnemy.GetBodyPartPosition());
                if (goalEnemy.Person.AIData.Player.MovementContext.IsInPronePose)
                {
                    bot.Mover.SetTargetPose(0f);
                }
            }
            else
            {
                bot.Steering.LookToMovingDirection(true);
            }

            if (__instance.Running && goalEnemy.Distance > __instance.DIST_TO_STOP_SPRINT)
            {
                bot.Mover.ActivePath?.RequestStartSprint(ESprintUrgency.High, "melee");
            }
            if (__instance._nextTryHitTime < Time.time)
            {
                __instance.ResetHitTime((flag && __instance.TryHit(goalEnemy)) ? 10f : __instance.TRY_HIT_PERIOD_FALSE);
            }
            if (bot.Mover.Running)
            {
                if (__instance._runPathCheck < Time.time)
                {
                    float num;
                    if (__instance._useZigZag)
                    {
                        num = (
                            (goalEnemy.Distance > __instance.FAR_DIST)
                                ? __instance.farRecalc
                                : ((goalEnemy.Distance > __instance.MID_DIST) ? __instance.midRecalcZZ : __instance.closeRecalcZZ)
                        );
                    }
                    else
                    {
                        num =
                            (goalEnemy.Distance > __instance.FAR_DIST)
                                ? __instance.farRecalc
                                : ((goalEnemy.Distance > __instance.MID_DIST) ? __instance.midRecalc : __instance.closeRecalc);
                    }
                    __instance._runPathCheck = Time.time + num;
                    if (!__instance.CanRunToEnemyToHit(goalEnemy, out Vector3[] way))
                    {
                        __instance.ShallEndRun = true;
                        return false;
                    }
                    if (goalEnemy.Distance < __instance._owner.Settings.FileSettings.Shoot.MELEE_STOP_MOVE_DISTANCE)
                    {
                        bot.Mover.ActivePath.Cancel(0.1f);
                    }
                    else
                    {
                        bot.Mover.RunToPoint(goalEnemy.CurrPosition, true, -1, ESprintUrgency.High, true);
                    }
                }
            }
            else
            {
                if (!__instance.CanRunToEnemyToHit(goalEnemy, out Vector3[] way2))
                {
                    __instance.ShallEndRun = true;
                    return false;
                }
                if (goalEnemy.Distance < __instance._owner.Settings.FileSettings.Shoot.MELEE_STOP_MOVE_DISTANCE)
                {
                    bot.Mover.ActivePath?.Cancel(0.1f);
                }
                else
                {
                    bot.Mover.RunToPoint(goalEnemy.CurrPosition, true, -1, ESprintUrgency.High, true);
                }
            }
            return false;
        }
        return false;
    }
}

internal class EnableVaultPatch : ModulePatch
{
    protected override MethodBase GetTargetMethod()
    {
        return AccessTools.Method(typeof(Player), nameof(Player.InitVaultingComponent));
    }

    [PatchPrefix]
    public static void Patch(Player __instance, ref bool aiControlled)
    {
        if (__instance.UsedSimplifiedSkeleton)
        {
            return;
        }

        aiControlled = false;
    }
}

internal class FightShallReloadFixPatch : ModulePatch
{
    protected override MethodBase GetTargetMethod()
    {
        return AccessTools.Method(typeof(BotReload), nameof(BotReload.FightShallReload));
    }

    [PatchPrefix]
    public static bool Patch(BotReload __instance, ref bool __result)
    {
        if (SAINEnableClass.IsBotInCombat(__instance._owner))
        {
            __result = true;
            return false;
        }
        return true;
    }
}

//Todo: Still necessary?
internal class FixItemTakerPatch : ModulePatch
{
    protected override MethodBase GetTargetMethod()
    {
        return AccessTools.Method(typeof(BotItemTaker), nameof(BotItemTaker.OnThrowItem));
    }

    [PatchPrefix]
    public static bool PatchPrefix(BotItemTaker __instance)
    {
        return __instance._owner.IsBotNotNullOrDead();
    }
}

//Todo: Still necessary?
internal class FixItemTakerPatch2 : ModulePatch
{
    protected override MethodBase GetTargetMethod()
    {
        return AccessTools.Method(typeof(BotItemTaker), nameof(BotItemTaker.RefreshClosestItems));
    }

    [PatchPrefix]
    public static bool PatchPrefix(BotItemTaker __instance)
    {
        return __instance._owner.IsBotNotNullOrDead();
    }
}

internal class RotateClampPatch : ModulePatch
{
    protected override MethodBase GetTargetMethod()
    {
        return AccessTools.Method(typeof(Player), nameof(Player.Rotate));
    }

    [PatchPrefix]
    public static void PatchPrefix(Player __instance, ref bool ignoreClamp)
    {
        if (__instance != null && __instance.IsAI && __instance.IsSprintEnabled && SAINEnableClass.IsBotInCombat(__instance))
        {
            ignoreClamp = true;
        }
    }
}

internal class BotGroupAddEnemyPatch : ModulePatch
{
    protected override MethodBase GetTargetMethod()
    {
        return AccessTools.Method(typeof(BotsGroup), nameof(BotsGroup.AddEnemy));
    }

    [PatchPrefix]
    public static bool PatchPrefix(IPlayer person)
    {
        if (person == null)
        {
            return false;
        }
        if (person.IsAI)
        {
            BotOwner botOwner = person.AIData?.BotOwner;
            if (botOwner == null || botOwner.GetPlayer == null)
            {
                return false;
            }
        }

        return true;
    }
}
internal class GroupHostilityPatch : ModulePatch
{
    private static readonly ManualLogSource _log = BepInEx.Logging.Logger.CreateLogSource("SAIN Hostility");

    protected override MethodBase GetTargetMethod()
    {
        return AccessTools.Method(typeof(BotOwner), nameof(BotOwner.PreActivate));
    }

    [PatchPostfix]
    public static void PatchPostfix(BotOwner __instance)
    {
        BotsGroup ownGroup = __instance.BotsGroup;
        if (ownGroup == null)
        {
            return;
        }
        try
        {
            AddToInactiveGroups(__instance, ownGroup);
            if (ownGroup._initialBot == __instance)
            {
                AddExistingBots(__instance, ownGroup);
            }
        }
        catch (Exception exception)
        {
            _log.LogError($"Could not check hostility for {__instance.Profile.Info.Nickname}: {exception}");
        }
    }

    private static void AddToInactiveGroups(BotOwner bot, BotsGroup ownGroup)
    {
        foreach (BotZoneGroups zoneGroups in bot.BotsController.Groups().Values)
        {
            foreach (BotsGroup group in zoneGroups._bossGroup)
            {
                CheckInactiveGroup(group, ownGroup, bot);
            }
            foreach (BotsGroup group in zoneGroups.data.Values)
            {
                CheckInactiveGroup(group, ownGroup, bot);
            }
            List<BotsGroup> noConnectionGroups = zoneGroups._noConnectionGroups;
            for (int i = 0; i < noConnectionGroups.Count; i++)
            {
                CheckInactiveGroup(noConnectionGroups[i], ownGroup, bot);
            }
        }
    }

    private static void CheckInactiveGroup(BotsGroup group, BotsGroup ownGroup, BotOwner bot)
    {
        // Groups that had an active member got the bot from the game, or have all died
        if (group == null || group == ownGroup || group._isFirstMemberAdded)
        {
            return;
        }
        // The group's first bot never finished spawning
        if (group._initialBot == null || group._initialBot.BotsGroup == null)
        {
            return;
        }
        TryAddEnemy(group, bot);
    }

    private static void AddExistingBots(BotOwner bot, BotsGroup ownGroup)
    {
        List<Player> players = Singleton<EFT.GameWorld>.Instance.AllAlivePlayersList;
        for (int i = 0; i < players.Count; i++)
        {
            Player player = players[i];
            // Humans are handled when the group is created
            if (player == null || !player.IsAI || player.AIData == null)
            {
                continue;
            }
            BotOwner other = player.AIData.BotOwner;
            // A bot without a group yet checks this one itself once it gets its own
            if (other == null || other == bot || other.BotsGroup == null || other.BotsGroup == ownGroup)
            {
                continue;
            }
            TryAddEnemy(ownGroup, other);
        }
    }

    private static void TryAddEnemy(BotsGroup group, BotOwner bot)
    {
        string profileId = bot.ProfileId;
        // Enemies a group gets before its first member activates wait in the recheck list
        if (
            BotsGroupHelpers.ContainsPlayer(group._recheckPersonsAfterInit, profileId)
            || BotsGroupHelpers.ContainsPlayer(group.Enemies, profileId)
            || BotsGroupHelpers.ContainsPlayer(group.Allies, profileId)
        )
        {
            return;
        }
        try
        {
            if (!group.IsPlayerEnemy(bot))
            {
                return;
            }
            group.AddEnemy(bot, EBotEnemyCause.initial);
#if DEBUG
            if (HearingDiagnostics.Enabled)
            {
                _log.LogInfo(
                    $"Added {bot.Profile.Info.Nickname} ({bot.name}, {bot.Profile.Info.Settings.Role}) to the enemies of group {group.Id}"
                        + $" (first {group._initialBot.Profile.Info.Nickname})"
                );
            }
#endif
        }
        catch (Exception exception)
        {
            _log.LogError($"Could not check {bot.Profile.Info.Nickname} against group {group.Id}: {exception}");
        }
    }
}

internal class BotMemoryAddEnemyPatch : ModulePatch
{
    protected override MethodBase GetTargetMethod()
    {
        return AccessTools.Method(typeof(BotMemory), nameof(BotMemory.AddEnemy));
    }

    [PatchPrefix]
    public static bool PatchPrefix(IPlayer enemy)
    {
        if (enemy == null)
        {
            return false;
        }
        if (enemy.IsAI)
        {
            BotOwner botOwner = enemy.AIData?.BotOwner;
            if (botOwner == null || botOwner.GetPlayer == null)
            {
                return false;
            }
        }

        return true;
    }
}

public class StopSetToNavMeshPatch : ModulePatch
{
    protected override MethodBase GetTargetMethod()
    {
        return AccessTools.Method(typeof(BotMover), nameof(BotMover.SetPosition));
    }

    [PatchPrefix]
    public static bool PatchPrefix(BotMover __instance)
    {
        if (SAINEnableClass.IsBotInCombat(__instance._owner))
        {
            __instance.PositionOnWayInner = __instance._owner.Position;
            __instance._owner.Mover.LocalAvoidance.DropOffset();
            return false;
        }
        return true;
    }
}

public class StopSetToNavMeshPatch2 : ModulePatch
{
    protected override MethodBase GetTargetMethod()
    {
        return AccessTools.Method(typeof(BotMoverImpostor), nameof(BotMoverImpostor.OnMotionApplied));
    }

    [PatchPrefix]
    public static bool PatchPrefix(BotMoverImpostor __instance)
    {
        if (SAINEnableClass.IsBotInCombat(__instance._owner))
        {
            return false;
        }
        return true;
    }
}
