namespace KatzuoOgust.Cqrs;

public sealed partial class EventDispatcherTests
{
	private sealed record UserCreatedEvent(string Name) : IEvent;

	private sealed class TrackingHandler : IEventHandler<UserCreatedEvent>
	{
		public List<UserCreatedEvent> Received { get; } = [];
		public CancellationToken LastToken { get; private set; }

		public Task HandleAsync(UserCreatedEvent @event, CancellationToken cancellationToken = default)
		{
			Received.Add(@event);
			LastToken = cancellationToken;
			return Task.CompletedTask;
		}
	}
}
