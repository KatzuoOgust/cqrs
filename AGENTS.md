# AGENTS.md — Cqrs

> Contributor workflow, naming conventions, and design rules are in **[CONTRIBUTING.md](CONTRIBUTING.md)** — read it first.

## Before you start

Always build and test before and after making changes:

```sh
make build   # dotnet build Cqrs.slnx
make test    # dotnet test Cqrs.slnx
make pack    # dotnet pack → ./artifacts/nupkgs (local smoke-check only)
make clean   # remove bin/obj/artifacts
make format  # dotnet format Cqrs.slnx

# Run a single test class:
dotnet test tests/Cqrs.Tests --filter "FullyQualifiedName~NullCommandHandlerTests"

# Run a single test method:
dotnet test tests/Cqrs.Tests --filter "FullyQualifiedName~NullCommandHandlerTests.HandleAsync_CompletesWithoutThrowing"
```

## Where things live

| What | Where |
|---|---|
| Core interfaces & null objects | `src/Cqrs/` |
| Typed per-request/event middleware | `src/Cqrs.Pipeline.Middlewares/` |
| Non-generic cross-cutting behaviours | `src/Cqrs.Pipeline.Behaviours/` |
| `IServiceProvider` decorator with handler decoration | `src/Cqrs.DependencyInjection/` |
| Roslyn analyzers | `src/Cqrs.Analyzer/` |
| All tests | `tests/Cqrs.Tests/` — subdirectory mirrors the subject's namespace |
| Runnable examples | `examples/Cqrs.Examples/` |

## Rules — never break these

- **`src/Cqrs` is abstractions only.** Do not add concrete dispatcher or bus implementations there; those belong in consumer packages.
- **`Cqrs.csproj` has zero NuGet dependencies.** Do not add any package references, including `Microsoft.Extensions.*`.
- **Void commands return `Unit`.** `ICommand` inherits `IRequest<Unit>`. Do not add a void overload of `IDispatcher.InvokeAsync`.
- **Covariant type parameters.** Keep `out TResponse` on `IRequest<out TResponse>`, `ICommand<out TResponse>`, and `IQuery<out TResponse>`.
- **`IEventBus` fans out; `IDispatcher` routes to one.** Do not conflate the two.

## Namespaces

Strip `.Tests`, `.Core`, `.Abstractions` when deriving `RootNamespace`; keep all other suffixes.

| Project | `<RootNamespace>` |
|---|---|
| `Cqrs` | `KatzuoOgust.Cqrs` |
| `Cqrs.Tests` | `KatzuoOgust.Cqrs` |
| `Cqrs.Pipeline.Middlewares` | `KatzuoOgust.Cqrs.Pipeline.Middlewares` |
| `Cqrs.Pipeline.Behaviours` | `KatzuoOgust.Cqrs.Pipeline.Behaviours` |
| `Cqrs.DependencyInjection` | `KatzuoOgust.Cqrs.DependencyInjection` |
| `Cqrs.Analyzer` | `KatzuoOgust.Cqrs.Analyzer` |

Add `using KatzuoOgust.Cqrs;` in any file under `Cqrs.Pipeline.*` that references core types.

## Adding a new abstraction

1. Add the interface in `src/Cqrs/` with a file-scoped `namespace KatzuoOgust.Cqrs;`.
2. If a null-object makes sense, add `Null{Name}.cs` beside it — private constructor, static `Instance` property.
3. Place tests under `tests/Cqrs.Tests/` in the subdirectory that matches the subject's namespace.
4. Update the relevant section description in `README.md`.

## Writing tests

Test classes are named `{Subject}Tests`. Test methods follow **`Subject_Result_WhenCondition`** — result before condition, `When` not `If`, condition omitted if unconditional.

### Placement of fixture types (stubs, fakes, records, handler implementations)

| Situation | Where |
|---|---|
| ≤ 5 lines total across all fixtures for the test class | Inline `private` nested type in the test class |
| > 5 lines, used by one test class | `{TestClass}.Fakes.cs` sibling file — add `partial` to the test class |
| Used by two or more test classes | `tests/Cqrs.Tests/Fakes/` — `internal` top-level type, namespace `KatzuoOgust.Cqrs` |

Full detail (including the `Error` helper pattern for production code) is in [CONTRIBUTING.md](CONTRIBUTING.md).

