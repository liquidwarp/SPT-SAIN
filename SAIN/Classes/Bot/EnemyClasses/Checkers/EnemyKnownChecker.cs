using SAIN.SAINComponent.Classes.EnemyClasses;
using UnityEngine;

namespace SAIN.Components.BotComponentSpace.Classes.EnemyClasses;

public class EnemyKnownChecker(EnemyData enemyData)
{
    private readonly Enemy Enemy = enemyData.Enemy;

    public void Init(BotComponent bot)
    {
        bot.BotActivation.BotActiveToggle.OnToggle += BotStateChanged;
    }

    public void TickEnemy(float currentTime, float forgetEnemyTime, bool botSearching)
    {
        bool enemyKnown = ShallKnowEnemy(currentTime, forgetEnemyTime, botSearching);
        SetEnemyKnown(enemyKnown, currentTime);
    }

    public void Dispose(BotComponent bot)
    {
        bot.BotActivation.BotActiveToggle.OnToggle -= BotStateChanged;
    }

    private void BotStateChanged(bool botActive)
    {
        if (!botActive)
        {
            SetEnemyKnown(false, Time.time);
        }
    }

    public void SetEnemyKnown(bool enemyKnown, float currentTime)
    {
        Enemy.Events.OnEnemyKnownChanged.CheckToggle(enemyKnown, currentTime);
    }

    private bool ShallKnowEnemy(float currentTime, float forgetEnemyTime, bool searching)
    {
        if (!Enemy.IsEnemyActive(Enemy))
        {
            return false;
        }

        var places = Enemy.KnownPlaces;
        if (places.LastKnownPlace == null)
        {
            return false;
        }

        float timeSinceUpdate = currentTime - places.TimeLastKnownUpdated;

        if (timeSinceUpdate > LAST_KNOWN_TIME_UPDATE_UPPER_LIMIT)
        {
            return false;
        }

        if (timeSinceUpdate <= forgetEnemyTime)
        {
            return true;
        }

        if (searching && BotIsSearchingForMe())
        {
            return true;
        }

        return false;
    }

    private const float LAST_KNOWN_TIME_UPDATE_UPPER_LIMIT = 400f;

    public bool BotIsSearchingForMe()
    {
        if (Enemy.Events.OnSearch.Value)
        {
            return !Enemy.KnownPlaces.SearchedAllKnownLocations;
        }
        return false;
    }
}
