using Microsoft.Extensions.Logging;

namespace KatzuoOgust.Cqrs.Pipeline.Middlewares.Logging;

using KatzuoOgust.Cqrs;

/// <summary>
/// An event middleware that logs event entry, exit, and exceptions.
/// </summary>
public sealed class LoggingEventMiddleware<TEvent> : IEventMiddleware<TEvent>
	where TEvent : IEvent
{
	private readonly ILogger<LoggingEventMiddleware<TEvent>> _logger;

	/// <summary>Initializes a new instance of <see cref="LoggingEventMiddleware{TEvent}"/>.</summary>
	/// <param name="logger">The logger instance.</param>
	public LoggingEventMiddleware(ILogger<LoggingEventMiddleware<TEvent>> logger)
	{
		_logger = logger ?? throw new ArgumentNullException(nameof(logger));
	}

	/// <inheritdoc/>
	public async Task HandleAsync(TEvent @event, CancellationToken ct, EventMiddlewareDelegate next)
	{
		ArgumentNullException.ThrowIfNull(@event);
		ArgumentNullException.ThrowIfNull(next);

		var eventType = typeof(TEvent).Name;

		Log.HandlingEvent(_logger, eventType);

		var startTime = Environment.TickCount64;
		try
		{
			await next(ct).ConfigureAwait(false);
			var elapsed = Environment.TickCount64 - startTime;
			Log.EventCompleted(_logger, eventType, elapsed);
		}
		catch (Exception ex)
		{
			var elapsed = Environment.TickCount64 - startTime;
			Log.EventFailed(_logger, ex, eventType, elapsed);
			throw;
		}
	}

	private static class Log
	{
		internal static void HandlingEvent(ILogger logger, string eventType) =>
			logger.LogDebug("Handling event: {EventType}", eventType);

		internal static void EventCompleted(ILogger logger, string eventType, long elapsed) =>
			logger.LogDebug("Event completed: {EventType} ({ElapsedMs}ms)", eventType, elapsed);

		internal static void EventFailed(ILogger logger, Exception ex, string eventType, long elapsed) =>
			logger.LogError(ex, "Event failed: {EventType} ({ElapsedMs}ms)", eventType, elapsed);
	}
}
