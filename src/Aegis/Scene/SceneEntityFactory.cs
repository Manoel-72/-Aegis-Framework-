using Aegis.Core;
using Aegis.Display;
using Aegis.Resource;
using AegisEditor.Shared.Models;

namespace Aegis.Scene;

public sealed class SceneEntityFactory
{
    private readonly SceneComponentRegistry _components;

    public SceneEntityFactory(SceneComponentRegistry? components = null)
    {
        _components = components ?? new SceneComponentRegistry();
    }

    public Object2D Create(SceneEntityDto entity, Object2D parent)
    {
        var obj = CreateObject(entity, parent);
        _components.ApplyAll(obj, entity);

        if (entity.Components.Count == 0)
            ApplyLegacyTransform(obj, entity);

        return obj;
    }

    private static Object2D CreateObject(SceneEntityDto entity, Object2D parent)
    {
        var type = NormalizeType(entity.Type);
        var sprite = SceneComponentJson.Get(entity, "SpriteRenderer");
        var texturePath = SceneComponentJson.String(sprite, "sprite", "texture", "texturePath") ?? entity.TexturePath;

        if (sprite is not null && (type is "empty" or "group"))
            type = "sprite";

        if (type is "sprite" or "bitmap")
            return CreateBitmap(entity, parent, texturePath);

        if (type is "rect" or "rectangle")
            return new Bitmap(ResManager.Pixel, parent);

        return new Object2D(parent);
    }

    private static Bitmap CreateBitmap(SceneEntityDto entity, Object2D parent, string? texturePath)
    {
        if (string.IsNullOrWhiteSpace(texturePath))
            return new Bitmap(ResManager.Pixel, parent);

        try
        {
            return new Bitmap(ResManager.LoadTexture(texturePath), parent);
        }
        catch (Exception ex)
        {
            AegisLog.Warn("Scene", $"Sprite '{entity.Name}' ignorou textura ausente/invalida '{texturePath}': {ex.Message}");
            return new Bitmap(ResManager.Pixel, parent);
        }
    }

    private static void ApplyLegacyTransform(Object2D obj, SceneEntityDto entity)
    {
        obj.X = Finite(entity.X);
        obj.Y = Finite(entity.Y);
        obj.ScaleX = Finite(entity.ScaleX, 1f);
        obj.ScaleY = Finite(entity.ScaleY, 1f);
        obj.Rotation = Finite(entity.Rotation);
    }

    private static string NormalizeType(string? type)
        => string.IsNullOrWhiteSpace(type)
            ? "empty"
            : type.Trim().Replace("_", "", StringComparison.Ordinal).Replace("-", "", StringComparison.Ordinal).ToLowerInvariant();

    private static float Finite(float value, float fallback = 0f)
        => float.IsFinite(value) ? value : fallback;
}
