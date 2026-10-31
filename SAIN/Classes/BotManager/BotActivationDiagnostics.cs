#if DEBUG
using System;
using System.Collections.Generic;
using System.Runtime.ExceptionServices;
using System.Threading;
using EFT;
using SAIN.Classes.Bot.Sense.Hearing;
using UnityEngine;
using UnityEngine.AI;

namespace SAIN.Components.BotController;

/// <summary>
/// Temporary bot activation diagnostics, Debug builds only, enabled by the Debug Bot Activation preset setting. Reports AI
/// players that still haven't become active bots a while after spawning, with how far they got: never pre-activated,
/// PreActivate threw, waiting in PreActive for their weapon or the navmesh, or failed in BotOwner.method_10, which
/// swallows its exceptions and only sets ActiveFail. Logs to the shared diagnostics source.
/// </summary>
internal static class BotActivationDiagnostics
{
    private const float CHECK_INTERVAL = 5f;
    private const float ACTIVATION_GRACE = 15f;

    // Matches the distance EFT samples the navmesh at before activating a PreActive bot.
    private const float NAVMESH_SAMPLE_DISTANCE = 0.6f;

    private sealed class Entry
    {
        public float FirstSeen;
        public float PreActivateStart = -1f;
        public float PreActivateEnd = -1f;
        public Exception PreActivateException;
        public int ActivateAttempts;
        public float ActivateStart = -1f;
        public float ActivateEnd = -1f;
        public Exception ActivateException;
        public bool Reported;
        public bool Resolved;
    }

    private static readonly Dictionary<string, Entry> _entries = [];
    private static Entry _activating;
    private static int _mainThreadId = -1;
    private static bool _subscribed;
    private static float _nextCheckTime;

    public static bool Enabled
    {
        get { return SAINPlugin.DebugSettings.Logs.DebugBotActivation; }
    }

    public static void OnPreActivateStart(BotOwner botOwner)
    {
        if (!Enabled || botOwner == null)
        {
            return;
        }
        Entry entry = GetEntry(botOwner.ProfileId, Time.time);
        entry.PreActivateStart = Time.time;
    }

    public static void OnPreActivateEnd(BotOwner botOwner, Exception exception)
    {
        if (!Enabled || botOwner == null)
        {
            return;
        }
        Entry entry = GetEntry(botOwner.ProfileId, Time.time);
        entry.PreActivateEnd = Time.time;
        entry.PreActivateException = exception;
    }

    public static void OnActivateStart(BotOwner botOwner)
    {
        if (!Enabled || botOwner == null)
        {
            return;
        }
        if (!_subscribed)
        {
            // method_10 catches everything itself, so the only way to see what failed is the first-chance notification.
            _mainThreadId = Thread.CurrentThread.ManagedThreadId;
            AppDomain.CurrentDomain.FirstChanceException += OnFirstChanceException;
            _subscribed = true;
        }
        Entry entry = GetEntry(botOwner.ProfileId, Time.time);
        entry.ActivateAttempts++;
        entry.ActivateStart = Time.time;
        entry.ActivateEnd = -1f;
        entry.ActivateException = null;
        _activating = entry;
    }

    public static void OnActivateEnd(BotOwner botOwner)
    {
        _activating = null;
        if (!Enabled || botOwner == null)
        {
            return;
        }
        Entry entry = GetEntry(botOwner.ProfileId, Time.time);
        entry.ActivateEnd = Time.time;
        // Bots that activated once and were put to sleep later, such as ORBIT ghosts, are not activation failures.
        if (botOwner.BotState == EBotState.Active)
        {
            entry.Resolved = true;
        }
    }

    private static void OnFirstChanceException(object sender, FirstChanceExceptionEventArgs args)
    {
        Entry entry = _activating;
        // Keeps the first one only: later ones are often consequences, and storing it can't throw again.
        if (entry == null || entry.ActivateException != null || Thread.CurrentThread.ManagedThreadId != _mainThreadId)
        {
            return;
        }
        entry.ActivateException = args.Exception;
    }

    public static void Update(List<Player> players, float time)
    {
        if (time < _nextCheckTime || players == null)
        {
            return;
        }
        _nextCheckTime = time + CHECK_INTERVAL;
        for (int i = 0; i < players.Count; i++)
        {
            Player player = players[i];
            if (player == null || !player.IsAI)
            {
                continue;
            }
            Entry entry = GetEntry(player.ProfileId, time);
            if (entry.Resolved)
            {
                continue;
            }
            BotOwner botOwner = player.AIData == null ? null : player.AIData.BotOwner;
            if (botOwner != null && botOwner.BotState == EBotState.Active)
            {
                entry.Resolved = true;
                if (entry.Reported)
                {
                    Report(player, botOwner, entry, time, "activated late");
                }
                continue;
            }
            if (!entry.Reported && time - entry.FirstSeen > ACTIVATION_GRACE)
            {
                entry.Reported = true;
                Report(player, botOwner, entry, time, "still not active");
            }
        }
    }

    public static void EndRaid()
    {
        if (_subscribed)
        {
            AppDomain.CurrentDomain.FirstChanceException -= OnFirstChanceException;
            _subscribed = false;
        }
        _activating = null;
        _entries.Clear();
        _nextCheckTime = 0f;
    }

    private static Entry GetEntry(string profileId, float time)
    {
        if (!_entries.TryGetValue(profileId, out Entry entry))
        {
            entry = new Entry { FirstSeen = time };
            _entries.Add(profileId, entry);
        }
        return entry;
    }

    private static void Report(Player player, BotOwner botOwner, Entry entry, float time, string what)
    {
        Vector3 position = player.Position;
        string state;
        if (botOwner == null)
        {
            state = "no BotOwner";
        }
        else
        {
            BotsGroup group = botOwner.BotsGroup;
            bool weaponReady = botOwner.WeaponManager != null && botOwner.WeaponManager.IsReady;
            bool onNavMesh = NavMesh.SamplePosition(position, out _, NAVMESH_SAMPLE_DISTANCE, NavMesh.AllAreas);
            state =
                $"BotState {botOwner.BotState}, group {(group == null ? "none" : group.Id.ToString())}"
                + $", weapon ready {weaponReady}, on navmesh {onNavMesh}, brain {botOwner.Brain != null}"
                + $", SAIN component {botOwner.GetComponent<BotComponent>() != null}";
        }
        HearingDiagnostics.Log.LogInfo(
            $"[Activation] {player.Profile.Nickname} ({(botOwner == null ? "?" : botOwner.name)}, {player.Profile.Info.Settings.Role}, {player.Side})"
                + $" {what} {time - entry.FirstSeen:F0}s after first seen | {state}, object active {player.gameObject.activeSelf}"
                + $" at {position} | PreActivate {Since(entry.PreActivateStart, time)}, ended {Since(entry.PreActivateEnd, time)}"
                + $" | method_10 attempts {entry.ActivateAttempts}, last {Since(entry.ActivateStart, time)}, ended {Since(entry.ActivateEnd, time)}"
                + (entry.PreActivateException == null ? "" : $" | PreActivate threw: {entry.PreActivateException}")
                + (entry.ActivateException == null ? "" : $" | first exception in method_10: {entry.ActivateException}")
        );
    }

    private static string Since(float eventTime, float time)
    {
        return eventTime < 0f ? "never" : $"{time - eventTime:F1}s ago";
    }
}
#endif
