using KatzuoOgust.Cqrs.DependencyInjection;
using KatzuoOgust.Cqrs.DependencyInjection.Decoration;

namespace KatzuoOgust.Cqrs.Examples.Decorators;

#region Domain

public record PlaceOrderCommand(Guid OrderId, string Product) : ICommand;
public record GetOrderQuery(Guid OrderId) : IQuery<OrderDto>;
public record OrderDto(Guid OrderId, string Product);

#endregion

#region Handlers

internal sealed class PlaceOrderHandler : ICommandHandler<PlaceOrderCommand>
{
	public Task HandleAsync(PlaceOrderCommand command, CancellationToken ct = default)
	{
		Console.WriteLine($"    [Handler] Placing order {command.OrderId} — '{command.Product}'");
		return Task.CompletedTask;
	}
}

internal sealed class GetOrderHandler : IQueryHandler<GetOrderQuery, OrderDto>
{
	public Task<OrderDto> HandleAsync(GetOrderQuery query, CancellationToken ct = default)
	{
		Console.WriteLine($"    [Handler] Getting order {query.OrderId}");
		return Task.FromResult(new OrderDto(query.OrderId, "Widget"));
	}
}

#endregion

#region Exact decorator — PlaceOrderCommand only
//
// Wraps a specific closed ICommandHandler<PlaceOrderCommand>.
// Registered via Decorate<ICommandHandler<PlaceOrderCommand>>(factory).

internal sealed class PlaceOrderValidationDecorator(ICommandHandler<PlaceOrderCommand> inner)
	: ICommandHandler<PlaceOrderCommand>
{
	public Task HandleAsync(PlaceOrderCommand command, CancellationToken ct = default)
	{
		if (string.IsNullOrWhiteSpace(command.Product))
			throw new ArgumentException("Product is required.", nameof(command));

		Console.WriteLine($"    [Validation] '{command.Product}' — OK");
		return inner.HandleAsync(command, ct);
	}
}

#endregion

#region Open-generic decorator — every ICommandHandler<T>
//
// One class, registered once:  Decorate(typeof(ICommandHandler<>), typeof(LoggingCommandDecorator<>))
// The closed type is constructed automatically on first resolve of each TCommand.

internal sealed class LoggingCommandDecorator<TCommand>(ICommandHandler<TCommand> inner)
	: ICommandHandler<TCommand>
	where TCommand : ICommand
{
	public async Task HandleAsync(TCommand command, CancellationToken ct = default)
	{
		Console.WriteLine($"    [Log] → {typeof(TCommand).Name}");
		await inner.HandleAsync(command, ct);
		Console.WriteLine($"    [Log] ← {typeof(TCommand).Name}");
	}
}

#endregion

#region Example

internal static class DecoratorsExample
{
	public static async Task RunAsync()
	{
		Console.WriteLine("=== Decorators ===");

		// Raw handlers registered in the base provider.
		var raw = new SimpleServiceProvider()
			.Register<ICommandHandler<PlaceOrderCommand>>(new PlaceOrderHandler())
			.Register<IQueryHandler<GetOrderQuery, OrderDto>>(new GetOrderHandler());

		// DecoratingServiceProvider layers decorators over the base provider.
		var sp = new DecoratingServiceProvider(raw);

		// 1. Exact: PlaceOrderValidationDecorator wraps PlaceOrderHandler.
		sp.Decorate<ICommandHandler<PlaceOrderCommand>>(
			(inner, _) => new PlaceOrderValidationDecorator(inner));

		// 2. Open-generic: LoggingCommandDecorator<T> wraps every ICommandHandler<T>.
		//    Applied after (1), so it becomes the outermost layer.
		sp.Decorate(typeof(ICommandHandler<>), typeof(LoggingCommandDecorator<>));

		// Resulting call chain for PlaceOrderCommand (outermost first):
		//   LoggingCommandDecorator<PlaceOrderCommand>  ← registered 2nd → outermost
		//   └─ PlaceOrderValidationDecorator            ← registered 1st → middle
		//      └─ PlaceOrderHandler                     ← raw handler

		IDispatcher dispatcher = new Dispatcher(sp);

		Console.WriteLine("\n-- PlaceOrderCommand (exact + open-generic decorators) --");
		await dispatcher.InvokeAsync(new PlaceOrderCommand(Guid.NewGuid(), "Widget"));

		Console.WriteLine("\n-- GetOrderQuery (not a command — open-generic decorator not applied) --");
		var order = await dispatcher.InvokeAsync(new GetOrderQuery(Guid.NewGuid()));
		Console.WriteLine($"    Result: {order}");

		Console.WriteLine();
	}
}

#endregion
