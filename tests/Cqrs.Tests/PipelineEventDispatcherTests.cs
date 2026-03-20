using KatzuoOgust.Cqrs;
using KatzuoOgust.Cqrs.Middlewares;
using KatzuoOgust.Cqrs.Pipelines;

namespace KatzuoOgust.Cqrs;

public sealed class PipelineEventDispatcherTests
{
	private sealed record OrderShippedEvent(int OrderId) : IEvent;

	private sealed class TrackingHandler : IEventHandler<OrderShippedEvent>
	{
		public List<int> Received { get; } = [];
		public Task HandleAsync(OrderShippedEvent @event, CancellationToken ct = default)
		{
			Received.Add(@event.OrderId);
			return Task.CompletedTask;
		}
	}

	private sealed class LoggingBehaviour(List<string> log, string name) : IEventPipelineBehaviour
	{
		public async Task HandleAsync(IEvent @event, CancellationToken ct, Func<CancellationToken, Task> next)
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

	[Fact]
	public async Task PublishAsync_NoBehaviours_PassesThroughToHandler()
	{
		var handler = new TrackingHandler();
		var sp = new SimpleServiceProvider();
		sp.Register<IEnumerable<IEventHandler<OrderShippedEvent>>>([handler]);

		await new PipelineEventDispatcher(MakeBus(sp), sp).PublishAsync(new OrderShippedEvent(1));

		Assert.Equal([1], handler.Received);
	}

	[Fact]
	public async Task PublishAsync_SingleBehaviour_WrapsPublication()
	{
		var log = new List<string>();
		var handler = new TrackingHandler();
		var sp = new SimpleServiceProvider();
		sp.Register<IEnumerable<IEventHandler<OrderShippedEvent>>>([handler]);
		sp.Register<IEnumerable<IEventPipelineBehaviour>>([new LoggingBehaviour(log, "b")]);

		await new PipelineEventDispatcher(MakeBus(sp), sp).PublishAsync(new OrderShippedEvent(42));

		Assert.Equal([42], handler.Received);
		Assert.Equal(["b:before", "b:after"], log);
	}

	[Fact]
	public async Task PublishAsync_MultipleBehaviours_ChainedOutermostFirst()
	{
		var log = new List<string>();
		var sp = new SimpleServiceProvider();
		sp.Register<IEnumerable<IEventHandler<OrderShippedEvent>>>([new TrackingHandler()]);
		sp.Register<IEnumerable<IEventPipelineBehaviour>>([
			new LoggingBehaviour(log, "b0"),
			new LoggingBehaviour(log, "b1"),
		]);

		await new PipelineEventDispatcher(MakeBus(sp), sp).PublishAsync(new OrderShippedEvent(1));

		Assert.Equal(["b0:before", "b1:before", "b1:after", "b0:after"], log);
	}

	[Fact]
	public async Task PublishAsync_BehaviourReceivesEvent()
	{
		IEvent? captured = null;
		var sp = new SimpleServiceProvider();
		sp.Register<IEnumerable<IEventHandler<OrderShippedEvent>>>([new TrackingHandler()]);
		sp.Register<IEnumerable<IEventPipelineBehaviour>>([new CapturingBehaviour(e => captured = e)]);

		var evt = new OrderShippedEvent(7);
		await new PipelineEventDispatcher(MakeBus(sp), sp).PublishAsync(evt);

		Assert.Same(evt, captured);
	}

	[Fact]
	public async Task PublishAsync_NullEvent_ThrowsArgumentNullException()
	{
		await Assert.ThrowsAsync<ArgumentNullException>(
			() => new PipelineEventDispatcher(MakeBus(new SimpleServiceProvider()), new SimpleServiceProvider())
				.PublishAsync<OrderShippedEvent>(null!));
	}

	private sealed class CapturingBehaviour(Action<IEvent> capture) : IEventPipelineBehaviour
	{
		public async Task HandleAsync(IEvent @event, CancellationToken ct, Func<CancellationToken, Task> next)
		{
			capture(@event);
			await next(ct);
		}
	}
}
