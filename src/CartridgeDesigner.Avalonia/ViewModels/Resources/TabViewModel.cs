using System;
using Avalonia.Media.Imaging;
using ReactiveUI;

public abstract class TabViewModel : ReactiveObject
{
    public required string Title { get; set; }

    public virtual void Dispose()
    {
    }
}