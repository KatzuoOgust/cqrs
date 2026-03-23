using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace KatzuoOgust.Cqrs.Analyzer;

/// <summary>
///     Enforces CQRS030 and CQRS031:
///     <list type="bullet">
///         <item>
///             CQRS030 — void command handler whose <c>HandleAsync</c> only returns <c>Task.CompletedTask</c>
///             should use <c>NullCommandHandler&lt;T&gt;.Instance</c> instead.
///         </item>
///         <item>
///             CQRS031 — handler whose <c>HandleAsync</c> returns <c>default!</c> without a 'Null' class name
///             likely has a forgotten implementation.
///         </item>
///     </list>
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class CqrsNullObjectAnalyzer : DiagnosticAnalyzer
{
	public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics =>
		ImmutableArray.Create(Diagnostics.Cqrs030, Diagnostics.Cqrs031);

	public override void Initialize(AnalysisContext context)
	{
		context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
		context.EnableConcurrentExecution();

		context.RegisterCompilationStartAction(compilationCtx =>
		{
			var iCmdHandlerT1 = compilationCtx.Compilation
				.GetTypeByMetadataName("KatzuoOgust.Cqrs.ICommandHandler`1");
			var iCmdHandlerT2 = compilationCtx.Compilation
				.GetTypeByMetadataName("KatzuoOgust.Cqrs.ICommandHandler`2");
			var iQueryHandler = compilationCtx.Compilation
				.GetTypeByMetadataName("KatzuoOgust.Cqrs.IQueryHandler`2");

			if (iCmdHandlerT1 is null && iCmdHandlerT2 is null && iQueryHandler is null)
				return;

			compilationCtx.RegisterSyntaxNodeAction(
				ctx => Analyze(ctx, iCmdHandlerT1, iCmdHandlerT2, iQueryHandler),
				SyntaxKind.ClassDeclaration);
		});
	}

	private static void Analyze(
		SyntaxNodeAnalysisContext ctx,
		INamedTypeSymbol? iCmdHandlerT1,
		INamedTypeSymbol? iCmdHandlerT2,
		INamedTypeSymbol? iQueryHandler)
	{
		var classDecl = (ClassDeclarationSyntax)ctx.Node;
		if (ctx.SemanticModel.GetDeclaredSymbol(classDecl) is not INamedTypeSymbol typeSymbol)
			return;

		// Skip the built-in null handlers and abstract types
		if (typeSymbol.IsAbstract || typeSymbol.Name.StartsWith("Null", StringComparison.Ordinal))
			return;

		CheckCqrs030(ctx, classDecl, typeSymbol, iCmdHandlerT1);
		CheckCqrs031(ctx, classDecl, typeSymbol, iCmdHandlerT2, iQueryHandler);
	}

	private static void CheckCqrs030(
		SyntaxNodeAnalysisContext ctx,
		ClassDeclarationSyntax classDecl,
		INamedTypeSymbol typeSymbol,
		INamedTypeSymbol? iCmdHandlerT1)
	{
		// Only applies to ICommandHandler<TCommand> (not ICommandHandler<TCommand, TResult> or IQueryHandler<TQuery, TResult>)
		if (iCmdHandlerT1 is null)
			return;

		foreach (var (cmdInterface, handleAsync, _) in FindHandleAsyncImplementations(ctx, typeSymbol, iCmdHandlerT1))
		{
			// If the matching handler is not effectively a no-op, it's not a null object — no diagnostic.
			if (!IsEffectivelyCompletedTask(handleAsync))
				continue;

			var commandType = cmdInterface.TypeArguments[0];
			var commandTypeName = commandType.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat);
			ReportClassDiagnostic(ctx, classDecl, Diagnostics.Cqrs030, typeSymbol.Name, commandTypeName);
		}
	}

	private static void CheckCqrs031(
		SyntaxNodeAnalysisContext ctx,
		ClassDeclarationSyntax classDecl,
		INamedTypeSymbol typeSymbol,
		INamedTypeSymbol? iCmdHandlerT2,
		INamedTypeSymbol? iQueryHandler)
	{
		// Only applies to ICommandHandler<TCommand, TResult> and IQueryHandler<TQuery, TResult>
		if (iQueryHandler is null && iCmdHandlerT2 is null)
			return;

		var hasDefaultBangBody =
			(iQueryHandler is not null && FindHandleAsyncImplementations(ctx, typeSymbol, iQueryHandler)
				.Any(x => ReturnsDefaultBang(x.handleAsync)))
			|| (iCmdHandlerT2 is not null && FindHandleAsyncImplementations(ctx, typeSymbol, iCmdHandlerT2)
				.Any(x => ReturnsDefaultBang(x.handleAsync)));

		if (hasDefaultBangBody)
			ReportClassDiagnostic(ctx, classDecl, Diagnostics.Cqrs031, typeSymbol.Name);
	}

	private static void ReportClassDiagnostic(
		SyntaxNodeAnalysisContext ctx,
		ClassDeclarationSyntax classDecl,
		DiagnosticDescriptor descriptor,
		params object?[] messageArgs)
	{
		ctx.ReportDiagnostic(Diagnostic.Create(
			descriptor,
			classDecl.Identifier.GetLocation(),
			messageArgs));
	}

	private static IEnumerable<(INamedTypeSymbol iface, MethodDeclarationSyntax handleAsync, IMethodSymbol methodSymbol)>
		FindHandleAsyncImplementations(
			SyntaxNodeAnalysisContext ctx,
			INamedTypeSymbol typeSymbol,
			INamedTypeSymbol openGeneric)
	{
		var ifaces = typeSymbol.AllInterfaces
			.Where(i => SymbolEqualityComparer.Default.Equals(i.OriginalDefinition, openGeneric));

		foreach (var iface in ifaces)
		{
			foreach (var ifaceMethod in iface.GetMembers("HandleAsync").OfType<IMethodSymbol>())
			{
				if (typeSymbol.FindImplementationForInterfaceMember(ifaceMethod) is not IMethodSymbol implMethod)
					continue;

				var syntax = implMethod.DeclaringSyntaxReferences
					.Select(r => r.GetSyntax(ctx.CancellationToken))
					.OfType<MethodDeclarationSyntax>()
					.FirstOrDefault();

				if (syntax is null)
					continue;

				yield return (iface, syntax, implMethod);
			}
		}
	}

	// ── Body shape detection ──────────────────────────────────────────────────

	/// <summary>
	///     Returns true when <c>HandleAsync</c> is a no-op void command handler, matching:
	///     <code>=> Task.CompletedTask</code>  or  <code>{ return Task.CompletedTask; }</code>
	///     or an empty <c>async Task</c> body.
	/// </summary>
	private static bool IsEffectivelyCompletedTask(MethodDeclarationSyntax method)
	{
		// Expression body: => Task.CompletedTask
		if (method.ExpressionBody is { Expression: var expr })
			return IsCompletedTaskAccess(expr);

		if (method.Body is null)
			return false;

		var statements = method.Body.Statements;

		// Empty body on an async method
		if (statements.Count == 0 && method.Modifiers.Any(SyntaxKind.AsyncKeyword))
			return true;

		// Single return: return Task.CompletedTask;
		return statements.Count == 1
		       && statements[0] is ReturnStatementSyntax { Expression: var retExpr }
		       && retExpr is not null
		       && IsCompletedTaskAccess(retExpr);
	}

	/// <summary>
	///     Returns true when <c>HandleAsync</c> body is effectively <c>default!</c>, matching:
	///     <code>=> Task.FromResult&lt;T&gt;(default!)</code>  or  <code>{ return Task.FromResult(default!); }</code>
	/// </summary>
	private static bool ReturnsDefaultBang(MethodDeclarationSyntax method)
	{
		// Expression body: => Task.FromResult(default!)  or  => default!
		if (method.ExpressionBody is { Expression: var expr })
			return IsDefaultBangExpression(expr);

		if (method.Body is null)
			return false;

		var statements = method.Body.Statements;

		return statements.Count == 1
		       && statements[0] is ReturnStatementSyntax { Expression: var retExpr }
		       && retExpr is not null
		       && IsDefaultBangExpression(retExpr);
	}

	// ── Expression helpers ────────────────────────────────────────────────────

	private static bool IsCompletedTaskAccess(ExpressionSyntax expr) =>
		expr is MemberAccessExpressionSyntax
		{
			Expression: IdentifierNameSyntax { Identifier.ValueText: "Task" },
			Name.Identifier.ValueText: "CompletedTask"
		};

	/// <summary>
	///     Matches <c>default!</c> or <c>Task.FromResult[&lt;T&gt;](default!)</c>.
	/// </summary>
	private static bool IsDefaultBangExpression(ExpressionSyntax expr)
	{
		// default!
		if (IsSuppressedDefault(expr))
			return true;

		// Task.FromResult(default!)  or  Task.FromResult<T>(default!)
		if (expr is InvocationExpressionSyntax invocation
		    && invocation.Expression is MemberAccessExpressionSyntax
		    {
			    Expression: IdentifierNameSyntax { Identifier.ValueText: "Task" },
			    Name.Identifier.ValueText: "FromResult"
		    }
		    && invocation.ArgumentList.Arguments.Count == 1
		    && IsSuppressedDefault(invocation.ArgumentList.Arguments[0].Expression))
			return true;

		return false;
	}

	/// <summary>Matches <c>default!</c> — a null-forgiving operator applied to the <c>default</c> literal.</summary>
	private static bool IsSuppressedDefault(ExpressionSyntax expr) =>
		expr is PostfixUnaryExpressionSyntax
		{
			RawKind: (int)SyntaxKind.SuppressNullableWarningExpression,
			Operand: LiteralExpressionSyntax { RawKind: (int)SyntaxKind.DefaultLiteralExpression }
			or DefaultExpressionSyntax
		};
}
