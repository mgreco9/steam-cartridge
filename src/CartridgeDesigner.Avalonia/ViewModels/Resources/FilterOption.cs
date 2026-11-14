using System;
using CommunityToolkit.Mvvm.ComponentModel;

public class FilterOption<T>
{
    public T Value { get; }

    public bool IsEnabled 
    { 
        get; 
        set 
        {
            if(field==value)
                return;

            field = value;
            OnFilterUpdate?.Invoke();
        }
    }

    public FilterOption(T value)
    {
        Value = value;
        IsEnabled = true;
    }

    public FilterOption(T value, bool enabled)
    {
        Value = value;
        IsEnabled = enabled;
    }

    public Action? OnFilterUpdate;
}