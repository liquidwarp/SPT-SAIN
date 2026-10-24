using System.Collections.Generic;
using System.Runtime.Serialization;
using SAIN.Preset.Shared.Attributes;
using SAIN.Preset.Shared.Enums;

namespace SAIN.Preset.Shared.GlobalSettings.Categories;

[DataContract]
public class HearingSettings : SAINSettingsBase<HearingSettings>, ISAINSettings
{
    static HearingSettings()
    {
        // Hearing Position Estimation
        HEAR_DIRECTION_ERROR_VALUES_Defaults = new Dictionary<SAINSoundType, float>()
        {
            { SAINSoundType.SuppressedShot, 30f },
            { SAINSoundType.BulletImpact, 30f },
        };
        const float defaultDirectionError = 20f;
        Helpers.ListHelpers.PopulateKeys(HEAR_DIRECTION_ERROR_VALUES_Defaults, defaultDirectionError);

        HEAR_CONFIDENCE_GAIN_VALUES_Defaults = new Dictionary<SAINSoundType, float>()
        {
            { SAINSoundType.Shot, 0.35f },
            { SAINSoundType.SuppressedShot, 0.35f },
            { SAINSoundType.FootStep, 0.15f },
            { SAINSoundType.Sprint, 0.15f },
        };
        const float defaultConfidenceGain = 0.1f;
        Helpers.ListHelpers.PopulateKeys(HEAR_CONFIDENCE_GAIN_VALUES_Defaults, defaultConfidenceGain);

        // Gunfire Hearing Distances
        HearingDistancesDefaults = new Dictionary<string, float>()
        {
            { Calibers.Caliber20x1mm, 50 },
            { Calibers.Caliber9x18PM, 110f },
            { Calibers.Caliber9x19PARA, 110f },
            { Calibers.Caliber46x30, 120f },
            { Calibers.Caliber9x21, 120f },
            { Calibers.Caliber57x28, 120f },
            { Calibers.Caliber762x25TT, 120f },
            { Calibers.Caliber1143x23ACP, 115f },
            { Calibers.Caliber9x33R, 125 },
            { Calibers.Caliber545x39, 160 },
            { Calibers.Caliber556x45NATO, 160 },
            { Calibers.Caliber9x39, 160 },
            { Calibers.Caliber762x35, 175 },
            { Calibers.Caliber762x39, 175 },
            { Calibers.Caliber366TKM, 175 },
            { Calibers.Caliber762x51, 200f },
            { Calibers.Caliber127x55, 200f },
            { Calibers.Caliber762x54R, 225f },
            { Calibers.Caliber86x70, 250f },
            { Calibers.Caliber20g, 185 },
            { Calibers.Caliber12g, 185 },
            { Calibers.Caliber23x75, 210 },
            { Calibers.Caliber26x75, 50 },
            { Calibers.Caliber30x29, 50 },
            { Calibers.Caliber40x46, 50 },
            { Calibers.Caliber40mmRU, 50 },
            { Calibers.Caliber127x99, 275 },
            { Calibers.Caliber127x108, 300 },
            { Calibers.Caliber725, 400 },
            { Calibers.Caliber68x51, 200f },
            { Calibers.Default, 125 },
        };
        const float defaultDistance = 125;
        Helpers.ListHelpers.PopulateKeys(HearingDistancesDefaults, Calibers.All, defaultDistance);
    }

    [DataMember]
    [Name("Door Open Sound Range")]
    [Description("The Maximum range a bot can hear a door opening")]
    [Category("Hearing Distance")]
    [MinMax(0, 100, 1)]
    public float DOOR_OPEN_SOUND_RANGE = 40;

    [DataMember]
    [Name("Door Kick Sound Range")]
    [Description("The Maximum range a bot can hear a door being kicked")]
    [Category("Hearing Distance")]
    [MinMax(0, 100, 1)]
    public float DOOR_KICK_SOUND_RANGE = 65;

    [DataMember]
    [Name("Rain Sound Multiplier - Outdoors")]
    [Description(
        "If it is raining, reduce heard distances by up to X amount. Depending on intensity of rain. Scales linearly with rain value."
    )]
    [Category("Hearing Distance")]
    [MinMax(0.01f, 1f, 1000f)]
    public float RAIN_SOUND_COEF_OUTSIDE = 0.5f;

