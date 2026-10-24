using System;
using System.Collections.Generic;
using EFT;
using EFT.Ballistics;
using SAIN.Components;
using SAIN.Components.PlayerComponentSpace;
using SAIN.Helpers;
using SAIN.Models.Enums;
using SAIN.Models.Structs;
using SAIN.Plugin;
using SAIN.Preset;
using SAIN.Preset.Shared.Enums;
using SAIN.SAINComponent.Classes.EnemyClasses;
using UnityEngine;

namespace SAIN.BotController.Classes;

public class Squad
{
    public event Action<ECombatDecision, ESquadDecision, ESelfActionType, string, BotComponent> OnMemberDecisionMade;

    public event Action<EnemyPlace, Enemy, SAINSoundType> OnMemberHeardEnemy;

    public event Action<Squad> OnSquadEmpty;

    public event Action<IPlayer, DamageInfo, float> LeaderKilled;

    public event Action<IPlayer, DamageInfo, float> OnMemberKilled;

    public event Action<BotComponent, float> NewLeaderFound;

    public Dictionary<string, BotComponent> Members { get; } = new Dictionary<string, BotComponent>();
    public Dictionary<string, MemberInfo> MemberInfos { get; } = new Dictionary<string, MemberInfo>();
    public string Id { get; private set; } = string.Empty;
    public string GUID { get; } = Guid.NewGuid().ToString("N");
    public bool SquadReady { get; private set; }
    public ESquadPersonality SquadPersonality { get; private set; }
    public SquadPersonalitySettings SquadPersonalitySettings { get; private set; }
    public BotComponent LeaderComponent { get; private set; }
    public string LeaderId { get; private set; }
    public float LeaderPowerLevel { get; private set; }
    public bool LeaderIsDeadorNull
    {
        get
        {
            if (LeaderComponent == null || LeaderComponent.Player == null)
            {
                return true;
            }
            return !LeaderComponent.Player.HealthController.IsAlive;
        }
    }

    public float TimeThatLeaderDied { get; private set; }

    public bool MemberIsRegrouping
    {
        get { return MemberHasDecision(ESquadDecision.Regroup); }
    }

    public float SquadPowerLevel
    {
        get
        {
            float result = 0f;
            foreach (var memberInfo in MemberInfos.Values)
            {
                if (memberInfo.Bot != null && memberInfo.Bot.IsDead == false)
                {
                    result += memberInfo.PowerLevel;
                }
            }
            return result;
        }
    }

    public Squad()
    {
        _checkSquadTime = Time.time + 10f;
        PresetHandler.OnPresetUpdated += updateSettings;
        updateSettings(SAINPresetClass.Instance);
    }

    private void updateSettings(SAINPresetClass preset)
    {
        _maxReportActionRangeSqr = preset.GlobalSettings.Hearing.MaxRangeToReportEnemyActionNoHeadset.Sqr();
    }

    public void ReportEnemyPosition(Enemy reportedEnemy, EnemyPlace place, bool seen, float currentTime)
    {
        if (Members == null || Members.Count <= 1)
        {
            return;
        }
        if (place == null)
        {
#if DEBUG
            Logger.LogError($"Tried to report enemy position for Squad [{Id}] but Place is null!");
#endif
            return;
        }

        float squadCoordination = 3f;
        if (SquadPersonalitySettings != null)
        {
            squadCoordination = SquadPersonalitySettings.CoordinationLevel;
            squadCoordination = Mathf.Clamp(squadCoordination, 1f, 5f);
        }
        float baseChance = 25f;
        float finalChance = baseChance + (squadCoordination * 15f);

        foreach (var member in Members.Values)
        {
            if (EFTMath.RandomBool(finalChance))
            {
                if (
                    member != null
                    && member.Player != null
                    && reportedEnemy.Player != null
                    && reportedEnemy.EnemyPlayer != null
                    && reportedEnemy.Player.ProfileId != member.ProfileId
                )
                {
                    member
                        .EnemyController.GetEnemy(reportedEnemy.EnemyPlayer.ProfileId, true)
                        ?.EnemyPositionReported(place, seen, currentTime);
                }
            }
        }
    }

    public string GetId()
    {
        if (Id.IsNullOrEmpty())
        {
            return GUID;
        }
        else
        {
            return Id;
        }
    }

