using KatzuoOgust.Cqrs;
using KatzuoOgust.Cqrs.Pipeline.Behaviours;
using KatzuoOgust.Cqrs.Pipeline.Middlewares;

namespace KatzuoOgust.Cqrs.Examples;

// ----- Domain -------------------------------------------------------

public record CreateProductCommand(string Name, decimal Price) : ICommand;
public record GetProductQuery(Guid Id) : IQuery<ProductDto>;
public record ProductDto(Guid Id, string Name, decimal Price);

// ----- Handlers -----------------------------------------------------

internal sealed class CreateProductHandler : ICommandHandler<CreateProductCommand>
{
	public Task HandleAsync(CreateProductCommand command, CancellationToken ct = default)
	{
		Console.WriteLine($"    [Handler] Creating '{command.Name}' @ {command.Price:C}");
		return Task.CompletedTask;
	}
}

internal sealed class GetProductHandler : IQueryHandler<GetProductQuery, ProductDto>
{
	public Task<ProductDto> HandleAsync(GetProductQuery query, CancellationToken ct = default)
	{
		Console.WriteLine($"    [Handler] Getting product {query.Id}");
		return Task.FromResult(new ProductDto(query.Id, "Widget", 9.99m));
	}
}

// ----- Non-generic behaviours — apply to every request --------------
//
// IRequestPipelineBehaviour sees only IRequest (not the concrete type)
// and Task<object?> as the result. Use for truly cross-cutting concerns.

internal sealed class LoggingBehaviour : IRequestPipelineBehaviour
{
	public async Task<object?> HandleAsync(
		IRequest request,
		CancellationToken ct,
		Func<CancellationToken, Task<object?>> next)
	{
		Console.WriteLine($"    [Behaviour:Log] → {request.GetType().Name}");
		var result = await next(ct);
		Console.WriteLine($"    [Behaviour:Log] ← {request.GetType().Name}");
		return result;
	}
}

internal sealed class ValidationBehaviour : IRequestPipelineBehaviour
{
	public async Task<object?> HandleAsync(
		IRequest request,
		CancellationToken ct,
		Func<CancellationToken, Task<object?>> next)
	{
		Console.WriteLine($"    [Behaviour:Validation] Checking {request.GetType().Name}");
		return await next(ct);
	}
}

// ----- Typed middleware — CreateProductCommand only -----------------
//
// Sees the concrete request type and Unit result; applied inside behaviours.

internal sealed class ProductPriceMiddleware : IRequestMiddleware<CreateProductCommand, Unit>
{
	public async Task<Unit> HandleAsync(
		CreateProductCommand request,
		CancellationToken ct,
		Func<CancellationToken, Task<Unit>> next)
	{
		if (request.Price <= 0)
			throw new ArgumentException("Price must be positive.");

		Console.WriteLine($"    [Middleware] Price {request.Price:C} — OK");
		return await next(ct);
	}
}

// ----- Example ------------------------------------------------------

internal static class BehavioursExample
{
	public static async Task RunAsync()
	{
		Console.WriteLine("=== Behaviours ===");

		var sp = new SimpleServiceProvider()
			.Register<ICommandHandler<CreateProductCommand>>(new CreateProductHandler())
			.Register<IQueryHandler<GetProductQuery, ProductDto>>(new GetProductHandler())
			.RegisterMany<IRequestPipelineBehaviour>(new LoggingBehaviour())
			.RegisterMany<IRequestPipelineBehaviour>(new ValidationBehaviour())
			.RegisterMany<IRequestMiddleware<CreateProductCommand, Unit>>(new ProductPriceMiddleware());

		// Full pipeline — outermost first:
		//   LoggingBehaviour           (behaviour, registered 1st)
		//   └─ ValidationBehaviour     (behaviour, registered 2nd)
		//      └─ ProductPriceMiddleware  (middleware, CreateProductCommand only)
		//         └─ CreateProductHandler / GetProductHandler
		IDispatcher dispatcher =
			new BehaviourAwareDispatcher(
				new MiddlewareAwareDispatcher(
					new Dispatcher(sp),
					sp),
				sp);

		Console.WriteLine("\n-- CreateProductCommand (behaviours + middleware) --");
		await dispatcher.InvokeAsync(new CreateProductCommand("Widget", 9.99m));

		Console.WriteLine("\n-- GetProductQuery (behaviours only — middleware skipped, it's a query) --");
		var product = await dispatcher.InvokeAsync(new GetProductQuery(Guid.NewGuid()));
		Console.WriteLine($"    Result: {product}");

		Console.WriteLine();
	}
}
