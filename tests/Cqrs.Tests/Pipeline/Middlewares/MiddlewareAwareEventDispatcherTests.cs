namespace KatzuoOgust.Cqrs.Pipeline.Middlewares;

public sealed class MiddlewareAwareEventDispatcherTests
{
	// -----------------------------------------------------------------------
	// Fixtures
	// -----------------------------------------------------------------------

	private sealed record OrderPlacedEvent(int OrderId) : IEvent;

	private sealed class TrackingHandler : IEventHandler<OrderPlacedEvent>
	{
		public List<int> Received { get; } = [];
		public Task HandleAsync(OrderPlacedEvent @event, CancellationToken ct = default)
		{
			Received.Add(@event.OrderId);
			return Task.CompletedTask;
		}
	}

	private sealed class LoggingMiddleware(List<string> log, string name)
		: IEventMiddleware<OrderPlacedEvent>
	{
		public async Task HandleAsync(OrderPlacedEvent @event, CancellationToken ct, Func<CancellationToken, Task> next)
		{
			log.Add($"{name}:before");
			await next(ct);
			log.Add($"{name}:after");
		}
	}

	private sealed class SimpleServiceProvider : IServiceProvider
	{
		private readonly Dictionary<Type, object> _services = [];
		public void Register<T>(T impl) where T : class => _services[typeof(T)] = impl;
		public object? GetService(Type t) => _services.GetValueOrDefault(t);
	}

	private static IEventDispatcher MakeDispatcher(IServiceProvider sp) => new EventDispatcher(sp);

	// -----------------------------------------------------------------------
	// Tests
	// -----------------------------------------------------------------------

	[Fact]
	public async Task DispatchAsync_PassesThroughToInner_WhenNoMiddlewares()
	{
		var handler = new TrackingHandler();
		var sp = new SimpleServiceProvider();
		sp.Register<IEnumerable<IEventHandler<OrderPlacedEvent>>>([handler]);

		await new MiddlewareAwareEventDispatcher(MakeDispatcher(sp), sp).DispatchAsync(new OrderPlacedEvent(1));

		Assert.Equal([1], handler.Received);
	}

	[Fact]
	public async Task DispatchAsync_WrapsHandler_WhenSingleMiddleware()
	{
		var log = new List<string>();
		var handler = new TrackingHandler();
		var sp = new SimpleServiceProvider();
		sp.Register<IEnumerable<IEventHandler<OrderPlacedEvent>>>([handler]);
		sp.Register<IEnumerable<IEventMiddleware<OrderPlacedEvent>>>([new LoggingMiddleware(log, "m")]);

		await new MiddlewareAwareEventDispatcher(MakeDispatcher(sp), sp).DispatchAsync(new OrderPlacedEvent(42));

		Assert.Equal([42], handler.Received);
		Assert.Equal(["m:before", "m:after"], log);
	}

	[Fact]
	public async Task DispatchAsync_ChainsOutermostFirst_WhenMultipleMiddlewares()
	{
		var log = new List<string>();
		var sp = new SimpleServiceProvider();
		sp.Register<IEnumerable<IEventHandler<OrderPlacedEvent>>>([new TrackingHandler()]);
		sp.Register<IEnumerable<IEventMiddleware<OrderPlacedEvent>>>([
			new LoggingMiddleware(log, "m0"),
			new LoggingMiddleware(log, "m1"),
		]);

		await new MiddlewareAwareEventDispatcher(MakeDispatcher(sp), sp).DispatchAsync(new OrderPlacedEvent(1));

		Assert.Equal(["m0:before", "m1:before", "m1:after", "m0:after"], log);
	}

	[Fact]
	public async Task DispatchAsync_ThrowsArgumentNullException_WhenEventIsNull()
	{
		await Assert.ThrowsAsync<ArgumentNullException>(
			() => new MiddlewareAwareEventDispatcher(MakeDispatcher(new SimpleServiceProvider()), new SimpleServiceProvider())
				.DispatchAsync<OrderPlacedEvent>(null!));
	}

	[Fact]
	public async Task DispatchAsync_UsesCachedInvoker_WhenCalledMultipleTimes()
	{
		var handler = new TrackingHandler();
		var sp = new SimpleServiceProvider();
		sp.Register<IEnumerable<IEventHandler<OrderPlacedEvent>>>([handler]);
		var dispatcher = new MiddlewareAwareEventDispatcher(MakeDispatcher(sp), sp);

		await dispatcher.DispatchAsync(new OrderPlacedEvent(1));
		await dispatcher.DispatchAsync(new OrderPlacedEvent(2));

		Assert.Equal([1, 2], handler.Received);
	}
}