    public bool SquadIsSuppressEnemy(string profileId, out BotComponent suppressingMember)
    {
        foreach (var member in Members)
        {
            if (member.Value == null)
            {
                continue;
            }
            Enemy enemy = member.Value.GoalEnemy;
            if (enemy?.EnemyPlayer != null && enemy.EnemyPlayer.ProfileId == profileId && enemy.Status.EnemyIsSuppressed)
            {
                suppressingMember = member.Value;
                return true;
            }
        }
        suppressingMember = null;
        return false;
    }

    public void AddPointToSearch(Enemy Enemy, Vector3 EstimatedPosition, AISoundData Sound, BotComponent sain)
    {
        AddPlaceForCheck(
            EstimatedPosition,
            Sound.SoundType,
            EEnemyPlaceType.Hearing,
            sain,
            Enemy,
            true,
            CheckSoundIsDanger(Sound),
            Time.time
        );
    }

    private static bool CheckSoundIsDanger(AISoundData sound)
    {
        if (sound.Enemy.InLineOfSight)
        {
            return true;
        }
        if (sound.PlayerDistance < SOUND_DIST_ALWAYS_DANGER)
        {
            return true;
        }
        if (sound.IsGunShot && sound.PlayerDistance < SOUND_DIST_GUNSHOT_ALWAYS_DANGER)
        {
            return true;
        }
        return false;
    }

    private const float SOUND_DIST_ALWAYS_DANGER = 25f;
    private const float SOUND_DIST_GUNSHOT_ALWAYS_DANGER = 100f;

    private void AddPlaceForCheck(
        Vector3 position,
        SAINSoundType soundType,
        EEnemyPlaceType placeType,
        BotComponent bot,
        Enemy enemy,
        bool heard,
        bool isDanger,
        float currentTime
    )
    {
        BotsGroup ??= bot.BotOwner.BotsGroup;

        SAINHearingReport report = new()
        {
            position = position,
            soundType = soundType,
            placeType = placeType,
            isDanger = isDanger,
            shallReportToSquad = true,
        };
        EnemyPlace place = enemy.Hearing.SetHeard(report, currentTime);

        if (heard && place != null)
        {
            OnMemberHeardEnemy?.Invoke(place, enemy, soundType);
        }
    }

    public void AddPointToSearch(
        Vector3 position,
        float soundPower,
        BotComponent sain,
        AISoundType soundType,
        IPlayer player,
        EEnemyPlaceType placeType = EEnemyPlaceType.Hearing
    )
    {
        Enemy enemy = sain.EnemyController.CheckAddEnemy(player);
        if (enemy == null)
        {
            return;
        }

        bool isDanger = (position - sain.Position).sqrMagnitude < SOUND_DIST_ALWAYS_DANGER * SOUND_DIST_ALWAYS_DANGER;
        AddPlaceForCheck(position, soundType.Convert(), placeType, sain, enemy, false, isDanger, Time.time);
    }

    public bool MemberHasDecision(params ESquadDecision[] decisionsToCheck)
    {
        foreach (var member in MemberInfos.Values)
        {
            if (member != null && member.Bot != null)
            {
                var memberDecision = member.SquadDecision;
                foreach (var decision in decisionsToCheck)
                {
                    if (decision == memberDecision)
                    {
                        return true;
                    }
                }
            }
        }
        return false;
    }

    private void getSquadPersonality()
    {
        SquadPersonality = SquadPersonalityManager.GetSquadPersonality(Members, out var settings);
        SquadPersonalitySettings = settings;
    }

    public void Update(float currentTime, float DeltaTime)
    {
        // After 10 seconds since squad is originally created,
        // find a squad leader and activate the squad to give time for all bots to spawn in
        // since it can be staggered over a few seconds.
        if (!SquadReady && _checkSquadTime < currentTime && Members.Count > 0)
        {
            SquadReady = true;
            findSquadLeader();
            // Timer before starting to recheck
            _recheckSquadTime = currentTime + 10f;
            if (Members.Count > 1)
            {
                getSquadPersonality();
            }
        }

        // Check happens once the squad is originally "activated" and created
        // Wait until all members are out of combat to find a squad leader, or 60 seconds have passed to find a new squad leader is they are KIA
        if (SquadReady)
        {
            if (_recheckSquadTime < currentTime && LeaderIsDeadorNull)
            {
                _recheckSquadTime = currentTime + 3f;

                if (TimeThatLeaderDied < currentTime + LEADER_KILL_COOLDOWN)
                {
                    findSquadLeader();
                }
                else
                {
                    bool outOfCombat = true;
                    foreach (var member in MemberInfos.Values)
                    {
                        if (member.HasEnemy)
                        {
                            outOfCombat = false;
                            break;
                        }
                    }
                    if (outOfCombat)
                    {
                        findSquadLeader();
                    }
                }
            }
        }
    }

