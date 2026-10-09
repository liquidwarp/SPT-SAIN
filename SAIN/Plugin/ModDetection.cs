using System;
using System.Reflection;
using BepInEx.Bootstrap;
using UnityEngine;
using static SAIN.AssemblyInfoClass;

namespace SAIN;

public static class ModDetection
{
    static ModDetection()
    {
        ModsCheckTimer = Time.time + 5f;
    }

    public static void ManualUpdate()
    {
        if (!ModsChecked && ModsCheckTimer < Time.time && ModsCheckTimer > 0)
        {
            ModsChecked = true;
            CheckPlugins();
        }
    }

    public static bool ProjectFikaLoaded { get; private set; }
    public static bool ProjectFikaHeadlessLoaded { get; private set; }

    public static class FikaInterop
    {
        private static Assembly FikaAssembly { get; set; }
        private static Type FikaBackendUtils { get; set; }
        private static PropertyInfo IsServerProperty { get; set; }
        private static PropertyInfo IsClientProperty { get; set; }

        public static void InitializeInterop()
        {
            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                if (assembly.GetName().Name == "Fika.Core")
                {
                    FikaAssembly = assembly;
                    break;
                }
            }

            if (FikaAssembly == null)
            {
                return;
            }

            FikaBackendUtils = FikaAssembly.GetType("Fika.Core.Main.Utils.FikaBackendUtils");
            IsServerProperty = FikaBackendUtils.GetProperty("IsServer", BindingFlags.Public | BindingFlags.Static);
            IsClientProperty = FikaBackendUtils.GetProperty("IsClient", BindingFlags.Public | BindingFlags.Static);
        }

        public static bool IsClient()
        {
            if (IsClientProperty == null)
            {
                return false;
            }

            return (bool)IsClientProperty.GetValue(null);
        }
    }

    public static void CheckPlugins()
    {
        if (Chainloader.PluginInfos.ContainsKey(FikaGUID))
        {
            ProjectFikaLoaded = true;

            FikaInterop.InitializeInterop();

            Logger.LogInfo($"SAIN: Project Fika Detected.");
        }

        if (Chainloader.PluginInfos.ContainsKey(FikaHeadlessGUID))
        {
            ProjectFikaHeadlessLoaded = true;
            Logger.LogInfo($"SAIN: Project Fika Headless Detected.");
        }
    }

    private static readonly float ModsCheckTimer = -1f;
    private static bool ModsChecked = false;
}
