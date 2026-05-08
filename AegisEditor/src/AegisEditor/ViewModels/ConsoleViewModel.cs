using System.Collections.ObjectModel;
using AegisEditor.Services;
using CommunityToolkit.Mvvm.ComponentModel;

namespace AegisEditor.ViewModels;

public sealed partial class ConsoleViewModel(IUiThreadScheduler ui) : ObservableObject, IEditorLogSink
{
    public ObservableCollection<string> Lines { get; } = new();

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
        });
    }
}
