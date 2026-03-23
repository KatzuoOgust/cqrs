namespace KatzuoOgust.Cqrs.Pipeline.Behaviours;

public sealed partial class BehaviourAwareEventDispatcherTests
{
	[Fact]
	public async Task DispatchAsync_PassesThroughToHandler_WhenNoBehaviours()
	{
		var handler = new TrackingHandler();
		var sp = new SimpleServiceProvider();
		sp.Register<IEnumerable<IEventHandler<OrderShippedEvent>>>([handler]);

		await new BehaviourAwareEventDispatcher(MakeDispatcher(sp), sp).DispatchAsync(new OrderShippedEvent(1));

		Assert.Equal([1], handler.Received);
	}

	[Fact]
	public async Task DispatchAsync_WrapsDispatch_WhenSingleBehaviour()
	{
		var log = new List<string>();
		var handler = new TrackingHandler();
		var sp = new SimpleServiceProvider();
		sp.Register<IEnumerable<IEventHandler<OrderShippedEvent>>>([handler]);
		sp.Register<IEnumerable<IEventPipelineBehaviour>>([new LoggingBehaviour(log, "b")]);

		await new BehaviourAwareEventDispatcher(MakeDispatcher(sp), sp).DispatchAsync(new OrderShippedEvent(42));

		Assert.Equal([42], handler.Received);
		Assert.Equal(["b:before", "b:after"], log);
	}

	[Fact]
	public async Task DispatchAsync_ChainsOutermostFirst_WhenMultipleBehaviours()
	{
		var log = new List<string>();
		var sp = new SimpleServiceProvider();
		sp.Register<IEnumerable<IEventHandler<OrderShippedEvent>>>([new TrackingHandler()]);
		sp.Register<IEnumerable<IEventPipelineBehaviour>>([
			new LoggingBehaviour(log, "b0"),
			new LoggingBehaviour(log, "b1"),
		]);

		await new BehaviourAwareEventDispatcher(MakeDispatcher(sp), sp).DispatchAsync(new OrderShippedEvent(1));

		Assert.Equal(["b0:before", "b1:before", "b1:after", "b0:after"], log);
	}

	[Fact]
	public async Task DispatchAsync_PassesEventToBehaviour()
	{
		IEvent? captured = null;
		var sp = new SimpleServiceProvider();
		sp.Register<IEnumerable<IEventHandler<OrderShippedEvent>>>([new TrackingHandler()]);
		sp.Register<IEnumerable<IEventPipelineBehaviour>>([new CapturingBehaviour(e => captured = e)]);

		var evt = new OrderShippedEvent(7);
		await new BehaviourAwareEventDispatcher(MakeDispatcher(sp), sp).DispatchAsync(evt);

		Assert.Same(evt, captured);
	}

	[Fact]
	public async Task DispatchAsync_ThrowsArgumentNullException_WhenEventIsNull()
	{
		await Assert.ThrowsAsync<ArgumentNullException>(
			() => new BehaviourAwareEventDispatcher(MakeDispatcher(new SimpleServiceProvider()), new SimpleServiceProvider())
				.DispatchAsync<OrderShippedEvent>(null!));
	}

	private static IEventDispatcher MakeDispatcher(IServiceProvider sp) => new EventDispatcher(sp);
}