    public void Dispose()
    {
        if (MemberInfos.Count > 0)
        {
            foreach (var id in MemberInfos.Keys)
            {
                RemoveMember(id);
            }
        }

        if (BotsGroup != null)
        {
            BotsGroup.OnMemberRemove -= removeMember;
        }

        PresetHandler.OnPresetUpdated -= updateSettings;
        MemberInfos.Clear();
        Members.Clear();
    }

    private bool isInCommunicationRange(BotComponent a, BotComponent b)
    {
        if (a != null && b != null)
        {
            if (a.PlayerComponent.Equipment.GearInfo.HasEarPiece && b.PlayerComponent.Equipment.GearInfo.HasEarPiece)
            {
                return true;
            }
            if ((a.Position - b.Position).sqrMagnitude <= _maxReportActionRangeSqr)
            {
                return true;
            }
        }
        return false;
    }

    public void UpdateSharedEnemyStatus(
        IPlayer player,
        EEnemyAction action,
        BotComponent sain,
        SAINSoundType soundType,
        Vector3 position,
        float currentTime
    )
    {
        if (sain == null)
        {
            return;
        }

        foreach (var member in Members.Values)
        {
            if (member == null || member.ProfileId == sain.ProfileId)
            {
                continue;
            }
            if (!isInCommunicationRange(sain, member))
            {
                continue;
            }
            Enemy memberEnemy = member.EnemyController.CheckAddEnemy(player);
            if (memberEnemy == null)
            {
                continue;
            }

            SAINHearingReport report = new()
            {
                position = position,
                soundType = soundType,
                placeType = EEnemyPlaceType.Hearing,
                isDanger = memberEnemy.InLineOfSight,
                shallReportToSquad = false,
            };

            memberEnemy.Hearing.SetHeard(report, currentTime);
            if (action != EEnemyAction.None)
            {
                memberEnemy.Status.SetVulnerableAction(action);
            }
        }
    }

    private void memberWasKilled(Player player, IPlayer lastAggressor, DamageInfo lastDamageInfoStruct, EBodyPart lastBodyPart)
    {
#if DEBUG
        if (SAINPlugin.DebugMode)
        {
            string memberName = null;
            if (player != null)
            {
                memberName = player.Profile.Nickname;
            }
            string aggressorName = null;
            if (lastAggressor != null)
            {
                aggressorName = lastAggressor.Profile.Nickname;
            }
            Logger.LogInfo(
                $"Member [{memberName}] "
                    + $"was killed for Squad: [{Id}] "
                    + $"by [{aggressorName}] "
                    + $"at Time: [{Time.time}] "
                    + $"by damage type: [{lastDamageInfoStruct.DamageType}] "
                    + $"to Body part: [{lastBodyPart}]"
            );
        }
#endif

        OnMemberKilled?.Invoke(lastAggressor, lastDamageInfoStruct, Time.time);

        if (player == null)
        {
            return;
        }
        string profileId = player.ProfileId;
        if (MemberInfos.TryGetValue(profileId, out var member) && member != null)
        {
            // If this killed Member is the squad leader then
            if (member.ProfileId == LeaderId)
            {
#if DEBUG
                if (SAINPlugin.DebugMode)
                {
                    Logger.LogInfo($"Leader [{player.Profile.Nickname}] was killed for Squad: [{Id}]");
                }
#endif

                LeaderKilled?.Invoke(lastAggressor, lastDamageInfoStruct, Time.time);
                TimeThatLeaderDied = Time.time;
                LeaderComponent = null;
            }
        }

        RemoveMember(profileId);
    }

    private void findSquadLeader()
    {
        float power = 0f;
        BotComponent leadComponent = null;

        // Iterate through each memberInfo memberInfo in friendly group to see who has the highest power level or if any are bosses
        foreach (var memberInfo in MemberInfos.Values)
        {
            if (memberInfo.Bot == null || memberInfo.Bot.IsDead)
            {
                continue;
            }

            // If this memberInfo is a boss type, they are the squad leader
            bool isBoss = memberInfo.Bot.Info.Profile.IsBoss;
            // or If this memberInfo has a higher power level than the last one we checked, they are the squad leader
            if (isBoss || memberInfo.PowerLevel > power)
            {
                power = memberInfo.PowerLevel;
                leadComponent = memberInfo.Bot;

                if (isBoss)
                {
                    break;
                }
            }
        }

        if (leadComponent != null)
        {
            assignSquadLeader(leadComponent);
        }
    }

