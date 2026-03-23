// ReSharper disable CheckNamespace
namespace KatzuoOgust.Cqrs;

internal sealed record PingCommand : ICommand;

internal sealed class PingHandler : ICommandHandler<PingCommand>
{
	public bool Invoked { get; private set; }

	public Task HandleAsync(PingCommand command, CancellationToken cancellationToken = default)
	{
		Invoked = true;
		return Task.CompletedTask;
	}
}
