using Microsoft.Extensions.Logging;
using Xunit;

namespace KatzuoOgust.Cqrs.Pipeline.Behaviours.Logging;

public sealed class LoggingRequestPipelineBehaviourTests
{
	private sealed record TestRequest : IRequest<string>;

	[Fact]
	public async Task HandleAsync_ReturnsResponse_WhenNextCompletes()
	{
		var logger = new TestLogger<LoggingRequestPipelineBehaviour>();
		var behaviour = new LoggingRequestPipelineBehaviour(logger);
		var request = new TestRequest();
		var response = (object?)"result";

		var result = await behaviour.HandleAsync(request, default, _ => Task.FromResult(response));

		Assert.Equal(response, result);
		Assert.NotEmpty(logger.Logs);
	}

	[Fact]
	public async Task HandleAsync_LogsRequestEntry_WhenCalled()
	{
		var logger = new TestLogger<LoggingRequestPipelineBehaviour>();
		var behaviour = new LoggingRequestPipelineBehaviour(logger);
		var request = new TestRequest();

		await behaviour.HandleAsync(request, default, _ => Task.FromResult((object?)"result"));

		var entryLog = logger.Logs.FirstOrDefault(l => l.Message.Contains("Behaviour: handling request"));
		Assert.NotNull(entryLog);
		Assert.Equal(LogLevel.Debug, entryLog.Level);
	}

	[Fact]
	public async Task HandleAsync_LogsRequestExit_WhenNextCompletes()
	{
		var logger = new TestLogger<LoggingRequestPipelineBehaviour>();
		var behaviour = new LoggingRequestPipelineBehaviour(logger);
		var request = new TestRequest();

		await behaviour.HandleAsync(request, default, _ => Task.FromResult((object?)"result"));

		var exitLog = logger.Logs.FirstOrDefault(l => l.Message.Contains("Behaviour: request completed"));
		Assert.NotNull(exitLog);
		Assert.Equal(LogLevel.Debug, exitLog.Level);
	}

	[Fact]
	public async Task HandleAsync_LogsException_WhenNextThrows()
	{
		var logger = new TestLogger<LoggingRequestPipelineBehaviour>();
		var behaviour = new LoggingRequestPipelineBehaviour(logger);
		var request = new TestRequest();
		var ex = new InvalidOperationException("test error");

		await Assert.ThrowsAsync<InvalidOperationException>(async () =>
			await behaviour.HandleAsync(request, default, _ => throw ex)
		);

		var errorLog = logger.Logs.FirstOrDefault(l => l.Level == LogLevel.Error);
		Assert.NotNull(errorLog);
		Assert.Contains("Behaviour: request failed", errorLog.Message);
	}

	[Fact]
	public async Task HandleAsync_ThrowsArgumentNullException_WhenRequestIsNull()
	{
		var logger = new TestLogger<LoggingRequestPipelineBehaviour>();
		var behaviour = new LoggingRequestPipelineBehaviour(logger);

		await Assert.ThrowsAsync<ArgumentNullException>(async () =>
			await behaviour.HandleAsync(null!, default, _ => Task.FromResult((object?)"result"))
		);
	}

	[Fact]
	public async Task HandleAsync_ThrowsArgumentNullException_WhenNextIsNull()
	{
		var logger = new TestLogger<LoggingRequestPipelineBehaviour>();
		var behaviour = new LoggingRequestPipelineBehaviour(logger);
		var request = new TestRequest();

		await Assert.ThrowsAsync<ArgumentNullException>(async () =>
			await behaviour.HandleAsync(request, default, null!)
		);
	}

	[Fact]
	public void Ctor_ThrowsArgumentNullException_WhenLoggerIsNull()
	{
		Assert.Throws<ArgumentNullException>(() =>
			new LoggingRequestPipelineBehaviour(null!)
		);
	}

	private sealed class TestLogger<T> : ILogger<T>
	{
		private readonly List<LogEntry> _logs = new();

		public IReadOnlyList<LogEntry> Logs => _logs.AsReadOnly();

		public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

		public bool IsEnabled(LogLevel logLevel) => true;

		public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
		{
			var message = formatter(state, exception);
			_logs.Add(new LogEntry(logLevel, message, exception));
		}
	}

	public sealed record LogEntry(LogLevel Level, string Message, Exception? Exception);
}
