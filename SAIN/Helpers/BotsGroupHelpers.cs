using System.Collections.Generic;
using EFT;

namespace SAIN.Helpers;

public static class BotsGroupHelpers
{
    public static bool ContainsPlayer(Dictionary<IPlayer, BotGroupEnemyInfo> players, string profileId)
    {
        foreach (KeyValuePair<IPlayer, BotGroupEnemyInfo> entry in players)
        {
            if (entry.Key != null && entry.Key.ProfileId == profileId)
            {
                return true;
            }
        }
        return false;
    }

    public static bool ContainsPlayer(List<IPlayer> players, string profileId)
    {
        for (int i = 0; i < players.Count; i++)
        {
            IPlayer player = players[i];
            if (player != null && player.ProfileId == profileId)
            {
                return true;
            }
        }
        return false;
    }
}
