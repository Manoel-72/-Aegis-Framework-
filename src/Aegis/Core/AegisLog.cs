namespace Aegis.Core;

/// <summary>
/// Logger central da engine. Mantém prefixos consistentes e facilita debugar.
/// </summary>
public static class AegisLog
{
    /// <summary>Encaminha texto para o servidor de pipe do editor, se ativo.</summary>
    public static event Action<string, string>? EchoToEditorIpc;

    public static bool Verbose { get; set; } = true;

    public static void Info(string area, string message)
    {
        if (Verbose)
            Console.WriteLine($"[Aegis|{area}] {message}");

        if (!area.Equals("Lua", StringComparison.OrdinalIgnoreCase))
            return;

        EchoToEditorIpc?.Invoke("info", $"[{area}] {message}");
    }

    public static void Warn(string area, string message)
    {
        Console.WriteLine($"[Aegis|{area}|WARN] {message}");
        EchoToEditorIpc?.Invoke("warn", $"[{area}] {message}");
    }

    public static void Error(string area, string message)
    {
        Console.Error.WriteLine($"[Aegis|{area}|ERROR] {message}");
        EchoToEditorIpc?.Invoke("error", $"[{area}] {message}");
    }

    public static void Exception(string area, Exception ex)
    {
        Console.Error.WriteLine($"[Aegis|{area}|ERROR] {ex.GetType().Name}: {ex.Message}\n{ex}");
        EchoToEditorIpc?.Invoke("error", $"[{area}] {ex.GetType().Name}: {ex.Message}");
    }
}
