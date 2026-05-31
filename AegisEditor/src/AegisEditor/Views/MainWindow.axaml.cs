using AegisEditor.Services;
using AegisEditor.ViewModels;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.Platform.Storage;
using Microsoft.Extensions.DependencyInjection;

namespace AegisEditor.Views;

public sealed partial class MainWindow : Window
{
    public MainWindow()
    {
        AvaloniaXamlLoader.Load(this);
        Closed += OnClosedDetachPipe;
    }

    private static void OnClosedDetachPipe(object? sender, EventArgs e)
    {
        try
        {
            if (Avalonia.Application.Current is App app)
                app.Services.GetRequiredService<IEditorBridgeClient>()
                    .DisconnectAsync(CancellationToken.None)
                    .GetAwaiter()
                    .GetResult();
        }
        catch
        {
            // Intentional swallow on shutdown.
        }
    }

    private async void OpenProjectButton_OnClick(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        var folders = await StorageProvider.OpenFolderPickerAsync(new FolderPickerOpenOptions
        {
            Title = "Abrir Projeto Aegis",
            AllowMultiple = false
        });

        var selected = folders.Count > 0 ? folders[0].TryGetLocalPath() : null;
        if (!string.IsNullOrWhiteSpace(selected) && DataContext is MainWindowViewModel vm)
            vm.OpenProject(selected);
    }
}
