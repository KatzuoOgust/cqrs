namespace KatzuoOgust.Cqrs.Pipeline.Behaviours;

/// <summary>
/// Decorates an <see cref="IEventDispatcher"/> so that every dispatched event passes through all registered
/// <see cref="IEventPipelineBehaviour"/> instances before reaching the handlers.
/// </summary>
public sealed class BehaviourAwareEventDispatcher(IEventDispatcher inner, IServiceProvider serviceProvider) : IEventDispatcher
{
	public async Task DispatchAsync<TEvent>(TEvent @event, CancellationToken cancellationToken = default)
		where TEvent : IEvent
	{
		ArgumentNullException.ThrowIfNull(@event);

		var behaviours = ((IEnumerable<IEventPipelineBehaviour>?)
			serviceProvider.GetService(typeof(IEnumerable<IEventPipelineBehaviour>)) ?? [])
			.ToArray();

		Func<CancellationToken, Task> terminal = c => inner.DispatchAsync(@event, c);

		for (var i = behaviours.Length - 1; i >= 0; i--)
		{
			var behaviour = behaviours[i];
			var next = terminal;
			terminal = c => behaviour.HandleAsync(@event, c, next);
		}

		await terminal(cancellationToken).ConfigureAwait(false);
	}
}
