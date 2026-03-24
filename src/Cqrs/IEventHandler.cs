namespace KatzuoOgust.Cqrs;

/// <summary>Handles a domain event of type <typeparamref name="TEvent"/>.</summary>
public interface IEventHandler<in TEvent>
	where TEvent : IEvent
{
	/// <summary>Handles <paramref name="event"/> asynchronously.</summary>
	/// <param name="event">The event to handle.</param>
	/// <param name="cancellationToken">A token to cancel the operation.</param>
	public Task HandleAsync(TEvent @event, CancellationToken cancellationToken = default);
}
