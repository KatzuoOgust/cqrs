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
			var iMiddleware = compilationCtx.Compilation
				.GetTypeByMetadataName("KatzuoOgust.Cqrs.Pipeline.Middlewares.IRequestMiddleware`2");
			var iBehaviour = compilationCtx.Compilation
				.GetTypeByMetadataName("KatzuoOgust.Cqrs.Pipeline.Behaviours.IRequestPipelineBehaviour");
			var iRequest = compilationCtx.Compilation
				.GetTypeByMetadataName("KatzuoOgust.Cqrs.IRequest");

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
		if (ctx.SemanticModel.GetDeclaredSymbol(classDecl) is not INamedTypeSymbol typeSymbol)
			return;

		if (iMiddleware is not null && ImplementsOpenGeneric(typeSymbol, iMiddleware))
			CheckCqrs020(ctx, classDecl, typeSymbol);

		if (iBehaviour is not null && iRequest is not null
		                           && typeSymbol.AllInterfaces.Any(i =>
			                           SymbolEqualityComparer.Default.Equals(i, iBehaviour)))
			CheckCqrs021(ctx, classDecl, typeSymbol, iRequest);
	}

	// ── CQRS020 ──────────────────────────────────────────────────────────────

	private static void CheckCqrs020(
		SyntaxNodeAnalysisContext ctx,
		ClassDeclarationSyntax classDecl,
		INamedTypeSymbol typeSymbol)
	{
		var handleAsync = FindHandleAsyncMethod(classDecl);
		if (handleAsync is null)
			return;

		if (!BodyCallsNext(handleAsync))
			ctx.ReportDiagnostic(Diagnostic.Create(
				Diagnostics.Cqrs020,
				handleAsync.Identifier.GetLocation(),
				typeSymbol.Name));
	}

	/// <summary>Returns true if <c>next(...)</c> is invoked anywhere in the method body.</summary>
	private static bool BodyCallsNext(MethodDeclarationSyntax method)
	{
		var root = (SyntaxNode?)method.Body ?? method.ExpressionBody;
		if (root is null)
			return false;

		return root.DescendantNodes()
			.OfType<InvocationExpressionSyntax>()
			.Any(inv => inv.Expression is IdentifierNameSyntax { Identifier.ValueText: "next" });
	}

	// ── CQRS021 ──────────────────────────────────────────────────────────────

	private static void CheckCqrs021(
		SyntaxNodeAnalysisContext ctx,
		ClassDeclarationSyntax classDecl,
		INamedTypeSymbol typeSymbol,
		INamedTypeSymbol iRequest)
	{
		var handleAsync = FindHandleAsyncMethod(classDecl);
		if (handleAsync is null)
			return;

		var root = (SyntaxNode?)handleAsync.Body ?? handleAsync.ExpressionBody;
		if (root is null)
			return;

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

	private static MethodDeclarationSyntax? FindHandleAsyncMethod(ClassDeclarationSyntax classDecl) =>
		classDecl.Members
			.OfType<MethodDeclarationSyntax>()
			.FirstOrDefault(m => m.Identifier.ValueText == "HandleAsync");

	private static bool ImplementsOpenGeneric(INamedTypeSymbol type, INamedTypeSymbol openGeneric) =>
		type.AllInterfaces.Any(i =>
			SymbolEqualityComparer.Default.Equals(i.OriginalDefinition, openGeneric));
}
