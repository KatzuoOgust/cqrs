# Contributing

Thank you for your interest in contributing! This document covers the development workflow, design rules, and conventions you need to follow.

## Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download)
- Any editor with C# support (VS Code + C# Dev Kit, Rider, Visual Studio)

## Build & test

```sh
make build   # dotnet build Cqrs.slnx
make test    # dotnet test Cqrs.slnx
make pack    # dotnet pack → ./artifacts/nupkgs (local smoke-check only)
make clean   # remove bin/obj/artifacts
```

Always build and run tests both before and after your changes:

```sh
# Run a single test class
dotnet test tests/Cqrs.Tests --filter "FullyQualifiedName~NullCommandHandlerTests"

# Run a single test method
dotnet test tests/Cqrs.Tests --filter "FullyQualifiedName~NullCommandHandlerTests.HandleAsync_CompletesWithoutThrowing"
```

## Repository layout

| Path | Contents |
|---|---|
| `src/Cqrs/` | Core interfaces and null objects |
| `src/Cqrs.Pipeline.Middlewares/` | Typed per-request/event middleware |
| `src/Cqrs.Pipeline.Behaviours/` | Non-generic cross-cutting behaviours |
| `src/Cqrs.DependencyInjection/` | `IServiceProvider` decorator with handler decoration |
| `src/Cqrs.Analyzer/` | Roslyn analyzers enforcing CQRS usage rules (see `README.md` for diagnostic IDs) |
| `tests/Cqrs.Tests/` | All tests — subdirectory mirrors the subject's namespace |
| `examples/Cqrs.Examples/` | Runnable examples for all three pipeline layers |

## Design rules

These invariants must never be broken:

- **`src/Cqrs` is abstractions only.** Concrete dispatcher/bus implementations belong in consumer packages.
- **`Cqrs.csproj` has zero NuGet dependencies.** Do not add any package references, including `Microsoft.Extensions.*`.
- **Void commands return `Unit`.** `ICommand` inherits `IRequest<Unit>`. Do not add a void overload of `IDispatcher.InvokeAsync`.
- **Covariant type parameters.** Keep `out TResponse` on `IRequest<out TResponse>`, `ICommand<out TResponse>`, and `IQuery<out TResponse>`.
- **`IEventBus` fans out; `IDispatcher` routes to one.** Do not conflate the two.

## Adding a new abstraction

1. Add the interface in `src/Cqrs/` with a file-scoped `namespace KatzuoOgust.Cqrs;`.
2. If a null-object makes sense, add `Null{Name}.cs` beside it — private constructor, static `Instance` property.
3. Place tests under `tests/Cqrs.Tests/` in the subdirectory that matches the subject's namespace.
4. Add a row to the table in `README.md`.

## Naming conventions

### Production code

| Thing | Convention | Example |
|---|---|---|
| Interface | `I` prefix | `ICommandHandler<TCommand>` |
| Null object | `Null` prefix | `NullCommandHandler<TCommand>` |
| Handler method | `HandleAsync(T input, CancellationToken cancellationToken = default)` | — |

### Tests

Test classes are named `{Subject}Tests`. Test methods follow:

```
Subject_Result_WhenCondition
```

| Part | What it captures | Examples |
|---|---|---|
| **Subject** | Method or member under test | `InvokeAsync`, `Ctor`, `GetService` |
| **Result** | Expected outcome | `ThrowsArgumentNullException`, `ReturnsUnit`, `InvokesHandler` |
| **Condition** | `When…` scenario; omit if unconditional | `WhenRequestIsNull`, `WhenVoidCommand` |

Result always comes **before** the condition. Use `When`, not `If`.

## Test fakes

Private fixture types (stubs, fakes, records, handler implementations) follow these placement rules:

| Situation | Where to put it |
|---|---|
| ≤ 5 lines total across all fixtures for a test class | Inline as `private` nested types in the test class |
| > 5 lines, specific to one test class | `{TestClass}.Fakes.cs` sibling file, same namespace, `partial` on the test class |
| Used by two or more test classes | `tests/Cqrs.Tests/Fakes/` — `internal` top-level type, namespace `KatzuoOgust.Cqrs` |

### Per-class fakes file

Add `partial` to the test class declaration and create a sibling file:

```
DispatcherTests.cs           ← partial sealed class DispatcherTests { … tests … }
DispatcherTests.Fakes.cs     ← partial sealed class DispatcherTests { … fixtures … }
```

### Shared fakes

Shared fakes live in `tests/Cqrs.Tests/Fakes/` with namespace `KatzuoOgust.Cqrs`. Because all test namespaces are children of `KatzuoOgust.Cqrs`, types there are visible everywhere without a `using`.

```
tests/Cqrs.Tests/Fakes/
  PingCommand.cs             ← internal sealed record PingCommand + PingHandler
  AddCommand.cs              ← internal sealed record AddCommand
```

`SimpleServiceProvider` lives in `src/Cqrs.DependencyInjection/` (`public sealed class`, namespace `KatzuoOgust.Cqrs.DependencyInjection`). Test files outside that namespace must add `using KatzuoOgust.Cqrs.DependencyInjection;`.

## Namespace policy

Strip `.Tests`, `.Core`, `.Abstractions` when deriving `RootNamespace`; keep all other suffixes.

| Project | `<RootNamespace>` |
|---|---|
| `Cqrs` | `KatzuoOgust.Cqrs` |
| `Cqrs.Tests` | `KatzuoOgust.Cqrs` |
| `Cqrs.Pipeline.Middlewares` | `KatzuoOgust.Cqrs.Pipeline.Middlewares` |
| `Cqrs.Pipeline.Behaviours` | `KatzuoOgust.Cqrs.Pipeline.Behaviours` |
| `Cqrs.DependencyInjection` | `KatzuoOgust.Cqrs.DependencyInjection` |

Add `using KatzuoOgust.Cqrs;` in any file under `Cqrs.Pipeline.*` that references core types.

## Error helper pattern

Non-trivial exception throws are centralised in a `private static class Error` nested inside the owning class (or partial class). This keeps throw sites readable and stack traces accurate.

Two styles — pick based on where the throw happens:

| Style | Shape | When to use |
|---|---|---|
| `ThrowIfXxx` | `void` — throws internally | Guard at the top of a method |
| Exception factory | Returns the exception | End-of-method after exhausting all branches |

```csharp
private static class Error
{
    // ThrowIfXxx: void, throws itself — paramName inferred via [CallerArgumentExpression]
    internal static void ThrowIfNotOpenGenericTypeDefinition(
        Type type,
        [CallerArgumentExpression(nameof(type))] string paramName = "") { … }

    // Exception factory: caller writes "throw" so the call site appears in the stack trace
    internal static InvalidOperationException NoSuitableConstructor(
        Type serviceType, Type decoratorType) => new($"…");
}
```

Call sites:

```csharp
// ThrowIfXxx — no nameof() needed, argument expression is captured automatically
Error.ThrowIfNotOpenGenericTypeDefinition(openGenericServiceType);

// Exception factory
throw Error.NoSuitableConstructor(serviceType, decoratorType);
```

**Do not** replace `ArgumentNullException.ThrowIfNull(…)` or any other BCL `XxxException.ThrowIfXxx(…)` calls — leave those untouched. Only raw `new XxxException(…)` throws and `if (!cond) throw new …` blocks move into `Error`.

## Pull requests

- Keep changes focused — one concern per PR.
- All tests must pass (`make test`).
- New public API surface needs at least one test covering the happy path and one covering argument validation.
- Update `README.md` if you add, remove, or change public types.
