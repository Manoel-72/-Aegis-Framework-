using Avalonia.Media.Imaging;

namespace AegisEditor.ViewModels;

public sealed class AssetBrowserItemViewModel
{
    public required string Name { get; init; }

    public required string FullPath { get; init; }

    public required string RelativePath { get; init; }

    public required string Kind { get; init; }

    public bool IsDirectory { get; init; }

    public bool IsSprite => Kind == "Sprite";

    public bool IsBroken { get; init; }

    public string ValidationMessage { get; init; } = string.Empty;

    public Bitmap? Thumbnail { get; init; }

    public string Badge => Kind switch
    {
        "Folder" => "DIR",
        "Sprite" => "IMG",
        "Audio" => "SND",
        "Tilemap" => "MAP",
        "Script" => "LUA",
        "Font" => "TTF",
        _ => "FILE",
    };
}
