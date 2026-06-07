using System.Collections.ObjectModel;
using AegisEditor.Services;
using CommunityToolkit.Mvvm.ComponentModel;

namespace AegisEditor.ViewModels;

public sealed partial class ConsoleViewModel(IUiThreadScheduler ui) : ObservableObject, IEditorLogSink
{
    public ObservableCollection<string> Lines { get; } = new();

    public ObservableCollection<string> Problems { get; } = new();

    public ObservableCollection<string> Samples { get; } = new()
    {
        "Drag a sprite from Assets into Scene View.",
        "Use mouse wheel to zoom and right mouse button to pan.",
        "Press Play to run scenes/active.scene.json.",
    };

    public ObservableCollection<string> Tags { get; } = new()
    {
        "INFO: normal editor/runtime event",
        "WARN: something works but needs attention",
        "ERROR: action failed or asset is invalid",
        "SCENE: scene load/save/runtime sync",
        "ASSET: asset validation and references",
    };

    public ObservableCollection<string> FormatLines { get; } = new()
    {
        "Console line format:",
        "HH:mm:ss [LEVEL] message",
        "Problem line format:",
        "[SEVERITY] message",
    };

    public void Post(EditorLogLevel level, string message)
    {
        var prefix = level switch
        {
            EditorLogLevel.Warning => "[WARN]",
            EditorLogLevel.Error => "[ERROR]",
            _ => "[INFO]",
        };

        var line = $"{DateTime.Now:HH:mm:ss} {prefix} {message}";
        ui.Post(() =>
        {
            Lines.Add(line);
            if (Lines.Count > 500)
                Lines.RemoveAt(0);

            if (level is EditorLogLevel.Warning or EditorLogLevel.Error)
            {
                Problems.Add($"{prefix} {message}");
                if (Problems.Count > 250)
                    Problems.RemoveAt(0);
            }
        });
    }
}
