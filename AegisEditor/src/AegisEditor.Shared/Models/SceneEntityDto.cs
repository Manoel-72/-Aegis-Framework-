using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Text.Json.Serialization;

namespace AegisEditor.Shared.Models;

public sealed class SceneEntityDto : INotifyPropertyChanged
{
    private string _id = string.Empty;
    private string _name = string.Empty;
    private string _type = string.Empty;
    private float _x;
    private float _y;
    private float _scaleX = 1f;
    private float _scaleY = 1f;
    private float _rotation;
    private string? _texturePath;
    private string? _scriptPath;
    private string? _parentId;

    public event PropertyChangedEventHandler? PropertyChanged;

    public string Id
    {
        get => _id;
        set => Set(ref _id, value ?? string.Empty);
    }

    public string Name
    {
        get => _name;
        set => Set(ref _name, value ?? string.Empty);
    }

    public string Type
    {
        get => _type;
        set => Set(ref _type, value ?? string.Empty);
    }

    public float X
    {
        get => _x;
        set => Set(ref _x, value);
    }

    public float Y
    {
        get => _y;
        set => Set(ref _y, value);
    }

    public float ScaleX
    {
        get => _scaleX;
        set => Set(ref _scaleX, value);
    }

    public float ScaleY
    {
        get => _scaleY;
        set => Set(ref _scaleY, value);
    }

    public float Rotation
    {
        get => _rotation;
        set => Set(ref _rotation, value);
    }

    public string? TexturePath
    {
        get => _texturePath;
        set => Set(ref _texturePath, value);
    }

    public string? ScriptPath
    {
        get => _scriptPath;
        set => Set(ref _scriptPath, value);
    }

    [JsonConverter(typeof(ComponentListJsonConverter))]
    public List<ComponentDto> Components { get; set; } = new();

    public List<string> Children { get; set; } = new();

    public string? ParentId
    {
        get => _parentId;
        set => Set(ref _parentId, value);
    }

    private void Set<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value)) return;
        field = value;
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
