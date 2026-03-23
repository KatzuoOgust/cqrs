using KatzuoOgust.Cqrs.DependencyInjection;
using KatzuoOgust.Cqrs.Pipeline.Middlewares;

namespace KatzuoOgust.Cqrs.Examples.Middlewares;

// ----- Domain -------------------------------------------------------

public record ShipOrderCommand(Guid OrderId, string Destination) : ICommand;

// ----- Handler ------------------------------------------------------

internal sealed class ShipOrderHandler : ICommandHandler<ShipOrderCommand>
{
	public Task HandleAsync(ShipOrderCommand command, CancellationToken ct = default)
	{
		Console.WriteLine($"    [Handler] Shipping {command.OrderId} → {command.Destination}");
		return Task.CompletedTask;
	}
}

// ----- Typed middlewares — bound to (ShipOrderCommand, Unit) --------
//
// IRequestMiddleware<TRequest, TResult> sees the exact request type and result type.
// Can short-circuit by returning a value without calling next.

internal sealed class ShipOrderLoggingMiddleware : IRequestMiddleware<ShipOrderCommand, Unit>
{
	public async Task<Unit> HandleAsync(
		ShipOrderCommand request,
		CancellationToken ct,
		Func<CancellationToken, Task<Unit>> next)
	{
		Console.WriteLine($"    [Middleware:Log] → ShipOrderCommand {request.OrderId}");
		var result = await next(ct);
		Console.WriteLine($"    [Middleware:Log] ← ShipOrderCommand done");
		return result;
	}
}

internal sealed class ShipOrderValidationMiddleware : IRequestMiddleware<ShipOrderCommand, Unit>
{
	public async Task<Unit> HandleAsync(
		ShipOrderCommand request,
		CancellationToken ct,
		Func<CancellationToken, Task<Unit>> next)
	{
		if (string.IsNullOrWhiteSpace(request.Destination))
			throw new ArgumentException("Destination is required.");

		Console.WriteLine($"    [Middleware:Validation] Destination '{request.Destination}' — OK");
		return await next(ct);
	}
}

// ----- Example ------------------------------------------------------

internal static class MiddlewaresExample
{
	public static async Task RunAsync()
	{
		Console.WriteLine("=== Middlewares ===");

		// MiddlewareAwareDispatcher resolves IEnumerable<IRequestMiddleware<TRequest, TResult>>
		// from the service provider on every call. First registered → outermost in the chain.
		var sp = new SimpleServiceProvider()
			.Register<ICommandHandler<ShipOrderCommand>>(new ShipOrderHandler())
			.RegisterMany<IRequestMiddleware<ShipOrderCommand, Unit>>(new ShipOrderLoggingMiddleware())
			.RegisterMany<IRequestMiddleware<ShipOrderCommand, Unit>>(new ShipOrderValidationMiddleware());

		// Call chain for ShipOrderCommand (outermost first):
		//   ShipOrderLoggingMiddleware      ← registered 1st → outermost
		//   └─ ShipOrderValidationMiddleware ← registered 2nd
		//      └─ ShipOrderHandler           ← terminal

		IDispatcher dispatcher = new MiddlewareAwareDispatcher(new Dispatcher(sp), sp);

		Console.WriteLine("\n-- ShipOrderCommand --");
		await dispatcher.InvokeAsync(new ShipOrderCommand(Guid.NewGuid(), "Berlin"));

		Console.WriteLine();
	}
}
