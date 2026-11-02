using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;

namespace Swapping.Api.Tests;

public sealed record LogLine(string Category, LogLevel Level, string Message, IReadOnlyDictionary<string, string> Values)
{
    /// <summary>The message and every structured value, for "does any log contain X" checks.</summary>
    public string AllText => Message + " " + string.Join(" ", Values.Select(v => $"{v.Key}={v.Value}"));
}

/// <summary>Keeps every log line written while a test runs.</summary>
public sealed class TestLoggerProvider : ILoggerProvider
{
    public ConcurrentQueue<LogLine> Lines { get; } = new();

    public ILogger CreateLogger(string categoryName) => new Logger(categoryName, Lines);

    public void Dispose()
    {
    }

    private sealed class Logger(string category, ConcurrentQueue<LogLine> lines) : ILogger
    {
        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
        {
            var values = state is IEnumerable<KeyValuePair<string, object?>> pairs
                ? pairs.ToDictionary(p => p.Key, p => Convert.ToString(p.Value) ?? "")
                : new Dictionary<string, string>();
            lines.Enqueue(new LogLine(category, logLevel, formatter(state, exception) + (exception is null ? "" : " " + exception), values));
        }
    }
}
