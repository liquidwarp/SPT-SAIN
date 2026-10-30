#if DEBUG
using System.Collections.Generic;
using System.Text;
using BepInEx.Logging;
using EFT;
using SAIN.Components;
using SAIN.Components.BotComponentSpace.Classes.EnemyClasses;
using SAIN.Components.PlayerComponentSpace;
using SAIN.Helpers;
using SAIN.Preset.Shared.Enums;
using SAIN.SAINComponent.Classes.EnemyClasses;
using UnityEngine;

namespace SAIN.Classes.Bot.Sense.Hearing;

public enum EGunshotStage
{
    OutsideGunfireRadius,
    IgnoringSounds,
    NotEnemy,
    BeyondBaseRange,
    Deafened,
    AILimit,
    OutOfRange,
    NotChased,
    NoSquad,

    // Keep last, it sizes the per-stage counters
    Placed,
}

/// <summary>
/// Temporary bot-vs-bot awareness diagnostics, Debug builds only, enabled by the Debug Bot Awareness preset setting:
/// where gunfire from a nearby bot gets
/// dropped on its way to the listener, when a bot drops an enemy that just shot at it, how hearing estimates move
/// for bots that have held SeekCover for a long time, and whether bots of different sides close to each other are
/// enemies at all. Uses its own log source to keep investigation output separate.
/// </summary>
internal static class HearingDiagnostics
{
    private const float MAX_DISTANCE = 150f;
    private const float WINDOW = 5f;
    private const float PAIR_EXPIRE = 60f;
    private const float SHOT_AT_RECENT = 10f;
    private const float DROP_LOG_INTERVAL = 5f;
    private const float SEEK_COVER_HOLD = 60f;
    private const float ESTIMATE_LOG_INTERVAL = 1f;
    private const int STAGE_COUNT = (int)EGunshotStage.Placed + 1;
    private const float SIDES_MAX_DISTANCE = 30f;
    private const float SIDES_CHECK_INTERVAL = 1f;
    private const float SIDES_PAIR_LOG_INTERVAL = 10f;
    private const float SNAP_SUMMARY_INTERVAL = 120f;
    private const int SNAP_COUNT = (int)EHearingSnap.RawGuess + 1;

    private static readonly ManualLogSource _log = BepInEx.Logging.Logger.CreateLogSource("SAIN Diagnostics");

    public static bool Enabled
    {
        get { return SAINPlugin.DebugSettings.Logs.DebugBotAwareness; }
    }

    private sealed class PairStats(BotComponent listener, PlayerComponent shooter)
    {
        public readonly BotComponent Listener = listener;
        public readonly string ListenerName = listener.PlayerComponent.Name;
        public readonly string ShooterId = shooter.ProfileId;
        public readonly string ShooterName = shooter.Name;
        public readonly int[] Counts = new int[STAGE_COUNT];
        public float WindowStart = -1f;
        public float LastGunshotTime = -1f;
        public float LastActivity;
        public float MinDistance = float.MaxValue;
        public float MaxDistance;
        public bool HasRange;
        public float FinalRange;
        public float BaseRange;
        public float Volume;
        public float Modifier;
        public float Environment;
        public float Condition;
        public float Occlusion;
        public float Difficulty;
        public int FlyBys;
        public int FlyBysUnderFire;
        public float ClosestFlyBy = float.MaxValue;

        public bool HasData
        {
            get { return WindowStart >= 0f; }
        }

        public void Begin(float time)
        {
            if (WindowStart < 0f)
            {
                WindowStart = time;
            }
            LastActivity = time;
        }

        public void Reset()
        {
            WindowStart = -1f;
            MinDistance = float.MaxValue;
            MaxDistance = 0f;
            HasRange = false;
            FlyBys = 0;
            FlyBysUnderFire = 0;
            ClosestFlyBy = float.MaxValue;
            for (int i = 0; i < STAGE_COUNT; i++)
            {
                Counts[i] = 0;
            }
        }
    }

    // listener profile id -> shooter profile id -> stats
    private static readonly Dictionary<string, Dictionary<string, PairStats>> _pairs = [];
    private static readonly List<string> _expiredKeys = [];
    private static readonly List<string> _emptyListeners = [];
    private static readonly Dictionary<string, float> _nextDropLogTime = [];
    private static readonly Dictionary<string, float> _nextEstimateLogTime = [];
    private static readonly Dictionary<string, int> _skippedEstimates = [];
    private static readonly StringBuilder _builder = new();
    private static readonly int[] _snapCounts = new int[SNAP_COUNT];
    private static float _nextSnapSummaryTime = -1f;

