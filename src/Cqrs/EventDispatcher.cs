namespace KatzuoOgust.Cqrs;

public sealed class EventDispatcher : IEventBus, IEventDispatcher
{
	private readonly IServiceProvider _serviceProvider;

	public EventDispatcher(IServiceProvider serviceProvider)
	{
		ArgumentNullException.ThrowIfNull(serviceProvider);
		_serviceProvider = serviceProvider;
	}

	public Task PublishAsync<TEvent>(TEvent @event, CancellationToken cancellationToken = default)
		where TEvent : IEvent => DispatchAsync(@event, cancellationToken);

	public async Task DispatchAsync<TEvent>(TEvent @event, CancellationToken cancellationToken = default)
		where TEvent : IEvent
	{
		ArgumentNullException.ThrowIfNull(@event);

		var handlers = (IEnumerable<IEventHandler<TEvent>>?)_serviceProvider.GetService(typeof(IEnumerable<IEventHandler<TEvent>>)) ?? [];

		foreach (var handler in handlers)
			await handler.HandleAsync(@event, cancellationToken).ConfigureAwait(false);
	}
}
