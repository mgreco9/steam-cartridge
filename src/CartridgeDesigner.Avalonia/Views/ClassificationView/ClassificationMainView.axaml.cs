using System.Text.Json;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using CartridgeDesigner.Avalonia.ViewModels.ClassificationViewModel;
using CartridgeDesigner.Avalonia.ViewModels.LibraryViewModel;
using SteamCartridge.Core.Workspace;

namespace CartridgeDesigner.Avalonia.Views.ClassificationView;

public partial class ClassificationMainView : UserControl
{
    private static DataFormat<GameViewModel> _gameFormat = DataFormat.CreateInProcessFormat<GameViewModel>("GameViewModel");
    private ClassificationMainViewModel? _vm => DataContext as ClassificationMainViewModel;

    public ClassificationMainView()
    {
        InitializeComponent();
    }

    private async void Game_PointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (sender is not Control control)
            return;

        if (control.DataContext is not GameViewModel item)
            return;

        DataTransfer data = new DataTransfer();
        data.Add(DataTransferItem.Create(_gameFormat, item));

        await DragDrop.DoDragDropAsync(
            e,
            data,
            DragDropEffects.Copy);
    }

    private async void MiddlePanel_Drop(object? sender, DragEventArgs e)
    {
        if(_vm == null)
            return;

        if (_vm.SelectedCategory == null)
            return;

        GameViewModel? item = e.DataTransfer.TryGetValue(_gameFormat);

        if (item == null)
            return;

        Window? window = TopLevel.GetTopLevel(this) as Window;
        if(window == null)
            return;

        AddGameDialog dialog = new AddGameDialog();
        AddGameDialogModel model = new AddGameDialogModel(dialog, item.Game.Franchise);
        dialog.DataContext = model;

        string? result = await dialog.ShowDialog<string?>(window);

        if (result != null)
        {
            _vm.AddGameToCategory(_vm.SelectedCategory, item, result);
        }
    }

    private async void Category_Drop(object? sender, DragEventArgs e)
    {
        if(_vm == null)
            return;

        if (sender is not Control control)
            return;

        if (control.DataContext is not CategoryViewModel category)
            return;

        GameViewModel? item = e.DataTransfer.TryGetValue(_gameFormat);

        if (item == null)
            return;

        Window? window = TopLevel.GetTopLevel(this) as Window;
        if(window == null)
            return;

        AddGameDialog dialog = new AddGameDialog();
        AddGameDialogModel model = new AddGameDialogModel(dialog, item.Game.Franchise);
        dialog.DataContext = model;

        string? result = await dialog.ShowDialog<string?>(window);

        if (result != null)
        {
            _vm.AddGameToCategory(category, item, result);
        }
    }

    public async void CreateCategory_Click(object sender, RoutedEventArgs e)
    {
        if(_vm == null)
            return;

        Window? window = TopLevel.GetTopLevel(this) as Window;
        if(window == null)
            return;

        CreateCategoryDialog dialog = new CreateCategoryDialog();
        CreateCategoryDialogModel model = new CreateCategoryDialogModel(dialog);
        dialog.DataContext = model;

        Category? result = await dialog.ShowDialog<Category?>(window);

        if (result != null)
        {
            _vm.CreateCategory(result);
        }
    }

    private void OnCategoryPressed(object? sender, PointerPressedEventArgs e)
    {
        if ((sender as Control)?.DataContext is not CategoryViewModel item)
            return;

        _vm?.SelectedCategory = item;
    }
}