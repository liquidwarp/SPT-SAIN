using System.Runtime.Serialization;
using SAIN.Preset.Shared.Attributes;
using SAIN.Preset.Shared.GlobalSettings;

namespace SAIN.Preset.Shared.BotSettings.SAINSettings.Categories;

[DataContract]
public class SAINCoreSettings : SAINSettingsBase<SAINCoreSettings>, ISAINSettings
{
    [DataMember]
    [Category("Vision")]
    [Name("Field of View")]
    [MinMax(45f, 180f)]
    public float VisibleAngle = 160f;

    [DataMember]
    [Category("Hearing")]
    [Name("Hearing Distance Multiplier")]
    [Description("Modifies the distance that this bot can hear sounds")]
    [MinMax(0.1f, 3f, 1000f)]
    public float HearingDistanceMulti = 1f;

    [DataMember]
    [Name("Can Use Grenades")]
    public bool CanGrenade = true;
}
