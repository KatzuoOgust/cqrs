namespace KatzuoOgust.Cqrs;

public sealed partial class DispatcherTests
{
	private sealed record EchoCommand(string Text) : ICommand<string>;

	private sealed record LengthQuery(string Text) : IQuery<int>;

	private sealed class EchoHandler : ICommandHandler<EchoCommand, string>
	{
		public Task<string> HandleAsync(EchoCommand command, CancellationToken cancellationToken = default) =>
			Task.FromResult(command.Text);
	}

	private sealed class LengthHandler : IQueryHandler<LengthQuery, int>
	{
		public Task<int> HandleAsync(LengthQuery query, CancellationToken cancellationToken = default) =>
			Task.FromResult(query.Text.Length);
	}

	private sealed class CapturingHandler(Action<CancellationToken> capture) : ICommandHandler<PingCommand>
	{
		public Task HandleAsync(PingCommand command, CancellationToken cancellationToken = default)
		{
			capture(cancellationToken);
			return Task.CompletedTask;
		}
	}
}
