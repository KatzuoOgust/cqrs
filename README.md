# Cqrs

Lightweight, framework-agnostic CQRS abstractions for .NET 10. Provides interfaces and null-object implementations only — no IoC container coupling, no reflection, no runtime magic.

## Abstractions

| Type | Description |
|---|---|
| `IRequest` / `IRequest<TResponse>` | Base marker for all requests |
| `ICommand` | Command with no return value (returns `Unit`) |
| `ICommand<TResponse>` | Command that returns a value |
| `IQuery<TResponse>` | Query that returns a value |
| `IEvent` | Domain event marker |
| `ICommandHandler<TCommand>` | Handles a void command |
| `ICommandHandler<TCommand, TResponse>` | Handles a command returning a value |
| `IQueryHandler<TQuery, TResponse>` | Handles a query |
| `IEventHandler<TEvent>` | Handles a domain event |
| `IDispatcher` | Sends a request to its single handler |
| `IEventBus` | Publishes an event to all registered handlers |
| `NullCommandHandler<TCommand>` | No-op command handler (Null Object) |
| `NullQueryHandler<TQuery, TResponse>` | No-op query handler returning `default` |
| `NullEventHandler<TEvent>` | No-op event handler (Null Object) |
| `Unit` | Void substitute for command results |

## Usage

```csharp
// Define a command
public record CreateOrderCommand(Guid OrderId, string Product) : ICommand;

// Implement a handler
public class CreateOrderHandler : ICommandHandler<CreateOrderCommand>
{
    public Task HandleAsync(CreateOrderCommand command, CancellationToken ct = default)
    {
        // ...
        return Task.CompletedTask;
    }
}

// Define a query
public record GetOrderQuery(Guid OrderId) : IQuery<OrderDto>;

// Null handler as a safe default / stub
ICommandHandler<CreateOrderCommand> handler = NullCommandHandler<CreateOrderCommand>.Instance;
```

## Commands vs Queries

- **`ICommand`** — mutates state, returns `Unit`.  
- **`ICommand<TResponse>`** — mutates state and returns a value (use sparingly).  
- **`IQuery<TResponse>`** — read-only, always returns a value.  
- **`IEvent`** — something that already happened; published to *all* handlers via `IEventBus`.

## Build

```sh
make build   # dotnet build Cqrs.slnx
make test    # dotnet test Cqrs.slnx
```

Requires [.NET 10 SDK](https://dotnet.microsoft.com/download).
