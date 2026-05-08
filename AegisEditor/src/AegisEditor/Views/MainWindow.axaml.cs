using AegisEditor.Services;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
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
}
