#if DEBUG
using SAIN.Classes.Bot.Sense.Hearing;
using SAIN.Components;
using SAIN.Helpers;
using SAIN.SAINComponent.Classes.EnemyClasses;
using UnityEngine;
using UnityEngine.AI;

namespace SAIN.Classes.Bot.Search;

/// <summary>
/// Temporary search diagnostics, Debug builds only, enabled by the Debug Bot Awareness preset setting. Reports bots that
/// keep the Search decision while their move toward the search target fails, to tell apart a path the decision and the
/// mover see differently from an enemy instance they don't share. Logs the first failure of a streak, then periodically
/// while it lasts, and once more when it ends. Logs to the shared diagnostics source.
/// </summary>
internal sealed class SearchDiagnostics
{
    private const float REPORT_INTERVAL = 10f;

    // Shared, the log is built on the main thread one bot at a time.
    private static Vector3[] _liveCorners = new Vector3[32];

    public static bool Enabled
    {
        get { return SAINPlugin.DebugSettings.Logs.DebugBotAwareness; }
    }

    private float _streakStart = -1f;
    private float _nextReportTime;
    private int _failures;
    private int _failuresSinceReport;

    public void OnMoveFailed(BotComponent bot, SearchClass search, Enemy target)
    {
        float time = Time.time;
        _failures++;
        _failuresSinceReport++;
        if (_streakStart < 0f)
        {
            _streakStart = time;
            _nextReportTime = time;
        }
        if (_nextReportTime > time)
        {
            return;
        }
        _nextReportTime = time + REPORT_INTERVAL;
        Report(bot, search, target, time);
        _failuresSinceReport = 0;
    }

    /// <summary>Ends the current failure streak, if any: the bot moved again, or stopped searching.</summary>
    public void EndStreak(BotComponent bot, string why)
    {
        if (_streakStart < 0f)
        {
            return;
        }
        HearingDiagnostics.Log.LogInfo(
            $"[Search stuck] {bot.PlayerComponent.Name} ({bot.Info.Personality}) streak ended, {why}, after {Time.time - _streakStart:0.0}s and {_failures} failed moves"
        );
        _streakStart = -1f;
        _failures = 0;
        _failuresSinceReport = 0;
    }

    private void Report(BotComponent bot, SearchClass search, Enemy target, float time)
    {
        SAINEnemyController controller = bot.EnemyController;
        Enemy goal = controller.GoalEnemy;
        string targetText;
        string pathText;
        if (target == null)
        {
            targetText = "target none";
            pathText = "path -";
        }
        else
        {
            Enemy byId = controller.GetEnemy(target.EnemyProfileId, false);
            targetText =
                $"target {target.EnemyName}: same as goal {ReferenceEquals(target, goal)} (goal {(goal == null ? "none" : goal.EnemyName)})"
                + $", same as by id {ReferenceEquals(target, byId)} (by id {(byId == null ? "none" : "found")}), in known {controller.KnownEnemies.Contains(target)}"
                + $", known {target.EnemyKnown}, valid {target.WasValid}, active {Enemy.IsEnemyActive(target)}, on search {target.Events.OnSearch.Value}"
                + $", sinceKnownUpdate {target.TimeSinceLastKnownUpdated:0.0}s";
            pathText = DescribePath(bot, search, target);
        }

        EnemyPlace targetPlace = search.PathFinder.TargetPlace;
        EnemyPlace lastKnown = target?.KnownPlaces.LastKnownPlace;
        string placeText = targetPlace == null
            ? "target place none"
            : $"target place {targetPlace.DistanceToBot:0.0}m, is last known {ReferenceEquals(targetPlace, lastKnown)}, arrived {targetPlace.HasArrivedPersonal}/{targetPlace.HasArrivedSquad}";

        var decision = bot.Decision;
        var reasons = decision.EnemyDecisions.DebugSearchReasons;
        string shallSearch = decision.EnemyDecisions.DebugShallSearch == null ? "not reached" : decision.EnemyDecisions.DebugShallSearch.Value.ToString();
        Vector3 position = bot.Position;
        HearingDiagnostics.Log.LogInfo(
            $"[Search stuck] {bot.PlayerComponent.Name} ({bot.Info.Personality}) move failed for {time - _streakStart:0.0}s, {_failuresSinceReport} since last report ({_failures} total)"
                + $" | state {search.CurrentState}/{search.LastState}/{search.NextState}"
                + $" | decision {decision.CurrentCombatDecision}/{decision.CurrentSquadDecision}/{decision.CurrentSelfDecision} for {decision.TimeSinceChangeDecision:0.0}s"
                + $", shallSearch {shallSearch} ({reasons.NotSearchReason}, {reasons.WantSearchReasons.NotWantToSearchReason}/{reasons.WantSearchReasons.WantToSearchReason}, path {reasons.PathCalcFailReason})"
                + $" | {targetText} | {pathText} | {placeText}"
                + $" | bot at {position}, navmesh {(bot.NavMeshPosition - position).magnitude:0.00}m off, navdata {(bot.Transform.NavData.Position - position).magnitude:0.00}m off"
        );
    }

    private static string DescribePath(BotComponent bot, SearchClass search, Enemy target)
    {
        SAINEnemyPath enemyPath = target.Path;
        NavMeshPath path = enemyPath.PathToEnemy;
        if (path == null)
        {
            return "path null";
        }
        int cachedCount = enemyPath.PathCornerCount;
        int liveCount = NavMeshHelpers.GetCorners(path, ref _liveCorners);
        string ends = "-";
        if (cachedCount > 0 && liveCount > 0)
        {
            Vector3 cachedLast = enemyPath.PathCorners[cachedCount - 1];
            Vector3 liveLast = _liveCorners[liveCount - 1];
            ends = $"last corners {(cachedLast - liveLast).magnitude:0.00}m apart, live last {(liveLast - bot.Position).magnitude:0.0}m from bot";
        }
        string lastKnown = target.LastKnownPosition == null ? "none" : $"{(target.LastKnownPosition.Value - bot.Position).magnitude:0.0}m";
        return $"path {path.status}, corners cached {cachedCount} live {liveCount}, {ends}, length {enemyPath.PathLength:0.0}"
            + $", can still reach {search.PathFinder.CanStillReachTarget(target)}, last known {lastKnown}";
    }
}
#endif