    private void assignSquadLeader(BotComponent sain)
    {
        if (sain == null || sain.Player == null)
        {
#if DEBUG
            Logger.LogError($"Tried to Assign Null SAIN Component or Player for Squad [{Id}], skipping");
#endif
            return;
        }

        LeaderComponent = sain;
        LeaderPowerLevel = sain.Info.Profile.PowerLevel;
        LeaderId = sain.Player.ProfileId;

        NewLeaderFound?.Invoke(sain, Time.time);

#if DEBUG
        if (SAINPlugin.DebugMode)
        {
            string leaderName = null;
            if (sain.BotOwner != null)
            {
                leaderName = sain.BotOwner.Profile?.Nickname;
            }
            Logger.LogInfo(
                $" Found New Leader. Name [{leaderName}]"
                    + $" for Squad: [{Id}]"
                    + $" at Time: [{Time.time}]"
                    + $" Group Size: [{Members.Count}]"
            );
        }
#endif
    }

    public void AddMember(BotComponent bot)
    {
        // Make sure nothing is null as a safety check.
        if (bot != null && bot.Player != null && bot.BotOwner != null)
        {
            // Make sure this profile ID doesn't already exist for whatever reason
            if (!Members.ContainsKey(bot.ProfileId))
            {
                // If this is the first member, add their side to the start of their ID for easier identifcation during debug
                if (Members.Count == 0)
                {
                    BotsGroup = bot.BotOwner.BotsGroup;
                    Id = bot.Info.Profile.Side.ToString() + "_" + GUID;
                }

                bot.Decision.DecisionManager.OnDecisionMade += memberMadeDecision;

                var memberInfo = new MemberInfo(bot, this);
                MemberInfos.Add(bot.ProfileId, memberInfo);
                Members.Add(bot.ProfileId, bot);

                // if this new member is a boss, set them to leader automatically
                if (bot.Info.Profile.IsBoss)
                {
                    assignSquadLeader(bot);
                }
                // If this new memberInfo has a higher power level than the existing squad leader, set them as the new squad leader if they aren't a boss type
                else if (LeaderComponent != null && bot.Info.Profile.PowerLevel > LeaderPowerLevel && !LeaderComponent.Info.Profile.IsBoss)
                {
                    assignSquadLeader(bot);
                }

                // Subscribe when this member is killed
                bot.Player.OnPlayerDead += memberWasKilled;
                if (Members.Count > 1)
                {
                    getSquadPersonality();
                }
            }
        }
    }

    private void memberMadeDecision(ECombatDecision solo, ESquadDecision squad, ESelfActionType self, Enemy enemy, BotComponent member)
    {
        OnMemberDecisionMade?.Invoke(solo, squad, self, enemy?.EnemyProfileId, member);
    }

    public void RemoveMember(BotComponent sain)
    {
        if (sain == null)
        {
            return;
        }
        RemoveMember(sain.ProfileId);
    }

    public void RemoveMember(string id)
    {
        if (Members.ContainsKey(id))
        {
            Members.Remove(id);
        }
        if (MemberInfos.TryGetValue(id, out var memberInfo))
        {
            if (memberInfo.Bot != null && memberInfo.Bot.Player != null)
            {
                memberInfo.Bot.Player.OnPlayerDead -= memberWasKilled;
            }
            memberInfo.Bot.Decision.DecisionManager.OnDecisionMade -= memberMadeDecision;
            memberInfo.Dispose();
            MemberInfos.Remove(id);
        }
        if (Members.Count == 0)
        {
            OnSquadEmpty?.Invoke(this);
        }
    }

    public BotsGroup BotsGroup
    {
        get { return _botsGroup; }
        private set
        {
            if (_botsGroup == value)
            {
                return;
            }

            if (_botsGroup != null)
            {
                _botsGroup.OnMemberRemove -= removeMember;
            }

            if (value != null)
            {
                value.OnMemberRemove += removeMember;
            }

            _botsGroup = value;
        }
    }

    private void removeMember(BotOwner botOwner)
    {
        if (botOwner == null)
        {
            return;
        }

        if (botOwner.HealthController?.IsAlive == true && Members.TryGetValue(botOwner.ProfileId, out BotComponent bot) && bot != null)
        {
            bot.Squad.RemoveFromSquad();
            RemoveMember(bot);
        }
    }

    private BotsGroup _botsGroup;

    private float _recheckSquadTime;
    private float _checkSquadTime;
    private float _maxReportActionRangeSqr;
    public const float LEADER_KILL_COOLDOWN = 60f;
}
