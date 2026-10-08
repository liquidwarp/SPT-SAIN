using System;
using UnityEngine;

namespace SAIN.Helpers.Events;

public abstract class ToggleEventBase
{
    public bool Value { get; protected set; }

    public ToggleEventBase(bool defaultValue)
    {
        SetValue(defaultValue, 0f);
    }

    protected virtual void SetValue(bool value, float currentTime)
    {
        Value = value;
    }
}

public abstract class ToggleEventTimeTrackBase : ToggleEventBase
{
    public ToggleEventTimeTrackBase(bool defaultValue)
        : base(defaultValue) { }

    public float TimeLastTrue { get; private set; }
    public float TimeLastFalse { get; private set; }
    public float TimeSinceTrue
    {
        get { return Time.time - TimeLastTrue; }
    }

    protected override void SetValue(bool value, float currentTime)
    {
        if (value)
        {
            TimeLastTrue = currentTime;
        }
        else
        {
            TimeLastFalse = currentTime;
        }

        base.SetValue(value, currentTime);
    }
}

public class ToggleEventForObjectTimeTracked<T> : ToggleEventTimeTrackBase
{
    public Action<bool, T> OnToggle;

    public void CheckToggle(bool value, float currentTime)
    {
        if (Value != value)
        {
            base.SetValue(value, currentTime);
            OnToggle?.Invoke(value, Object);
        }
    }

    private readonly T Object;

    public ToggleEventForObjectTimeTracked(T _object, bool defaultValue = false)
        : base(defaultValue)
    {
        Object = _object;
    }
}

public class ToggleEvent : ToggleEventBase
{
    public Action<bool> OnToggle;

    public void CheckToggle(bool value, float currentTime)
    {
        if (Value != value)
        {
            base.SetValue(value, currentTime);
            OnToggle?.Invoke(value);
        }
    }

    public ToggleEvent(bool defaultValue = false)
        : base(defaultValue) { }
}

public class ToggleEventTimeTracked : ToggleEventTimeTrackBase
{
    public Action<bool> OnToggle;

    public void CheckToggle(bool value, float currentTime)
    {
        if (Value != value)
        {
            base.SetValue(value, currentTime);
            OnToggle?.Invoke(value);
        }
    }

    public ToggleEventTimeTracked(bool defaultValue = false)
        : base(defaultValue) { }
}
