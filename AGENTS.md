# AGENTS.md — Cqrs

## Before you start

Always build and test before and after making changes:

```sh
make build   # dotnet build Cqrs.slnx
make test    # dotnet test Cqrs.slnx
make pack    # dotnet pack → ./artifacts/nupkgs (local smoke-check only)

# Run a single test class:
dotnet test tests/Cqrs.Tests --filter "FullyQualifiedName~NullCommandHandlerTests"
```

## Where things live

| What | Where |
|---|---|
| Core interfaces & null objects | `src/Cqrs/` |
| Typed per-request/event middleware | `src/Cqrs.Pipeline.Middlewares/` |
| Non-generic cross-cutting behaviours | `src/Cqrs.Pipeline.Behaviours/` |
| All tests | `tests/Cqrs.Tests/` — subdirectory mirrors the subject's namespace |

## Adding a new abstraction

1. Add the interface in `src/Cqrs/` with a file-scoped `namespace KatzuoOgust.Cqrs;`.
2. If a null-object makes sense, add `Null{Name}.cs` beside it — private constructor, static `Instance` property.
3. Place tests under `tests/Cqrs.Tests/` in the subdirectory that matches the subject's namespace.
4. Add a row to the table in `README.md`.

## Rules — never break these

- **`src/Cqrs` is abstractions only.** Do not add concrete dispatcher or bus implementations there; those belong in consumer packages.
- **`Cqrs.csproj` has zero NuGet dependencies.** Do not add any package references, including `Microsoft.Extensions.*`.
- **Void commands return `Unit`.** `ICommand` inherits `IRequest<Unit>`. Do not add a void overload of `IDispatcher.InvokeAsync`.
- **Covariant type parameters.** Keep `out TResponse` on `IRequest<out TResponse>`, `ICommand<out TResponse>`, and `IQuery<out TResponse>`.
- **`IEventBus` fans out; `IDispatcher` routes to one.** Do not conflate the two.

## Naming

- Interfaces: `I` prefix — `ICommandHandler<TCommand>`.
- Null objects: `Null` prefix — `NullCommandHandler<TCommand>`.
- Handler method signature: `HandleAsync(T input, CancellationToken cancellationToken = default)`.
- Test classes: `{Subject}Tests`.

## Namespaces

Strip `.Tests`, `.Core`, `.Abstractions` when deriving `RootNamespace`; keep all other suffixes.

| Project | `<RootNamespace>` |
|---|---|
| `Cqrs` | `KatzuoOgust.Cqrs` |
| `Cqrs.Tests` | `KatzuoOgust.Cqrs` |
| `Cqrs.Core` | `KatzuoOgust.Cqrs` |
| `Cqrs.Abstractions` | `KatzuoOgust.Cqrs` |
| `Cqrs.Pipeline.Middlewares` | `KatzuoOgust.Cqrs.Pipeline.Middlewares` |
| `Cqrs.Pipeline.Behaviours` | `KatzuoOgust.Cqrs.Pipeline.Behaviours` |

Add `using KatzuoOgust.Cqrs;` in any file under `Cqrs.Pipeline.*` that references core types.
