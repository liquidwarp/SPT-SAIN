using System;
using EFT;
using SAIN.Components.PlayerComponentSpace;

namespace SAIN.SAINComponent;

public abstract class PlayerComponentBase(PlayerComponent player) : IDisposable
{
    public PlayerComponent PlayerComponent { get; private set; } = player;

    public Player Player
    {
        get { return PlayerComponent.Player; }
    }

    public virtual void Dispose() { }
}
