using System.Collections.Concurrent;
using System.Linq.Expressions;
using System.Reflection;

namespace KatzuoOgust.Cqrs;

public sealed class EventDispatcher : IEventBus
{
	private readonly IServiceProvider _serviceProvider;

	public EventDispatcher(IServiceProvider serviceProvider)
	{
		ArgumentNullException.ThrowIfNull(serviceProvider);
		_serviceProvider = serviceProvider;
	}

	public async Task PublishAsync<TEvent>(TEvent @event, CancellationToken cancellationToken = default)
		where TEvent : IEvent
	{
		ArgumentNullException.ThrowIfNull(@event);

		var handlers = (IEnumerable<IEventHandler<TEvent>>?)_serviceProvider.GetService(typeof(IEnumerable<IEventHandler<TEvent>>)) ?? [];

		foreach (var handler in handlers)
			await handler.HandleAsync(@event, cancellationToken).ConfigureAwait(false);
	}
}
