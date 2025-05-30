using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Logging.Console;

namespace Common.Formatters;

public class MinimalConsoleFormatter : ConsoleFormatter
{
    public MinimalConsoleFormatter()
        : base("minimal") { }

    public override void Write<TState>(
        in LogEntry<TState> logEntry,
        IExternalScopeProvider? scopeProvider,
        TextWriter textWriter
    )
    {
        var message = logEntry.Formatter?.Invoke(logEntry.State, logEntry.Exception);
        if (logEntry.Exception != null)
        {
            message += System.Environment.NewLine + logEntry.Exception.ToString();
        }

        if (!string.IsNullOrEmpty(message))
        {
            textWriter.WriteLine(message);
        }
    }
}
