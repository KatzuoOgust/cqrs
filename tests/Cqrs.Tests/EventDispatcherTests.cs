using KatzuoOgust.Cqrs.DependencyInjection;

namespace KatzuoOgust.Cqrs;

public sealed partial class EventDispatcherTests
{
	#region IEventBus.PublishAsync

	[Fact]
	public async Task PublishAsync_InvokesHandler_WhenSingleHandlerRegistered()
	{
		var handler = new TrackingHandler();
		var sp = new SimpleServiceProvider();
		sp.Register<IEnumerable<IEventHandler<UserCreatedEvent>>>([handler]);
		var evt = new UserCreatedEvent("Alice");

		await new EventDispatcher(sp).PublishAsync(evt);

		Assert.Single(handler.Received);
		Assert.Equal(evt, handler.Received[0]);
	}

	[Fact]
	public async Task PublishAsync_InvokesAllHandlers_WhenMultipleHandlersRegistered()
	{
		var h1 = new TrackingHandler();
		var h2 = new TrackingHandler();
		var sp = new SimpleServiceProvider();
		sp.Register<IEnumerable<IEventHandler<UserCreatedEvent>>>([h1, h2]);

		await new EventDispatcher(sp).PublishAsync(new UserCreatedEvent("Bob"));

		Assert.Single(h1.Received);
		Assert.Single(h2.Received);
	}

	[Fact]
	public async Task PublishAsync_Completes_WhenNoHandlersRegistered()
	{
		// IEnumerable<IEventHandler<T>> not registered → treated as empty collection.
		await new EventDispatcher(new SimpleServiceProvider()).PublishAsync(new UserCreatedEvent("Carol"));
	}

	[Fact]
	public async Task PublishAsync_ForwardsCancellationToken()
	{
		var handler = new TrackingHandler();
		var sp = new SimpleServiceProvider();
		sp.Register<IEnumerable<IEventHandler<UserCreatedEvent>>>([handler]);

		using var cts = new CancellationTokenSource();
		await new EventDispatcher(sp).PublishAsync(new UserCreatedEvent("Dave"), cts.Token);

		Assert.Equal(cts.Token, handler.LastToken);
	}

	[Fact]
	public async Task PublishAsync_ThrowsArgumentNullException_WhenEventIsNull()
	{
		await Assert.ThrowsAsync<ArgumentNullException>(
			() => new EventDispatcher(new SimpleServiceProvider()).PublishAsync<UserCreatedEvent>(null!));
	}

	[Fact]
	public void Ctor_ThrowsArgumentNullException_WhenServiceProviderIsNull()
	{
		Assert.Throws<ArgumentNullException>(() => new EventDispatcher(null!));
	}

	[Fact]
	public async Task PublishAsync_Completes_WhenCalledMultipleTimes()
	{
		var handler = new TrackingHandler();
		var sp = new SimpleServiceProvider();
		sp.Register<IEnumerable<IEventHandler<UserCreatedEvent>>>([handler]);
		var dispatcher = new EventDispatcher(sp);

		await dispatcher.PublishAsync(new UserCreatedEvent("Eve"));
		await dispatcher.PublishAsync(new UserCreatedEvent("Frank"));

		Assert.Equal(2, handler.Received.Count);
	}

	#endregion

	#region DispatchAsync

	[Fact]
	public async Task DispatchAsync_InvokesHandler_WhenSingleHandlerRegistered()
	{
		var handler = new TrackingHandler();
		var sp = new SimpleServiceProvider();
		sp.Register<IEnumerable<IEventHandler<UserCreatedEvent>>>([handler]);
		var evt = new UserCreatedEvent("Alice");

		await new EventDispatcher(sp).DispatchAsync(evt);

		Assert.Single(handler.Received);
		Assert.Equal(evt, handler.Received[0]);
	}

	[Fact]
	public async Task DispatchAsync_InvokesAllHandlers_WhenMultipleHandlersRegistered()
	{
		var h1 = new TrackingHandler();
		var h2 = new TrackingHandler();
		var sp = new SimpleServiceProvider();
		sp.Register<IEnumerable<IEventHandler<UserCreatedEvent>>>([h1, h2]);

		await new EventDispatcher(sp).DispatchAsync(new UserCreatedEvent("Bob"));

		Assert.Single(h1.Received);
		Assert.Single(h2.Received);
	}

	[Fact]
	public async Task DispatchAsync_ForwardsCancellationToken()
	{
		var handler = new TrackingHandler();
		var sp = new SimpleServiceProvider();
		sp.Register<IEnumerable<IEventHandler<UserCreatedEvent>>>([handler]);

		using var cts = new CancellationTokenSource();
		await new EventDispatcher(sp).DispatchAsync(new UserCreatedEvent("Dave"), cts.Token);

		Assert.Equal(cts.Token, handler.LastToken);
	}

	[Fact]
	public async Task DispatchAsync_ThrowsArgumentNullException_WhenEventIsNull()
	{
		await Assert.ThrowsAsync<ArgumentNullException>(
			() => new EventDispatcher(new SimpleServiceProvider()).DispatchAsync<UserCreatedEvent>(null!));
	}

	[Fact]
	public void EventDispatcher_ImplementsIEventDispatcher()
	{
		Assert.IsAssignableFrom<IEventDispatcher>(new EventDispatcher(new SimpleServiceProvider()));
	}

	#endregion
}
