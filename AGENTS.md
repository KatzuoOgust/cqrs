# AGENTS.md — Cqrs

## Project layout

```
src/Cqrs/            # Library — interfaces and null-object implementations only
tests/Cqrs.Tests/    # xUnit test project
```

## Build & test commands

```sh
make build                        # dotnet build Cqrs.slnx
make test                         # dotnet test Cqrs.slnx
dotnet test tests/Cqrs.Tests --filter "FullyQualifiedName~NullCommandHandlerTests"  # single class
```

## Design rules

- **Abstractions only** — `src/Cqrs` must not contain concrete dispatcher or event bus implementations. Those belong in consumer packages.
- **No dependencies** — `Cqrs.csproj` must not reference any NuGet packages (not even `Microsoft.Extensions.*`).
- **`ICommand` returns `Unit`** — void commands implement `ICommand`, which inherits `IRequest<Unit>`. Do not introduce a separate void overload of `IDispatcher.InvokeAsync`.
- **Null handlers are singletons** — expose via a static `Instance` property with a private constructor.
- **Covariant markers** — `IRequest<out TResponse>`, `ICommand<out TResponse>`, `IQuery<out TResponse>` use covariant type parameters.
- **`IEventBus` fan-out** — events go to *all* handlers; `IDispatcher` routes to *one* handler.

## Naming conventions

- Interfaces: `I` prefix, e.g. `ICommandHandler<TCommand>`.
- Null objects: `Null` prefix, e.g. `NullCommandHandler<TCommand>`.
- Handler method: always `HandleAsync(T input, CancellationToken cancellationToken = default)`.
- Test classes: `{Subject}Tests`, one file per subject group (`HandlerTests.cs`).

## Namespace convention

Strip `.Tests`, `.Core`, `.Abstractions` suffixes when setting `RootNamespace`. Keep all other suffixes (`.Middlewares`, `.Pipelines`, etc.).

| Project | `<RootNamespace>` |
|---|---|
| `Cqrs` | `KatzuoOgust.Cqrs` |
| `Cqrs.Tests` | `KatzuoOgust.Cqrs` |
| `Cqrs.Core` | `KatzuoOgust.Cqrs` |
| `Cqrs.Abstractions` | `KatzuoOgust.Cqrs` |
| `Cqrs.Pipeline.Middlewares` | `KatzuoOgust.Cqrs.Pipeline.Middlewares` |
| `Cqrs.Pipeline.Behaviours` | `KatzuoOgust.Cqrs.Pipeline.Behaviours` |

Projects in `Cqrs.Pipeline.Middlewares` and `Cqrs.Pipeline.Behaviours` must add `using KatzuoOgust.Cqrs;` to reference core types.

## Adding a new abstraction

1. Add the interface file in `src/Cqrs/` using the `Cqrs` namespace (file-scoped).
2. If a null-object implementation makes sense, add `Null{Name}.cs` next to it.
3. Add tests in `tests/Cqrs.Tests/HandlerTests.cs` (or a new `*Tests.cs` file if unrelated).
4. Update the table in `README.md`.
