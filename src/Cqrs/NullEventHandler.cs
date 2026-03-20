namespace KatzuoOgust.Cqrs;

/// <summary>A no-op handler that discards any event.</summary>
public sealed class NullEventHandler<TEvent> : IEventHandler<TEvent>
	where TEvent : IEvent
{
	public static readonly NullEventHandler<TEvent> Instance = new();

	private NullEventHandler() { }

	public Task HandleAsync(TEvent @event, CancellationToken cancellationToken = default)
		=> Task.CompletedTask;
}
