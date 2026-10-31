using System.Reflection;
using EFT;
using HarmonyLib;
using SAIN.Components;
using SAIN.Components.BotController;
using SPT.Reflection.Patching;

namespace SAIN.Patches.GameWorld;

/// <summary>
/// This patch makes sure that whenever a new bot is activated, that bot is also activated in SAIN
/// </summary>
public class ActivateBotComponentPatch : ModulePatch
{
    protected override MethodBase GetTargetMethod()
    {
        return AccessTools.Method(typeof(BotOwner), nameof(BotOwner.method_10));
    }

#if DEBUG
    [PatchPrefix]
    public static void PatchPrefix(BotOwner __instance)
    {
        BotActivationDiagnostics.OnActivateStart(__instance);
    }
#endif

    [PatchPostfix]
    public static void PatchPostfix(ref BotOwner __instance)
    {
#if DEBUG
        BotActivationDiagnostics.OnActivateEnd(__instance);
#endif
        var component = __instance.GetComponent<BotComponent>();

        if (component != null)
        {
            component.Activate(__instance);
        }
    }
}
