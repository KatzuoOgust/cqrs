namespace KatzuoOgust.Cqrs.Pipeline.Behaviours;

public sealed partial class BehaviourAwareEventDispatcherTests
{
	private sealed record OrderShippedEvent(int OrderId) : IEvent;

	private sealed class TrackingHandler : IEventHandler<OrderShippedEvent>
	{
		public List<int> Received { get; } = [];
		public Task HandleAsync(OrderShippedEvent @event, CancellationToken ct = default)
		{
			Received.Add(@event.OrderId);
			return Task.CompletedTask;
		}
	}

	private sealed class LoggingBehaviour(List<string> log, string name) : IEventPipelineBehaviour
	{
		public async Task HandleAsync(IEvent @event, CancellationToken ct, Func<CancellationToken, Task> next)
		{
			log.Add($"{name}:before");
			await next(ct);
			log.Add($"{name}:after");
		}
	}

	private sealed class CapturingBehaviour(Action<IEvent> capture) : IEventPipelineBehaviour
	{
		public async Task HandleAsync(IEvent @event, CancellationToken ct, Func<CancellationToken, Task> next)
		{
			capture(@event);
			await next(ct);
		}
	}
}
