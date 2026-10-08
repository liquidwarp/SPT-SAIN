using System.Runtime.Serialization;
using SAIN.Preset.Shared.Attributes;

namespace SAIN.Preset.Shared.GlobalSettings.Categories.Look;

[DataContract]
public class NotLookingSettings : SAINSettingsBase<NotLookingSettings>, ISAINSettings
{
    [DataMember]
    [Name("Bot Reaction Changes Toggle")]
    [Section("Unseen Bot")]
    [Description(
        "Bots will notice you more slowly if you are not looking in their direction. "
            + "So if a bot spots you while your back is turned, you get a little more time to react."
    )]
    public bool NotLookingToggle = true;

    [DataMember]
    [Name("Bot Reaction Changes Time Limit")]
    [Section("Unseen Bot")]
    [Description(
        "How long the slower reaction lasts once you are in a bot's line of sight. "
            + "After X seconds in its line of sight, the bot spots you at its normal speed even if your back is turned."
    )]
    [MinMax(0.5f, 20f, 100f)]
    [Advanced]
    public float NotLookingTimeLimit = 4f;

    [DataMember]
    [Name("Bot Reaction Changes Angle")]
    [Section("Unseen Bot")]
    [Advanced]
    [Description("The Maximum Angle for the player to be considered looking at a bot.")]
    [MinMax(5f, 45f, 1f)]
    public float NotLookingAngle = 45f;

    [DataMember]
    [Name("Bot Reaction Multiplier When Out of Sight")]
    [Section("Unseen Bot")]
    [Description(
        "How much to multiply bot vision speed by if you aren't looking at them when they notice you. Higher = More time before reacting."
    )]
    [MinMax(1f, 2f, 100f)]
    [Advanced]
    public float NotLookingVisionSpeedModifier = 1.1f;
}
