using System.Text;
using DrakiaXYZ.BigBrain.Brains;
using EFT;
using SAIN.Components;
using UnityEngine;

namespace SAIN.Layers.Combat.Solo.Cover;

internal class DoSurgeryAction(BotOwner botOwner) : BotAction(botOwner), IBotAction
{
    public override void Update(CustomLayer.ActionData data)
    {
        checkDoSurgery();
    }

    private void checkDoSurgery()
    {
        BotComponent bot = Bot;
        var sainSurgery = bot.Medical.Surgery;
        if (sainSurgery.AreaClearForSurgery)
        {
            bot.Mover.ActivePath?.Cancel(0.1f);
            bot.Mover.SetTargetMoveSpeed(0f);
            bot.Cover.TrySetProneConditional(bot.GoalEnemy);
            var eftSurgery = bot.BotOwner.Medecine.SurgicalKit;
            if (_startSurgeryTime < Time.time && !eftSurgery.Using && eftSurgery.ShallStartUse())
            {
                eftSurgery.ApplyToCurrentPart(new System.Action(onSurgeryDone));
                if (eftSurgery.Using)
                {
                    sainSurgery.OnSurgeryStarted();
                }
                else
                {
                    sainSurgery.OnSurgeryFailedToStart();
                    bot.Decision.ResetDecisions();
                }
            }
        }
        else
        {
            bot.Mover.SetTargetPose(1);

            sainSurgery.CancelSurgery();
            bot.Mover.DogFight.DogFightMove(false, bot.GoalEnemy);
        }
    }

    public override void OnSteeringTicked()
    {
        if (!TryShootAnyTarget(Bot.GoalEnemy)) { }
        Bot.Steering.SteerByPriority(Bot.GoalEnemy, true);
    }

    private void onSurgeryDone()
    {
        Bot.Medical.Surgery.OnSurgeryDone();
        _startSurgeryTime = Time.time + 1f;

        if (!BotOwner.Medecine.SurgicalKit.HaveWork)
        {
            Bot.Decision.ResetDecisions();
        }
    }

    public override void Start()
    {
        base.Start();
        Bot.Mover.PauseMovement(3f);
        _startSurgeryTime = Time.time + 1f;
    }

    private float _startSurgeryTime;

    public override void Stop()
    {
        base.Stop();
        // Ending the AI action must not abandon a medical operation in hands.
        Bot.Medical.Surgery.CancelSurgery();
        Bot.Mover?.ActivePath?.UnPause();
    }

    public override void BuildDebugText(StringBuilder stringBuilder)
    {
        stringBuilder.AppendLine($"Health Status {Bot.Memory.Health.HealthStatus}");
        stringBuilder.AppendLine($"Using Kit? {BotOwner.Medecine.SurgicalKit.Using}");
        stringBuilder.AppendLine($"Time Since Surgery Started {Time.time - Bot.Medical.Surgery.SurgeryStartTime}");
        stringBuilder.AppendLine($"Area Clear? {Bot.Medical.Surgery.AreaClearForSurgery}");
        stringBuilder.AppendLine($"ShallStartUse Surgery? {BotOwner.Medecine.SurgicalKit.ShallStartUse()}");
        stringBuilder.AppendLine($"IsBleeding? {BotOwner.Medecine.FirstAid.IsBleeding}");
    }
}
