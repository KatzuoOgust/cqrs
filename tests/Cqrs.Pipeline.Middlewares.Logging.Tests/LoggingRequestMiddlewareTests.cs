using Microsoft.Extensions.Logging;
using Xunit;

namespace KatzuoOgust.Cqrs.Pipeline.Middlewares.Logging;

public sealed class LoggingRequestMiddlewareTests
{
	private sealed record TestRequest(string Value) : IRequest<string>;

	[Fact]
	public async Task HandleAsync_ReturnsResponse_WhenNextCompletes()
	{
		var logger = new TestLogger<LoggingRequestMiddleware<TestRequest, string>>();
		var middleware = new LoggingRequestMiddleware<TestRequest, string>(logger);
		var request = new TestRequest("test");
		var response = "result";

		var result = await middleware.HandleAsync(request, default, _ => Task.FromResult(response));

		Assert.Equal(response, result);
		Assert.NotEmpty(logger.Logs);
	}

	[Fact]
	public async Task HandleAsync_LogsRequestEntry_WhenCalled()
	{
		var logger = new TestLogger<LoggingRequestMiddleware<TestRequest, string>>();
		var middleware = new LoggingRequestMiddleware<TestRequest, string>(logger);
		var request = new TestRequest("test");

		await middleware.HandleAsync(request, default, _ => Task.FromResult("result"));

		var entryLog = logger.Logs.FirstOrDefault(l => l.Message.Contains("Handling request"));
		Assert.NotNull(entryLog);
		Assert.Equal(LogLevel.Debug, entryLog.Level);
	}

	[Fact]
	public async Task HandleAsync_LogsRequestExit_WhenNextCompletes()
	{
		var logger = new TestLogger<LoggingRequestMiddleware<TestRequest, string>>();
		var middleware = new LoggingRequestMiddleware<TestRequest, string>(logger);
		var request = new TestRequest("test");

		await middleware.HandleAsync(request, default, _ => Task.FromResult("result"));

		var exitLog = logger.Logs.FirstOrDefault(l => l.Message.Contains("Request completed"));
		Assert.NotNull(exitLog);
		Assert.Equal(LogLevel.Debug, exitLog.Level);
	}

	[Fact]
	public async Task HandleAsync_LogsException_WhenNextThrows()
	{
		var logger = new TestLogger<LoggingRequestMiddleware<TestRequest, string>>();
		var middleware = new LoggingRequestMiddleware<TestRequest, string>(logger);
		var request = new TestRequest("test");
		var ex = new InvalidOperationException("test error");

		await Assert.ThrowsAsync<InvalidOperationException>(async () =>
			await middleware.HandleAsync(request, default, _ => throw ex)
		);

		var errorLog = logger.Logs.FirstOrDefault(l => l.Level == LogLevel.Error);
		Assert.NotNull(errorLog);
		Assert.Contains("Request failed", errorLog.Message);
	}

	[Fact]
	public async Task HandleAsync_ThrowsArgumentNullException_WhenRequestIsNull()
	{
		var logger = new TestLogger<LoggingRequestMiddleware<TestRequest, string>>();
		var middleware = new LoggingRequestMiddleware<TestRequest, string>(logger);

		await Assert.ThrowsAsync<ArgumentNullException>(async () =>
			await middleware.HandleAsync(null!, default, _ => Task.FromResult("result"))
		);
	}

	[Fact]
	public async Task HandleAsync_ThrowsArgumentNullException_WhenNextIsNull()
	{
		var logger = new TestLogger<LoggingRequestMiddleware<TestRequest, string>>();
		var middleware = new LoggingRequestMiddleware<TestRequest, string>(logger);
		var request = new TestRequest("test");

		await Assert.ThrowsAsync<ArgumentNullException>(async () =>
			await middleware.HandleAsync(request, default, null!)
		);
	}

	[Fact]
	public void Ctor_ThrowsArgumentNullException_WhenLoggerIsNull()
	{
		Assert.Throws<ArgumentNullException>(() =>
			new LoggingRequestMiddleware<TestRequest, string>(null!)
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
