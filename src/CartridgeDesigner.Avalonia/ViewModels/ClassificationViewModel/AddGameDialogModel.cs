using Avalonia.Controls;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SteamCartridge.Core.Workspace;

namespace CartridgeDesigner.Avalonia.ViewModels.ClassificationViewModel;

public partial class AddGameDialogModel : ObservableObject
{ 
    private readonly Window _dialog;
    public string Franchise { get; set; }

    public AddGameDialogModel(Window dialog, string franchise)
    {
        _dialog = dialog;
        Franchise = franchise;
    }

    [RelayCommand]
    private void Confirm() => _dialog.Close(Franchise);

    [RelayCommand]
    private void Cancel() => _dialog.Close(null);
}