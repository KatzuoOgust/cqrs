using Microsoft.Extensions.Logging;

namespace KatzuoOgust.Cqrs.Pipeline.Middlewares.Logging;

using KatzuoOgust.Cqrs;

/// <summary>
/// A request middleware that logs request entry, exit, and exceptions.
/// </summary>
public sealed class LoggingRequestMiddleware<TRequest, TResponse> : IRequestMiddleware<TRequest, TResponse>
	where TRequest : IRequest<TResponse>
{
	private readonly ILogger<LoggingRequestMiddleware<TRequest, TResponse>> _logger;

	/// <summary>Initializes a new instance of <see cref="LoggingRequestMiddleware{TRequest,TResponse}"/>.</summary>
	/// <param name="logger">The logger instance.</param>
	public LoggingRequestMiddleware(ILogger<LoggingRequestMiddleware<TRequest, TResponse>> logger)
	{
		_logger = logger ?? throw new ArgumentNullException(nameof(logger));
	}

	/// <inheritdoc/>
	public async Task<TResponse> HandleAsync(TRequest request, CancellationToken ct, RequestMiddlewareDelegate<TResponse> next)
	{
		ArgumentNullException.ThrowIfNull(request);
		ArgumentNullException.ThrowIfNull(next);

		var requestType = typeof(TRequest).Name;
		var responseType = typeof(TResponse).Name;

		Log.HandlingRequest(_logger, requestType, responseType);

		var startTime = Environment.TickCount64;
		try
		{
			var result = await next(ct).ConfigureAwait(false);
			var elapsed = Environment.TickCount64 - startTime;
			Log.RequestCompleted(_logger, requestType, responseType, elapsed);
			return result;
		}
		catch (Exception ex)
		{
			var elapsed = Environment.TickCount64 - startTime;
			Log.RequestFailed(_logger, ex, requestType, responseType, elapsed);
			throw;
		}
	}

	private static class Log
	{
		internal static void HandlingRequest(ILogger logger, string requestType, string responseType) =>
			logger.LogDebug("Handling request: {RequestType} → {ResponseType}", requestType, responseType);

		internal static void RequestCompleted(ILogger logger, string requestType, string responseType, long elapsed) =>
			logger.LogDebug("Request completed: {RequestType} → {ResponseType} ({ElapsedMs}ms)", requestType, responseType, elapsed);

		internal static void RequestFailed(ILogger logger, Exception ex, string requestType, string responseType, long elapsed) =>
			logger.LogError(ex, "Request failed: {RequestType} → {ResponseType} ({ElapsedMs}ms)", requestType, responseType, elapsed);
	}
}
