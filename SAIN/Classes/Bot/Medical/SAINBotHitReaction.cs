using System.Collections.Generic;
using EFT;
using EFT.Ballistics;
using EFT.HealthSystem;
using SAIN.Components;

namespace SAIN.SAINComponent.Classes;

public class SAINBotHitReaction : BotBase
{
    public IHealthController HealthController
    {
        get { return Player.HealthController; }
    }

    public BodyPartHitEffectClass BodyHitEffect { get; private set; }

    public AimHitEffectClass AimHitEffect { get; private set; }

    public SAINBotHitReaction(BotComponent bot)
        : base(bot)
    {
        BodyHitEffect = new BodyPartHitEffectClass(bot);
        AimHitEffect = new AimHitEffectClass(bot);

        addPart(EBodyPart.Head);
        addPart(EBodyPart.Chest);
        addPart(EBodyPart.LeftArm);
        addPart(EBodyPart.RightArm);
        addPart(EBodyPart.LeftLeg);
        addPart(EBodyPart.RightLeg);
        addPart(EBodyPart.Stomach);
    }

    private void addPart(EBodyPart part)
    {
        BodyParts.Add(part, new BodyPartStatus(part, this));
    }

    public override void ManualUpdate()
    {
        BodyHitEffect.ManualUpdate();
        base.ManualUpdate();
    }

    public EInjurySeverity LeftArmInjury { get; private set; }
    public EInjurySeverity RightArmInjury { get; private set; }

    public bool ArmsInjured
    {
        get { return BodyHitEffect.LeftArmInjury != EInjurySeverity.None || BodyHitEffect.RightArmInjury != EInjurySeverity.None; }
    }

    public override void Dispose()
    {
        BodyHitEffect.Dispose();
        BodyParts.Clear();
        AimHitEffect.Dispose();
        base.Dispose();
    }

    public void GetHit(DamageInfo DamageInfo, EBodyPart bodyPart, float floatVal)
    {
        BodyHitEffect.GetHit(DamageInfo, bodyPart, floatVal);
        AimHitEffect.GetHit(DamageInfo);
    }

    private const float StunDamageThreshold = 50;
    private const float BaseStunTime = 3f;

    public Dictionary<EBodyPart, BodyPartStatus> BodyParts = new();
}
