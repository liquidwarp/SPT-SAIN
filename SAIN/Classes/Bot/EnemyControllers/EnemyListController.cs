using System.Collections.Generic;
using EFT;
using SAIN.Components;
using SAIN.Components.PlayerComponentSpace;
using SAIN.Helpers;
using UnityEngine;

namespace SAIN.SAINComponent.Classes.EnemyClasses;

public class EnemyListController : BotSubClass<SAINEnemyController>, IBotClass
{
    public Dictionary<string, Enemy> Enemies { get; } = [];
    public HashSet<Enemy> EnemiesArray { get; } = [];

    public EnemyListController(SAINEnemyController controller)
        : base(controller) { }

    public override void Init()
    {
        GameWorldComponent.Instance.PlayerTracker.OnPlayerRemoved += RemoveEnemy;
        BotOwner.Memory.OnAddEnemy += enemyAdded;
        if (BotOwner.BotsGroup is BotsGroup botsGroup)
        {
            botsGroup.OnEnemyAdd += enemyAdded;
            botsGroup.OnEnemyRemove += RemoveEnemy;
        }
        compareEnemyLists();
        base.Init();
    }

    private void RemoveEnemy(IPlayer player)
    {
        if (player != null)
        {
            RemoveEnemy(player.ProfileId);
        }
    }

    public override void ManualUpdate()
    {
        compareEnemyLists();
#if DEBUG
        if (SAINPlugin.DebugMode)
        {
            foreach (Enemy enemy in Bot.EnemyController.VisibleEnemies)
            {
                if (enemy.Path.PathCornerCount > 0)
                {
                    GameObject pathObject = DebugGizmos.DrawLine(Vector3.zero, Vector3.forward, Color.red, 0.1f, 0.02f);
                    DebugGizmos.SetLinePositions(pathObject, enemy.Path.PathCorners, enemy.Path.PathCornerCount);
                }
                //DebugGizmos.DrawLine(Bot.Transform.EyePosition, enemy.EnemyPosition, Color.red, 0.025f, 0.02f);
                //if (enemy.LastKnownPosition != null)
                //    DebugGizmos.DrawLine(enemy.LastKnownPosition.Value, enemy.EnemyPosition, Color.red, 0.025f, 0.02f);
            }
            foreach (Enemy enemy in Bot.EnemyController.EnemiesInLineOfSight)
            {
                if (enemy.Path.PathCornerCount > 0)
                {
                    GameObject pathObject = DebugGizmos.DrawLine(Vector3.zero, Vector3.forward, Color.yellow, 0.1f, 0.02f);
                    DebugGizmos.SetLinePositions(pathObject, enemy.Path.PathCorners, enemy.Path.PathCornerCount);
                }
                //DebugGizmos.DrawLine(Bot.Transform.EyePosition, enemy.EnemyPosition, Color.yellow, 0.025f, 0.02f);
                //if (enemy.LastKnownPosition != null)
                //    DebugGizmos.DrawLine(enemy.LastKnownPosition.Value, enemy.EnemyPosition, Color.yellow, 0.025f, 0.02f);
            }
            foreach (Enemy enemy in Bot.EnemyController.KnownEnemies)
            {
                if (enemy.Path.PathCornerCount > 0)
                {
                    GameObject pathObject = DebugGizmos.DrawLine(Vector3.zero, Vector3.forward, Color.blue, 0.1f, 0.02f);
                    DebugGizmos.SetLinePositions(pathObject, enemy.Path.PathCorners, enemy.Path.PathCornerCount);
                }
                //DebugGizmos.DrawLine(Bot.Transform.EyePosition, enemy.EnemyPosition, Color.blue, 0.025f, 0.02f);
                //if (enemy.LastKnownPosition != null)
                //    DebugGizmos.DrawLine(enemy.LastKnownPosition.Value, enemy.EnemyPosition, Color.blue, 0.025f, 0.02f);
            }
        }
#endif

        base.ManualUpdate();
    }