    [DataMember]
    [Name("Rain Sound Multiplier - Inside Building")]
    [Description(
        "If it is raining, reduce heard distances by up to X amount. Depending on intensity of rain. Scales linearly with rain value."
    )]
    [Category("Hearing Distance")]
    [MinMax(0.01f, 1f, 1000f)]
    public float RAIN_SOUND_COEF_INSIDE = 0.75f;

    [DataMember]
    [Name("Max Footstep Audio Distance")]
    [Description(
        "The Maximum Range that a bot can hear footsteps, sprinting, and jumping, turning, gear sounds, and any movement related sounds, in meters. "
            + "This is a theoretical max range, actual range heavily changes depending on conditions."
    )]
    [Category("Hearing Distance")]
    [MinMax(10f, 150f, 100f)]
    public float MaxFootstepAudioDistance = 70f;

    [DataMember]
    [Name("Max Footstep Audio Distance without Headphones")]
    [Description(
        "The Maximum Range that a bot can hear footsteps, sprinting, and jumping, turning, gear sounds, and any movement related sounds, in meters. "
            + "This is a theoretical max range, actual range heavily changes depending on conditions."
    )]
    [Category("Hearing Distance")]
    [MinMax(10f, 150f, 100f)]
    public float MaxFootstepAudioDistanceNoHeadphones = 50f;

    [DataMember]
    [Name("Hearing Direction Error")]
    [Description(Direction_error_descr)]
    [Category("Position Randomization")]
    [Advanced]
    [MinMax(0f, 90f, 10f)]
    [DefaultDictionary(nameof(HEAR_DIRECTION_ERROR_VALUES_Defaults))]
    public Dictionary<SAINSoundType, float> HEAR_DIRECTION_ERROR_VALUES = new()
    {
        { SAINSoundType.SuppressedShot, 30f },
        { SAINSoundType.BulletImpact, 30f },
    };

    [IgnoreDataMember]
    [Hidden]
    public static readonly Dictionary<SAINSoundType, float> HEAR_DIRECTION_ERROR_VALUES_Defaults;

    [DataMember]
    [Name("Hearing Direction Error - Indoors")]
    [Description("Multiplies the direction error when the sound is made inside a building, where it echoes.")]
    [Category("Position Randomization")]
    [Advanced]
    [MinMax(1f, 3f, 100f)]
    public float HEAR_DIRECTION_ERROR_INDOOR_MULTI = 1.5f;

    [DataMember]
    [Name("Hearing Distance Error")]
    [Description(
        "How far off a bot's guess of a sound's distance can be, as a fraction of the real distance. "
            + "0.4 means a sound 100 meters away can be guessed anywhere from 60 to 140 meters away, before confidence shrinks it."
    )]
    [Category("Position Randomization")]
    [Advanced]
    [MinMax(0f, 1f, 100f)]
    public float HEAR_DISTANCE_ERROR = 0.4f;

    [DataMember]
    [Name("Hearing Confidence Gain")]
    [Description(Confidence_gain_descr)]
    [Category("Position Randomization")]
    [Advanced]
    [MinMax(0f, 1f, 100f)]
    [DefaultDictionary(nameof(HEAR_CONFIDENCE_GAIN_VALUES_Defaults))]
    public Dictionary<SAINSoundType, float> HEAR_CONFIDENCE_GAIN_VALUES = new()
    {
        { SAINSoundType.Shot, 0.35f },
        { SAINSoundType.SuppressedShot, 0.35f },
        { SAINSoundType.FootStep, 0.15f },
        { SAINSoundType.Sprint, 0.15f },
    };

    [IgnoreDataMember]
    [Hidden]
    public static readonly Dictionary<SAINSoundType, float> HEAR_CONFIDENCE_GAIN_VALUES_Defaults;

    [DataMember]
    [Name("Hearing Error at Full Confidence")]
    [Description(
        "At full confidence, the direction and distance errors are multiplied by this. "
            + "0.3 means a bot that has heard an enemy many times is still up to 30% as wrong as on the first sound. Never pinpoints from sound alone above 0."
    )]
    [Category("Position Randomization")]
    [Advanced]
    [MinMax(0f, 1f, 100f)]
    public float HEAR_MIN_ERROR_COEF = 0.3f;

