using System.Runtime.Serialization;

namespace SAIN.Preset.Shared.GearStealthValues;

[DataContract]
public class ItemStealthValue
{
    [DataMember]
    public string Name;

    [DataMember]
    public EEquipmentType EquipmentType;

    [DataMember]
    public string ItemID;

    [DataMember]
    public float StealthValue;
}
