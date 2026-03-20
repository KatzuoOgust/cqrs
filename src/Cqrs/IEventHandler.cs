namespace KatzuoOgust.Cqrs;

/// <summary>Handles a domain event of type <typeparamref name="TEvent"/>.</summary>
public interface IEventHandler<in TEvent>
	where TEvent : IEvent
{
	public Task HandleAsync(TEvent @event, CancellationToken cancellationToken = default);
}