    [DataMember]
    [Name("Hearing Direction Error - Sound Behind")]
    [Description(Hear_angle_descr)]
    [Category("Position Randomization")]
    [Advanced]
    [MinMax(0.1f, 3f, 1000f)]
    public float HEAR_DIRECTION_ERROR_BEHIND_MULTI = 1.5f;

    [DataMember]
    [Name("Hearing Direction Error - Sound In Front")]
    [Description(Hear_angle_descr)]
    [Category("Position Randomization")]
    [Advanced]
    [MinMax(0.1f, 3f, 1000f)]
    public float HEAR_DIRECTION_ERROR_FRONT_MULTI = 0.5f;

    [DataMember]
    [Name("Bunker Audio Range")]
    [Description("Reduces audio range if a bot and an enemy are not in the same bunker")]
    [Category("Hearing Environment Modifiers")]
    [Advanced]
    [MinMax(0.01f, 1f, 1000f)]
    public float BUNKER_REDUCTION_COEF = 0.2f;

    [DataMember]
    [Name("Bunker Elevation Range")]
    [Description("Reduces audio range if a bot and an enemy are both in a bunker, but at different levels")]
    [Category("Hearing Environment Modifiers")]
    [Advanced]
    [MinMax(0.01f, 1f, 100f)]
    public float BUNKER_ELEV_DIFF_COEF = 0.66f;

    [DataMember]
    [Name("Gunshot Occlusion")]
    [Description("If an obstacle is inbetween a bot's head and the position of a sound, reduce its range by this amount")]
    [Category("Hearing Environment Modifiers")]
    [Advanced]
    [MinMax(0.01f, 1f, 1000f)]
    public float GUNSHOT_OCCLUSION_MOD = 0.8f;

    [DataMember]
    [Name("Suppressed Gunshot Occlusion")]
    [Description("If an obstacle is inbetween a bot's head and the position of a sound, reduce its range by this amount")]
    [Category("Hearing Environment Modifiers")]
    [Advanced]
    [MinMax(0.01f, 1f, 100f)]
    public float GUNSHOT_OCCLUSION_MOD_SUPP = 0.65f;

    [DataMember]
    [Name("Footstep Occlusion")]
    [Description("If an obstacle is inbetween a bot's head and the position of a sound, reduce its range by this amount")]
    [Category("Hearing Environment Modifiers")]
    [Advanced]
    [MinMax(0.01f, 1f, 1000f)]
    public float FOOTSTEP_OCCLUSION_MOD = 0.6f;

    [DataMember]
    [Name("Sprint Occlusion")]
    [Description("If an obstacle is inbetween a bot's head and the position of a sound, reduce its range by this amount")]
    [Category("Hearing Environment Modifiers")]
    [Advanced]
    [MinMax(0.01f, 1f, 100f)]
    public float FOOTSTEP_OCCLUSION_MOD_SPRINT = 0.8f;

    [DataMember]
    [Name("Other Occlusion")]
    [Description("If an obstacle is inbetween a bot's head and the position of a sound, reduce its range by this amount")]
    [Category("Hearing Environment Modifiers")]
    [Advanced]
    [MinMax(0.01f, 1f, 100f)]
    public float OTHER_OCCLUSION_MOD = 0.6f;

    [DataMember]
    [Name("Indoor / Outdoor Difference - Gunfire")]
    [Description("If bots are not in the same area as the source of a sound, reduce audio range by this amount")]
    [Category("Hearing Environment Modifiers")]
    [Advanced]
    [MinMax(0.01f, 1f, 1000f)]
    public float GUNSHOT_ENVIR_MOD = 0.65f;

    [DataMember]
    [Name("Indoor / Outdoor Difference - Footsteps/Other")]
    [Description("If bots are not in the same area as the source of a sound, reduce audio range by this amount")]
    [Category("Hearing Environment Modifiers")]
    [Advanced]
    [MinMax(0.01f, 1f, 100f)]
    public float FOOTSTEP_ENVIR_MOD = 0.7f;

    [DataMember]
    [Name("Environment Modifier Minimum")]
    [Description("")]
    [Category("Hearing Environment Modifiers")]
    [DeveloperOption]
    [MinMax(0.01f, 1f, 1000f)]
    public float MIN_ENVIRONMENT_MOD = 0.05f;

