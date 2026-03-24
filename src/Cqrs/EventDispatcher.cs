namespace KatzuoOgust.Cqrs;

/// <summary>
/// Default implementation of <see cref="IEventBus"/> and <see cref="IEventDispatcher"/>
/// that resolves handlers from an <see cref="IServiceProvider"/> and fans out to all of them.
/// </summary>
public sealed class EventDispatcher : IEventBus, IEventDispatcher
{
	private readonly IServiceProvider _serviceProvider;

	/// <summary>
	/// Initializes a new instance of <see cref="EventDispatcher"/>.
	/// </summary>
	/// <param name="serviceProvider">
	/// The service provider used to resolve <see cref="IEventHandler{TEvent}"/> registrations.
	/// </param>
	/// <exception cref="ArgumentNullException">Thrown when <paramref name="serviceProvider"/> is <see langword="null"/>.</exception>
	public EventDispatcher(IServiceProvider serviceProvider)
	{
		ArgumentNullException.ThrowIfNull(serviceProvider);
		_serviceProvider = serviceProvider;
	}

	/// <inheritdoc/>
	public Task PublishAsync<TEvent>(TEvent @event, CancellationToken cancellationToken = default)
		where TEvent : IEvent => DispatchAsync(@event, cancellationToken);

	/// <inheritdoc/>
	public async Task DispatchAsync<TEvent>(TEvent @event, CancellationToken cancellationToken = default)
		where TEvent : IEvent
	{
		ArgumentNullException.ThrowIfNull(@event);

		var handlers = (IEnumerable<IEventHandler<TEvent>>?)_serviceProvider.GetService(typeof(IEnumerable<IEventHandler<TEvent>>)) ?? [];

		foreach (var handler in handlers)
			await handler.HandleAsync(@event, cancellationToken).ConfigureAwait(false);
	}
}
