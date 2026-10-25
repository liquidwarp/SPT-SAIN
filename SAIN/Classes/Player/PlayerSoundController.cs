using System;
using CommonAssets.Scripts.Audio;
using EFT;
using SAIN.Components;
using SAIN.Components.PlayerComponentSpace;
using SAIN.Preset.Shared.Enums;
using SAIN.SAINComponent;
using UnityEngine;

namespace SAIN.Classes;

public sealed class PlayerSoundController(PlayerComponent player) : PlayerComponentBase(player)
{
    private const float RunSurfaceRange = 40f;
    private const float SprintSurfaceRange = 60f;

    private enum EStepLoop
    {
        None,
        Run,
        Sprint,
    }

    private EStepLoop _stepLoop;
    private bool _stepLoopRunning;
    private EPlayerState _stepLoopState;

    private float _lastStepTime = 0f;
    private bool _playedAtLeastOneStep;
    private float _lastAnimatorSign;

    public void HandleMovementState(EPlayerState previousState, EPlayerState nextState)
    {
        if (Player.IsAI && Player.AIData != null)
        {
            return;
        }

        if (BotManagerComponent.Instance == null)
        {
            return;
        }

        switch (previousState)
        {
            case EPlayerState.Idle:
            case EPlayerState.IdleZombieState:
            case EPlayerState.TurnZombieState:
                break;
            case EPlayerState.Run:
            case EPlayerState.MoveZombieState:
            case EPlayerState.StartMoveZombieState:
            case EPlayerState.EndMoveZombieState:
                if (_stepLoop != EStepLoop.Run)
                {
                    break;
                }
                StopStepLoop();
                if (!_playedAtLeastOneStep && Player.SinceLastStep > 0.66f)
                {
                    if (Player.CheckSurface(RunSurfaceRange))
                    {
                        BotManagerComponent.Instance.BotHearing.PlayAISound(
                            Player.ProfileId,
                            SAINSoundType.FootStep,
                            Player.Position,
                            SAINPlugin.LoadedPreset.GlobalSettings.Hearing.BaseSoundRange_Footstep,
                            CalculateStepVolume()
                        );
                    }
                    _lastStepTime = Time.time;
                }
                break;
            case EPlayerState.Sprint:
                if (_stepLoop == EStepLoop.Sprint)
                {
                    StopStepLoop();
                    if (!_playedAtLeastOneStep && Player.CheckSurface(SprintSurfaceRange))
                    {
                        BotManagerComponent.Instance.BotHearing.PlayAISound(
                            Player.ProfileId,
                            SAINSoundType.Sprint,
                            Player.Position,
                            SAINPlugin.LoadedPreset.GlobalSettings.Hearing.BaseSoundRange_Sprint,
                            Player.CalculateMovementVolumeDefaultMultiplier(EAudioMovementState.Sprint)
                        );
                    }
                }

                if (nextState == EPlayerState.Transition || nextState == EPlayerState.Idle)
                {
                    float num = 1f * Player.MovementContext.CovertMovementVolume;
                    num *= Player.CalculateMovementVolumeDefaultMultiplier(EAudioMovementState.Stop);
                    BotManagerComponent.Instance.BotHearing.PlayAISound(
                        Player.ProfileId,
                        SAINSoundType.Sprint,
                        Player.Position,
                        SAINPlugin.LoadedPreset.GlobalSettings.Hearing.BaseSoundRange_Sprint,
                        num
                    );
                }
                break;
        }
        switch (nextState)
        {
            case EPlayerState.Prone2Stand:
                BotManagerComponent.Instance.BotHearing.PlayAISound(
                    Player.ProfileId,
                    SAINSoundType.GearSound,
                    Player.Position,
                    SAINPlugin.LoadedPreset.GlobalSettings.Hearing.BaseSoundRange_Prone,
                    0.7f
                );
                break;
            case EPlayerState.Sprint:
                StartStepLoop(EStepLoop.Sprint, nextState);
                break;
            case EPlayerState.Jump:
                float vol = Player.CalculateMovementVolumeDefaultMultiplier(EAudioMovementState.Jump);
                BotManagerComponent.Instance.BotHearing.PlayAISound(
                    Player.ProfileId,
                    SAINSoundType.Jump,
                    Player.Position,
                    SAINPlugin.LoadedPreset.GlobalSettings.Hearing.BaseSoundRange_Jump,
                    vol
                );
                break;
            case EPlayerState.Idle:
            case EPlayerState.IdleZombieState:
            case EPlayerState.TurnZombieState:
                break;
            case EPlayerState.Run:
            case EPlayerState.MoveZombieState:
            case EPlayerState.StartMoveZombieState:
            case EPlayerState.EndMoveZombieState:
                StartStepLoop(EStepLoop.Run, nextState);
                break;
            case EPlayerState.Transit2Prone:
            {
                float volume = 0.7f * Player.MovementContext.CovertMovementVolume;
                if (previousState == EPlayerState.Sprint)
                {
                    BotManagerComponent.Instance.BotHearing.PlayAISound(
                        Player.ProfileId,
                        SAINSoundType.Prone,
                        Player.Position,
                        // Prone is slightly louder coming from a sprint into prone
                        SAINPlugin.LoadedPreset.GlobalSettings.Hearing.BaseSoundRange_Prone * 1.2f,
                        volume
                    );
                }
                else
                {
                    BotManagerComponent.Instance.BotHearing.PlayAISound(
                        Player.ProfileId,
                        SAINSoundType.Prone,
                        Player.Position,
                        SAINPlugin.LoadedPreset.GlobalSettings.Hearing.BaseSoundRange_Prone,
                        volume
                    );
                }
                break;
            }
        }
    }

