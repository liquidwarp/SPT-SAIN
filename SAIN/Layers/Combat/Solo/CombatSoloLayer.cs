using EFT;
using SAIN.Extensions;
using SAIN.Layers.Combat.Solo.Cover;
using SAIN.Models.Enums;
using SAIN.Preset.Shared.Enums;

namespace SAIN.Layers.Combat.Solo;

internal class CombatSoloLayer(BotOwner bot, int priority) : SAINLayer(bot, priority, Name, ESAINLayer.Combat)
{
    public static readonly string Name = BuildLayerName("Combat Layer");

    public override Action GetNextAction()
    {
        _lastSelfDecision = _currentSelfDecision;
        _lastDecision = _currentDecision;

        if (_doSurgeryAction)
        {
            _doSurgeryAction = false;
            return new Action(typeof(DoSurgeryAction), $"Surgery");
        }

        System.Type actionType = GetActionType(_lastDecision);
        if (actionType == null)
        {
            return new Action(typeof(StandAndShootAction), $"DEFAULT! {_lastDecision}");
        }
        if (actionType == typeof(SeekCoverAction) && _lastSelfDecision != ESelfActionType.None)
        {
            return new Action(actionType, $"{_lastDecision} + {_lastSelfDecision}");
        }
        return new Action(actionType, $"{_lastDecision}");
    }

    private static System.Type GetActionType(ECombatDecision decision)
    {
        return decision switch
        {
            ECombatDecision.MoveToEngage => typeof(MoveToEngageAction),
            ECombatDecision.MeleeAttack => typeof(MeleeAttackAction),
            ECombatDecision.FightZombies => typeof(FightZombiesAction),
            ECombatDecision.RushEnemy => typeof(RushEnemyAction),
            ECombatDecision.ThrowGrenade => typeof(ThrowGrenadeAction),
            ECombatDecision.ShiftCover => typeof(ShiftCoverAction),
            ECombatDecision.SeekCover or ECombatDecision.Retreat => typeof(SeekCoverAction),
            ECombatDecision.ShootDistantEnemy or ECombatDecision.StandAndShoot => typeof(StandAndShootAction),
            ECombatDecision.Search => typeof(SearchAction),
            ECombatDecision.Freeze => typeof(FreezeAction),
            _ => null,
        };
    }

    public override bool IsActive()
    {
        if (!BotOwner.IsBotActive())
        {
            CheckActiveChanged(false);
            return false;
        }

        bool active = GetBotComponent() && _currentDecision != ECombatDecision.None;
        CheckActiveChanged(active);
        return active;
    }

    public override bool IsCurrentActionEnding()
    {
        if (base.IsCurrentActionEnding())
        {
            return true;
        }

        // this is dumb im sorry
        if (!_doSurgeryAction && _currentSelfDecision == ESelfActionType.Surgery && Bot.Cover.CoverInUse != null)
        {
            _doSurgeryAction = true;
            return true;
        }

        if (_lastSelfDecision == ESelfActionType.Surgery && _currentSelfDecision != ESelfActionType.Surgery)
        {
            return true;
        }
        if (_currentDecision == _lastDecision)
        {
            return false;
        }
        // Decisions that share an action (SeekCover and Retreat) keep it running: restarting SeekCoverAction cancels the move to cover
        System.Type currentActionType = GetActionType(_currentDecision);
        return currentActionType == null || currentActionType != GetActionType(_lastDecision);
    }

    private bool _doSurgeryAction;

    private ECombatDecision _lastDecision = ECombatDecision.None;
    private ESelfActionType _lastSelfDecision = ESelfActionType.None;
    public ECombatDecision _currentDecision
    {
        get { return Bot.Decision.CurrentCombatDecision; }
    }

    public ESelfActionType _currentSelfDecision
    {
        get { return Bot.Decision.CurrentSelfDecision; }
    }
}
