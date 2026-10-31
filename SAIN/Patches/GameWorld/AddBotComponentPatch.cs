using System;
using System.Reflection;
using EFT;
using HarmonyLib;
using SAIN.Components.BotController;
using SPT.Reflection.Patching;

namespace SAIN.Patches.GameWorld;

/// <summary>
/// This patch makes sure that whenever a new bot is spawned, that bot is also added in SAIN
/// </summary>
public class AddBotComponentPatch : ModulePatch
{
    protected override MethodBase GetTargetMethod()
    {
        return AccessTools.Method(typeof(BotOwner), nameof(BotOwner.PreActivate));
    }

#if DEBUG
    [PatchPrefix]
    public static void PatchPrefix(BotOwner __instance)
    {
        BotActivationDiagnostics.OnPreActivateStart(__instance);
    }

    // Runs even when PreActivate throws, which the postfix doesn't; the exception still propagates.
    [PatchFinalizer]
    public static void PatchFinalizer(BotOwner __instance, Exception __exception)
    {
        BotActivationDiagnostics.OnPreActivateEnd(__instance, __exception);
    }
#endif

    [PatchPostfix]
    public static void PatchPostfix(ref BotOwner __instance)
    {
        try
        {
            BotSpawnController.Instance.AddBot(__instance);
        }
        catch (Exception ex)
        {
            Logger.LogError($" SAIN Add Bot Error: {ex}");
        }
    }
}
