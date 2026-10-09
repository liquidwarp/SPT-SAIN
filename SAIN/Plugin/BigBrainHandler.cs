using System;
using System.Collections.Generic;
using System.Reflection;
using DrakiaXYZ.BigBrain.Brains;
using EFT;
using SAIN.Layers;
using SAIN.Layers.Combat.Run;
using SAIN.Layers.Combat.Solo;
using SAIN.Layers.Combat.Squad;
using SAIN.Layers.Flashed;
using SAIN.Preset;
using SAIN.Preset.GlobalSettings.Categories;
using SAIN.Preset.Shared.Enums;
using SAIN.Preset.Shared.GlobalSettings.Categories.General;

namespace SAIN;

public static class BigBrainHandler
{
    public const bool INCLUDE_RAIDER_BRAIN_FOR_PMCS = true;

    private static readonly string[] _commonVanillaLayersToRemove =
    [
        //"FightReqNull",
        //"PeacecReqNull",
        //"Follow Player",
        //"HideHW", // Used by Scavs, Raiders, and Bloodhounds when zombies spawn
        //"Khorovod",
        //"GeneratorDef", // Used by Scavs and Killa
        "GroupForce", // Used by Cursed Scavs, Rogues, and Oni (event) Cultist
        "AdvAssaultTarget",
        "Simple Target",
        "Pmc", // Used by Raiders, Bloodhounds, and Scav groups
        "AssaultHaveEnemy",
        "PushAndSup", // Used by Scavs and Shadow of Tagilla's followers
        "Pursuit",
        "Kill logic",
    ];

    private static readonly string[] _commonVanillaBossAndFollowerLayersToRemove =
    [
        //"ExURequest", // Used by Rogues and Kaban's followers to warn neutral players/bots
        "BirdEyeFight", // Used by Birdeye and Ghost (event) Cultist
        "BossSanitarFight", // Used by Sanitar and Harbinger (event) Cultist
        "SanitarGoal", // Used by Sanitar and his followers
        "TagillaAmbush", // Used by Tagilla and his followers
        "TagillaMain", // Used by Tagilla and his followers
        "BoarGrenadeDanger", // Used by Kaban and his followers
        "HoldOrCoverF", // Used by Cultists and Gluhar's followers
        "SecurityGluhar", // Used by Gluhar and his followers
        "HoldNearBoss", // Used by Gluhar's and Kollontay's followers
        "Kln_NIMH", // Used by Kollontay and his followers
        "KlnTrg", // Used by Kollontay and his followers
        "Kojaniy Target", // Used by Shturman and his followers
    ];

    private static readonly List<Type> _SAINLayers = [];
    private static readonly List<string> _SAINLayerNames = [];

    public static List<string> SAINLayerNames
    {
        get { return FindAllSAINLayers(); }
    }

    public static List<Type> SAINLayers
    {
        get
        {
            if (_SAINLayers.Count == 0)
            {
                Type[] allTypes = typeof(SAINPlugin).Assembly.GetTypes();
                for (int i = 0; i < allTypes.Length; i++)
                {
                    Type type = allTypes[i];
                    if (type.IsSubclassOf(typeof(SAINLayer)))
                    {
                        _SAINLayers.Add(type);
                    }
                }
            }

            return _SAINLayers;
        }
    }

    public static void Init()
    {
        BrainAssignment.Init();
    }

    private static List<string> FindAllSAINLayers()
    {
        if (_SAINLayerNames.Count != 0)
        {
            return _SAINLayerNames;
        }

        foreach (Type layerType in SAINLayers)
        {
            FieldInfo nameFieldInfo = layerType.GetField("Name", BindingFlags.Public | BindingFlags.Static);
            if (nameFieldInfo == null)
            {
                Logger.LogError(
                    $"{layerType.Name} does not have a public static Name field. This is required for enabling vanilla layers!"
                );
                continue;
            }

            _SAINLayerNames.Add((string)nameFieldInfo.GetValue(null));
        }

        return _SAINLayerNames;
    }

    public static bool BigBrainInitialized;

