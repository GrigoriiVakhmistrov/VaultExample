using System.Collections.Concurrent;

namespace Platform.Storage;

internal static class Secrets
{
    public static ConcurrentQueue<string> Queue { get; } = new();
}