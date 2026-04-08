using Microsoft.Extensions.Logging;
using Xunit;

namespace KatzuoOgust.Cqrs.Pipeline.Behaviours.Logging;

public sealed class LoggingEventPipelineBehaviourTests
{
	private sealed record TestEvent : IEvent;

	[Fact]
	public async Task HandleAsync_Completes_WhenNextCompletes()
	{
		var logger = new TestLogger<LoggingEventPipelineBehaviour>();
		var behaviour = new LoggingEventPipelineBehaviour(logger);
		var @event = new TestEvent();

		await behaviour.HandleAsync(@event, default, _ => Task.CompletedTask);

		Assert.NotEmpty(logger.Logs);
	}

	[Fact]
	public async Task HandleAsync_LogsEventEntry_WhenCalled()
	{
		var logger = new TestLogger<LoggingEventPipelineBehaviour>();
		var behaviour = new LoggingEventPipelineBehaviour(logger);
		var @event = new TestEvent();

		await behaviour.HandleAsync(@event, default, _ => Task.CompletedTask);

		var entryLog = logger.Logs.FirstOrDefault(l => l.Message.Contains("Behaviour: handling event"));
		Assert.NotNull(entryLog);
		Assert.Equal(LogLevel.Debug, entryLog.Level);
	}

	[Fact]
	public async Task HandleAsync_LogsEventExit_WhenNextCompletes()
	{
		var logger = new TestLogger<LoggingEventPipelineBehaviour>();
		var behaviour = new LoggingEventPipelineBehaviour(logger);
		var @event = new TestEvent();

		await behaviour.HandleAsync(@event, default, _ => Task.CompletedTask);

		var exitLog = logger.Logs.FirstOrDefault(l => l.Message.Contains("Behaviour: event completed"));
		Assert.NotNull(exitLog);
		Assert.Equal(LogLevel.Debug, exitLog.Level);
	}

	[Fact]
	public async Task HandleAsync_LogsException_WhenNextThrows()
	{
		var logger = new TestLogger<LoggingEventPipelineBehaviour>();
		var behaviour = new LoggingEventPipelineBehaviour(logger);
		var @event = new TestEvent();
		var ex = new InvalidOperationException("test error");

		await Assert.ThrowsAsync<InvalidOperationException>(async () =>
			await behaviour.HandleAsync(@event, default, _ => throw ex)
		);

		var errorLog = logger.Logs.FirstOrDefault(l => l.Level == LogLevel.Error);
		Assert.NotNull(errorLog);
		Assert.Contains("Behaviour: event failed", errorLog.Message);
	}

	[Fact]
	public async Task HandleAsync_ThrowsArgumentNullException_WhenEventIsNull()
	{
		var logger = new TestLogger<LoggingEventPipelineBehaviour>();
		var behaviour = new LoggingEventPipelineBehaviour(logger);

		await Assert.ThrowsAsync<ArgumentNullException>(async () =>
			await behaviour.HandleAsync(null!, default, _ => Task.CompletedTask)
		);
	}

	[Fact]
	public async Task HandleAsync_ThrowsArgumentNullException_WhenNextIsNull()
	{
		var logger = new TestLogger<LoggingEventPipelineBehaviour>();
		var behaviour = new LoggingEventPipelineBehaviour(logger);
		var @event = new TestEvent();

		await Assert.ThrowsAsync<ArgumentNullException>(async () =>
			await behaviour.HandleAsync(@event, default, null!)
		);
	}

	[Fact]
	public void Ctor_ThrowsArgumentNullException_WhenLoggerIsNull()
	{
		Assert.Throws<ArgumentNullException>(() =>
			new LoggingEventPipelineBehaviour(null!)
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
