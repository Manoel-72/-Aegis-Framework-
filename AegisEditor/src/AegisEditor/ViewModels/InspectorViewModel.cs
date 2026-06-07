using AegisEditor.Shared.Models;
using CommunityToolkit.Mvvm.ComponentModel;
using System.Text.Json;

namespace AegisEditor.ViewModels;

public sealed partial class InspectorViewModel : ObservableObject
{
    [ObservableProperty]
    private SceneEntityDto? _target;

    private SceneEntityDto? _editBefore;

    public event EventHandler<InspectorEditCommit>? EditCommitted;

    public void ApplySelection(SceneEntityDto? entity)
    {
        CommitEdit();
        Target = entity;
    }

    public void BeginEdit()
    {
        if (Target is null || _editBefore is not null)
            return;

        _editBefore = Clone(Target);
    }

    public void CommitEdit()
    {
        if (_editBefore is null || Target is null)
        {
            _editBefore = null;
            return;
        }

        var before = _editBefore;
        var after = Clone(Target);
        _editBefore = null;

        if (JsonSerializer.Serialize(before) == JsonSerializer.Serialize(after))
            return;

        EditCommitted?.Invoke(this, new InspectorEditCommit(before, after));
    }

    private static SceneEntityDto Clone(SceneEntityDto entity)
    {
        var json = JsonSerializer.Serialize(entity);
        return JsonSerializer.Deserialize<SceneEntityDto>(json)
            ?? throw new InvalidOperationException("Falha ao clonar entidade do Inspector.");
    }
}

public sealed record InspectorEditCommit(SceneEntityDto Before, SceneEntityDto After);