    public override void Dispose()
    {
        GameWorldComponent.Instance.PlayerTracker.OnPlayerRemoved -= RemoveEnemy;
        BotMemory memory = BotOwner?.Memory;
        if (memory != null)
        {
            memory.OnAddEnemy -= enemyAdded;
        }
        if (BotOwner?.BotsGroup is BotsGroup botsGroup)
        {
            botsGroup.OnEnemyAdd -= enemyAdded;
            botsGroup.OnEnemyRemove -= RemoveEnemy;
        }

        foreach (var enemy in EnemiesArray)
        {
            destroyEnemy(enemy);
        }
        EnemiesArray.Clear();
        Enemies.Clear();
        base.Dispose();
    }

    public Enemy GetEnemy(string profileID, bool mustBeActive)
    {
        if (!Enemies.TryGetValue(profileID, out Enemy enemy))
        {
            return null;
        }
        if (enemy == null || !enemy.CheckValid())
        {
            destroyEnemy(enemy);
            Enemies.Remove(profileID);
            EnemiesArray.Remove(enemy);
            return null;
        }
        if (mustBeActive && !Enemy.IsEnemyActive(enemy))
        {
            return null;
        }
        return enemy;
    }

    public void RemoveEnemy(string id, PlayerComponent playerComp)
    {
        RemoveEnemy(id);
    }

    public void RemoveEnemy(string profileId)
    {
        if (Enemies.TryGetValue(profileId, out Enemy enemy))
        {
            destroyEnemy(enemy);
            Enemies.Remove(profileId);
            EnemiesArray.Remove(enemy);
        }
    }

    private void destroyEnemy(Enemy enemy)
    {
        if (enemy == null)
        {
            return;
        }

        BaseClass.Events.EnemyRemoved(enemy.EnemyProfileId, enemy);
        enemy.Dispose();
        removeEnemyInfo(enemy);
    }

    public Enemy CheckAddEnemy(IPlayer IPlayer)
    {
        return tryAddEnemy(IPlayer);
    }

    private void enemyAdded(IPlayer player)
    {
        tryAddEnemy(player);
    }

    private void enemyAdded(IPlayer player, EBotEnemyCause cause)
    {
        tryAddEnemy(player);
    }

    public bool IsBotInBotsGroup(BotOwner botOwner)
    {
        int count = BotOwner.BotsGroup.MembersCount;
        for (int i = 0; i < count; i++)
        {
            var member = BotOwner.BotsGroup.Member(i);
            if (member == null)
            {
                continue;
            }

            if (member.ProfileId == botOwner.ProfileId)
            {
                return true;
            }
        }
        return false;
    }

    private Enemy tryAddEnemy(IPlayer enemyPlayer)
    {
        if (enemyPlayer == null)
        {
            return null;
        }
        if (!enemyPlayer.HealthController.IsAlive)
        {
            return null;
        }
        if (enemyPlayer.ProfileId == Bot.ProfileId)
        {
            return null;
        }

        if (enemyPlayer.IsAI)
        {
            BotOwner botOwner = enemyPlayer.AIData?.BotOwner;
            if (botOwner == null)
            {
                return null;
            }
            if (IsBotInBotsGroup(botOwner))
            {
                return null;
            }
        }

        PlayerComponent enemyPlayerComponent = getEnemyPlayerComponent(enemyPlayer);
        if (enemyPlayerComponent == null)
        {
            return null;
        }

        if (Enemies.TryGetValue(enemyPlayer.ProfileId, out Enemy sainEnemy))
        {
            return sainEnemy;
        }

        EnemyInfo enemyInfo = getEnemyInfo(enemyPlayer);
        if (enemyInfo == null)
        {
            return null;
        }

        return createEnemy(enemyPlayerComponent, enemyInfo);
    }

