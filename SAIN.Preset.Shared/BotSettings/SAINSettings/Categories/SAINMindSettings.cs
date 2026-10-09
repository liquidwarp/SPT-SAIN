using System.Runtime.Serialization;
using SAIN.Preset.Shared.Attributes;
using SAIN.Preset.Shared.GlobalSettings;

namespace SAIN.Preset.Shared.BotSettings.SAINSettings.Categories;

[DataContract]
public class SAINMindSettings : SAINSettingsBase<SAINMindSettings>, ISAINSettings
{
    [DataMember]
    [Category("Weapon Control")]
    [Name("Weapon Proficiency")]
    [Description(
        "How Well this bot can fire any weapon type, affects recoil, fire-rate, and burst length. Higher number equals harder bots."
    )]
    [Percentage01to99]
    public float WeaponProficiency = 0.5f;

    [DataMember]
    [Name("Suppression Resistance")]
    [Description(
        "Higher = Less affected by suppression. A Value of 0 means No Resistance. "
            + "A Value of 1 means Full Resistance. "
            + "The final resistance number is the mid-point between their personality and bot type resistance. "
            + "So a value of 0.25 for personality and a value of 0.75 for bot type would result in 0.5"
    )]
    [MinMax(0.0f, 1f, 100)]
    public float SuppressionResistance = 0f;

    [DataMember]
    [Category("Talk")]
    [Name("Talk Frequency")]
    [Description("How often to check if a bot wants to talk. Higher = More Delay between Talking.")]
    [MinMax(0f, 30f)]
    public float TalkFrequency = 1f;

    [DataMember]
    [Category("Talk")]
    public bool CanTalk = true;

    [DataMember]
    [Category("Talk")]
    public bool BotTaunts = true;

    [DataMember]
    [Category("Talk")]
    public bool SquadTalk = true;

    [DataMember]
    [Category("Talk")]
    [Name("Squad Talk Frequency. Higher = More Delay between Talking.")]
    [MinMax(0f, 60f)]
    public float SquadMemberTalkFreq = 3f;

    [DataMember]
    [Category("Talk")]
    [Name("Squad Leader Talk Frequency. Higher = More Delay between Talking.")]
    [MinMax(0f, 60f)]
    public float SquadLeadTalkFreq = 3f;
}