    /// <summary>
    /// Counts how each hearing guess got onto walkable ground, to see how often the nav mesh cast and the enemy's own
    /// spot are needed. Collected only with the diagnostics on.
    /// </summary>
    public static void CountSnap(EHearingSnap snap)
    {
        if (Enabled)
        {
            _snapCounts[(int)snap]++;
        }
    }

    public static void EndRaid()
    {
        LogSnapSummary("raid end");
        _pairs.Clear();
        _expiredKeys.Clear();
        _emptyListeners.Clear();
        _nextDropLogTime.Clear();
        _nextEstimateLogTime.Clear();
        _skippedEstimates.Clear();
        _sideBots.Clear();
        _nextSidesPairLogTime.Clear();
        _builder.Clear();
        _nextSidesCheckTime = 0f;
        for (int i = 0; i < SNAP_COUNT; i++)
        {
            _snapCounts[i] = 0;
        }
        _nextSnapSummaryTime = -1f;
    }

    private static void UpdateSnapSummary(float time)
    {
        if (_nextSnapSummaryTime < 0f)
        {
            _nextSnapSummaryTime = time + SNAP_SUMMARY_INTERVAL;
            return;
        }
        if (_nextSnapSummaryTime <= time)
        {
            _nextSnapSummaryTime = time + SNAP_SUMMARY_INTERVAL;
            LogSnapSummary("so far");
        }
    }

    private static void LogSnapSummary(string when)
    {
        if (!Enabled)
        {
            return;
        }
        int total = 0;
        for (int i = 0; i < SNAP_COUNT; i++)
        {
            total += _snapCounts[i];
        }
        if (total == 0)
        {
            return;
        }
        StringBuilder sb = _builder;
        sb.Clear();
        sb.Append($"[Hearing guesses] {when}: {total} placed:");
        for (int i = 0; i < SNAP_COUNT; i++)
        {
            sb.Append($" {(EHearingSnap)i} {_snapCounts[i]} ({100f * _snapCounts[i] / total:0.0}%)");
        }
        _log.LogInfo(sb.ToString());
    }
    private static readonly List<PlayerComponent> _sideBots = [];
    private static readonly Dictionary<string, float> _nextSidesPairLogTime = [];
    private static float _nextSidesCheckTime;

    /// <summary>
    /// For bots of different sides within 30 m of each other, logs once per pair per 10 s whether each one's group
    /// counts the other as an enemy, neutral or ally, and whether SAIN has it as an enemy. SAIN only takes enemies the
    /// group already has, and a group never adds an AI enemy on sight, so a missing entry makes them peaceful.
    /// </summary>
    public static void CheckSides(HashSet<PlayerComponent> players, float time)
    {
        if (_nextSidesCheckTime > time)
        {
            return;
        }
        _nextSidesCheckTime = time + SIDES_CHECK_INTERVAL;

        _sideBots.Clear();
        foreach (PlayerComponent player in players)
        {
            if (player != null && player.IsAI && player.BotOwner != null && player.Player != null && player.BotOwner.BotsGroup != null)
            {
                _sideBots.Add(player);
            }
        }

        const float maxDistanceSqr = SIDES_MAX_DISTANCE * SIDES_MAX_DISTANCE;
        for (int i = 0; i < _sideBots.Count; i++)
        {
            PlayerComponent a = _sideBots[i];
            for (int j = i + 1; j < _sideBots.Count; j++)
            {
                PlayerComponent b = _sideBots[j];
                if (a.Player.Side == b.Player.Side)
                {
                    continue;
                }
                float distanceSqr = (a.Position - b.Position).sqrMagnitude;
                if (distanceSqr > maxDistanceSqr)
                {
                    continue;
                }
                string key = string.CompareOrdinal(a.ProfileId, b.ProfileId) < 0 ? a.ProfileId + b.ProfileId : b.ProfileId + a.ProfileId;
                if (_nextSidesPairLogTime.TryGetValue(key, out float nextTime) && nextTime > time)
                {
                    continue;
                }
                _nextSidesPairLogTime[key] = time + SIDES_PAIR_LOG_INTERVAL;
                _log.LogInfo(
                    $"[Sides] {Describe(a)} / {Describe(b)} at {Mathf.Sqrt(distanceSqr):0}m | {Relation(a, b)} | {Relation(b, a)}"
                );
            }
        }
    }

