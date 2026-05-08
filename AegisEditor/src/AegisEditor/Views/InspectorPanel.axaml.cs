using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace AegisEditor.Views;

public partial class InspectorPanel : UserControl
{
    public InspectorPanel()
    {
        AvaloniaXamlLoader.Load(this);
    }
}
