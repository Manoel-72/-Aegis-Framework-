using AegisEditor.ViewModels;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;

namespace AegisEditor.Views;

#pragma warning disable CS0618 // Avalonia 11 keeps IDataObject working; DataTransfer migration is isolated here.
public sealed partial class AssetBrowserPanel : UserControl
{
    public const string DragTexturePathFormat = "Aegis.TexturePath";

    public AssetBrowserPanel()
    {
        InitializeComponent();
    }

    private async void AssetList_OnDoubleTapped(object? sender, TappedEventArgs e)
    {
        if (DataContext is AssetBrowserViewModel vm && vm.OpenSelectedCommand.CanExecute(null))
            await vm.OpenSelectedCommand.ExecuteAsync(null);
    }

    private async void AssetList_OnPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (DataContext is not AssetBrowserViewModel vm) return;
        await TryStartSpriteDragAsync(e, vm.SelectedAsset);
    }

    private async void AssetItem_OnPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (sender is not StyledElement { DataContext: AssetBrowserItemViewModel item }) return;
        if (DataContext is AssetBrowserViewModel vm)
            vm.SelectedAsset = item;

        await TryStartSpriteDragAsync(e, item);
    }

    private async Task TryStartSpriteDragAsync(PointerPressedEventArgs e, AssetBrowserItemViewModel? asset)
    {
        if (asset is not { IsSprite: true, IsBroken: false }) return;
        if (!e.GetCurrentPoint(this).Properties.IsLeftButtonPressed) return;

        var data = new DataObject();
        data.Set(DragTexturePathFormat, asset.RelativePath);
        data.Set(DataFormats.Text, asset.RelativePath);

        await DragDrop.DoDragDrop(e, data, DragDropEffects.Copy);
    }
}
#pragma warning restore CS0618
