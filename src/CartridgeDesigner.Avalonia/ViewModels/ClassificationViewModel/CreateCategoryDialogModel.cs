using Avalonia.Controls;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SteamCartridge.Core.Workspace;

namespace CartridgeDesigner.Avalonia.ViewModels.ClassificationViewModel;

public partial class CreateCategoryDialogModel : ObservableObject
{ 
    private readonly Window _dialog;
    public string Name { get; set; }
    public string Prefix { get; set; }

    public CreateCategoryDialogModel(Window dialog)
    {
        _dialog = dialog;
        Name = "";
        Prefix = "";
    }

    [RelayCommand]
    private void Confirm() => _dialog.Close(new Category{Name = Name, Prefix = Prefix});

    [RelayCommand]
    private void Cancel() => _dialog.Close(null);
}