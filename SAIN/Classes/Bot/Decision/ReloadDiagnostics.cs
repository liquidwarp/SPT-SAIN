#if DEBUG
using System;
using EFT;
using SAIN.Classes.Bot.Sense.Hearing;
using UnityEngine;

namespace SAIN.SAINComponent.Classes.Decision;

public enum EStuckReloadOutcome
{
    // The flag was set with no reload operation in hands; it was cleared.
    Cleared,

    // A reload operation was still running; it was finished, then the flag was cleared.
    FinishedOperation,

    // A reload operation is still running and did not finish; retried later.
    OperationStillRunning,

    // A reload operation is running on another item than the AI's current weapon; retried later.
    OtherWeapon,
}

/// <summary>
/// Temporary per-bot reload diagnostics, Debug builds only, enabled by the Debug Bot Reloads preset setting. Follows each
/// reload a bot starts and reports the ones that run long, get stuck, or are left behind by a weapon change, to tell the
/// causes apart: a reload that never started a hands operation, an operation that ended without clearing the flag,
/// an operation that never finishes, or something else taking the hands meanwhile. Logs to the shared diagnostics source.
/// </summary>
internal sealed class ReloadDiagnostics
{
    // Matches the point where SAIN's reload decision gives up and other self-actions such as healing may start.
    private const float SLOW_RELOAD = 8f;

    public static bool Enabled
    {
        get { return SAINPlugin.DebugSettings.Logs.DebugBotReloads; }
    }

    private BotReload _reload;
    private float _startTime = -1f;
    private Type _lastOperation;
    private float _lastOperationTime;
    private Type _otherHands;
    private bool _reported;
    private EStuckReloadOutcome _lastOutcome;

    public void Update(BotOwner botOwner, Player player)
    {
        if (!Enabled)
        {
            // Forget the followed reload, so re-enabling doesn't judge it from state that went stale meanwhile.
            _startTime = -1f;
            return;
        }
        BotReload reload = botOwner.WeaponManager?.Reload;
        if (_startTime >= 0f && !ReferenceEquals(reload, _reload))
        {
            // BotReload is per weapon: a stale flag on the previous weapon resurfaces when switching back to it.
            // Reported even after a stuck report, since being left behind is what makes it resurface.
            if (_reload.Reloading)
            {
                Report(botOwner, player, _reload, "left behind by a weapon change, flag still set");
            }
            _startTime = -1f;
        }
        if (reload == null)
        {
            return;
        }
        if (!reload.Reloading)
        {
            if (_startTime >= 0f)
            {
                // A reload operation that ran until the flag cleared, with nothing else in hands, is just a long reload,
                // such as loading an internal magazine round by round.
                bool healthy = _lastOperation != null && Time.time - _lastOperationTime < 1f && _otherHands == null;
                if (!_reported && !healthy && Time.time - _startTime > SLOW_RELOAD)
                {
                    Report(botOwner, player, reload, "finished normally but slowly");
                }
                _startTime = -1f;
            }
            return;
        }
        if (_startTime != reload._reloadStartTime)
        {
            _reload = reload;
            _startTime = reload._reloadStartTime;
            _lastOperation = null;
            _lastOperationTime = -1f;
            _otherHands = null;
            _reported = false;
        }
        var hands = player.HandsController;
        if (hands is Player.FirearmController firearm)
        {
            if (firearm.IsInReloadOperation())
            {
                _lastOperation = firearm.CurrentOperation.GetType();
                _lastOperationTime = Time.time;
            }
        }
        else if (hands != null)
        {
            _otherHands = hands.GetType();
        }
    }

    public void ReportStuck(BotOwner botOwner, Player player, BotReload reload, EStuckReloadOutcome outcome)
    {
        // Retried outcomes are reported once per reload; the final clear is always reported.
        if (_reported && outcome == _lastOutcome)
        {
            return;
        }
        _lastOutcome = outcome;
        Report(botOwner, player, reload, $"stuck: {outcome}");
    }

    private void Report(BotOwner botOwner, Player player, BotReload reload, string what)
    {
        _reported = true;
        var hands = player.HandsController;
        string operation = hands is Player.FirearmController firearm && firearm.CurrentOperation != null
            ? firearm.CurrentOperation.GetType().Name
            : "-";
        string lastOperation = _lastOperation == null
            ? "never seen"
            : $"{_lastOperation.Name} {Time.time - _lastOperationTime:F1}s ago";
        HearingDiagnostics.Log.LogInfo(
            $"[{botOwner.name}] {botOwner.Profile.Info.Settings.Role} {reload._weapon?.Name} {reload._reloadType} reload {what}"
                + $" after {Time.time - reload._reloadStartTime:F1}s;"
                + $" hands {(hands == null ? "none" : hands.GetType().Name)} op {operation}, process {player.ProcessStatus};"
                + $" reload op {lastOperation}; other hands meanwhile {(_otherHands == null ? "none" : _otherHands.Name)};"
                + $" meds using {botOwner.Medecine?.Using == true}, weapon ready {botOwner.WeaponManager.Selector.IsWeaponReady}"
        );
    }
}
#endif