    private static string Describe(PlayerComponent bot)
    {
        BotOwner botOwner = bot.BotOwner;
        return $"{bot.Name} ({bot.Player.Side} {botOwner.Profile.Info.Settings.Role}, group {botOwner.BotsGroup.Id})";
    }

    private static string Relation(PlayerComponent from, PlayerComponent to)
    {
        BotsGroup group = from.BotOwner.BotsGroup;
        Player target = to.Player;
        string targetId = to.ProfileId;
        string sainEnemy;
        BotComponent bot = from.BotComponent;
        if (bot == null)
        {
            sainEnemy = "no SAIN";
        }
        else
        {
            Enemy enemy = bot.EnemyController.GetEnemy(targetId, false);
            sainEnemy = enemy == null ? "no" : (enemy.EnemyKnown ? "known" : "unknown");
        }
        return $"{from.Name} -> {to.Name}: IsPlayerEnemy {group.IsPlayerEnemy(target)}, Enemies {BotsGroupHelpers.ContainsPlayer(group.Enemies, targetId)}"
            + $", Neutrals {BotsGroupHelpers.ContainsPlayer(group.Neutrals, targetId)}, Allies {BotsGroupHelpers.ContainsPlayer(group.Allies, targetId)}, SAIN enemy {sainEnemy}";
    }

    public static void RecordGunshot(BotComponent listener, PlayerComponent shooter, float distance, EGunshotStage stage)
    {
        PairStats stats = GetPairStats(listener, shooter, distance);
        if (stats == null)
        {
            return;
        }
        float time = Time.time;
        stats.Begin(time);
        stats.LastGunshotTime = time;
        stats.Counts[(int)stage]++;
        if (distance < stats.MinDistance)
        {
            stats.MinDistance = distance;
        }
        if (distance > stats.MaxDistance)
        {
            stats.MaxDistance = distance;
        }
    }

    public static void RecordGunshot(AISoundData sound, EGunshotStage stage)
    {
        if (sound.IsGunShot)
        {
            RecordGunshot(sound.Bot, sound.Sound.PlayerComponent, sound.PlayerDistance, stage);
        }
    }

    public static void RecordHearingRange(
        AISoundData sound,
        float finalRange,
        float modifier,
        float environment,
        float condition,
        float occlusion,
        float difficulty
    )
    {
        if (!sound.IsGunShot)
        {
            return;
        }
        PairStats stats = GetPairStats(sound.Bot, sound.Sound.PlayerComponent, sound.PlayerDistance);
        if (stats == null)
        {
            return;
        }
        stats.HasRange = true;
        stats.FinalRange = finalRange;
        stats.BaseRange = sound.Sound.Range;
        stats.Volume = sound.Sound.Volume;
        stats.Modifier = modifier;
        stats.Environment = environment;
        stats.Condition = condition;
        stats.Occlusion = occlusion;
        stats.Difficulty = difficulty;
    }

    public static void RecordFlyBy(AISoundData sound, float flyByDistance, bool underFire)
    {
        PairStats stats = GetPairStats(sound.Bot, sound.Sound.PlayerComponent, sound.PlayerDistance);
        if (stats == null)
        {
            return;
        }
        stats.Begin(Time.time);
        stats.FlyBys++;
        if (underFire)
        {
            stats.FlyBysUnderFire++;
        }
        if (flyByDistance < stats.ClosestFlyBy)
        {
            stats.ClosestFlyBy = flyByDistance;
        }
    }

    private static PairStats GetPairStats(BotComponent listener, PlayerComponent shooter, float distance)
    {
        if (listener == null || shooter == null || !shooter.IsAI || distance > MAX_DISTANCE)
        {
            return null;
        }
        string listenerId = listener.ProfileId;
        if (!_pairs.TryGetValue(listenerId, out Dictionary<string, PairStats> shooters))
        {
            shooters = [];
            _pairs[listenerId] = shooters;
        }
        if (!shooters.TryGetValue(shooter.ProfileId, out PairStats stats))
        {
            stats = new PairStats(listener, shooter);
            shooters[shooter.ProfileId] = stats;
        }
        return stats;
    }

