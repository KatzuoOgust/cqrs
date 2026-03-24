namespace KatzuoOgust.Cqrs;

/// <summary>A no-op handler that discards any event.</summary>
public sealed class NullEventHandler<TEvent> : IEventHandler<TEvent>
	where TEvent : IEvent
{
	/// <summary>The shared singleton instance of <see cref="NullEventHandler{TEvent}"/>.</summary>
	public static readonly NullEventHandler<TEvent> Instance = new();

	private NullEventHandler() { }

	/// <inheritdoc/>
	public Task HandleAsync(TEvent @event, CancellationToken cancellationToken = default)
		=> Task.CompletedTask;
}
