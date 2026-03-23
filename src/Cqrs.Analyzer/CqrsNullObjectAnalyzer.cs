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

		var handleAsync = classDecl.Members
			.OfType<MethodDeclarationSyntax>()
			.FirstOrDefault(m => m.Identifier.ValueText == "HandleAsync");

		if (handleAsync is null)
			return;

		// CQRS030: void command handler that only returns Task.CompletedTask
		if (iCmdHandlerT1 is not null)
		{
			var cmdInterface = typeSymbol.Interfaces
				.FirstOrDefault(i => SymbolEqualityComparer.Default.Equals(i.OriginalDefinition, iCmdHandlerT1));

			if (cmdInterface is not null && IsEffectivelyCompletedTask(handleAsync))
			{
				var commandTypeName = cmdInterface.TypeArguments[0]
					.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat);

				ctx.ReportDiagnostic(Diagnostic.Create(
					Diagnostics.Cqrs030,
					classDecl.Identifier.GetLocation(),
					typeSymbol.Name,
					commandTypeName));
			}
		}

		// CQRS031: returning handler with default! body (query or returning command handler)
		if (iQueryHandler is not null || iCmdHandlerT2 is not null)
		{
			var isReturningHandler =
				(iQueryHandler is not null && ImplementsOpenGeneric(typeSymbol, iQueryHandler))
				|| (iCmdHandlerT2 is not null && ImplementsOpenGeneric(typeSymbol, iCmdHandlerT2));

			if (isReturningHandler && ReturnsDefaultBang(handleAsync))
				ctx.ReportDiagnostic(Diagnostic.Create(
					Diagnostics.Cqrs031,
					classDecl.Identifier.GetLocation(),
					typeSymbol.Name));
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
		if (statements.Count == 1
		    && statements[0] is ReturnStatementSyntax { Expression: var retExpr }
		    && retExpr is not null
		    && IsCompletedTaskAccess(retExpr))
			return true;

		return false;
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

		if (statements.Count == 1
		    && statements[0] is ReturnStatementSyntax { Expression: var retExpr }
		    && retExpr is not null
		    && IsDefaultBangExpression(retExpr))
			return true;

		return false;
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

	private static bool ImplementsOpenGeneric(INamedTypeSymbol type, INamedTypeSymbol openGeneric) =>
		type.AllInterfaces.Any(i =>
			SymbolEqualityComparer.Default.Equals(i.OriginalDefinition, openGeneric));
}
