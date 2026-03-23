namespace KatzuoOgust.Cqrs.Pipeline.Middlewares;

public sealed partial class MiddlewareAwareEventDispatcherTests
{
	private sealed record OrderPlacedEvent(int OrderId) : IEvent;

	private sealed class TrackingHandler : IEventHandler<OrderPlacedEvent>
	{
		public List<int> Received { get; } = [];
		public Task HandleAsync(OrderPlacedEvent @event, CancellationToken ct = default)
		{
			Received.Add(@event.OrderId);
			return Task.CompletedTask;
		}
	}

	private sealed class LoggingMiddleware(List<string> log, string name)
		: IEventMiddleware<OrderPlacedEvent>
	{
		public async Task HandleAsync(OrderPlacedEvent @event, CancellationToken ct, Func<CancellationToken, Task> next)
		{
			log.Add($"{name}:before");
			await next(ct);
			log.Add($"{name}:after");
		}
	}
}
