namespace KatzuoOgust.Cqrs;

/// <summary>Publishes events to all registered handlers.</summary>
public interface IEventBus
{
	/// <summary>Publishes <paramref name="event"/> to all registered <see cref="IEventHandler{TEvent}"/> instances.</summary>
	/// <typeparam name="TEvent">The event type.</typeparam>
	/// <param name="event">The event to publish.</param>
	/// <param name="cancellationToken">A token to cancel the operation.</param>
	public Task PublishAsync<TEvent>(TEvent @event, CancellationToken cancellationToken = default)
		where TEvent : IEvent;
}