    private float CalculateStepVolume()
    {
        EAudioMovementState movementState = ((Player.Pose != EPlayerPose.Duck) ? EAudioMovementState.Run : EAudioMovementState.Duck);
        float covertMovementVolumeBySpeed = Player.MovementContext.CovertMovementVolumeBySpeed;
        float speed = Player.CalculateStepSoundVolumeSpeedFactor();
        float state = Player.CalculateMovementVolumeDefaultMultiplier(movementState);
        float randomVolume = (Player.FirstPersonPointOfView || Player.IsAiUseSimpleSkeleton()) ? UnityEngine.Random.Range(0.75f, 1.1f) : 1f;

        float calc = covertMovementVolumeBySpeed * speed * state * randomVolume;

        return calc;
    }

    private void StartStepLoop(EStepLoop loop, EPlayerState state)
    {
        _stepLoop = loop;
        _stepLoopState = state;
        _stepLoopRunning = true;
        _playedAtLeastOneStep = false;
        TickStepLoop();
    }

    /// <summary>
    /// Also called while the player component is inactive. ResumeStepLoop restarts it.
    /// </summary>
    public void StopStepLoop()
    {
        _stepLoop = EStepLoop.None;
        _stepLoopRunning = false;
    }

    /// <summary>
    /// Plays a footstep each time the animator's stance curve changes, while the player stays in the run or sprint state the loop started in.
    /// </summary>
    public void TickStepLoop()
    {
        if (!_stepLoopRunning)
        {
            return;
        }
        var currentState = Player.CurrentState;
        if (currentState == null || currentState.Name != _stepLoopState)
        {
            _stepLoopRunning = false;
            return;
        }

        float single = Player.LeftStanceCurrentCurveValue;
        if (Math.Abs(_lastAnimatorSign - single) < float.Epsilon)
        {
            return;
        }
        _lastAnimatorSign = single;

        bool sprint = _stepLoop == EStepLoop.Sprint;
        float sinceLastStep = Time.time - _lastStepTime;
        if (sinceLastStep <= 0.2f || Player.MovementContext.FreefallTime >= (sprint ? 0.6f : 1f))
        {
            return;
        }
        _lastStepTime = Time.time;
        _playedAtLeastOneStep = true;
        if (!Player.CheckSurface(sprint ? SprintSurfaceRange : RunSurfaceRange))
        {
            return;
        }
        BotManagerComponent botManager = BotManagerComponent.Instance;
        if (botManager == null)
        {
            return;
        }
        if (sprint)
        {
            botManager.BotHearing.PlayAISound(
                Player.ProfileId,
                SAINSoundType.Sprint,
                Player.Position,
                SAINPlugin.LoadedPreset.GlobalSettings.Hearing.BaseSoundRange_Sprint,
                Player.CalculateMovementVolumeDefaultMultiplier(EAudioMovementState.Sprint)
            );
        }
        else
        {
            botManager.BotHearing.PlayAISound(
                Player.ProfileId,
                SAINSoundType.FootStep,
                Player.Position,
                SAINPlugin.LoadedPreset.GlobalSettings.Hearing.BaseSoundRange_Footstep,
                CalculateStepVolume()
            );
        }
    }

    /// <summary>
    /// Restarts the step loop for the state the player is already in, after StopStepLoop.
    /// </summary>
    public void ResumeStepLoop()
    {
        if (Player.IsAI && Player.AIData != null)
        {
            return;
        }

        if (Player.CurrentState == null)
        {
            return;
        }

        EPlayerState state = Player.CurrentState.Name;
        switch (state)
        {
            case EPlayerState.Sprint:
                if (_stepLoop == EStepLoop.None)
                {
                    StartStepLoop(EStepLoop.Sprint, state);
                }
                break;
            case EPlayerState.Run:
            case EPlayerState.MoveZombieState:
            case EPlayerState.StartMoveZombieState:
            case EPlayerState.EndMoveZombieState:
                if (_stepLoop == EStepLoop.None)
                {
                    StartStepLoop(EStepLoop.Run, state);
                }
                break;
        }
    }
}
