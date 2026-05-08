namespace Aegis.Core;

/// <summary>
/// Logger central da engine. Mantém prefixos consistentes e facilita debugar.
/// </summary>
public static class AegisLog
{
    public static bool Verbose { get; set; } = true;

    public static void Info(string area, string message)
    {
        if (!Verbose) return;
        Console.WriteLine($"[Aegis|{area}] {message}");
    }

    public static void Warn(string area, string message)
        => Console.WriteLine($"[Aegis|{area}|WARN] {message}");

    public static void Error(string area, string message)
        => Console.Error.WriteLine($"[Aegis|{area}|ERROR] {message}");

    public static void Exception(string area, Exception ex)
        => Console.Error.WriteLine($"[Aegis|{area}|ERROR] {ex.GetType().Name}: {ex.Message}\n{ex}");
}
