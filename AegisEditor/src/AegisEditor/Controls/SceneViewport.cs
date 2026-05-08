using System.ComponentModel;
using AegisEditor.Shared.Models;
using AegisEditor.ViewModels;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using SkiaSharp;

namespace AegisEditor.Controls;

/// <summary>
/// SkiaSharp draws directly into an Avalonia <see cref="WriteableBitmap"/> framebuffer.
/// </summary>
public sealed class SceneViewport : Control
{
    private WriteableBitmap? _bitmap;

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        if (change.Property == DataContextProperty)
        {
            if (change.OldValue is ViewportViewModel oldVm)
                oldVm.PropertyChanged -= Viewport_PropertyChanged;

            if (change.NewValue is ViewportViewModel newVm)
                newVm.PropertyChanged += Viewport_PropertyChanged;

            InvalidateVisual();
        }

        base.OnPropertyChanged(change);
    }

    private void Viewport_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName is nameof(ViewportViewModel.Entities)
            or nameof(ViewportViewModel.Tilemaps)
            or nameof(ViewportViewModel.PaintTick))
            InvalidateVisual();
    }

    public sealed override void Render(DrawingContext context)
    {
        base.Render(context);

        var vw = Bounds.Width;
        var vh = Bounds.Height;

        var topLevel = TopLevel.GetTopLevel(this);
        var scale = topLevel?.RenderScaling ?? 1.0;

        if (vw <= 4 || vh <= 4)
            return;

        var pixelW = Math.Max(8, (int)Math.Ceiling(vw * scale));
        var pixelH = Math.Max(8, (int)Math.Ceiling(vh * scale));

        var needed = new PixelSize(pixelW, pixelH);

        if (_bitmap is null || _bitmap.PixelSize != needed)
        {
            _bitmap?.Dispose();

            var dpi = new Vector(96d * scale, 96d * scale);
            _bitmap = new WriteableBitmap(
                needed,
                dpi,
                PixelFormat.Bgra8888,
                AlphaFormat.Premul);
        }

        if (DataContext is not ViewportViewModel viewport)
            return;

        var entities = viewport.Entities.ToArray();

        var info = new SKImageInfo(pixelW, pixelH, SKColorType.Bgra8888, SKAlphaType.Premul);

        using (var framebuffer = _bitmap.Lock())
        {
            using var surface = SKSurface.Create(info, framebuffer.Address, framebuffer.RowBytes);
            DrawWorld(surface.Canvas!, pixelW, pixelH, entities);
            surface.Flush();
        }

        context.DrawImage(_bitmap, new Rect(default, Bounds.Size));
    }

    private static void DrawWorld(SKCanvas canvas, float w, float h, IReadOnlyList<SceneEntityDto> entities)
    {
        canvas.Clear(new SKColor(0x08, 0x0d, 0x12));

        var gridPaint = new SKPaint
        {
            Color = SKColor.Parse("#12151e"),
            StrokeWidth = 1,
            IsStroke = true,
            IsAntialias = false,
        };

        const float step = 32f;
        for (var x = 0f; x < w; x += step)
            canvas.DrawLine(x, 0f, x, h, gridPaint);
        for (var y = 0f; y < h; y += step)
            canvas.DrawLine(0f, y, w, y, gridPaint);

        var spritePaint = new SKPaint { IsAntialias = true, Style = SKPaintStyle.Fill };

        foreach (var e in entities)
        {
            spritePaint.Color = PickColor(e.Type);
            var rw = MathF.Max(6f, 24f * e.ScaleX);
            var rh = MathF.Max(6f, 24f * e.ScaleY);
            var rect = new SKRect(e.X, e.Y, e.X + rw, e.Y + rh);
            canvas.Save();
            canvas.RotateRadians(e.Rotation, rect.MidX, rect.MidY);
            canvas.DrawRect(rect, spritePaint);
            canvas.Restore();
        }
    }

    private static SKColor PickColor(string type)
        => type.ToUpperInvariant() switch
        {
            "TILEMAP" => SKColors.DarkSlateGray,
            "CAMERA" => SKColors.MediumPurple,
            "SPRITE" or _ => SKColor.Parse("#5b7fff"),
        };
}
