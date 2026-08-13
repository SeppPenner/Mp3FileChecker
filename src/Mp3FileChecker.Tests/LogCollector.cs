// --------------------------------------------------------------------------------------------------------------------
// <copyright file="LogCollector.cs" company="Hämmer Electronics">
//   Copyright (c) All rights reserved.
// </copyright>
// <summary>
//   A Serilog sink that keeps the written log events in memory.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace Mp3FileChecker.Tests;

/// <summary>
/// A Serilog sink that keeps the written log events in memory. The checked code reports its findings
/// through the static <see cref="Log"/> class only, so the log is the way to observe what it did.
/// </summary>
public sealed class LogCollector : ILogEventSink
{
    /// <summary>
    /// The collected log events.
    /// </summary>
    private readonly List<LogEvent> logEvents = [];

    /// <summary>
    /// Gets the collected log events.
    /// </summary>
    public IReadOnlyList<LogEvent> LogEvents => this.logEvents;

    /// <summary>
    /// Adds the given log event to the collected ones.
    /// </summary>
    /// <param name="logEvent">The log event.</param>
    public void Emit(LogEvent logEvent)
    {
        this.logEvents.Add(logEvent);
    }

    /// <summary>
    /// Gets the collected log events of the given level that were written with the given message template.
    /// </summary>
    /// <param name="level">The log event level.</param>
    /// <param name="messageTemplate">The message template as it stands in the source code.</param>
    /// <returns>The matching log events.</returns>
    public List<LogEvent> GetEvents(LogEventLevel level, string messageTemplate)
    {
        return this.logEvents.Where(e => e.Level == level && e.MessageTemplate.Text == messageTemplate).ToList();
    }

    /// <summary>
    /// Gets the value of a scalar property of the given log event.
    /// </summary>
    /// <param name="logEvent">The log event.</param>
    /// <param name="propertyName">The property name as it stands in the message template.</param>
    /// <returns>The property value as text.</returns>
    public static string GetPropertyValue(LogEvent logEvent, string propertyName)
    {
        if (logEvent.Properties.TryGetValue(propertyName, out var property) && property is ScalarValue scalarValue)
        {
            return scalarValue.Value?.ToString() ?? string.Empty;
        }

        return string.Empty;
    }
}
