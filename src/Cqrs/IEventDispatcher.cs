namespace KatzuoOgust.Cqrs;

/// <summary>Dispatches an event to its single registered handler.</summary>
public interface IEventDispatcher
{
	/// <summary>Dispatches <paramref name="event"/> to its registered handler.</summary>
	/// <typeparam name="TEvent">The event type.</typeparam>
	/// <param name="event">The event to dispatch.</param>
	/// <param name="cancellationToken">A token to cancel the operation.</param>
	public Task DispatchAsync<TEvent>(TEvent @event, CancellationToken cancellationToken = default)
		where TEvent : IEvent;
}