    [DataMember]
    [Name("No Headphones")]
    [Description("If a bot does not have headphones, multiply the audible range of all sounds except unsuppressed gunshots by this amount.")]
    [Category("Hearing Modifiers")]
    [MinMax(0.01f, 1f, 100f)]
    public float HEAR_MODIFIER_NO_EARS = 0.7f;

    [DataMember]
    [Name("Light Helmet")]
    [Description("If a bot without headphones is wearing a helmet or face cover that slightly muffles hearing, multiply the audible range of all sounds except unsuppressed gunshots by this amount, on top of No Headphones.")]
    [Category("Hearing Modifiers")]
    [MinMax(0.01f, 1f, 100f)]
    public float HEAR_MODIFIER_LIGHT_HELMET = 0.9f;

    [DataMember]
    [Name("Heavy Helmet")]
    [Description("If a bot without headphones is wearing a helmet or face cover that strongly muffles hearing, multiply the audible range of all sounds except unsuppressed gunshots by this amount, on top of No Headphones.")]
    [Category("Hearing Modifiers")]
    [MinMax(0.01f, 1f, 100f)]
    public float HEAR_MODIFIER_HEAVY_HELMET = 0.69f;

    [DataMember]
    [Name("Dying")]
    [Description("If a bot is dying or seriously injured, reduce audible range of all sounds by this amount.")]
    [Category("Hearing Modifiers")]
    [MinMax(0.01f, 1f, 1000f)]
    public float HEAR_MODIFIER_DYING = 0.8f;

    [DataMember]
    [Name("Sprinting")]
    [Description("If a bot is sprinting, reduce audible range of all sounds by this amount.")]
    [Category("Hearing Modifiers")]
    [Advanced]
    [MinMax(0.01f, 1f, 1000f)]
    public float HEAR_MODIFIER_SPRINT = 0.85f;

    [DataMember]
    [Name("Heavy Breathing")]
    [Description("If a bot is breathing heavily, reduce audible range of all sounds by this amount.")]
    [Category("Hearing Modifiers")]
    [Advanced]
    [MinMax(0.01f, 1f, 1000f)]
    public float HEAR_MODIFIER_HEAVYBREATH = 0.65f;

    [DataMember]
    [Name("Minimum Hear Modifier")]
    [Description("Final Multiplier will not go below this value.")]
    [Category("Hearing Modifiers")]
    [DeveloperOption]
    [MinMax(0.01f, 1f, 1000f)]
    public float HEAR_MODIFIER_MIN_CLAMP = 0.01f;

    [DataMember]
    [Name("Maximum Hear Modifier")]
    [Description("Final Multiplier will not go above this value.")]
    [Category("Hearing Modifiers")]
    [DeveloperOption]
    [MinMax(1f, 5f, 1000f)]
    public float HEAR_MODIFIER_MAX_CLAMP = 5f;

    [DataMember]
    [Name("Scale Start Distance - No Headphones")]
    [Description("Sounds that originate closer than this have a 100% chance of being heard.")]
    [Category("Hearing Chance")]
    [Advanced]
    [MinMax(0, 10, 100)]
    public float HEAR_CHANCE_MIN_DIST = 0.25f;

    [DataMember]
    [Name("Scale Start Distance - Headphones")]
    [Description("Sounds that originate closer than this have a 100% chance of being heard.")]
    [Category("Hearing Chance")]
    [Advanced]
    [MinMax(0, 100, 100)]
    public float HEAR_CHANCE_MIN_DIST_HEADPHONES = 1;

    [DataMember]
    [Name("Midrange Coefficient")]
    [Description(
        "If the distance between a sound and the bot is X distance of its max range, increase its chance to hear slightly. So if a sound has a range of 50 meters, and the distance to a bot hears is 25 meters away, that would result in 25 / 50, so 0.5, which is below this mid range value."
    )]
    [Category("Hearing Chance")]
    [Advanced]
    [MinMax(0.00f, 1f, 1000f)]
    public float HEAR_CHANCE_MIDRANGE_COEF = 0.66f;

    [DataMember]
    [Name("Mid range Minimum Chance - Headphones")]
    [Description("If a sound is within mid-range. Increase the minimum chance by this amount")]
    [Category("Hearing Chance")]
    [Advanced]
    [MinMax(0, 100, 1)]
    public float HEAR_CHANCE_MIDRANGE_MINCHANCE_HEADPHONES = 3;

