# Cqrs

![CI](https://github.com/KatzuoOgust/cqrs/actions/workflows/ci.yml/badge.svg)

Lightweight, framework-agnostic CQRS abstractions for .NET 10. Zero NuGet dependencies in the core library.

## Packages

| Package | Namespace | Description |
|---|---|---|
| `Cqrs` | `KatzuoOgust.Cqrs` | Core interfaces, null-object handlers, `Dispatcher`, `EventDispatcher` |
| `Cqrs.Pipeline.Middlewares` | `KatzuoOgust.Cqrs.Pipeline.Middlewares` | Typed per-request/event middleware with full result access |
| `Cqrs.Pipeline.Behaviours` | `KatzuoOgust.Cqrs.Pipeline.Behaviours` | Non-generic cross-cutting pipeline behaviours |
| `Cqrs.DependencyInjection` | `KatzuoOgust.Cqrs.DependencyInjection` | `IServiceProvider` decorator that layers exact and open-generic handler decorators |

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

Resolved as `IEnumerable<IRequestMiddleware<TRequest, TResult>>` — first registered is outermost.

## Behaviours (`Cqrs.Pipeline.Behaviours`)

Non-generic cross-cutting behaviours — apply to every request or event regardless of type.

| Type | Description |
|---|---|
| `IRequestPipelineBehaviour` | Applied to all requests; `next` returns `Task<object?>` |
| `IEventPipelineBehaviour` | Applied to all events |
| `BehaviourAwareDispatcher` | `IDispatcher` decorator applying `IRequestPipelineBehaviour` chain |
| `BehaviourAwareEventDispatcher` | `IEventDispatcher` decorator applying `IEventPipelineBehaviour` chain |

Resolved as `IEnumerable<IRequestPipelineBehaviour>` — first registered is outermost.

## Handler decorators (`Cqrs.DependencyInjection`)

`DecoratingServiceProvider` wraps any `IServiceProvider` and layers handler decorators at resolve time.
`DecoratingServiceProviderExtensions` provides a fluent API over it.

| Method | Description |
|---|---|
| `.Decorate<TService>(Func<TService, IServiceProvider, TService>)` | Exact-type lambda decorator |
| `.Decorate<TDecorator>()` | Exact-type decorator; service type inferred from constructor |
| `.Decorate<TService, TDecorator>()` | Exact-type decorator; service type explicit |
| `.Decorate(openServiceType, Func<Type, object, IServiceProvider, object>)` | Open-generic lambda decorator |
| `.Decorate(openServiceType, openDecoratorType)` | Open-generic type decorator; constructor resolved via Expression trees |
| `.When(predicate, configure)` | Applies inner decorators only when predicate returns `true` |

Decorators are applied in **registration order**: the first registered wraps the raw service, each subsequent one wraps the result of the previous, making the last registered the outermost call.

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

### Adding handler decorators

```csharp
var sp = new DecoratingServiceProvider(innerProvider);

// Exact: wraps only ICommandHandler<CreateOrderCommand>
sp.Decorate<ICommandHandler<CreateOrderCommand>>(
    (inner, _) => new ValidationDecorator(inner));

// Open-generic: wraps every ICommandHandler<T> — one registration covers all command types
sp.Decorate(typeof(ICommandHandler<>), typeof(LoggingCommandDecorator<>));

// Call chain (outermost first):
//   LoggingCommandDecorator   ← registered 2nd → outermost
//   └─ ValidationDecorator    ← registered 1st
//      └─ raw handler

IDispatcher dispatcher = new Dispatcher(sp);
```

### Adding middleware

```csharp
// Typed — bound to one (TRequest, TResult) pair, sees the concrete result
public class ValidationMiddleware : IRequestMiddleware<CreateOrderCommand, Unit>
{
    public async Task<Unit> HandleAsync(CreateOrderCommand req, CancellationToken ct,
        Func<CancellationToken, Task<Unit>> next)
    {
        // validate...
        return await next(ct);
    }
}

// Register as IEnumerable<IRequestMiddleware<CreateOrderCommand, Unit>> in your container
IDispatcher dispatcher = new MiddlewareAwareDispatcher(new Dispatcher(sp), sp);
```

### Adding pipeline behaviours

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

// Register as IEnumerable<IRequestPipelineBehaviour> in your container
IDispatcher dispatcher = new BehaviourAwareDispatcher(new Dispatcher(sp), sp);
```

### Combining all layers

```csharp
// Outermost → innermost:
//   BehaviourAwareDispatcher  — non-generic, cross-cutting concerns
//   └─ MiddlewareAwareDispatcher — typed, per-request concerns
//      └─ Dispatcher          — routes to handler
IDispatcher dispatcher =
    new BehaviourAwareDispatcher(
        new MiddlewareAwareDispatcher(
            new Dispatcher(sp),
            sp),
        sp);
```

## Examples

Runnable examples covering all three layers live in [`examples/Cqrs.Examples`](examples/Cqrs.Examples):

| File | What it shows |
|---|---|
| `Decorators/DecoratorsExample.cs` | Exact + open-generic handler decorators via `DecoratingServiceProvider` |
| `Middlewares/MiddlewaresExample.cs` | Typed `IRequestMiddleware` chain via `MiddlewareAwareDispatcher` |
| `Behaviours/BehavioursExample.cs` | `IRequestPipelineBehaviour` + full combined pipeline stack |

```sh
dotnet run --project examples/Cqrs.Examples
```

## Build

```sh
make build   # dotnet build Cqrs.slnx
make test    # dotnet test Cqrs.slnx
```

Requires [.NET 10 SDK](https://dotnet.microsoft.com/download).
