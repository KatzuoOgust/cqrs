using System.Collections.Concurrent;
using System.Linq.Expressions;
using System.Reflection;

namespace KatzuoOgust.Cqrs;

/// <summary>
/// Resolves and invokes the single registered handler for a request.
/// On first call per request type, <see cref="CreateRequestProcessor"/> compiles a
/// delegate via Expression trees and caches it in <see cref="_processorFactoryMap"/>.
/// Every subsequent dispatch is a dictionary lookup + direct delegate invocation.
/// </summary>
public sealed class Dispatcher : IDispatcher, ICommandQueue
{
	private readonly IServiceProvider _serviceProvider;

	// Request type → compiled (IServiceProvider, object, CancellationToken) → Task<object?>.
	// Populated lazily; each entry is a direct call to RequestProcessor<TRequest,TResult>.InvokeAsync.
	private static readonly ConcurrentDictionary<Type, Func<IServiceProvider, object, CancellationToken, Task<object?>>>
		_processorFactoryMap = new();

	public Dispatcher(IServiceProvider serviceProvider)
	{
		ArgumentNullException.ThrowIfNull(serviceProvider);
		_serviceProvider = serviceProvider;
	}

	public async Task<TResult> InvokeAsync<TResult>(
		IRequest<TResult> request,
		CancellationToken cancellationToken = default)
	{
		ArgumentNullException.ThrowIfNull(request);

		var invoke = _processorFactoryMap.GetOrAdd(request.GetType(), CreateRequestProcessor, typeof(TResult));

		return (TResult)(await invoke(_serviceProvider, request, cancellationToken))!;
	}

	public Task EnqueueAsync<TCommand>(TCommand command, CancellationToken cancellationToken = default)
		where TCommand : ICommand =>
		InvokeAsync(command, cancellationToken);

	#region Factory

	/// <summary>
	/// Creates a compiled delegate for the given request/result type pair.
	/// <see cref="RequestProcessor{TRequest,TResult}.InvokeAsync"/> is static, so the
	/// expression is a plain static call — no instance creation needed.
	/// </summary>
	private static Func<IServiceProvider, object, CancellationToken, Task<object?>> CreateRequestProcessor(
		Type requestType, Type resultType)
	{
		var processorType = typeof(RequestProcessor<,>).MakeGenericType(requestType, resultType);

		// (IServiceProvider sp, object req, CancellationToken ct)
		var spParam = Expression.Parameter(typeof(IServiceProvider), "sp");
		var reqParam = Expression.Parameter(typeof(object), "req");
		var ctParam = Expression.Parameter(typeof(CancellationToken), "ct");

		// RequestProcessor<TRequest, TResult>.InvokeAsync(sp, (IRequest<TResult>)req, ct)
		var invokeMethod = processorType.GetMethod("InvokeAsync")!;
		var castReq = Expression.Convert(reqParam, typeof(IRequest<>).MakeGenericType(resultType));
		var invokeCall = Expression.Call(invokeMethod, spParam, castReq, ctParam);

		// Box Task<TResult> → Task<object?> via BoxAsync<TResult>(…)
		var boxMethod = typeof(Dispatcher)
			.GetMethod(nameof(BoxAsync), BindingFlags.Static | BindingFlags.NonPublic)!
			.MakeGenericMethod(resultType);

		var body = Expression.Call(boxMethod, invokeCall);

		return Expression
			.Lambda<Func<IServiceProvider, object, CancellationToken, Task<object?>>>(body, spParam, reqParam, ctParam)
			.Compile();
	}

	private static async Task<object?> BoxAsync<T>(Task<T> task) =>
		await task.ConfigureAwait(false);

	#endregion

	#region RequestProcessor — static fields + static InvokeAsync; no instances needed

	private sealed class RequestProcessor<TRequest, TResult>
		where TRequest : IRequest<TResult>
	{
		// Resolved once per (TRequest, TResult) pair when the class is first used.
		private static readonly Type s_handlerType;
		private static readonly MethodInfo s_handleMethod;
		private static readonly bool s_isVoidCommand;

		static RequestProcessor()
		{
			s_isVoidCommand = typeof(ICommand).IsAssignableFrom(typeof(TRequest));
			var isQuery = !s_isVoidCommand && typeof(IQuery<TResult>).IsAssignableFrom(typeof(TRequest));

			s_handlerType = s_isVoidCommand
				? typeof(ICommandHandler<>).MakeGenericType(typeof(TRequest))
				: isQuery
					? typeof(IQueryHandler<,>).MakeGenericType(typeof(TRequest), typeof(TResult))
					: typeof(ICommandHandler<,>).MakeGenericType(typeof(TRequest), typeof(TResult));

			s_handleMethod = s_handlerType.GetMethod("HandleAsync")!;
		}

		public static Task<TResult> InvokeAsync(IServiceProvider sp, IRequest<TResult> request, CancellationToken ct)
		{
			var handler = sp.GetService(s_handlerType)
				?? throw new InvalidOperationException(
					$"No handler registered for '{typeof(TRequest).Name}'. " +
					$"Expected service: '{s_handlerType.Name}'.");

			var task = (Task)s_handleMethod.Invoke(handler, [(TRequest)request, ct])!;

			return s_isVoidCommand ? WrapAsync(task) : (Task<TResult>)task;
		}

		// ICommandHandler<T>.HandleAsync returns Task; lift to Task<Unit> (TResult = Unit by constraint).
		private static async Task<TResult> WrapAsync(Task task)
		{
			await task.ConfigureAwait(false);
			return (TResult)(object)Unit.Value;
		}
	}

	#endregion
}