    [DataMember]
    [Name("Long range Minimum Chance - Headphones")]
    [Description("If a sound is further than mid-range. Increase the minimum chance by this amount")]
    [Category("Hearing Chance")]
    [Advanced]
    [MinMax(0, 100, 1)]
    public float HEAR_CHANCE_LONGRANGE_MINCHANCE_HEADPHONES = 1;

    [DataMember]
    [Name("Standing Still Velocity")]
    [Description("Boost Hearing chance slightly if a bot's velocity is under this value.")]
    [Category("Hearing Chance")]
    [Advanced]
    [MinMax(0.0f, 1f, 1000f)]
    public float HEAR_CHANCE_NOTMOVING_VELOCITY = 0.05f;

    [DataMember]
    [Name("Standing Still Min Chance - No Headphones")]
    [Description("If a bot is standing still, add X percent to a bot minimum hear chance for sounds that aren't gunshots.")]
    [Category("Hearing Chance")]
    [Advanced]
    [MinMax(0, 100, 1)]
    public float HEAR_CHANCE_NOTMOVING_MINCHANCE = 2;

    [DataMember]
    [Name("Standing Still Min Chance - Headphones")]
    [Description("If a bot is standing still, add X percent to a bot minimum hear chance for sounds that aren't gunshots.")]
    [Category("Hearing Chance")]
    [MinMax(0, 100, 1)]
    public float HEAR_CHANCE_NOTMOVING_MINCHANCE_HEADPHONES = 4;

    [DataMember]
    [Name("Other Sounds Min Chance - Headphones")]
    [Description("If the type of sound is not footsteps or gunfire, add X percent to a bot minimum hear chance.")]
    [Category("Hearing Chance")]
    [Advanced]
    [MinMax(0, 100, 1)]
    public float HEAR_CHANCE_HEADPHONES_OTHERSOUNDS = 3;

    [DataMember]
    [Name("Active Enemy Min Chance - No Headphones")]
    [Description(
        "If the source of a sound is from a bot's active primary enemy, add X percent to a bot minimum hear chance for sounds that aren't gunshots."
    )]
    [Category("Hearing Chance")]
    [Advanced]
    [MinMax(0, 100, 1)]
    public float HEAR_CHANCE_CURRENTENEMY_MINCHANCE = 2;

    [DataMember]
    [Name("Active Enemy Min Chance - Headphones")]
    [Description(
        "If the source of a sound is from a bot's active primary enemy, add X percent to a bot minimum hear chance for sounds that aren't gunshots."
    )]
    [Category("Hearing Chance")]
    [Advanced]
    [MinMax(0, 100, 1)]
    public float HEAR_CHANCE_CURRENTENEMY_MINCHANCE_HEADPHONES = 3;

    [DataMember]
    [MinMax(1f, 150f, 100f)]
    [Category("Hearing Distance")]
    [Name("Looting Sound")]
    [Advanced]
    public float BaseSoundRange_Looting = 40f;

    [DataMember]
    [MinMax(1f, 150f, 100f)]
    [Category("Hearing Distance")]
    [Name("Grenade Pullout and Pin Pull")]
    [Advanced]
    public float BaseSoundRange_GrenadePinDraw = 35f;

    [DataMember]
    [MinMax(1f, 150f, 100f)]
    [Category("Hearing Distance")]
    [Name("Footstep Sound")]
    [Advanced]
    public float BaseSoundRange_Footstep = 70f;

    [DataMember]
    [MinMax(1f, 150f, 100f)]
    [Category("Hearing Distance")]
    [Name("Sprint Sound")]
    [Advanced]
    public float BaseSoundRange_Sprint = 70f;

    [DataMember]
    [MinMax(1f, 150f, 100f)]
    [Category("Hearing Distance")]
    [Name("Jump Sound")]
    [Advanced]
    public float BaseSoundRange_Jump = 70f;

    [DataMember]
    [MinMax(1f, 150f, 100f)]
    [Category("Hearing Distance")]
    [Name("Prone Sound")]
    [Advanced]
    public float BaseSoundRange_Prone = 50f;

