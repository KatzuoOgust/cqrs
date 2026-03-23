namespace KatzuoOgust.Cqrs.Pipeline.Middlewares;

public sealed partial class MiddlewareAwareDispatcherTests
{
	private sealed record MultiplyQuery(int A, int B) : IQuery<int>;
	private sealed record LogCommand : ICommand;

	private sealed class AddHandler : ICommandHandler<AddCommand, int>
	{
		public Task<int> HandleAsync(AddCommand command, CancellationToken ct = default) =>
			Task.FromResult(command.A + command.B);
	}

	private sealed class MultiplyHandler : IQueryHandler<MultiplyQuery, int>
	{
		public Task<int> HandleAsync(MultiplyQuery query, CancellationToken ct = default) =>
			Task.FromResult(query.A * query.B);
	}

	private sealed class LogHandler : ICommandHandler<LogCommand>
	{
		public bool Invoked { get; private set; }
		public Task HandleAsync(LogCommand command, CancellationToken ct = default)
		{
			Invoked = true;
			return Task.CompletedTask;
		}
	}

	/// <summary>Records invocation order and multiplies the result by <paramref name="factor"/>.</summary>
	private sealed class MultiplierMiddleware(List<string> log, string name, int factor)
		: IRequestMiddleware<AddCommand, int>
	{
		public async Task<int> HandleAsync(AddCommand request, CancellationToken ct, Func<CancellationToken, Task<int>> next)
		{
			log.Add($"{name}:before");
			var result = await next(ct);
			log.Add($"{name}:after");
			return result * factor;
		}
	}

	private sealed class Doubling : IRequestMiddleware<MultiplyQuery, int>
	{
		public async Task<int> HandleAsync(MultiplyQuery request, CancellationToken ct, Func<CancellationToken, Task<int>> next) =>
			await next(ct) * 2;
	}

	private sealed class VoidPipeline : IRequestMiddleware<LogCommand, Unit>
	{
		public bool Invoked { get; private set; }

		public async Task<Unit> HandleAsync(LogCommand request, CancellationToken ct, Func<CancellationToken, Task<Unit>> next)
		{
			Invoked = true;
			return await next(ct);
		}
	}
}
