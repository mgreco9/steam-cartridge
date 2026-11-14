using System;
using System.Windows.Input;
using Avalonia.Controls;

namespace SteamCartridge.App.ViewModels;

public partial class MainWindowModel
{

    /////////////////////////////////
    //// CURRENT VIEW MANAGEMENT ////
    /////////////////////////////////
    public required UserControl CurrentView
    {
        get;
        init;
    }
}
