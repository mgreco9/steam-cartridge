using System;
using Avalonia.Threading;

namespace CartridgeDesigner.Avalonia.ViewModels;

/// <summary>
/// Cache events can be raised from a background load.
/// Observable collections must be updated on the UI thread.
/// </summary>
internal static class UiThread
{
    public static void Run(Action action)
    {
        if (Dispatcher.UIThread.CheckAccess())
            action();
        else
            Dispatcher.UIThread.Post(action);
    }
}
