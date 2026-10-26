using System.Collections.Generic;
using System.Runtime.Serialization;
using SAIN.Preset.Shared.Attributes;

namespace SAIN.Preset.Shared.GlobalSettings.Categories.General;

[DataContract]
public class DebugOverlaySettings : SAINSettingsBase<DebugOverlaySettings>
{
    [DataMember]
    public bool Overlay_Info = true;

    [DataMember]
    public bool Overlay_Info_Expanded = false;

    [DataMember]
    public bool Overlay_Search = true;

    [DataMember]
    public bool Overlay_EnemyLists = false;

    [DataMember]
    public bool Overlay_EnemyInfo = true;

    [DataMember]
    public bool Overlay_EnemyInfo_Expanded = false;

    [DataMember]
    public bool Overlay_Decisions = false;

    [DataMember]
    public bool OverLay_AimInfo = false;

    [DataMember]
    public bool OverLay_AlwaysShowClosestHumanInfo = false;

    [DataMember]
    public bool OverLay_AlwaysShowMainPlayerInfo = false;

    [DataMember]
    public bool Overlay_Hearing = false;
}

[DataContract]
public class DebugGizmoSettings : SAINSettingsBase<DebugGizmoSettings>
{
    [DataMember]
    [Name("Draw Debug Gizmos")]
    public bool DrawDebugGizmos;

    [DataMember]
    [Name("Draw Transform Gizmos")]
    public bool DrawTransformGizmos;

    [DataMember]
    [Name("Draw Line of Sight Checks")]
    public bool DrawLineOfSightGizmos;

    [DataMember]
    [Name("Draw Aim Gizmos")]
    public bool DebugDrawAimGizmos = false;

    [DataMember]
    [Name("Draw Hearing Estimates")]
    [Description("Draws where bots guess you are when they hear you, from red at low confidence to green at high.")]
    public bool DrawHearingEstimates = false;
}

[DataContract]
public class DebugLogSettings : SAINSettingsBase<DebugLogSettings>
{
    [DataMember]
    [Name("Global Debug Mode")]
    public bool GlobalDebugMode;

    [DataMember]
    [Name("Test Bot Sprint Pathfinder")]
    public bool ForceBotsToRunAround;

    [DataMember]
    [Name("Test Bot Crawling")]
    public bool ForceBotsToTryCrawl;

    [DataMember]
    [Name("Bots Can't See Me")]
    [Description("Bots ignore seeing you, so only hearing tells them where you are.")]
    public bool BotsCantSeeMainPlayer;

    [DataMember]
    [Name("Draw Debug Labels")]
    public bool DrawDebugLabels;

    [DataMember]
    [Name("Debug External")]
    public bool DebugExternal;

    [DataMember]
    [Name("Debug Recoil Calculations")]
    public bool DebugRecoilCalculations = false;

    [DataMember]
    [Name("Debug Aim Calculations")]
    public bool DebugAimCalculations = false;

    [DataMember]
    [Name("Debug Hearing Checks")]
    [Description("Logs why bots did or didn't hear the sounds you make.")]
    public bool DebugHearingChecks = false;
}

[DataContract]
public class DebugSettings : SAINSettingsBase<DebugSettings>, ISAINSettings
{
    [DataMember]
    public DebugLogSettings Logs = new();

    [DataMember]
    public DebugGizmoSettings Gizmos = new();

    [DataMember]
    public DebugOverlaySettings Overlay = new();

    public override void Init(List<ISAINSettings> list)
    {
        list.Add(Logs);
        list.Add(Gizmos);
        list.Add(Overlay);
    }
}
