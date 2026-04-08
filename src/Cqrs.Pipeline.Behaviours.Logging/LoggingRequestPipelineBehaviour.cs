using Microsoft.Extensions.Logging;

namespace KatzuoOgust.Cqrs.Pipeline.Behaviours.Logging;

using KatzuoOgust.Cqrs;

/// <summary>
/// A request pipeline behaviour that logs all requests flowing through the pipeline.
/// </summary>
public sealed class LoggingRequestPipelineBehaviour : IRequestPipelineBehaviour
{
	private readonly ILogger<LoggingRequestPipelineBehaviour> _logger;

	/// <summary>Initializes a new instance of <see cref="LoggingRequestPipelineBehaviour"/>.</summary>
	/// <param name="logger">The logger instance.</param>
	public LoggingRequestPipelineBehaviour(ILogger<LoggingRequestPipelineBehaviour> logger)
	{
		_logger = logger ?? throw new ArgumentNullException(nameof(logger));
	}

	/// <inheritdoc/>
	public async Task<object?> HandleAsync(IRequest request, CancellationToken ct, RequestBehaviourDelegate next)
	{
		ArgumentNullException.ThrowIfNull(request);
		ArgumentNullException.ThrowIfNull(next);

		var requestType = request.GetType().Name;

		Log.HandlingRequest(_logger, requestType);

		var startTime = Environment.TickCount64;
		try
		{
			var result = await next(ct).ConfigureAwait(false);
			var elapsed = Environment.TickCount64 - startTime;
			Log.RequestCompleted(_logger, requestType, elapsed);
			return result;
		}
		catch (Exception ex)
		{
			var elapsed = Environment.TickCount64 - startTime;
			Log.RequestFailed(_logger, ex, requestType, elapsed);
			throw;
		}
	}

	private static class Log
	{
		internal static void HandlingRequest(ILogger logger, string requestType) =>
			logger.LogDebug("Behaviour: handling request: {RequestType}", requestType);

		internal static void RequestCompleted(ILogger logger, string requestType, long elapsed) =>
			logger.LogDebug("Behaviour: request completed: {RequestType} ({ElapsedMs}ms)", requestType, elapsed);

		internal static void RequestFailed(ILogger logger, Exception ex, string requestType, long elapsed) =>
			logger.LogError(ex, "Behaviour: request failed: {RequestType} ({ElapsedMs}ms)", requestType, elapsed);
	}
}
