using KatzuoOgust.Cqrs;
using System.Collections.Concurrent;
using System.Linq.Expressions;

namespace KatzuoOgust.Cqrs.Middlewares;

/// <summary>
/// Wraps an <see cref="IEventBus"/> so that every published event runs through the registered
/// <see cref="IEventMiddleware{TEvent}"/> chain before reaching the actual handlers.
/// </summary>
public sealed class MiddlewareEventDispatcher(IEventBus inner, IServiceProvider serviceProvider) : IEventBus
{
	private static readonly ConcurrentDictionary<Type, Func<IServiceProvider, IEventBus, object, CancellationToken, Task>>
		_cache = new();

	public Task PublishAsync<TEvent>(TEvent @event, CancellationToken cancellationToken = default)
		where TEvent : IEvent
	{
		ArgumentNullException.ThrowIfNull(@event);

		return _cache.GetOrAdd(@event.GetType(), BuildInvoker)
			.Invoke(serviceProvider, inner, @event, cancellationToken);
	}

	private static Func<IServiceProvider, IEventBus, object, CancellationToken, Task> BuildInvoker(Type eventType)
	{
		var invokerType = typeof(Invoker<>).MakeGenericType(eventType);

		var sp  = Expression.Parameter(typeof(IServiceProvider), "sp");
		var bus = Expression.Parameter(typeof(IEventBus), "bus");
		var evt = Expression.Parameter(typeof(object), "evt");
		var ct  = Expression.Parameter(typeof(CancellationToken), "ct");

		var body = Expression.Call(
			invokerType.GetMethod("InvokeAsync")!,
			sp, bus, Expression.Convert(evt, eventType), ct);

		return Expression
			.Lambda<Func<IServiceProvider, IEventBus, object, CancellationToken, Task>>(body, sp, bus, evt, ct)
			.Compile();
	}

	private static class Invoker<TEvent>
		where TEvent : IEvent
	{
		public static async Task InvokeAsync(IServiceProvider sp, IEventBus inner, TEvent @event, CancellationToken ct)
		{
			var middlewares = ((IEnumerable<IEventMiddleware<TEvent>>?)
				sp.GetService(typeof(IEnumerable<IEventMiddleware<TEvent>>)) ?? [])
				.ToArray();

			Func<CancellationToken, Task> terminal = c => inner.PublishAsync(@event, c);

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