    public static class BrainAssignment
    {
        public static void Init()
        {
            List<string> raiderBrain = [nameof(EBrain.PMC)];

            AddSAINLayers(GetBrainList(AIBrains.PMCs));
            if (INCLUDE_RAIDER_BRAIN_FOR_PMCS)
            {
                AddSAINLayers(raiderBrain, [WildSpawnType.pmcBEAR, WildSpawnType.pmcUSEC]);
            }
            AddSAINLayers(GetBrainList(AIBrains.Scavs));
            AddSAINLayers(raiderBrain, [WildSpawnType.assaultGroup]);
            AddSAINLayers(raiderBrain, [WildSpawnType.pmcBot]);
            AddSAINLayers([nameof(EBrain.ExUsec)]);
            AddSAINLayers([nameof(EBrain.ArenaFighter)]);
            AddSAINLayers(GetBrainList(AIBrains.NormalBosses), squadPriority: 70, soloPriority: 69);
            AddSAINLayers(GetBrainList(AIBrains.NormalFollowers), squadPriority: 70, soloPriority: 69);
            AddSAINLayers(GetBrainList(AIBrains.Goons), squadPriority: 64, soloPriority: 62);
            AddSAINLayers(GetBrainList(AIBrains.LabyrinthBots));
            AddSAINLayers(
                GetBrainList(AIBrains.Cultists),
                debugPriority: 150,
                avoidThreatPriority: 118,
                flashedPriority: 123,
                squadPriority: 104,
                soloPriority: 102
            );
            AddSAINLayers(GetBrainList(AIBrains.SpecialBots));

            ToggleVanillaLayersForPMCs(false);
            ToggleVanillaLayersForAllBots();

            ApplyModdedBotLayers();
        }

        /// <summary>
        /// Attaches SAIN's layers to bot types registered by other mods through the server interop
        /// </summary>
        public static void ApplyModdedBotLayers()
        {
            foreach (BotType botType in BotTypeDefinitions.BotTypesList)
            {
                List<string> brains = botType.BrainsToApply;
                if (brains == null || brains.Count == 0)
                {
                    if (string.IsNullOrEmpty(botType.BaseBrain))
                    {
                        continue;
                    }

                    brains = [botType.BaseBrain];
                }

                List<string> layers = [.. _commonVanillaLayersToRemove];

                if (botType.LayersToRemove != null)
                {
                    layers.AddRange(botType.LayersToRemove);
                }

                List<WildSpawnType> roles = [botType.WildSpawnType];

                AddCustomLayersToBrainsAndRoles(brains, roles);
                ToggleVanillaLayersForBrainsAndRoles(brains, roles, layers, false);

                Logger.LogInfo($"[SAIN] Applied SAIN layers to modded bot '{botType.Name}' ({botType.WildSpawnType}).");
            }
        }

        public static void ToggleVanillaLayersForAllBots()
        {
            ToggleVanillaLayersForScavs(VanillaBotSettings.VanillaScavs);
            ToggleVanillaLayersForRogues(VanillaBotSettings.VanillaRogues);
            ToggleVanillaLayersForRaiders([WildSpawnType.pmcBot], VanillaBotSettings.VanillaRaiders);
            ToggleVanillaLayersForBloodHounds(VanillaBotSettings.VanillaBloodHounds);
            ToggleVanillaLayersForNormalBosses(VanillaBotSettings.VanillaBosses);
            ToggleVanillaLayersForNormalFollowers(VanillaBotSettings.VanillaFollowers);
            ToggleVanillaLayersForGoons(VanillaBotSettings.VanillaGoons);
            ToggleVanillaLayersForLaybrinthBots(VanillaBotSettings.VanillaLabyrinthBots);
            ToggleVanillaLayersForCultists(VanillaBotSettings.VanillaCultists);
            ToggleVanillaLayersForSpecialBots(VanillaBotSettings.VanillaSpecialBots);
        }

        public static void ToggleVanillaLayersForScavs(bool useVanillaLayers)
        {
            List<string> brainList = GetBrainList(AIBrains.Scavs);

            List<string> LayersToToggle = ["Help", "AssaultEnemyFar", .. _commonVanillaLayersToRemove];

            ToggleVanillaLayers(brainList, LayersToToggle, useVanillaLayers);

            ToggleVanillaLayersForRaiders([WildSpawnType.assaultGroup], useVanillaLayers);
        }

        public static void ToggleVanillaLayersForRaiders(List<WildSpawnType> roles, bool useVanillaLayers)
        {
            List<string> brainList = [nameof(EBrain.PMC)];

            List<string> LayersToToggle =
            [
                "AssaultEnemyFar", // Used by Raiders and Scav groups
                .. _commonVanillaLayersToRemove,
            ];

            ToggleVanillaLayers(brainList, LayersToToggle, roles, useVanillaLayers);
        }

        public static void ToggleVanillaLayersForSpecialBots(bool useVanillaLayers)
        {
            List<string> brainList = GetBrainList(AIBrains.SpecialBots);

            List<string> LayersToToggle =
            [
                "ObdolbosFight", // Used by crazy event Scavs
                .. _commonVanillaLayersToRemove,
            ];

            ToggleVanillaLayers(brainList, LayersToToggle, useVanillaLayers);
        }

