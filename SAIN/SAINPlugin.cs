global using EFTMath = MyExtensions;
using System.Reflection;
using BepInEx;
using BepInEx.Bootstrap;
using BepInEx.Configuration;
using SAIN.Components.BotController;
using SAIN.Editor;
using SAIN.Helpers;
using SAIN.Plugin;
using SAIN.Preset;
using SAIN.Preset.Server;
using SAIN.Preset.Shared;
using SAIN.Preset.Shared.Enums;
using SAIN.Preset.Shared.GlobalSettings.Categories.General;
using SPT.Reflection.Patching;
using UnityEngine;
using static SAIN.AssemblyInfoClass;

namespace SAIN;

[BepInPlugin(SAINGUID, SAINName, MyModInfo.MOD_VERSION)]
[BepInDependency(BigBrainGUID, BigBrainVersion)]
[BepInDependency(BotHealingFixesGUID, BotHealingFixesVersion)]
//[BepInDependency(SPTGUID, SPTVersion)]
[BepInProcess(EscapeFromTarkov)]
[BepInIncompatibility("com.dvize.BushNoESP")]
[BepInIncompatibility("com.dvize.NoGrenadeESP")]
public class SAINPlugin : BaseUnityPlugin
{
    private PatchManager _patchManager;

    public static DebugSettings DebugSettings
    {
#if DEBUG
        get { return LoadedPreset.GlobalSettings.General.Debug; }
#else
        get { return _releaseDebugSettings; }
#endif
    }

#if !DEBUG
    // Release builds hide the debug options, so ignore any a preset has saved.
    private static readonly DebugSettings _releaseDebugSettings = new();
#endif

    public static bool DebugMode
    {
        get { return DebugSettings.Logs.GlobalDebugMode; }
    }

    public static bool DrawDebugGizmos
    {
        get { return DebugSettings.Gizmos.DrawDebugGizmos; }
    }

    public static PresetEditorDefaults EditorDefaults
    {
        get { return PresetHandler.EditorDefaults; }
    }

    public static ECombatDecision ForceSoloDecision = ECombatDecision.None;

    public static ESquadDecision ForceSquadDecision = ESquadDecision.None;

    public static ESelfActionType ForceSelfDecision = ESelfActionType.None;

    public void Awake()
    {
        _patchManager = new(this, true);

        if (!HasForkedBotHealingFixes(out string botHealingFixesVersion))
        {
            Logger.LogError(
                $"[SAIN] Startup aborted: SAIN requires the forked BotHealingFixes ({BotHealingFixesVersion}-{ForkVersionInfo.Marker}-{BotHealingFixesForkRevision} or newer), found {botHealingFixesVersion}. Install it from https://github.com/liquidwarp/SPT-BotHealingFixes and restart."
            );

            AbortStartup();
            return;
        }
        if (!PresetHandler.Init() || !EFTCoreSettings.Load())
        {
            Logger.LogError(
                "[SAIN] Startup aborted: could not load the presets and core overrides from the SAIN server mod, re-install SAIN correctly and restart."
            );

            AbortStartup();
            return;
        }
        BotSpawnController.LoadExclusionList();
        BindConfigs();
        _patchManager.EnablePatches();
        BigBrainHandler.Init();
    }

    private void AbortStartup()
    {
        enabled = false;
    }

    private static bool HasForkedBotHealingFixes(out string version)
    {
        version = Chainloader.PluginInfos.TryGetValue(BotHealingFixesGUID, out var pluginInfo)
            ? pluginInfo.Instance?.GetType().Assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion
            : null;
        if (version == null)
        {
            version = "none";
            return false;
        }

        return ForkVersionInfo.TryGetRevision(version, out int forkRevision) && forkRevision >= BotHealingFixesForkRevision;
    }

    private void BindConfigs()
    {
        string category = "SAIN Editor";
        OpenEditorButton = Config.Bind(category, "Open Editor", false, "Opens the Editor on press");
        OpenEditorConfigEntry = Config.Bind(
            category,
            "Open Editor Shortcut",
            new KeyboardShortcut(KeyCode.F6),
            "The keyboard shortcut that toggles editor"
        );
    }

    public static ConfigEntry<bool> OpenEditorButton { get; private set; }

    public static ConfigEntry<KeyboardShortcut> OpenEditorConfigEntry { get; private set; }

    public static SAINPresetClass LoadedPreset
    {
        get { return PresetHandler.LoadedPreset; }
    }

    public void Update()
    {
        PresetSyncWebSocket.Resync();

        ModDetection.ManualUpdate();
        SAINEditor.ManualUpdate();
#if DEBUG
        DebugGizmos.ManualUpdate();
#endif
    }

    public void Start()
    {
        SAINEditor.Init();
    }

    public void OnDestroy()
    {
        PresetSyncWebSocket.Instance?.Close();
    }

    public void LateUpdate()
    {
        SAINEditor.LateUpdate();
    }

    public void OnGUI()
    {
        SAINEditor.OnGUI();
    }
}
