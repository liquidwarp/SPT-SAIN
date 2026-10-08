using System.Runtime.Serialization;
using SAIN.Preset.Shared.Attributes;

namespace SAIN.Preset.Shared.GlobalSettings.Categories.General;

[DataContract]
public class DoorSettings : SAINSettingsBase<DoorSettings>, ISAINSettings
{

    [DataMember]
    [Name("Disable All Doors")]
    [Description("Doors are hard, just turn them all off. Only targets doors that can be open/closed normally.")]
    public bool DisableAllDoors = false;
}
