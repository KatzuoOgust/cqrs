namespace KatzuoOgust.Cqrs;

/// <summary>Dispatches an event to its single registered handler.</summary>
public interface IEventDispatcher
{
	public Task DispatchAsync<TEvent>(TEvent @event, CancellationToken cancellationToken = default)
		where TEvent : IEvent;
}