        public static void ToggleVanillaLayersForRogues(bool useVanillaLayers)
        {
            List<string> brainList = [nameof(EBrain.ExUsec)];

            List<string> LayersToToggle = [.. _commonVanillaLayersToRemove];

            ToggleVanillaLayers(brainList, LayersToToggle, useVanillaLayers);
        }

        public static void ToggleVanillaLayersForBloodHounds(bool useVanillaLayers)
        {
            List<string> brainList = [nameof(EBrain.ArenaFighter)];

            List<string> LayersToToggle = [.. _commonVanillaLayersToRemove];

            ToggleVanillaLayers(brainList, LayersToToggle, useVanillaLayers);

            ToggleVanillaLayersForRaiders([WildSpawnType.arenaFighterEvent], useVanillaLayers);
        }

        public static void ToggleVanillaLayersForLaybrinthBots(bool useVanillaLayers)
        {
            List<string> brainList = GetBrainList(AIBrains.LabyrinthBots);

            List<string> LayersToToggle =
            [
                "KillaAgro", // Used by Vengeful Killa
                "TagillaAgro", // Used by Shadow of Tagilla
                .. _commonVanillaLayersToRemove,
            ];

            ToggleVanillaLayers(brainList, LayersToToggle, useVanillaLayers);
        }

        public static void ToggleVanillaLayersForCultists(bool useVanillaLayers)
        {
            List<string> brainList = GetBrainList(AIBrains.Cultists);

            List<string> LayersToToggle =
            [
                //"GrenSuicide", // Used by Cultist priests
                "Run&Strike", // Used by Cultist followers
                "MeleeS_IN", // Used by Cultist followers
                "R&H_OUT", // Used by Cultist priests
                "SupShootSect_IN", // Used by Cultist followers
                .. _commonVanillaLayersToRemove,
                .. _commonVanillaBossAndFollowerLayersToRemove,
            ];

            ToggleVanillaLayers(brainList, LayersToToggle, useVanillaLayers);
        }

        public static void ToggleVanillaLayersForNormalBosses(bool useVanillaLayers)
        {
            List<string> brainList = GetBrainList(AIBrains.NormalBosses);

            List<string> LayersToToggle =
            [
                //"PartisanMine", // Used by Partisan
                //"PartMineAll", // Used by Partisan
                "PrtFMN", // Used by Partisan
                "PrtPst", // Used by Partisan
                "PrtZrSvg", // Used by Partisan
                "PrtFight", // Used by Partisan
                "PrtBadTrg", // Used by Partisan
                "PrtMany", // Used by Partisan
                "PrtStalk", // Used by Partisan
                "HoldOrCoverT", // Used by Partisan
                "BossBoarFight", // Used by Kaban
                "BossGlFight", // Used by Gluhar
                "KojaniyB_Enemy", // Used by Shturman
                "Bully Layer", // Used by Reshala
                "KlnSolo", // Used by Kollontay
                "KolontayFight", // Used by Kollontay
                .. _commonVanillaLayersToRemove,
                .. _commonVanillaBossAndFollowerLayersToRemove,
            ];
            ToggleVanillaLayers(brainList, LayersToToggle, useVanillaLayers);
        }

        public static void ToggleVanillaLayersForNormalFollowers(bool useVanillaLayers)
        {
            List<string> brainList = GetBrainList(AIBrains.NormalFollowers);

            List<string> LayersToToggle =
            [
                "BoarStationary", // Used by Kaban's followers
                "BoarPatrol", // Used by Kaban's followers
                "BoarClPatrol", // Used by Kaban's followers
                "FBoarFght", // Used by Kaban's followers
                "SecurityKln", // Used by Kollontay's followers
                "KlnForceAtk", // Used by Kollontay's followers
                "KolontayAP", // Used by Kollontay's followers
                "FolKojEnemy", // Used by Shturman's followers
                "GluharKilla", // Used by Gluhar's followers
                "GluhAssKilla", // Used by Gluhar's followers
                "FlGlScout", // Used by Gluhar's followers
                "GlGoal", // Used by Gluhar's followers
                "FlSanFight", // Used by Sanitar's followers
                "TagillaFollower", // Used by Tagilla's followers
                "Follower bully", // Used by Reshala's followers
                .. _commonVanillaLayersToRemove,
                .. _commonVanillaBossAndFollowerLayersToRemove,
            ];

            ToggleVanillaLayers(brainList, LayersToToggle, useVanillaLayers);
        }