    public static void Update(float time)
    {
        UpdateSnapSummary(time);
        _emptyListeners.Clear();
        foreach (KeyValuePair<string, Dictionary<string, PairStats>> listenerEntry in _pairs)
        {
            Dictionary<string, PairStats> shooters = listenerEntry.Value;
            if (shooters.Count == 0)
            {
                _emptyListeners.Add(listenerEntry.Key);
                continue;
            }
            _expiredKeys.Clear();
            foreach (KeyValuePair<string, PairStats> shooterEntry in shooters)
            {
                PairStats stats = shooterEntry.Value;
                if (stats.Listener == null || time - stats.LastActivity > PAIR_EXPIRE)
                {
                    _expiredKeys.Add(shooterEntry.Key);
                    continue;
                }
                if (stats.HasData && time - stats.WindowStart >= WINDOW)
                {
                    LogPair(stats, time);
                    stats.Reset();
                }
            }
            for (int i = 0; i < _expiredKeys.Count; i++)
            {
                shooters.Remove(_expiredKeys[i]);
            }
        }
        for (int i = 0; i < _emptyListeners.Count; i++)
        {
            _pairs.Remove(_emptyListeners[i]);
        }
    }

    private static void LogPair(PairStats stats, float time)
    {
        StringBuilder sb = _builder;
        sb.Clear();
        int[] counts = stats.Counts;
        int gunshots = 0;
        for (int i = 0; i < STAGE_COUNT; i++)
        {
            gunshots += counts[i];
        }
        sb.Append($"[Gunfire] {stats.ListenerName} <- {stats.ShooterName}");
        if (gunshots > 0)
        {
            sb.Append($" {stats.MinDistance:0}-{stats.MaxDistance:0}m x{gunshots}:");
            for (int i = 0; i < STAGE_COUNT; i++)
            {
                if (counts[i] > 0)
                {
                    sb.Append($" {(EGunshotStage)i} {counts[i]}");
                }
            }
        }
        else
        {
            sb.Append(" no gunshots reached the listener");
        }
        if (stats.HasRange)
        {
            sb.Append(
                $" | range {stats.FinalRange:0.0}m = base {stats.BaseRange:0} x vol {stats.Volume:0.00} x mod {stats.Modifier:0.00}"
                    + $" (env {stats.Environment:0.00} cond {stats.Condition:0.00} occl {stats.Occlusion:0.00} diff {stats.Difficulty:0.00})"
            );
        }
        if (stats.FlyBys > 0)
        {
            sb.Append($" | flybys {stats.FlyBys} (closest {stats.ClosestFlyBy:0.0}m, under fire {stats.FlyBysUnderFire})");
        }
        AppendShooterState(sb, stats.Listener, stats.ShooterId, time);
        _log.LogInfo(sb.ToString());
    }

    private static void AppendShooterState(StringBuilder sb, BotComponent listener, string shooterId, float time)
    {
        Enemy enemy = listener.EnemyController.GetEnemy(shooterId, false);
        Enemy goal = listener.GoalEnemy;
        string goalName = goal == null ? "none" : goal.EnemyName;
        if (enemy == null)
        {
            sb.Append($" | shooter not an enemy | goal {goalName}");
            return;
        }
        sb.Append(
            $" | shooter known {enemy.EnemyKnown} chosen {goal == enemy} (goal {goalName}), sinceShotAtMe {SinceTime(enemy.Status.TimeLastShotAtMe, time)}"
                + $", sinceKnownUpdate {enemy.TimeSinceLastKnownUpdated:0.0}s | {listener.Decision.CurrentCombatDecision}/{listener.Decision.CurrentSquadDecision}/{listener.Decision.CurrentSelfDecision}"
        );
    }

