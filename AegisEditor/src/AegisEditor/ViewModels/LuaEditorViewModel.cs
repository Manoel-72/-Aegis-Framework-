using CommunityToolkit.Mvvm.ComponentModel;

namespace AegisEditor.ViewModels;

public sealed partial class LuaEditorViewModel : ObservableObject
{
    [ObservableProperty]
    private string _luaText = "-- Aegis Lua\r\nreturn function() end\r\n";
}
