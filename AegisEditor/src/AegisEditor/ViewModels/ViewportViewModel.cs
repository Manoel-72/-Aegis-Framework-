using System.Collections.ObjectModel;
using AegisEditor.Shared.Messages;
using AegisEditor.Shared.Models;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace AegisEditor.ViewModels;

public sealed partial class ViewportViewModel : ObservableObject
{
    [ObservableProperty]
    private int _paintTick;

    [ObservableProperty]
    private SceneEntityDto? _selectedEntity;

    [ObservableProperty]
    private SceneEntityDto? _hoveredEntity;

    [ObservableProperty]
    private string _projectRoot = string.Empty;

    [ObservableProperty]
    private float _zoom = 1f;

    [ObservableProperty]
    private float _panX;

    [ObservableProperty]
    private float _panY;

    [ObservableProperty]
    private bool _snapEnabled = true;

    [ObservableProperty]
    private bool _showGrid = true;

    [ObservableProperty]
    private int _gridSize = 32;

    public event EventHandler<SceneEntityDto?>? SelectedEntityChanged;

    public event EventHandler<SpriteDropRequest>? SpriteDropRequested;

    public event EventHandler<IReadOnlyList<SceneEntityDto>>? DeleteEntitiesRequested;

    public event EventHandler<EntityTransformCommit>? TransformCommitted;

    public ObservableCollection<SceneEntityDto> Entities { get; } = new();

    public ObservableCollection<SceneEntityDto> SelectedEntities { get; } = new();

    public ObservableCollection<TilemapDto> Tilemaps { get; } = new();

    public void ApplySceneState(SceneState state)
    {
        UnsubscribeEntities();
        Entities.Clear();
        SelectedEntities.Clear();
        SelectedEntity = null;

        foreach (var e in state.Entities)
        {
            Entities.Add(e);
            e.PropertyChanged += Entity_PropertyChanged;
        }

        Tilemaps.Clear();
        foreach (var t in state.Tilemaps)
            Tilemaps.Add(t);

        NotifyRedraw();
    }

    /// <remarks>Uso direto a partir do payload do evento <see cref="RuntimeEvent.SceneState"/>.</remarks>
    public void ApplyScenePayload(SceneStatePayload payload)
    {
        UnsubscribeEntities();
        Entities.Clear();
        SelectedEntities.Clear();
        SelectedEntity = null;

        foreach (var e in payload.Entities)
        {
            Entities.Add(e);
            e.PropertyChanged += Entity_PropertyChanged;
        }

        Tilemaps.Clear();
        if (payload.Tilemaps is not null)
        {
            foreach (var t in payload.Tilemaps)
                Tilemaps.Add(t);
        }

        NotifyRedraw();
    }

    public void NotifyRedraw() => PaintTick++;

    public void AddEntity(SceneEntityDto entity)
    {
        Entities.Add(entity);
        entity.PropertyChanged += Entity_PropertyChanged;
        SelectOnly(entity);
        NotifyRedraw();
    }

    public void RequestSpriteDrop(string texturePath, float x, float y)
    {
        if (string.IsNullOrWhiteSpace(texturePath)) return;
        SpriteDropRequested?.Invoke(this, new SpriteDropRequest(texturePath.Replace('\\', '/'), Snap(x), Snap(y)));
    }

    public void RequestDeleteSelected()
    {
        if (SelectedEntities.Count == 0 && SelectedEntity is not null)
            SelectOnly(SelectedEntity);

        if (SelectedEntities.Count == 0) return;
        DeleteEntitiesRequested?.Invoke(this, SelectedEntities.ToArray());
    }

    public void RequestTransformCommitted(IReadOnlyList<EntityTransform> before, IReadOnlyList<EntityTransform> after)
    {
        if (before.Count == 0 || after.Count == 0) return;
        TransformCommitted?.Invoke(this, new EntityTransformCommit(before.ToArray(), after.ToArray()));
    }

    public void SelectOnly(SceneEntityDto? entity)
    {
        SelectedEntities.Clear();
        if (entity is not null)
            SelectedEntities.Add(entity);
        SelectedEntity = entity;
        NotifyRedraw();
    }

    public void ToggleSelection(SceneEntityDto entity)
    {
        if (SelectedEntities.Contains(entity))
            SelectedEntities.Remove(entity);
        else
            SelectedEntities.Add(entity);

        SelectedEntity = SelectedEntities.LastOrDefault();
        NotifyRedraw();
    }

    public void SelectArea(Func<SceneEntityDto, bool> predicate)
    {
        SelectedEntities.Clear();
        foreach (var entity in Entities.Where(predicate))
            SelectedEntities.Add(entity);
        SelectedEntity = SelectedEntities.LastOrDefault();
        NotifyRedraw();
    }

    private void Entity_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
        => NotifyRedraw();

    partial void OnSelectedEntityChanged(SceneEntityDto? oldValue, SceneEntityDto? newValue)
    {
        if (newValue is not null && !SelectedEntities.Contains(newValue))
        {
            SelectedEntities.Clear();
            SelectedEntities.Add(newValue);
        }

        NotifyRedraw();
        SelectedEntityChanged?.Invoke(this, newValue);
    }

    partial void OnHoveredEntityChanged(SceneEntityDto? oldValue, SceneEntityDto? newValue) => NotifyRedraw();
    partial void OnZoomChanged(float oldValue, float newValue) => NotifyRedraw();
    partial void OnPanXChanged(float oldValue, float newValue) => NotifyRedraw();
    partial void OnPanYChanged(float oldValue, float newValue) => NotifyRedraw();
    partial void OnSnapEnabledChanged(bool oldValue, bool newValue) => NotifyRedraw();
    partial void OnShowGridChanged(bool oldValue, bool newValue) => NotifyRedraw();
    partial void OnGridSizeChanged(int oldValue, int newValue) => NotifyRedraw();

    public float Snap(float value, bool bypass = false)
    {
        if (bypass || !SnapEnabled || GridSize <= 1) return value;
        return MathF.Round(value / GridSize) * GridSize;
    }

    [RelayCommand]
    private void ToggleSnap()
        => SnapEnabled = !SnapEnabled;

    [RelayCommand]
    private void ToggleGrid()
        => ShowGrid = !ShowGrid;

    [RelayCommand]
    private void IncreaseGrid()
        => GridSize = Math.Clamp(GridSize + 8, 4, 256);

    [RelayCommand]
    private void DecreaseGrid()
        => GridSize = Math.Clamp(GridSize - 8, 4, 256);

    [RelayCommand]
    private void ResetGrid()
    {
        GridSize = 32;
        ShowGrid = true;
    }

    [RelayCommand]
    private void ResetView()
    {
        Zoom = 1f;
        PanX = 0f;
        PanY = 0f;
    }

    private void UnsubscribeEntities()
    {
        foreach (var entity in Entities)
            entity.PropertyChanged -= Entity_PropertyChanged;
    }
}

public sealed record SpriteDropRequest(string TexturePath, float X, float Y);

public sealed record EntityTransform(string Id, float X, float Y, float ScaleX, float ScaleY, float Rotation)
{
    public static EntityTransform From(SceneEntityDto entity)
        => new(entity.Id, entity.X, entity.Y, entity.ScaleX, entity.ScaleY, entity.Rotation);
}

public sealed record EntityTransformCommit(IReadOnlyList<EntityTransform> Before, IReadOnlyList<EntityTransform> After);
