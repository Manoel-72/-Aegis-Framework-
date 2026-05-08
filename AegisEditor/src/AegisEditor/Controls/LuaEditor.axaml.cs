using AegisEditor.ViewModels;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using AvaloniaEdit;

namespace AegisEditor.Controls;

public sealed partial class LuaEditor : UserControl
{
    private TextEditor? _textEditor;

    private bool _hooked;

    public LuaEditor()
    {
        AvaloniaXamlLoader.Load(this);
        Loaded += (_, _) => TryHookEditor();
    }

    /// <inheritdoc />
    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);
        TryHookEditor();
    }

    /// <inheritdoc />
    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        if (change.Property == DataContextProperty && change.NewValue is LuaEditorViewModel vm)
        {
            if (_textEditor is not null)
                _textEditor.Text = vm.LuaText;
        }

        base.OnPropertyChanged(change);
    }

    private void TryHookEditor()
    {
        if (_hooked) return;

        _textEditor = this.FindControl<TextEditor>("Editor");
        if (_textEditor is null) return;

        _hooked = true;

        if (DataContext is LuaEditorViewModel vm)
            _textEditor.Text = vm.LuaText;

        _textEditor.TextChanged += (_, _) =>
        {
            if (DataContext is LuaEditorViewModel v)
                v.LuaText = _textEditor?.Text ?? string.Empty;
        };
    }
}
