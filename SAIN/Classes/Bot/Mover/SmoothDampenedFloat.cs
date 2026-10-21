using UnityEngine;

namespace SAIN.SAINComponent.Classes.Mover;

public class SmoothDampenedFloat(float smoothing, float maxVelocity = 50)
{
    public float LastSmoothedValue { get; private set; }
    public float TargetValue { get; private set; }

    /// Smooths over the real time since the last call, so the speed doesn't depend on how often the owner ticks.
    public float GetSinceLastCall()
    {
        float now = Time.time;
        float elapsed = _lastGetTime < 0f ? 0f : Mathf.Min(now - _lastGetTime, MAX_STEP_TIME);
        _lastGetTime = now;
        return Get(elapsed);
    }

    public float Get(float deltaTime)
    {
        if (Mathf.Abs(LastSmoothedValue - TargetValue) < 0.001f)
        {
            LastSmoothedValue = TargetValue;
            _velocity = 0;
            return LastSmoothedValue;
        }
        LastSmoothedValue = Mathf.SmoothDamp(LastSmoothedValue, TargetValue, ref _velocity, _smoothingValue, _maxVelocity, deltaTime);
        return LastSmoothedValue;
    }

    public void Set(float targetValue)
    {
        TargetValue = targetValue;
    }

    private readonly float _smoothingValue = smoothing;
    private readonly float _maxVelocity = maxVelocity;
    private float _velocity = 0;

    private const float MAX_STEP_TIME = 0.25f;
    private float _lastGetTime = -1f;
}
