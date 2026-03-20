using KatzuoOgust.Cqrs.Middlewares;
using KatzuoOgust.Cqrs.Pipelines;
namespace KatzuoOgust.Cqrs;

public sealed class MiddlewareEventDispatcherTests
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

	private static IEventBus MakeBus(IServiceProvider sp) => new EventDispatcher(sp);

	// -----------------------------------------------------------------------
	// Tests
	// -----------------------------------------------------------------------

	[Fact]
	public async Task PublishAsync_NoMiddlewares_PassesThroughToInner()
	{
		var handler = new TrackingHandler();
		var sp = new SimpleServiceProvider();
		sp.Register<IEnumerable<IEventHandler<OrderPlacedEvent>>>([handler]);

		await new MiddlewareEventDispatcher(MakeBus(sp), sp).PublishAsync(new OrderPlacedEvent(1));

		Assert.Equal([1], handler.Received);
	}

	[Fact]
	public async Task PublishAsync_SingleMiddleware_WrapsHandler()
	{
		var log = new List<string>();
		var handler = new TrackingHandler();
		var sp = new SimpleServiceProvider();
		sp.Register<IEnumerable<IEventHandler<OrderPlacedEvent>>>([handler]);
		sp.Register<IEnumerable<IEventMiddleware<OrderPlacedEvent>>>([new LoggingMiddleware(log, "m")]);

		await new MiddlewareEventDispatcher(MakeBus(sp), sp).PublishAsync(new OrderPlacedEvent(42));

		Assert.Equal([42], handler.Received);
		Assert.Equal(["m:before", "m:after"], log);
	}

	[Fact]
	public async Task PublishAsync_MultipleMiddlewares_ChainedOutermostFirst()
	{
		var log = new List<string>();
		var sp = new SimpleServiceProvider();
		sp.Register<IEnumerable<IEventHandler<OrderPlacedEvent>>>([new TrackingHandler()]);
		sp.Register<IEnumerable<IEventMiddleware<OrderPlacedEvent>>>([
			new LoggingMiddleware(log, "m0"),
			new LoggingMiddleware(log, "m1"),
		]);

		await new MiddlewareEventDispatcher(MakeBus(sp), sp).PublishAsync(new OrderPlacedEvent(1));

		Assert.Equal(["m0:before", "m1:before", "m1:after", "m0:after"], log);
	}

	[Fact]
	public async Task PublishAsync_NullEvent_ThrowsArgumentNullException()
	{
		await Assert.ThrowsAsync<ArgumentNullException>(
			() => new MiddlewareEventDispatcher(MakeBus(new SimpleServiceProvider()), new SimpleServiceProvider())
				.PublishAsync<OrderPlacedEvent>(null!));
	}

	[Fact]
	public async Task PublishAsync_CalledMultipleTimes_UsesCachedInvoker()
	{
		var handler = new TrackingHandler();
		var sp = new SimpleServiceProvider();
		sp.Register<IEnumerable<IEventHandler<OrderPlacedEvent>>>([handler]);
		var dispatcher = new MiddlewareEventDispatcher(MakeBus(sp), sp);

		await dispatcher.PublishAsync(new OrderPlacedEvent(1));
		await dispatcher.PublishAsync(new OrderPlacedEvent(2));

		Assert.Equal([1, 2], handler.Received);
	}
}
