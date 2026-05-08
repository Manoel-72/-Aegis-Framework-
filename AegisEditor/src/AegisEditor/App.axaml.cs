using System;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using AegisEditor.Services;
using AegisEditor.ViewModels;
using AegisEditor.Views;
using Microsoft.Extensions.DependencyInjection;

namespace AegisEditor;

/// <remarks>Composition root DI (MVVM-safe: só ViewModels são resolvidos aqui).</remarks>
public partial class App : Application
{
    private ServiceProvider? _services;

    internal ServiceProvider Services =>
        _services ?? throw new InvalidOperationException("Services not initialized.");

    public override void Initialize()
    {
        base.Initialize();
        _services = BuildServices();
    }

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            var mainVm = _services!.GetRequiredService<MainWindowViewModel>();
            desktop.MainWindow = new MainWindow
            {
                DataContext = mainVm,
            };
        }

        base.OnFrameworkInitializationCompleted();
    }

    private static ServiceProvider BuildServices()
    {
        var sc = new ServiceCollection();
        sc.AddSingleton<IUiThreadScheduler, AvaloniaUiThreadScheduler>();
        sc.AddSingleton<ConsoleViewModel>();
        sc.AddSingleton<IEditorLogSink>(sp => sp.GetRequiredService<ConsoleViewModel>());
        sc.AddSingleton<HierarchyViewModel>();
        sc.AddSingleton<InspectorViewModel>();
        sc.AddSingleton<ViewportViewModel>();
        sc.AddSingleton<LuaEditorViewModel>();
        sc.AddSingleton<NamedPipeBridgeOptions>(_ => NamedPipeBridgeOptions.Default);
        sc.AddSingleton<EditorBridgeClient>();
        sc.AddSingleton<IEditorBridgeClient>(sp => sp.GetRequiredService<EditorBridgeClient>());
        sc.AddSingleton<IRuntimeLauncher, RuntimeLauncherService>();
        sc.AddSingleton<ISceneSerializer, SceneSerializer>();
        sc.AddSingleton<IAssetBrowserService, AssetBrowserService>();
        sc.AddTransient<MainWindowViewModel>();

        var provider = sc.BuildServiceProvider();
        return provider;
    }
}
