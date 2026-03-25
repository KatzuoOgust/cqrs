using System.Collections.Concurrent;
using System.Linq.Expressions;

namespace KatzuoOgust.Cqrs.Pipeline.Middlewares;

/// <summary>
/// Wraps an <see cref="IEventDispatcher"/> so that every dispatched event runs through the registered
/// <see cref="IEventMiddleware{TEvent}"/> chain before reaching the actual handlers.
/// </summary>
/// <param name="inner">The decorated <see cref="IEventDispatcher"/> that performs the actual dispatch.</param>
/// <param name="serviceProvider">
/// The service provider used to resolve <see cref="IEventMiddleware{TEvent}"/> registrations.
/// </param>
public sealed class MiddlewareAwareEventDispatcher(IEventDispatcher inner, IServiceProvider serviceProvider) : IEventDispatcher
{
	private delegate Task EventInvoker(IServiceProvider sp, IEventDispatcher inner, object evt, CancellationToken ct);

	private static readonly ConcurrentDictionary<Type, EventInvoker> _cache = new();

	/// <inheritdoc/>
	public Task DispatchAsync<TEvent>(TEvent @event, CancellationToken cancellationToken = default)
		where TEvent : IEvent
	{
		ArgumentNullException.ThrowIfNull(@event);

		return _cache.GetOrAdd(@event.GetType(), BuildInvoker)
			.Invoke(serviceProvider, inner, @event, cancellationToken);
	}

	private static EventInvoker BuildInvoker(Type eventType)
	{
		var invokerType = typeof(Invoker<>).MakeGenericType(eventType);

		var sp = Expression.Parameter(typeof(IServiceProvider), "sp");
		var bus = Expression.Parameter(typeof(IEventDispatcher), "bus");
		var evt = Expression.Parameter(typeof(object), "evt");
		var ct = Expression.Parameter(typeof(CancellationToken), "ct");

		var body = Expression.Call(
			invokerType.GetMethod("InvokeAsync")!,
			sp, bus, Expression.Convert(evt, eventType), ct);

		return Expression
			.Lambda<EventInvoker>(body, sp, bus, evt, ct)
			.Compile();
	}

	private static class Invoker<TEvent>
		where TEvent : IEvent
	{
		public static async Task InvokeAsync(IServiceProvider sp, IEventDispatcher inner, TEvent @event, CancellationToken ct)
		{
			var middlewares = ((IEnumerable<IEventMiddleware<TEvent>>?)
				sp.GetService(typeof(IEnumerable<IEventMiddleware<TEvent>>)) ?? [])
				.ToArray();

			EventMiddlewareDelegate terminal = c => inner.DispatchAsync(@event, c);

			for (var i = middlewares.Length - 1; i >= 0; i--)
			{
				var mw = middlewares[i];
				var next = terminal;
				terminal = c => mw.HandleAsync(@event, c, next);
			}

			await terminal(ct).ConfigureAwait(false);
		}
	}
}
