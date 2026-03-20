using KatzuoOgust.Cqrs;
using System.Collections.Concurrent;
using System.Linq.Expressions;

namespace KatzuoOgust.Cqrs.Middlewares;

/// <summary>
/// Wraps an <see cref="IDispatcher"/> so that every request runs through the registered
/// <see cref="IRequestMiddleware{TRequest,TResult}"/> chain before reaching the actual handler.
/// </summary>
public sealed class MiddlewareDispatcher(IDispatcher inner, IServiceProvider serviceProvider) : IDispatcher
{
	private static readonly ConcurrentDictionary<Type, Func<IServiceProvider, IDispatcher, object, CancellationToken, Task<object?>>>
		_cache = new();

	public async Task<TResult> InvokeAsync<TResult>(
		IRequest<TResult> request,
		CancellationToken cancellationToken = default)
	{
		ArgumentNullException.ThrowIfNull(request);

		var invoke = _cache.GetOrAdd(request.GetType(), BuildInvoker, typeof(TResult));

		return (TResult)(await invoke(serviceProvider, inner, request, cancellationToken))!;
	}

	private static Func<IServiceProvider, IDispatcher, object, CancellationToken, Task<object?>> BuildInvoker(
		Type requestType, Type resultType)
	{
		var invokerType = typeof(Invoker<,>).MakeGenericType(requestType, resultType);

		var sp   = Expression.Parameter(typeof(IServiceProvider), "sp");
		var disp = Expression.Parameter(typeof(IDispatcher), "disp");
		var req  = Expression.Parameter(typeof(object), "req");
		var ct   = Expression.Parameter(typeof(CancellationToken), "ct");

		var body = Expression.Call(
			invokerType.GetMethod("InvokeAsync")!,
			sp, disp, Expression.Convert(req, typeof(IRequest<>).MakeGenericType(resultType)), ct);

		return Expression
			.Lambda<Func<IServiceProvider, IDispatcher, object, CancellationToken, Task<object?>>>(body, sp, disp, req, ct)
			.Compile();
	}

	private static class Invoker<TRequest, TResult>
		where TRequest : IRequest<TResult>
	{
		public static async Task<object?> InvokeAsync(
			IServiceProvider sp, IDispatcher inner, IRequest<TResult> request, CancellationToken ct)
		{
			var middlewares = ((IEnumerable<IRequestMiddleware<TRequest, TResult>>?)
				sp.GetService(typeof(IEnumerable<IRequestMiddleware<TRequest, TResult>>)) ?? [])
				.ToArray();

			Func<CancellationToken, Task<TResult>> terminal = c => inner.InvokeAsync(request, c);

			for (var i = middlewares.Length - 1; i >= 0; i--)
			{
				var mw = middlewares[i];
				var next = terminal;
				terminal = c => mw.HandleAsync((TRequest)request, c, next);
			}

			return await terminal(ct).ConfigureAwait(false);
		}
	}
}
