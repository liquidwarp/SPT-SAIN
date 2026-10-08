using System.Collections.Generic;
using System.Runtime.Serialization;
using SAIN.Preset.Shared.Attributes;

namespace SAIN.Preset.Shared.GlobalSettings.Categories;

[DataContract]
public class MoveSettings : SAINSettingsBase<MoveSettings>, ISAINSettings
{
    [DataMember]
    [Name("Can Lean - Global")]
    [Description("Can any bot lean while peeking and while outside of cover?")]
    [Category("Movement Option Toggles")]
    public bool LEAN_TOGGLE = true;

    [DataMember]
    [Name("Can Lean in Cover - Global")]
    [Description("Can any bot lean while in cover?")]
    [Category("Movement Option Toggles")]
    public bool LEAN_INCOVER_TOGGLE = true;

    [DataMember]
    [Name("Can Jump - Global")]
    [Description("Can any bot Jump?")]
    [Category("Movement Option Toggles")]
    public bool JUMP_TOGGLE = true;

    [DataMember]
    [Name("Can Auto Pose - Global")]
    [Description("Does any bot automatically adjust their crouch height depending on objects between them and their enemy?")]
    [Category("Movement Option Toggles")]
    public bool AUTOCROUCH_TOGGLE = true;

    [DataMember]
    [Name("Can Go Prone - Global")]
    [Description("Can any bot go Prone at all?")]
    [Category("Movement Option Toggles")]
    public bool PRONE_TOGGLE = true;

    [DataMember]
    [Name("Not Moving Vault Time")]
    [Description("If a bot is not moving for this length of time or longer, try vaulting to get themselves unstuck")]
    [Category("Sprinting")]
    [Advanced]
    [MinMax(0.01f, 1.5f, 100f)]
    public float BOT_NOMOVE_TRYVAULT_TIME = 0.5f;

    [DataMember]
    [Name("Not Moving Jump Time")]
    [Description("If a bot is not moving for this length of time or longer, try jumping to get themselves unstuck")]
    [Category("Sprinting")]
    [Advanced]
    [MinMax(0.01f, 1.5f, 100f)]
    public float BOT_NOMOVE_TRYJUMP_TIME = 1f;

    [DataMember]
    [Name("Path Corner Sprint Reach Distance")]
    [Description("How far from a corner along a path before a bot considers it self arrived, and attemps to navigate to the next corner.")]
    [Category("Sprinting")]
    [Advanced]
    [MinMax(0.01f, 1f, 100f)]
    public float BotSprintCornerReachDist = 0.3f;

    [DataMember]
    [Name("Path Corner Walk Reach Distance")]
    [Description("How far from a corner along a path before a bot considers it self arrived, and attemps to navigate to the next corner.")]
    [Advanced]
    [MinMax(0.01f, 1f, 100f)]
    public float BotWalkCornerReachDist = 0.1f;

    public override void Init(List<ISAINSettings> list)
    {
        list.Add(this);
    }
}
