using Microsoft.Extensions.Logging;

namespace KatzuoOgust.Cqrs.Flow.Logging;

using KatzuoOgust.Cqrs;
using KatzuoOgust.Cqrs.Flow;

/// <summary>
/// A flow command handler wrapper that logs command execution and flow sequences.
/// </summary>
public sealed class LoggingFlowCommandHandler<TCommand> : IFlowCommandHandler<TCommand>
	where TCommand : ICommand
{
	private readonly IFlowCommandHandler<TCommand> _inner;
	private readonly ILogger<LoggingFlowCommandHandler<TCommand>> _logger;

	/// <summary>Initializes a new instance of <see cref="LoggingFlowCommandHandler{TCommand}"/>.</summary>
	/// <param name="inner">The wrapped flow command handler.</param>
	/// <param name="logger">The logger instance.</param>
	public LoggingFlowCommandHandler(IFlowCommandHandler<TCommand> inner, ILogger<LoggingFlowCommandHandler<TCommand>> logger)
	{
		_inner = inner ?? throw new ArgumentNullException(nameof(inner));
		_logger = logger ?? throw new ArgumentNullException(nameof(logger));
	}

	/// <inheritdoc/>
	public async Task<IEnumerable<ICommand>> ExecuteAsync(TCommand command, CancellationToken cancellationToken = default)
	{
		ArgumentNullException.ThrowIfNull(command);

		var commandType = typeof(TCommand).Name;
		Log.ExecutingFlow(_logger, commandType);

		var startTime = Environment.TickCount64;
		try
		{
			var result = await _inner.ExecuteAsync(command, cancellationToken).ConfigureAwait(false);
			var resultList = result.ToList();
			var elapsed = Environment.TickCount64 - startTime;
			Log.FlowCompleted(_logger, commandType, resultList.Count, elapsed);
			return resultList;
		}
		catch (Exception ex)
		{
			var elapsed = Environment.TickCount64 - startTime;
			Log.FlowFailed(_logger, ex, commandType, elapsed);
			throw;
		}
	}

	private static class Log
	{
		internal static void ExecutingFlow(ILogger logger, string commandType) =>
			logger.LogDebug("Flow: executing command: {CommandType}", commandType);

		internal static void FlowCompleted(ILogger logger, string commandType, int nextCommandCount, long elapsed) =>
			logger.LogDebug("Flow: command completed: {CommandType} → {NextCommandCount} commands ({ElapsedMs}ms)", commandType, nextCommandCount, elapsed);

		internal static void FlowFailed(ILogger logger, Exception ex, string commandType, long elapsed) =>
			logger.LogError(ex, "Flow: command failed: {CommandType} ({ElapsedMs}ms)", commandType, elapsed);
	}
}
