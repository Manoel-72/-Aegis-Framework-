namespace AegisEditor.Services;

public interface IUiThreadScheduler
{
    void Post(Action action);
}
