using Microsoft.Extensions.Logging;

namespace KatzuoOgust.Cqrs.Pipeline.Behaviours.Logging;

using KatzuoOgust.Cqrs;

/// <summary>
/// An event pipeline behaviour that logs all events flowing through the pipeline.
/// </summary>
public sealed class LoggingEventPipelineBehaviour : IEventPipelineBehaviour
{
	private readonly ILogger<LoggingEventPipelineBehaviour> _logger;

	/// <summary>Initializes a new instance of <see cref="LoggingEventPipelineBehaviour"/>.</summary>
	/// <param name="logger">The logger instance.</param>
	public LoggingEventPipelineBehaviour(ILogger<LoggingEventPipelineBehaviour> logger)
	{
		_logger = logger ?? throw new ArgumentNullException(nameof(logger));
	}

	/// <inheritdoc/>
	public async Task HandleAsync(IEvent @event, CancellationToken ct, EventBehaviourDelegate next)
	{
		ArgumentNullException.ThrowIfNull(@event);
		ArgumentNullException.ThrowIfNull(next);

		var eventType = @event.GetType().Name;

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
			logger.LogDebug("Behaviour: handling event: {EventType}", eventType);

		internal static void EventCompleted(ILogger logger, string eventType, long elapsed) =>
			logger.LogDebug("Behaviour: event completed: {EventType} ({ElapsedMs}ms)", eventType, elapsed);

		internal static void EventFailed(ILogger logger, Exception ex, string eventType, long elapsed) =>
			logger.LogError(ex, "Behaviour: event failed: {EventType} ({ElapsedMs}ms)", eventType, elapsed);
	}
}