    [DataMember]
    [MinMax(1f, 150f, 100f)]
    [Category("Hearing Distance")]
    [Name("Healing Sound")]
    [Advanced]
    public float BaseSoundRange_Healing = 40f;

    [DataMember]
    [MinMax(1f, 150f, 100f)]
    [Category("Hearing Distance")]
    [Name("Reload Sound")]
    [Advanced]
    public float BaseSoundRange_Reload = 30f;

    [DataMember]
    [MinMax(1f, 150f, 100f)]
    [Category("Hearing Distance")]
    [Name("Surgery Sound")]
    [Advanced]
    public float BaseSoundRange_Surgery = 55f;

    [DataMember]
    [MinMax(1f, 150f, 100f)]
    [Category("Hearing Distance")]
    [Name("Dryfire Sound")]
    [Advanced]
    public float BaseSoundRange_DryFire = 10f;

    [DataMember]
    [MinMax(1f, 150f, 100f)]
    [Category("Hearing Distance")]
    [Name("Aim Down Sights Sound")]
    [Advanced]
    public float BaseSoundRange_AimingandGearRattle = 35f;

    [DataMember]
    [MinMax(1f, 150f, 100f)]
    [Category("Hearing Distance")]
    [Name("Eat and Drink Sound")]
    [Advanced]
    public float BaseSoundRange_EatDrink = 40f;

    [DataMember]
    [MinMax(1f, 150f, 100f)]
    [Category("Hearing Distance")]
    [Name("Max Squad Communication Range - No Headphones")]
    public float MaxRangeToReportEnemyActionNoHeadset = 50f;

    [DataMember]
    [Name("Global Gunshot Audible Range Multiplier")]
    [MinMax(0.1f, 2f, 100f)]
    [Category("Hearing Distance")]
    public float GunshotAudioMultiplier = 1f;

    [DataMember]
    [Name("Global Footstep Audible Range Multiplier")]
    [MinMax(0.1f, 2f, 100f)]
    [Category("Hearing Distance")]
    public float FootstepAudioMultiplier = 1f;

    [DataMember]
    [Name("Suppressed Sound Modifier")]
    [Description("Audible Gun Range is multiplied by this number when using a suppressor")]
    [MinMax(0.1f, 0.95f, 100f)]
    [Category("Hearing Distance")]
    public float SuppressorModifier = 0.6f;

    [DataMember]
    [Name("Subsonic Sound Modifier")]
    [Description("Audible Gun Range is multiplied by this number when using a suppressor and subsonic ammo")]
    [MinMax(0.1f, 0.95f, 100f)]
    [Category("Hearing Distance")]
    public float SubsonicModifier = 0.33f;

    [DataMember]
    [Name("Hearing Distances by Ammo Type")]
    [Description("How far a bot can hear a gunshot when fired from each specific caliber listed here.")]
    [Category("Hearing Distance")]
    [MinMax(30f, 400f, 10f)]
    [Advanced]
    [DefaultDictionary(nameof(HearingDistancesDefaults))]
    public Dictionary<string, float> HearingDistances = new()
    {
        { Calibers.Caliber9x18PM, 110f },
        { Calibers.Caliber9x19PARA, 110f },
        { Calibers.Caliber46x30, 120f },
        { Calibers.Caliber9x21, 120f },
        { Calibers.Caliber57x28, 120f },
        { Calibers.Caliber762x25TT, 120f },
        { Calibers.Caliber1143x23ACP, 115f },
        { Calibers.Caliber9x33R, 125 },
        { Calibers.Caliber545x39, 160 },
        { Calibers.Caliber556x45NATO, 160 },
        { Calibers.Caliber9x39, 160 },
        { Calibers.Caliber762x35, 175 },
        { Calibers.Caliber762x39, 175 },
        { Calibers.Caliber366TKM, 175 },
        { Calibers.Caliber762x51, 200f },
        { Calibers.Caliber127x55, 200f },
        { Calibers.Caliber762x54R, 225f },
        { Calibers.Caliber86x70, 250f },
        { Calibers.Caliber20g, 185 },
        { Calibers.Caliber12g, 185 },
        { Calibers.Caliber23x75, 210 },
        { Calibers.Caliber20x1mm, 50 },
        { Calibers.Caliber26x75, 50 },
        { Calibers.Caliber30x29, 50 },
        { Calibers.Caliber40x46, 50 },
        { Calibers.Caliber40mmRU, 50 },
        { Calibers.Caliber127x99, 275 },
        { Calibers.Caliber127x108, 300 },
        { Calibers.Caliber725, 400 },
        { Calibers.Caliber68x51, 200f },
        { Calibers.Default, 125 },
    };

