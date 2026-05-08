using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace AegisEditor.Views;

public partial class ConsolePanel : UserControl
{
    public ConsolePanel()
    {
        AvaloniaXamlLoader.Load(this);
    }
}
