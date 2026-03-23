namespace KatzuoOgust.Cqrs.Pipeline.Behaviours;

public sealed partial class BehaviourAwareDispatcherTests
{
	private sealed class AddHandler : ICommandHandler<AddCommand, int>
	{
		public Task<int> HandleAsync(AddCommand command, CancellationToken ct = default) =>
			Task.FromResult(command.A + command.B);
	}

	private sealed class LoggingBehaviour(List<string> log, string name) : IRequestPipelineBehaviour
	{
		public async Task<object?> HandleAsync(IRequest request, CancellationToken ct, Func<CancellationToken, Task<object?>> next)
		{
			log.Add($"{name}:before");
			var result = await next(ct);
			log.Add($"{name}:after");
			return result;
		}
	}

	private sealed class CapturingBehaviour(Action<IRequest> capture) : IRequestPipelineBehaviour
	{
		public async Task<object?> HandleAsync(IRequest request, CancellationToken ct, Func<CancellationToken, Task<object?>> next)
		{
			capture(request);
			return await next(ct);
		}
	}
}
