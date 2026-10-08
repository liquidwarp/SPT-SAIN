using System;
using SAIN.Components;

namespace SAIN;

public enum ESAINTickState
{
    NeverUpdate,
    AlwaysUpdate,
    OnlyBotActive,
    OnlyNoSleep,
    OnlyBotInCombat,
}

/// <summary>
/// A generic interface for any class owned by a bot.
/// </summary>
public interface IBotClass : IDisposable
{
    void Init();

    void ManualUpdate();

    BotComponent Bot { get; }
    ESAINTickState TickRequirement { get; }
    bool CanEverTick { get; }
}
