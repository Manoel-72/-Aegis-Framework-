using System.Collections.ObjectModel;
using AegisEditor.Shared.Messages;
using AegisEditor.Shared.Models;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace AegisEditor.ViewModels;

public sealed partial class HierarchyViewModel : ObservableObject
{
    public ObservableCollection<SceneEntityDto> Entities { get; } = new();

    [ObservableProperty]
    private SceneEntityDto? _selectedEntity;

    public event EventHandler<SceneEntityDto?>? SelectedEntityChanged;

    public event EventHandler<string>? CreateEntityRequested;

    public event EventHandler<SceneEntityDto>? DeleteEntityRequested;

    public void ApplySceneState(SceneState state)
    {
        Entities.Clear();
        foreach (var e in state.Entities)
            Entities.Add(e);
    }

    public void ApplyScenePayload(SceneStatePayload payload)
    {
        Entities.Clear();
        foreach (var e in payload.Entities)
            Entities.Add(e);
    }

    partial void OnSelectedEntityChanged(SceneEntityDto? oldValue, SceneEntityDto? newValue)
        => SelectedEntityChanged?.Invoke(this, newValue);

    [RelayCommand]
    private void CreateEmpty()
        => CreateEntityRequested?.Invoke(this, "Empty");

    [RelayCommand]
    private void CreateSprite()
        => CreateEntityRequested?.Invoke(this, "Sprite");

    [RelayCommand]
    private void CreateCamera()
        => CreateEntityRequested?.Invoke(this, "Camera");

    [RelayCommand]
    private void DeleteSelected()
    {
        if (SelectedEntity is not null)
            DeleteEntityRequested?.Invoke(this, SelectedEntity);
    }
}
