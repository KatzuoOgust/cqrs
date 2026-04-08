using Microsoft.Extensions.Logging;
using Xunit;

namespace KatzuoOgust.Cqrs.Flow.Logging;

public sealed class LoggingFlowCommandHandlerTests
{
	private sealed record TestCommand : ICommand;

	private sealed class TestFlowHandler : IFlowCommandHandler<TestCommand>
	{
		private readonly IEnumerable<ICommand> _result;

		public TestFlowHandler(IEnumerable<ICommand> result) => _result = result;

		public Task<IEnumerable<ICommand>> ExecuteAsync(TestCommand command, CancellationToken cancellationToken = default) =>
			Task.FromResult(_result);
	}

	[Fact]
	public async Task ExecuteAsync_ReturnsCommands_WhenInnerCompletes()
	{
		var commands = new ICommand[] { new TestCommand() };
		var inner = new TestFlowHandler(commands);
		var logger = new TestLogger<LoggingFlowCommandHandler<TestCommand>>();
		var handler = new LoggingFlowCommandHandler<TestCommand>(inner, logger);

		var result = await handler.ExecuteAsync(new TestCommand());

		Assert.Equal(commands, result);
		Assert.NotEmpty(logger.Logs);
	}

	[Fact]
	public async Task ExecuteAsync_LogsFlowEntry_WhenCalled()
	{
		var inner = new TestFlowHandler(Array.Empty<ICommand>());
		var logger = new TestLogger<LoggingFlowCommandHandler<TestCommand>>();
		var handler = new LoggingFlowCommandHandler<TestCommand>(inner, logger);

		await handler.ExecuteAsync(new TestCommand());

		var entryLog = logger.Logs.FirstOrDefault(l => l.Message.Contains("Flow: executing command"));
		Assert.NotNull(entryLog);
		Assert.Equal(LogLevel.Debug, entryLog.Level);
	}

	[Fact]
	public async Task ExecuteAsync_LogsFlowExit_WhenInnerCompletes()
	{
		var commands = new ICommand[] { new TestCommand(), new TestCommand() };
		var inner = new TestFlowHandler(commands);
		var logger = new TestLogger<LoggingFlowCommandHandler<TestCommand>>();
		var handler = new LoggingFlowCommandHandler<TestCommand>(inner, logger);

		await handler.ExecuteAsync(new TestCommand());

		var exitLog = logger.Logs.FirstOrDefault(l => l.Message.Contains("Flow: command completed"));
		Assert.NotNull(exitLog);
		Assert.Equal(LogLevel.Debug, exitLog.Level);
		Assert.Contains("2 commands", exitLog.Message);
	}

	[Fact]
	public async Task ExecuteAsync_LogsException_WhenInnerThrows()
	{
		var ex = new InvalidOperationException("test error");
		var inner = new FailingFlowHandler(ex);
		var logger = new TestLogger<LoggingFlowCommandHandler<TestCommand>>();
		var handler = new LoggingFlowCommandHandler<TestCommand>(inner, logger);

		await Assert.ThrowsAsync<InvalidOperationException>(async () =>
			await handler.ExecuteAsync(new TestCommand())
		);

		var errorLog = logger.Logs.FirstOrDefault(l => l.Level == LogLevel.Error);
		Assert.NotNull(errorLog);
		Assert.Contains("Flow: command failed", errorLog.Message);
	}

	[Fact]
	public async Task ExecuteAsync_ThrowsArgumentNullException_WhenCommandIsNull()
	{
		var inner = new TestFlowHandler(Array.Empty<ICommand>());
		var logger = new TestLogger<LoggingFlowCommandHandler<TestCommand>>();
		var handler = new LoggingFlowCommandHandler<TestCommand>(inner, logger);

		await Assert.ThrowsAsync<ArgumentNullException>(async () =>
			await handler.ExecuteAsync(null!)
		);
	}

	[Fact]
	public void Ctor_ThrowsArgumentNullException_WhenInnerIsNull()
	{
		var logger = new TestLogger<LoggingFlowCommandHandler<TestCommand>>();

		Assert.Throws<ArgumentNullException>(() =>
			new LoggingFlowCommandHandler<TestCommand>(null!, logger)
		);
	}

	[Fact]
	public void Ctor_ThrowsArgumentNullException_WhenLoggerIsNull()
	{
		var inner = new TestFlowHandler(Array.Empty<ICommand>());

		Assert.Throws<ArgumentNullException>(() =>
			new LoggingFlowCommandHandler<TestCommand>(inner, null!)
		);
	}

	private sealed class FailingFlowHandler : IFlowCommandHandler<TestCommand>
	{
		private readonly Exception _ex;

		public FailingFlowHandler(Exception ex) => _ex = ex;

		public Task<IEnumerable<ICommand>> ExecuteAsync(TestCommand command, CancellationToken cancellationToken = default) =>
			throw _ex;
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
