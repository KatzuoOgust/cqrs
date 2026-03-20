using KatzuoOgust.Cqrs;
namespace KatzuoOgust.Cqrs.Pipelines;

/// <summary>
/// Decorates an <see cref="IDispatcher"/> so that every request passes through all registered
/// <see cref="IRequestPipelineBehaviour"/> instances before reaching the handler.
/// Behaviours are resolved once per <see cref="InvokeAsync{TResult}"/> call and are invoked
/// outermost-first. The actual dispatch result is captured via the terminal delegate.
/// </summary>
public sealed class PipelineDispatcher(IDispatcher inner, IServiceProvider serviceProvider) : IDispatcher
{
	public async Task<TResult> InvokeAsync<TResult>(
		IRequest<TResult> request,
		CancellationToken cancellationToken = default)
	{
		ArgumentNullException.ThrowIfNull(request);

		var behaviours = ((IEnumerable<IRequestPipelineBehaviour>?)
			serviceProvider.GetService(typeof(IEnumerable<IRequestPipelineBehaviour>)) ?? [])
			.ToArray();

		Func<CancellationToken, Task<object?>> terminal = async c => await inner.InvokeAsync(request, c);

		for (var i = behaviours.Length - 1; i >= 0; i--)
		{
			var behaviour = behaviours[i];
			var next = terminal;
			terminal = c => behaviour.HandleAsync(request, c, next);
		}

		return (TResult)(await terminal(cancellationToken).ConfigureAwait(false))!;
	}
}