    private PlayerComponent getEnemyPlayerComponent(IPlayer enemyPlayer)
    {
        var playerTracker = GameWorldComponent.Instance.PlayerTracker;
        PlayerComponent enemyPlayerComponent = playerTracker.GetPlayerComponent(enemyPlayer.ProfileId);
        if (enemyPlayerComponent == null)
        {
            if (Enemies.TryGetValue(enemyPlayer.ProfileId, out Enemy oldEnemy))
            {
                destroyEnemy(oldEnemy);
                Enemies.Remove(enemyPlayer.ProfileId);
            }
            enemyPlayerComponent = playerTracker.AddPlayerManual(enemyPlayer);
        }
        return enemyPlayerComponent;
    }

    private EnemyInfo getEnemyInfo(IPlayer enemyPlayer)
    {
        if (
            !BotOwner.EnemiesController.EnemyInfos.TryGetValue(enemyPlayer, out EnemyInfo enemyInfo)
            && BotOwner.BotsGroup.Enemies.TryGetValue(enemyPlayer, out BotGroupEnemyInfo value)
        )
        {
            enemyInfo = BotOwner.EnemiesController.AddNew(BotOwner.BotsGroup, enemyPlayer, value);
            BotEnemiesController controller = BotOwner.EnemiesController;
            if (enemyInfo != null && controller._countEnemies < controller.SortedInfos.Length)
            {
                controller.SetInfo(enemyPlayer, enemyInfo);
            }
        }
        return enemyInfo;
    }

    private Enemy createEnemy(PlayerComponent enemyPlayerComponent, EnemyInfo enemyInfo)
    {
        if (Bot.PlayerComponent.OtherPlayersData.GetOrAddData(enemyPlayerComponent) == null)
        {
            return null;
        }

        Enemy enemy = new(Bot, enemyPlayerComponent, enemyInfo);
        enemy.Init();
        Enemies.Add(enemy.EnemyProfileId, enemy);
        EnemiesArray.Add(enemy);
        BaseClass.Events.EnemyAdded(enemy);
        return enemy;
    }

    public bool IsPlayerAnEnemy(string profileID)
    {
        return !profileID.IsNullOrEmpty() && Enemies.ContainsKey(profileID);
    }

    private void removeEnemyInfo(Enemy enemy)
    {
        if (enemy == null)
        {
            return;
        }

        if (enemy.EnemyPlayer != null && BotOwner.EnemiesController.EnemyInfos.ContainsKey(enemy.EnemyPlayer))
        {
            BotOwner.EnemiesController.Remove(enemy.EnemyPlayer);
            return;
        }

        EnemyInfo badInfo = null;
        foreach (var enemyInfo in BotOwner.EnemiesController.EnemyInfos.Values)
        {
            if (enemyInfo?.Person != null && enemyInfo.ProfileId == enemy.EnemyProfileId)
            {
                badInfo = enemyInfo;
                break;
            }
        }

        if (badInfo?.Person != null)
        {
            BotOwner.EnemiesController.Remove(badInfo.Person);
        }
    }

    private void compareEnemyLists()
    {
        if (_nextCompareListsTime < Time.time)
        {
            _nextCompareListsTime = Time.time + COMPARE_ENEMY_LIST_FREQ;

            var groupEnemies = BotOwner.BotsGroup.Enemies;
            foreach (var person in groupEnemies.Keys)
            {
                // IPlayer could be a BotOwner instead of Player and sometimes the BotOwner persists even if its Player component is destroyed
                if (validateComponents(person))
                {
                    continue;
                }

                tryAddEnemy(person);
            }

            var myEnemies = BotOwner.EnemiesController.EnemyInfos;
            foreach (var person in myEnemies.Keys)
            {
                if (validateComponents(person))
                {
                    continue;
                }

                tryAddEnemy(person);
            }
        }
    }

    private static bool validateComponents(IPlayer person)
    {
        return person != null && person.IsAI && person.AIData.BotOwner.GetPlayer == null;
    }

    private float _nextCompareListsTime;
    private const float COMPARE_ENEMY_LIST_FREQ = 1;
}