        public static void ToggleVanillaLayersForGoons(bool useVanillaLayers)
        {
            List<string> brainList = GetBrainList(AIBrains.Goons);

            List<string> LayersToToggle =
            [
                "KnightFight", // Used by Knight
                "Assault Building", // Used by Knight, Big Pipe, and Birdeye
                "Enemy Building", // Used by Knight, Big Pipe, and Birdeye
                "BirdHold", // Used by Birdeye
                .. _commonVanillaLayersToRemove,
                .. _commonVanillaBossAndFollowerLayersToRemove,
            ];

            ToggleVanillaLayers(brainList, LayersToToggle, useVanillaLayers);
        }

        public static void ToggleVanillaLayersForPMCs(bool useVanillaLayers)
        {
            List<string> brainList = GetBrainList(AIBrains.PMCs);

            List<string> LayersToToggle = ["PmcBear", "PmcUsec", .. _commonVanillaLayersToRemove];

            ToggleVanillaLayers(brainList, LayersToToggle, useVanillaLayers);

            if (INCLUDE_RAIDER_BRAIN_FOR_PMCS)
            {
                ToggleVanillaLayersForRaiders([WildSpawnType.pmcBEAR, WildSpawnType.pmcUSEC], useVanillaLayers);
            }
        }

        public static void ToggleVanillaLayersForBrainsAndRoles(
            List<string> brainList,
            List<WildSpawnType> roles,
            List<string> layersToToggle,
            bool useVanillaLayers
        )
        {
            ToggleVanillaLayers(brainList, layersToToggle, roles, useVanillaLayers);
        }

        public static void AddCustomLayersToBrainsAndRoles(List<string> brainList, List<WildSpawnType> roles)
        {
            AddSAINLayers(brainList, roles);
        }

        /// <summary>
        /// Adds every SAIN layer to the given brains. Combat priorities default to the preset's layer settings;
        /// pass explicit values for brains whose vanilla layers need SAIN to sit at a different priority.
        /// When roles is null the layers apply to every role using these brains.
        /// </summary>
        private static void AddSAINLayers(
            List<string> brainList,
            List<WildSpawnType> roles = null,
            int? squadPriority = null,
            int? soloPriority = null,
            int debugPriority = 99,
            int avoidThreatPriority = 80,
            int flashedPriority = 85
        )
        {
            LayerSettings settings = SAINPlugin.LoadedPreset.GlobalSettings.General.Layers;

            AddLayer(typeof(DebugLayer), brainList, debugPriority, roles);
            AddLayer(typeof(SAINAvoidThreatLayer), brainList, avoidThreatPriority, roles);
            AddLayer(typeof(SAINFlashedLayer), brainList, flashedPriority, roles);
            AddLayer(typeof(CombatSquadLayer), brainList, squadPriority ?? settings.SAINCombatSquadLayerPriority, roles);
            AddLayer(typeof(CombatSoloLayer), brainList, soloPriority ?? settings.SAINCombatSoloLayerPriority, roles);
        }

        private static void AddLayer(Type layerType, List<string> brainList, int priority, List<WildSpawnType> roles)
        {
            if (roles == null)
            {
                BrainManager.AddCustomLayer(layerType, brainList, priority);
            }
            else
            {
                BrainManager.AddCustomLayer(layerType, brainList, priority, roles);
            }
        }

        private static void ToggleVanillaLayers(List<string> brainNames, List<string> layerNames, bool useVanillaLayers)
        {
            if (useVanillaLayers)
            {
                BrainManager.RemoveLayers(SAINLayerNames, brainNames);
                BrainManager.RestoreLayers(layerNames, brainNames);
            }
            else
            {
                BrainManager.RestoreLayers(SAINLayerNames, brainNames);
                BrainManager.RemoveLayers(layerNames, brainNames);
            }
        }

        private static void ToggleVanillaLayers(
            List<string> brainNames,
            List<string> layerNames,
            List<WildSpawnType> roles,
            bool useVanillaLayers
        )
        {
            if (useVanillaLayers)
            {
                BrainManager.RemoveLayers(SAINLayerNames, brainNames, roles);
                BrainManager.RestoreLayers(layerNames, brainNames, roles);
            }
            else
            {
                BrainManager.RestoreLayers(SAINLayerNames, brainNames, roles);
                BrainManager.RemoveLayers(layerNames, brainNames, roles);
            }
        }

        private static List<string> GetBrainList(List<EBrain> brains)
        {
            List<string> brainList = [];
            for (int i = 0; i < brains.Count; i++)
            {
                brainList.Add(brains[i].ToString());
            }
            return brainList;
        }

        private static VanillaBotSettings VanillaBotSettings
        {
            get { return SAINPlugin.LoadedPreset.GlobalSettings.General.VanillaBots; }
        }
    }
}
