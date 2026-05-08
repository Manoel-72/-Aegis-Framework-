using System.Collections.ObjectModel;
using AegisEditor.Shared.Messages;
using AegisEditor.Shared.Models;
using CommunityToolkit.Mvvm.ComponentModel;

namespace AegisEditor.ViewModels;

public sealed partial class ViewportViewModel : ObservableObject
{
    [ObservableProperty]
    private int _paintTick;

    public ObservableCollection<SceneEntityDto> Entities { get; } = new();

    public ObservableCollection<TilemapDto> Tilemaps { get; } = new();

    public void ApplySceneState(SceneState state)
    {
        Entities.Clear();
        foreach (var e in state.Entities)
            Entities.Add(e);

        Tilemaps.Clear();
        foreach (var t in state.Tilemaps)
            Tilemaps.Add(t);

        NotifyRedraw();
    }

    /// <remarks>Uso direto a partir do payload do evento <see cref="RuntimeEvent.SceneState"/>.</remarks>
    public void ApplyScenePayload(SceneStatePayload payload)
    {
        Entities.Clear();
        foreach (var e in payload.Entities)
            Entities.Add(e);

        Tilemaps.Clear();
        if (payload.Tilemaps is not null)
        {
            foreach (var t in payload.Tilemaps)
                Tilemaps.Add(t);
        }

        NotifyRedraw();
    }

    public void NotifyRedraw() => PaintTick++;
}