    public static void CheckChosenEnemy(BotComponent bot, Enemy previous, Enemy current)
    {
        if (previous == null || current != null || !Enabled)
        {
            return;
        }
        float time = Time.time;
        float sinceShotAtMe = previous.Status.TimeLastShotAtMe < 0f ? float.MaxValue : time - previous.Status.TimeLastShotAtMe;
        float sinceGunfire = SinceGunfire(bot, previous.EnemyProfileId, time);
        if (sinceShotAtMe > SHOT_AT_RECENT && sinceGunfire > SHOT_AT_RECENT)
        {
            return;
        }
        string botId = bot.ProfileId;
        if (_nextDropLogTime.TryGetValue(botId, out float nextTime) && nextTime > time)
        {
            return;
        }
        _nextDropLogTime[botId] = time + DROP_LOG_INTERVAL;

        bool active = previous.WasValid && Enemy.IsEnemyActive(previous);
        _log.LogInfo(
            $"[Dropped enemy] {bot.PlayerComponent.Name} ({bot.Info.Personality}) dropped {previous.EnemyName} at {previous.RealDistance:0}m"
                + $" | sinceShotAtMe {SinceTime(previous.Status.TimeLastShotAtMe, time)}, sinceGunfireHeard {FormatSince(sinceGunfire)}"
                + $" | knownEnemies {bot.EnemyController.KnownEnemies.Count}, known {previous.EnemyKnown}, valid {previous.WasValid}, active {active}"
                + $", lastKnown {(previous.LastKnownPosition != null)}, sinceKnownUpdate {previous.TimeSinceLastKnownUpdated:0.0}s"
                + $", visible {previous.IsVisible}, seen {previous.Seen} ({previous.TimeSinceSeen:0}s), heard {previous.Heard} ({previous.TimeSinceHeard:0}s)"
                + $" | was {bot.Decision.CurrentCombatDecision}/{bot.Decision.CurrentSquadDecision}/{bot.Decision.CurrentSelfDecision}"
                + $", layersActive {bot.SAINLayersActive}"
        );
    }

    public static void LogEstimate(BotComponent bot, SoundPositionEstimate? previous, SoundPositionEstimate result, AISoundData sound)
    {
        if (!Enabled || result.Step == EEstimateStep.Reused || result.Step == EEstimateStep.SightingHeld)
        {
            return;
        }
        var decision = bot.Decision;
        if (decision.CurrentCombatDecision != ECombatDecision.SeekCover)
        {
            return;
        }
        float time = Time.time;
        float held = time - decision.ChangeDecisionTime;
        if (held < SEEK_COVER_HOLD)
        {
            return;
        }
        string botId = bot.ProfileId;
        if (_nextEstimateLogTime.TryGetValue(botId, out float nextTime) && nextTime > time)
        {
            _skippedEstimates.TryGetValue(botId, out int skipped);
            _skippedEstimates[botId] = skipped + 1;
            return;
        }
        _nextEstimateLogTime[botId] = time + ESTIMATE_LOG_INTERVAL;
        _skippedEstimates.TryGetValue(botId, out int skippedSinceLast);
        _skippedEstimates[botId] = 0;

        Vector3 botPosition = bot.Position;
        string moved = "first guess";
        if (previous != null)
        {
            Vector3 before = previous.Value.Position - botPosition;
            Vector3 after = result.Position - botPosition;
            before.y = 0f;
            after.y = 0f;
            float angle = Vector3.Angle(before, after);
            float shift = (result.Position - previous.Value.Position).magnitude;
            moved = $"moved {angle:0.0}deg {shift:0.0}m";
        }
        Enemy enemy = sound.Enemy;
        string enemyName = enemy == null ? "?" : enemy.EnemyName;
        _log.LogInfo(
            $"[Estimate] {bot.PlayerComponent.Name} ({bot.Info.Personality}, SeekCover {held:0}s) {result.Step} {sound.SoundType} from {enemyName}"
                + $" at {sound.PlayerDistance:0}m: {moved}, confidence {result.Confidence:0.00}, skipped {skippedSinceLast}"
        );
    }

    private static float SinceGunfire(BotComponent bot, string shooterId, float time)
    {
        if (
            _pairs.TryGetValue(bot.ProfileId, out Dictionary<string, PairStats> shooters)
            && shooters.TryGetValue(shooterId, out PairStats stats)
            && stats.LastGunshotTime >= 0f
        )
        {
            return time - stats.LastGunshotTime;
        }
        return float.MaxValue;
    }

    private static string SinceTime(float eventTime, float time)
    {
        return eventTime < 0f ? "never" : FormatSince(time - eventTime);
    }

    private static string FormatSince(float since)
    {
        return since == float.MaxValue ? "never" : $"{since:0.0}s";
    }
}
#endif
