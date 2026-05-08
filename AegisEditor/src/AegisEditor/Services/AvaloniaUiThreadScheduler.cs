using Avalonia.Threading;

namespace AegisEditor.Services;

public sealed class AvaloniaUiThreadScheduler : IUiThreadScheduler
{
    public void Post(Action action)
        => Dispatcher.UIThread.Post(action);
}
