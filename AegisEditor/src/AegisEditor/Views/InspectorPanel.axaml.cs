using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using AegisEditor.ViewModels;

namespace AegisEditor.Views;

public partial class InspectorPanel : UserControl
{
    public InspectorPanel()
    {
        AvaloniaXamlLoader.Load(this);
        AddHandler(InputElement.GotFocusEvent, OnEditorFieldGotFocus, RoutingStrategies.Tunnel);
        AddHandler(InputElement.LostFocusEvent, OnEditorFieldLostFocus, RoutingStrategies.Bubble);
    }

    private void OnEditorFieldGotFocus(object? sender, GotFocusEventArgs e)
    {
        if (e.Source is TextBox { IsReadOnly: false } && DataContext is InspectorViewModel inspector)
            inspector.BeginEdit();
    }

    private void OnEditorFieldLostFocus(object? sender, RoutedEventArgs e)
    {
        if (e.Source is TextBox { IsReadOnly: false } && DataContext is InspectorViewModel inspector)
            inspector.CommitEdit();
    }
}
