using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace KatzuoOgust.Cqrs.Analyzer;

/// <summary>
///     Enforces CQRS020 and CQRS021:
///     <list type="bullet">
///         <item>
///             CQRS020 — <c>IRequestMiddleware&lt;TRequest,TResult&gt;.HandleAsync</c> never invokes <c>next</c>,
///             breaking the pipeline.
///         </item>
///         <item>
///             CQRS021 — <c>IRequestPipelineBehaviour.HandleAsync</c> casts the non-generic <c>request</c> to a specific
///             <c>IRequest</c> type.
///         </item>
///     </list>
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class CqrsMiddlewareAnalyzer : DiagnosticAnalyzer
{
	public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics =>
		ImmutableArray.Create(Diagnostics.Cqrs020, Diagnostics.Cqrs021);

	public override void Initialize(AnalysisContext context)
	{
		context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
		context.EnableConcurrentExecution();

		context.RegisterCompilationStartAction(compilationCtx =>
		{
			var iMiddleware = compilationCtx.Compilation.GetIRequestMiddlewareT2();
			var iBehaviour = compilationCtx.Compilation.GetIRequestPipelineBehaviour();
			var iRequest = compilationCtx.Compilation.GetIRequest();

			if (iMiddleware is null && iBehaviour is null)
				return;

			compilationCtx.RegisterSyntaxNodeAction(
				ctx => Analyze(ctx, iMiddleware, iBehaviour, iRequest),
				SyntaxKind.ClassDeclaration);
		});
	}

	private static void Analyze(
		SyntaxNodeAnalysisContext ctx,
		INamedTypeSymbol? iMiddleware,
		INamedTypeSymbol? iBehaviour,
		INamedTypeSymbol? iRequest)
	{
		var classDecl = (ClassDeclarationSyntax)ctx.Node;
		if (ctx.SemanticModel.GetDeclaredSymbol(classDecl) is not INamedTypeSymbol typeSymbol) return;

		CheckCqrs020(ctx, iMiddleware, typeSymbol);
		CheckCqrs021(ctx, iBehaviour, iRequest, typeSymbol, classDecl);
	}



	private static void CheckCqrs020(SyntaxNodeAnalysisContext ctx, INamedTypeSymbol? iMiddleware,
		INamedTypeSymbol typeSymbol)
	{
		// Only applies to IRequestMiddleware<TRequest, TResult>
		if (iMiddleware is null || !ImplementsOpenGeneric(typeSymbol, iMiddleware)) return;

		foreach (var (_, handleAsync, methodSymbol) in FindHandleAsyncImplementations(ctx, typeSymbol, iMiddleware))
		{
			if (!BodyCallsNext(ctx.SemanticModel, handleAsync, methodSymbol))
				ctx.ReportDiagnostic(Diagnostic.Create(
					Diagnostics.Cqrs020,
					handleAsync.Identifier.GetLocation(),
					typeSymbol.Name));
		}
	}

	/// <summary>
	/// Returns true if the middleware's continuation delegate parameter is invoked anywhere in the method body.
	/// </summary>
	private static bool BodyCallsNext(
		SemanticModel semanticModel,
		MethodDeclarationSyntax method,
		IMethodSymbol methodSymbol)
	{
		var root = (SyntaxNode?)method.Body ?? method.ExpressionBody;
		if (root is null)
			return false;

		if (methodSymbol.Parameters.Length < 3)
			return false;

		var nextParameter = methodSymbol.Parameters[2];

		return root.DescendantNodes()
			.OfType<InvocationExpressionSyntax>()
			.Any(inv => IsNextInvocation(semanticModel, inv, nextParameter));
	}

	private static void CheckCqrs021(SyntaxNodeAnalysisContext ctx, INamedTypeSymbol? iBehaviour,
		INamedTypeSymbol? iRequest, INamedTypeSymbol typeSymbol, ClassDeclarationSyntax classDecl)
	{
		// Only applies to IRequestPipelineBehaviour, which is non-generic, so we look for any cast to a specific IRequest<T> in the body
		if (iBehaviour is null
			|| iRequest is null
			|| !typeSymbol.AllInterfaces.Any(i => SymbolEqualityComparer.Default.Equals(i, iBehaviour)))
			return;

		var implementation = FindHandleAsyncImplementations(ctx, typeSymbol, iBehaviour).FirstOrDefault();
		if (implementation.handleAsync is null) return;

		var handleAsync = implementation.handleAsync;

		var root = (SyntaxNode?)handleAsync.Body ?? handleAsync.ExpressionBody;
		if (root is null) return;

		foreach (var castNode in FindRequestCasts(root))
		{
			var castTypeSymbol = ctx.SemanticModel.GetSymbolInfo(castNode.typeSyntax).Symbol as ITypeSymbol;
			if (castTypeSymbol is null)
				continue;

			if (ImplementsOrIsIRequest(castTypeSymbol, iRequest))
				ctx.ReportDiagnostic(Diagnostic.Create(
					Diagnostics.Cqrs021,
					castNode.location,
					typeSymbol.Name,
					castTypeSymbol.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat)));
		}
	}

	/// <summary>
	///     Yields (typeSyntax, location) for every cast, 'as', or 'is' pattern that targets a specific type.
	/// </summary>
	private static IEnumerable<(TypeSyntax typeSyntax, Location location)> FindRequestCasts(SyntaxNode root)
	{
		// (ConcreteType)expr
		foreach (var cast in root.DescendantNodes().OfType<CastExpressionSyntax>())
			yield return (cast.Type, cast.GetLocation());

		// expr as ConcreteType
		foreach (var binary in root.DescendantNodes().OfType<BinaryExpressionSyntax>())
			if (binary.IsKind(SyntaxKind.AsExpression) && binary.Right is TypeSyntax ts)
				yield return (ts, binary.GetLocation());

		// expr is ConcreteType or expr is ConcreteType id
		foreach (var isPattern in root.DescendantNodes().OfType<IsPatternExpressionSyntax>())
		{
			var typeSyntax = ExtractTypeFromPattern(isPattern.Pattern);
			if (typeSyntax is not null)
				yield return (typeSyntax, isPattern.GetLocation());
		}
	}

	private static TypeSyntax? ExtractTypeFromPattern(PatternSyntax pattern) => pattern switch
	{
		DeclarationPatternSyntax decl => decl.Type,
		TypePatternSyntax type => type.Type,
		_ => null
	};

	private static bool ImplementsOrIsIRequest(ITypeSymbol type, INamedTypeSymbol iRequest)
	{
		if (SymbolEqualityComparer.Default.Equals(type.OriginalDefinition, iRequest))
			return true;
		if (type is INamedTypeSymbol named)
			return named.AllInterfaces.Any(i => SymbolEqualityComparer.Default.Equals(i.OriginalDefinition, iRequest)
												|| SymbolEqualityComparer.Default.Equals(i, iRequest));
		return false;
	}

	// ── Helpers ───────────────────────────────────────────────────────────────

	private static bool IsNextInvocation(
		SemanticModel semanticModel,
		InvocationExpressionSyntax invocation,
		IParameterSymbol nextParameter)
	{
		var exprSymbol = semanticModel.GetSymbolInfo(invocation.Expression).Symbol;
		if (SymbolEqualityComparer.Default.Equals(exprSymbol, nextParameter))
			return true;

		if (invocation.Expression is MemberAccessExpressionSyntax memberAccess)
		{
			var targetSymbol = semanticModel.GetSymbolInfo(memberAccess.Expression).Symbol;
			if (SymbolEqualityComparer.Default.Equals(targetSymbol, nextParameter)
				&& memberAccess.Name.Identifier.ValueText == "Invoke")
				return true;
		}

		return false;
	}

	private static IEnumerable<(INamedTypeSymbol iface, MethodDeclarationSyntax handleAsync, IMethodSymbol methodSymbol)>
		FindHandleAsyncImplementations(
			SyntaxNodeAnalysisContext ctx,
			INamedTypeSymbol typeSymbol,
			INamedTypeSymbol openInterface)
	{
		var ifaces = typeSymbol.AllInterfaces
			.Where(i =>
				SymbolEqualityComparer.Default.Equals(i.OriginalDefinition, openInterface)
				|| SymbolEqualityComparer.Default.Equals(i, openInterface)
			);

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

				if (syntax is null) continue;

				yield return (iface, syntax, implMethod);
			}
		}
	}

	private static bool ImplementsOpenGeneric(INamedTypeSymbol type, INamedTypeSymbol openGeneric) =>
		type.AllInterfaces.Any(i =>
			SymbolEqualityComparer.Default.Equals(i.OriginalDefinition, openGeneric));
}
