using KatzuoOgust.Cqrs;

namespace KatzuoOgust.Cqrs.Pipelines;

/// <summary>
/// Decorates an <see cref="IEventBus"/> so that every published event passes through all registered
/// <see cref="IEventPipelineBehaviour"/> instances before reaching the handlers.
/// </summary>
public sealed class PipelineEventDispatcher(IEventBus inner, IServiceProvider serviceProvider) : IEventBus
{
	public async Task PublishAsync<TEvent>(TEvent @event, CancellationToken cancellationToken = default)
		where TEvent : IEvent
	{
		ArgumentNullException.ThrowIfNull(@event);

		var behaviours = ((IEnumerable<IEventPipelineBehaviour>?)
			serviceProvider.GetService(typeof(IEnumerable<IEventPipelineBehaviour>)) ?? [])
			.ToArray();

		Func<CancellationToken, Task> terminal = c => inner.PublishAsync(@event, c);

		for (var i = behaviours.Length - 1; i >= 0; i--)
		{
			var behaviour = behaviours[i];
			var next = terminal;
			terminal = c => behaviour.HandleAsync(@event, c, next);
		}

		await terminal(cancellationToken).ConfigureAwait(false);
	}
}
