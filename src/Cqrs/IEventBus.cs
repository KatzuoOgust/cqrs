namespace KatzuoOgust.Cqrs;

/// <summary>Publishes events to all registered handlers.</summary>
public interface IEventBus
{
	public Task PublishAsync<TEvent>(TEvent @event, CancellationToken cancellationToken = default)
		where TEvent : IEvent;
}
