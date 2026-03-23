using Microsoft.CodeAnalysis;

namespace KatzuoOgust.Cqrs.Analyzer;

internal static class Diagnostics
{
	private const string Category = "Cqrs";

	/// <summary>CQRS001 — type directly implements IRequest&lt;T&gt; instead of ICommand&lt;T&gt; or IQuery&lt;T&gt;.</summary>
	public static readonly DiagnosticDescriptor Cqrs001 = new(
		"CQRS001",
		"Implement ICommand<T> or IQuery<T> instead of IRequest<T> directly",
		"'{0}' directly implements IRequest<{1}>; use ICommand<{1}> or IQuery<{1}> instead",
		Category,
		DiagnosticSeverity.Warning,
		true,
		"Types should not directly implement IRequest<T>. Use ICommand<T> for commands or IQuery<T> for queries.");

	/// <summary>CQRS002 — IQuery&lt;Unit&gt; is meaningless; use ICommand.</summary>
	public static readonly DiagnosticDescriptor Cqrs002 = new(
		"CQRS002",
		"IQuery<T> must not return Unit; use ICommand instead",
		"'{0}' implements IQuery<Unit>; void queries are not meaningful — use ICommand instead",
		Category,
		DiagnosticSeverity.Warning,
		true,
		"IQuery<T> must produce a meaningful result. Use ICommand for void operations.");

	/// <summary>CQRS003 — ICommandHandler&lt;TCommand, Unit&gt; should be ICommandHandler&lt;TCommand&gt;.</summary>
	public static readonly DiagnosticDescriptor Cqrs003 = new(
		"CQRS003",
		"Use ICommandHandler<TCommand> instead of ICommandHandler<TCommand, Unit>",
		"'{0}' implements ICommandHandler<{1}, Unit>; use ICommandHandler<{1}> instead",
		Category,
		DiagnosticSeverity.Warning,
		true,
		"When a command handler produces no result, implement ICommandHandler<TCommand> instead of ICommandHandler<TCommand, Unit>.");

	/// <summary>CQRS020 — IRequestMiddleware.HandleAsync never calls next, breaking the pipeline.</summary>
	public static readonly DiagnosticDescriptor Cqrs020 = new(
		"CQRS020",
		"IRequestMiddleware.HandleAsync never calls next",
		"'{0}.HandleAsync' never invokes 'next'; the pipeline chain will be broken",
		Category,
		DiagnosticSeverity.Warning,
		true,
		"Middleware must call next() to continue the pipeline. If short-circuiting is intentional, suppress this diagnostic with a comment explaining why.");

	/// <summary>CQRS021 — IRequestPipelineBehaviour.HandleAsync casts the non-generic request to a specific type.</summary>
	public static readonly DiagnosticDescriptor Cqrs021 = new(
		"CQRS021",
		"IRequestPipelineBehaviour must not cast request to a specific IRequest type",
		"'{0}.HandleAsync' casts 'request' to '{1}'; IRequestPipelineBehaviour is non-generic — use IRequestMiddleware<TRequest, TResult> instead",
		Category,
		DiagnosticSeverity.Warning,
		true,
		"IRequestPipelineBehaviour is a non-generic cross-cutting concern. Casting to a specific IRequest type couples the behaviour to one request. Use IRequestMiddleware<TRequest, TResult> instead.");

	/// <summary>CQRS030 — void command handler whose body is empty; use NullCommandHandler&lt;T&gt;.Instance.</summary>
	public static readonly DiagnosticDescriptor Cqrs030 = new(
		"CQRS030",
		"Empty command handler should use NullCommandHandler<T>.Instance",
		"'{0}' is a no-op ICommandHandler<{1}>; replace with NullCommandHandler<{1}>.Instance",
		Category,
		DiagnosticSeverity.Info,
		true,
		"A command handler whose HandleAsync body only returns Task.CompletedTask is a null object. Use the built-in NullCommandHandler<T>.Instance instead of defining a new class.");

	/// <summary>CQRS031 — handler returns default! suggesting a forgotten implementation.</summary>
	public static readonly DiagnosticDescriptor Cqrs031 = new(
		"CQRS031",
		"Handler returns default! — possible forgotten implementation",
		"'{0}.HandleAsync' returns default!; if intentional rename the class with a 'Null' prefix or use the built-in null handlers",
		Category,
		DiagnosticSeverity.Warning,
		true,
		"Returning default! from a handler usually indicates a forgotten implementation. If this is intentional (null-object pattern), rename the class with a 'Null' prefix or use NullQueryHandler<TQuery, TResponse>.Instance.");
}
