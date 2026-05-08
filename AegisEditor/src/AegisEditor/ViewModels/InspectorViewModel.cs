using AegisEditor.Shared.Models;
using CommunityToolkit.Mvvm.ComponentModel;

namespace AegisEditor.ViewModels;

public sealed partial class InspectorViewModel : ObservableObject
{
    [ObservableProperty]
    private SceneEntityDto? _target;

    public void ApplySelection(SceneEntityDto? entity)
        => Target = entity;
}
