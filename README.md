# Cqrs

![CI](https://github.com/KatzuoOgust/cqrs/actions/workflows/ci.yml/badge.svg)

Lightweight, framework-agnostic CQRS abstractions for .NET 10. Zero NuGet dependencies in the core library.

## Packages

| Package | Namespace | Description |
|---|---|---|
| `Cqrs` | `KatzuoOgust.Cqrs` | Core interfaces, null-object handlers, `Dispatcher`, `EventDispatcher` |
| `Cqrs.Pipeline.Middlewares` | `KatzuoOgust.Cqrs.Pipeline.Middlewares` | Typed per-request/event middleware with full result access |
| `Cqrs.Pipeline.Behaviours` | `KatzuoOgust.Cqrs.Pipeline.Behaviours` | Non-generic cross-cutting pipeline behaviours |
| `Cqrs.DependencyInjection` | `KatzuoOgust.Cqrs.DependencyInjection` | `IServiceProvider` decorator that layers exact and open-generic decorators |

## Core abstractions (`Cqrs`)

| Type | Description |
|---|---|
| `IRequest` / `IRequest<TResponse>` | Base marker for all requests |
| `ICommand` | Void command (returns `Unit`) |
| `ICommand<TResponse>` | Command that returns a value |
| `IQuery<TResponse>` | Read-only query |
| `IEvent` | Domain event marker |
| `ICommandHandler<TCommand>` | Handles a void command |
| `ICommandHandler<TCommand, TResponse>` | Handles a valued command |
| `IQueryHandler<TQuery, TResponse>` | Handles a query |
| `IEventHandler<TEvent>` | Handles a domain event |
| `IDispatcher` / `IDispatcherFactory` | Routes a request to its single handler |
| `ICommandQueue` | Accepts void commands for deferred or immediate processing |
| `IEventBus` / `IEventBusFactory` | Publishes an event to all handlers |
| `IEventDispatcher` | Dispatches an event (delegates to `IEventBus`) |
| `Dispatcher` | `IDispatcher` + `ICommandQueue` implementation (expression-compiled, cached) |
| `EventDispatcher` | `IEventBus` + `IEventDispatcher` implementation (expression-compiled, cached) |
| `NullCommandHandler<T>` | No-op singleton command handler |
| `NullQueryHandler<T, TResponse>` | No-op singleton query handler |
| `NullEventHandler<T>` | No-op singleton event handler |
| `Unit` | Void substitute for command results |

## Middlewares (`Cqrs.Pipeline.Middlewares`)

Typed middleware — each middleware is bound to a specific `(TRequest, TResult)` or `TEvent` pair and can read and modify the result.

| Type | Description |
|---|---|
| `IRequestMiddleware<TRequest, TResult>` | Wraps a single request type; `next` returns `Task<TResult>` |
| `IEventMiddleware<TEvent>` | Wraps a single event type |
| `MiddlewareAwareDispatcher` | `IDispatcher` decorator applying `IRequestMiddleware` chain |
| `MiddlewareAwareEventDispatcher` | `IEventDispatcher` decorator applying `IEventMiddleware` chain |

## Pipelines (`Cqrs.Pipeline.Behaviours`)

Non-generic cross-cutting behaviours — apply to every request or event regardless of type.

| Type | Description |
|---|---|
| `IRequestPipelineBehaviour` | Applied to all requests; `next` returns `Task<object?>` |
| `IEventPipelineBehaviour` | Applied to all events |
| `BehaviourAwareDispatcher` | `IDispatcher` decorator applying `IRequestPipelineBehaviour` chain |
| `BehaviourAwareEventDispatcher` | `IEventDispatcher` decorator applying `IEventPipelineBehaviour` chain |

## Usage

```csharp
// Command
public record CreateOrderCommand(Guid OrderId) : ICommand;

public class CreateOrderHandler : ICommandHandler<CreateOrderCommand>
{
    public Task HandleAsync(CreateOrderCommand command, CancellationToken ct = default)
    {
        // ...
        return Task.CompletedTask;
    }
}

// Query
public record GetOrderQuery(Guid OrderId) : IQuery<OrderDto>;

// Dispatch
IDispatcher dispatcher = new Dispatcher(serviceProvider);
await dispatcher.InvokeAsync(new CreateOrderCommand(Guid.NewGuid()));
var order = await dispatcher.InvokeAsync(new GetOrderQuery(id));

// Event
public record OrderShipped(Guid OrderId) : IEvent;

IEventBus bus = new EventDispatcher(serviceProvider);
await bus.PublishAsync(new OrderShipped(id));
```

### Adding middleware

```csharp
// Typed — sees the result
public class ValidationMiddleware : IRequestMiddleware<CreateOrderCommand, Unit>
{
    public async Task<Unit> HandleAsync(CreateOrderCommand req, CancellationToken ct,
        Func<CancellationToken, Task<Unit>> next)
    {
        // validate...
        return await next(ct);
    }
}

IDispatcher dispatcher = new MiddlewareAwareDispatcher(new Dispatcher(sp), sp);
```

### Adding a pipeline behaviour

```csharp
// Non-generic — applies to every request
public class LoggingBehaviour : IRequestPipelineBehaviour
{
    public async Task<object?> HandleAsync(IRequest request, CancellationToken ct,
        Func<CancellationToken, Task<object?>> next)
    {
        Console.WriteLine($"→ {request.GetType().Name}");
        var result = await next(ct);
        Console.WriteLine($"← {request.GetType().Name}");
        return result;
    }
}

IDispatcher dispatcher = new BehaviourAwareDispatcher(new Dispatcher(sp), sp);
```

## Build

```sh
make build   # dotnet build Cqrs.slnx
make test    # dotnet test Cqrs.slnx
```

Requires [.NET 10 SDK](https://dotnet.microsoft.com/download).
