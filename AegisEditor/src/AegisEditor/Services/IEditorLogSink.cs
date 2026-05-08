namespace AegisEditor.Services;

public interface IEditorLogSink
{
    void Post(EditorLogLevel level, string message);
}

