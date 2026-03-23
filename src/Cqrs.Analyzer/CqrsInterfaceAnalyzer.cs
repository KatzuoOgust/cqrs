using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace KatzuoOgust.Cqrs.Analyzer;

/// <summary>
///     Enforces CQRS001, CQRS002, and CQRS003:
///     <list type="bullet">
///         <item>
///             CQRS001 — type must not directly implement <c>IRequest&lt;T&gt;</c>; use <c>ICommand&lt;T&gt;</c> or
///             <c>IQuery&lt;T&gt;</c>.
///         </item>
///         <item>CQRS002 — <c>IQuery&lt;Unit&gt;</c> is not meaningful; use <c>ICommand</c> instead.</item>
///         <item>CQRS003 — <c>ICommandHandler&lt;TCommand, Unit&gt;</c> should be <c>ICommandHandler&lt;TCommand&gt;</c>.</item>
///     </list>
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class CqrsInterfaceAnalyzer : DiagnosticAnalyzer
{
	public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics =>
		ImmutableArray.Create(Diagnostics.Cqrs001, Diagnostics.Cqrs002, Diagnostics.Cqrs003);

	public override void Initialize(AnalysisContext context)
	{
		context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
		context.EnableConcurrentExecution();

		context.RegisterCompilationStartAction(compilationCtx =>
		{
			var iRequestOfT = compilationCtx.Compilation.GetTypeByMetadataName("KatzuoOgust.Cqrs.IRequest`1");
			var iQueryOfT = compilationCtx.Compilation.GetTypeByMetadataName("KatzuoOgust.Cqrs.IQuery`1");
			var iCmdHandlerT2 = compilationCtx.Compilation.GetTypeByMetadataName("KatzuoOgust.Cqrs.ICommandHandler`2");
			var unit = compilationCtx.Compilation.GetTypeByMetadataName("KatzuoOgust.Cqrs.Unit");

			if (iRequestOfT is null && iQueryOfT is null && iCmdHandlerT2 is null)
				return;

			compilationCtx.RegisterSyntaxNodeAction(
				ctx => Analyze(ctx, iRequestOfT, iQueryOfT, iCmdHandlerT2, unit),
				SyntaxKind.ClassDeclaration,
				SyntaxKind.StructDeclaration,
				SyntaxKind.RecordDeclaration,
				SyntaxKind.RecordStructDeclaration);
		});
	}

	private static void Analyze(
		SyntaxNodeAnalysisContext ctx,
		INamedTypeSymbol? iRequestOfT,
		INamedTypeSymbol? iQueryOfT,
		INamedTypeSymbol? iCmdHandlerT2,
		INamedTypeSymbol? unit)
	{
		var typeDecl = (TypeDeclarationSyntax)ctx.Node;
		if (typeDecl.BaseList is null)
			return;

		if (ctx.SemanticModel.GetDeclaredSymbol(typeDecl) is not INamedTypeSymbol typeSymbol)
			return;

		foreach (var iface in typeSymbol.Interfaces)
		{
			var original = iface.OriginalDefinition;

			// CQRS001: directly implements IRequest<T> — should be ICommand<T> or IQuery<T>
			if (iRequestOfT is not null
			    && SymbolEqualityComparer.Default.Equals(original, iRequestOfT))
			{
				var location = FindBaseTypeSyntaxLocation(typeDecl, iface, ctx.SemanticModel)
				               ?? typeSymbol.Locations[0];
				ctx.ReportDiagnostic(Diagnostic.Create(
					Diagnostics.Cqrs001,
					location,
					typeSymbol.Name,
					iface.TypeArguments[0].ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat)));
			}

			// CQRS002: implements IQuery<Unit>
			if (iQueryOfT is not null && unit is not null
			                          && SymbolEqualityComparer.Default.Equals(original, iQueryOfT)
			                          && SymbolEqualityComparer.Default.Equals(
				                          iface.TypeArguments[0].OriginalDefinition, unit))
			{
				var location = FindBaseTypeSyntaxLocation(typeDecl, iface, ctx.SemanticModel)
				               ?? typeSymbol.Locations[0];
				ctx.ReportDiagnostic(Diagnostic.Create(
					Diagnostics.Cqrs002,
					location,
					typeSymbol.Name));
			}

			// CQRS003: implements ICommandHandler<TCommand, Unit>
			if (iCmdHandlerT2 is not null && unit is not null
			                              && SymbolEqualityComparer.Default.Equals(original, iCmdHandlerT2)
			                              && SymbolEqualityComparer.Default.Equals(
				                              iface.TypeArguments[1].OriginalDefinition, unit))
			{
				var location = FindBaseTypeSyntaxLocation(typeDecl, iface, ctx.SemanticModel)
				               ?? typeSymbol.Locations[0];
				ctx.ReportDiagnostic(Diagnostic.Create(
					Diagnostics.Cqrs003,
					location,
					typeSymbol.Name,
					iface.TypeArguments[0].ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat)));
			}
		}
	}

	/// <summary>
	///     Finds the syntax location of the specific base-type entry that resolves to
	///     <paramref name="targetInterface" /> (matched by original definition).
	/// </summary>
	private static Location? FindBaseTypeSyntaxLocation(
		TypeDeclarationSyntax typeDecl,
		INamedTypeSymbol targetInterface,
		SemanticModel model)
	{
		if (typeDecl.BaseList is null)
			return null;

		var targetOriginal = targetInterface.OriginalDefinition;

		foreach (var baseTypeSyntax in typeDecl.BaseList.Types)
			if (model.GetSymbolInfo(baseTypeSyntax.Type).Symbol is INamedTypeSymbol sym
			    && SymbolEqualityComparer.Default.Equals(sym.OriginalDefinition, targetOriginal))
				return baseTypeSyntax.GetLocation();

		return null;
	}
}
