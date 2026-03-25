# Contributing

Fork the repo, create a branch from `main`, make your changes following the conventions below, keep `make test` green throughout, and open a PR against `main`.

## Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download)
- Any editor with C# support (VS Code + C# Dev Kit, Rider, Visual Studio)

## Getting started

1. **Fork** the repository on GitHub, then clone your fork:

   ```sh
   git clone https://github.com/<your-username>/cqrs.git
   cd cqrs
   ```

2. **Verify your setup** — build and run all tests before touching any code:

   ```sh
   make build
   make test
   ```

   Both commands must complete with no errors. If they don't, confirm that the .NET 10 SDK is on your `PATH` (`dotnet --version`).

   To run a focused subset later:

   ```sh
   # Single test class
   dotnet test tests/Cqrs.Tests --filter "FullyQualifiedName~NullCommandHandlerTests"

   # Single test method
   dotnet test tests/Cqrs.Tests --filter "FullyQualifiedName~NullCommandHandlerTests.HandleAsync_CompletesWithoutThrowing"
   ```

## Workflow

1. **Create a branch** from `main` — use `feature/<short-description>` for new features, `fix/<short-description>` for bug fixes:

   ```sh
   git checkout -b feature/null-event-handler
   ```

2. **Make your changes.** Follow the design rules, naming conventions, and code style described below.

3. **Build, test, and format** after every meaningful change:

   ```sh
   make build && make test && make format
   ```

4. **Commit** with a clear, focused message. One concern per commit.

5. **Push** and open a pull request against `main` with a short summary of what changed and why. See [Pull requests](#pull-requests) for the checklist.

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

## Scope

**In scope:** new abstractions and null-objects in `src/Cqrs/`, new middleware/behaviour types, new analyzer rules, test improvements, documentation.

**Out of scope:** concrete dispatcher implementations (those belong in consumer packages), any `PackageReference` in `Cqrs.csproj`, breaking changes to public interfaces. If you're unsure whether a change is welcome, open an issue first.

## Design rules

These invariants must never be broken:

- **`src/Cqrs` is abstractions only.** Concrete dispatcher/bus implementations belong in consumer packages.
- **`Cqrs.csproj` has zero NuGet dependencies.** Do not add any package references, including `Microsoft.Extensions.*`.
- **Void commands return `Unit`.** `ICommand` inherits `IRequest<Unit>`. Do not add a void overload of `IDispatcher.InvokeAsync`.
- **Covariant type parameters.** Keep `out TResponse` on `IRequest<out TResponse>`, `ICommand<out TResponse>`, and `IQuery<out TResponse>`.
- **`IEventBus` fans out; `IDispatcher` routes to one.** Do not conflate the two.

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

```csharp
// ✅ condition needed — disambiguates two null scenarios
void InvokeAsync_ThrowsArgumentNullException_WhenRequestIsNull()
void InvokeAsync_ThrowsArgumentNullException_WhenHandlerIsNull()

// ✅ condition omitted — result is self-evident
void HandleAsync_CompletesWithoutThrowing()
void ReturnsUnit_WhenVoidCommand()

// ❌ missing Subject
void ThrowsArgumentNullException_WhenRequestIsNull()

// ❌ uses If instead of When
void InvokeAsync_ThrowsArgumentNullException_IfRequestIsNull()
```

## Test fakes

Private fixture types (stubs, fakes, records, handler implementations) follow these placement rules:

| Situation | Where to put it |
|---|---|
| ≤ 5 lines total across all fixtures for a test class | Inline as `private` nested types in the test class |
| > 5 lines, specific to one test class | `{TestClass}.Fakes.cs` sibling file, same namespace, `partial` on the test class |
| Used by two or more test classes | `tests/Cqrs.Tests/Fakes/` — `internal` top-level type, namespace `KatzuoOgust.Cqrs` |

```
DispatcherTests.cs           ← partial sealed class DispatcherTests { … tests … }
DispatcherTests.Fakes.cs     ← partial sealed class DispatcherTests { … fixtures … }
```

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
| `Cqrs.Analyzer` | `KatzuoOgust.Cqrs.Analyzer` |

Add `using KatzuoOgust.Cqrs;` in any file under `Cqrs.Pipeline.*` that references core types.

## Code style

Code style is enforced by `.editorconfig` at the repository root. Key settings:

- **Indentation:** tabs, width 4 (C#); spaces, width 2 (XML/project files)
- **Line endings:** LF
- **Namespaces:** file-scoped (`namespace Foo;`)

Run `make format` before committing — it runs `dotnet format` and fixes everything automatically.

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
- Include a short summary of what changed and why.
- All tests must pass (`make test`).
- New public API surface needs at least one test covering the happy path and one covering argument validation.
- Update `README.md` if you add, remove, or change public types or their behaviour.