    [IgnoreDataMember]
    [Hidden]
    public static readonly Dictionary<string, float> HearingDistancesDefaults = new()
    {
        { Calibers.Caliber9x18PM, 110f },
        { Calibers.Caliber9x19PARA, 110f },
        { Calibers.Caliber46x30, 120f },
        { Calibers.Caliber9x21, 120f },
        { Calibers.Caliber57x28, 120f },
        { Calibers.Caliber762x25TT, 120f },
        { Calibers.Caliber1143x23ACP, 115f },
        { Calibers.Caliber9x33R, 125 },
        { Calibers.Caliber545x39, 160 },
        { Calibers.Caliber556x45NATO, 160 },
        { Calibers.Caliber9x39, 160 },
        { Calibers.Caliber762x35, 175 },
        { Calibers.Caliber762x39, 175 },
        { Calibers.Caliber366TKM, 175 },
        { Calibers.Caliber762x51, 200f },
        { Calibers.Caliber127x55, 200f },
        { Calibers.Caliber762x54R, 225f },
        { Calibers.Caliber86x70, 250f },
        { Calibers.Caliber20g, 185 },
        { Calibers.Caliber12g, 185 },
        { Calibers.Caliber23x75, 210 },
        { Calibers.Caliber20x1mm, 50 },
        { Calibers.Caliber26x75, 50 },
        { Calibers.Caliber30x29, 50 },
        { Calibers.Caliber40x46, 50 },
        { Calibers.Caliber40mmRU, 50 },
        { Calibers.Caliber127x99, 275 },
        { Calibers.Caliber127x108, 300 },
        { Calibers.Caliber725, 400 },
        { Calibers.Caliber68x51, 200f },
        { Calibers.Default, 125 },
    };

    public override void Init(List<ISAINSettings> list)
    {
        Helpers.ListHelpers.CloneEntries(HEAR_DIRECTION_ERROR_VALUES_Defaults, HEAR_DIRECTION_ERROR_VALUES);
        Helpers.ListHelpers.CloneEntries(HEAR_CONFIDENCE_GAIN_VALUES_Defaults, HEAR_CONFIDENCE_GAIN_VALUES);
        Helpers.ListHelpers.CloneEntries(HearingDistancesDefaults, HearingDistances);
        list.Add(this);
    }

    [IgnoreDataMember]
    [Hidden]
    private const string Direction_error_descr =
        "How far off, in degrees either side, a bot's guess of the direction a sound came from can be. "
        + "Like a person, a bot gets the direction roughly right and the distance less so, so the error in meters grows with distance. "
        + "Multiplied by the Sound In Front and Sound Behind values below, and by the indoor multiplier. "
        + "Example: 20 degrees, bot facing the sound, Sound In Front 0.5: within 10 degrees either side, about 35 meters off sideways at 200 meters. "
        + "The guess keeps the enemy's height, and shrinks with confidence as the bot keeps hearing them.";

    [IgnoreDataMember]
    [Hidden]
    private const string Confidence_gain_descr =
        "How much each heard sound adds to a bot's confidence about where an enemy is, from 0 to 1. "
        + "Sounds within half a second of the last guess reuse it and add nothing, so rapid fire doesn't max it out instantly. "
        + "Confidence shrinks the direction and distance errors toward the 'Error at Full Confidence' value. "
        + "It is kept through silence, since a quiet enemy is assumed to have stayed put, "
        + "and resets when the enemy is heard again more than 3 meters away after more than 3 seconds of silence.";

    [IgnoreDataMember]
    [Hidden]
    private const string Hear_angle_descr =
        "If a bot is looking at the source of a sound, its direction error is multiplied by the Sound In Front value. "
        + "If it is directly behind them, it is multiplied by the Sound Behind value. "
        + "It is a linear scale between these, so a sound directly to their right or left is halfway between the two. "
        + "Setting both to 1.0 disables this.";
}
