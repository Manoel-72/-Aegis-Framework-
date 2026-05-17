using System.Runtime.InteropServices;

namespace Aegis.Platform;

internal static class DisplayWakeLock
{
    [Flags]
    private enum ExecutionState : uint
    {
        Continuous = 0x80000000,
        SystemRequired = 0x00000001,
        DisplayRequired = 0x00000002,
    }

    public static void Enable()
    {
        if (!OperatingSystem.IsWindows()) return;
        SetThreadExecutionState(ExecutionState.Continuous | ExecutionState.SystemRequired | ExecutionState.DisplayRequired);
    }

    public static void Disable()
    {
        if (!OperatingSystem.IsWindows()) return;
        SetThreadExecutionState(ExecutionState.Continuous);
    }

    [DllImport("kernel32.dll")]
    private static extern ExecutionState SetThreadExecutionState(ExecutionState esFlags);
}
