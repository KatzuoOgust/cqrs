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
			var iRequestOfT = compilationCtx.Compilation.GetIRequestOfT();
			var iQueryOfT = compilationCtx.Compilation.GetIQueryOfT();
			var iCmdHandlerT2 = compilationCtx.Compilation.GetICommandHandlerT2();
			var unit = compilationCtx.Compilation.GetUnit();

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
		if (typeDecl.BaseList is null) return;

		if (ctx.SemanticModel.GetDeclaredSymbol(typeDecl) is not INamedTypeSymbol typeSymbol) return;

		foreach (var iface in typeSymbol.Interfaces)
		{
			CheckCqrs001(ctx, typeDecl, typeSymbol, iface, iRequestOfT);
			CheckCqrs002(ctx, typeDecl, typeSymbol, iface, iQueryOfT, unit);
			CheckCqrs003(ctx, typeDecl, typeSymbol, iface, iCmdHandlerT2, unit);
		}
	}

	private static void CheckCqrs001(
		SyntaxNodeAnalysisContext ctx,
		TypeDeclarationSyntax typeDecl,
		INamedTypeSymbol typeSymbol,
		INamedTypeSymbol iface,
		INamedTypeSymbol? iRequestOfT)
	{
		if (iRequestOfT is null) return;

		// Check if the interface is IRequest<T> (regardless of T)
		if (!SymbolEqualityComparer.Default.Equals(iface.OriginalDefinition, iRequestOfT)) return;

		ReportInterfaceDiagnostic(
			ctx,
			typeDecl,
			typeSymbol,
			iface,
			Diagnostics.Cqrs001,
			typeSymbol.Name,
			iface.TypeArguments[0].ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat)
		);
	}

	private static void CheckCqrs002(
		SyntaxNodeAnalysisContext ctx,
		TypeDeclarationSyntax typeDecl,
		INamedTypeSymbol typeSymbol,
		INamedTypeSymbol iface,
		INamedTypeSymbol? iQueryOfT,
		INamedTypeSymbol? unit)
	{
		if (iQueryOfT is null || unit is null) return;

		// Check if the interface is IQuery<T> (regardless of T)
		if (!SymbolEqualityComparer.Default.Equals(iface.OriginalDefinition, iQueryOfT)) return;

		// Check if T is Unit
		if (!SymbolEqualityComparer.Default.Equals(iface.TypeArguments[0].OriginalDefinition, unit)) return;

		ReportInterfaceDiagnostic(
			ctx,
			typeDecl,
			typeSymbol,
			iface,
			Diagnostics.Cqrs002,
			typeSymbol.Name
		);
	}

	private static void CheckCqrs003(
		SyntaxNodeAnalysisContext ctx,
		TypeDeclarationSyntax typeDecl,
		INamedTypeSymbol typeSymbol,
		INamedTypeSymbol iface,
		INamedTypeSymbol? iCmdHandlerT2,
		INamedTypeSymbol? unit)
	{
		if (iCmdHandlerT2 is null || unit is null) return;

		// Check if the interface is ICommandHandler<TCommand, TResult> (regardless of TCommand and TResult)
		if (!SymbolEqualityComparer.Default.Equals(iface.OriginalDefinition, iCmdHandlerT2)) return;

		// Check if TResult is Unit
		if (!SymbolEqualityComparer.Default.Equals(iface.TypeArguments[1].OriginalDefinition, unit)) return;

		ReportInterfaceDiagnostic(
			ctx,
			typeDecl,
			typeSymbol,
			iface,
			Diagnostics.Cqrs003,
			typeSymbol.Name,
			iface.TypeArguments[0].ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat)
		);
	}

	private static void ReportInterfaceDiagnostic(
		SyntaxNodeAnalysisContext ctx,
		TypeDeclarationSyntax typeDecl,
		INamedTypeSymbol typeSymbol,
		INamedTypeSymbol iface,
		DiagnosticDescriptor descriptor,
		params object?[] messageArgs)
	{
		var location = FindBaseTypeSyntaxLocation(typeDecl, iface, ctx.SemanticModel)
					   ?? typeSymbol.Locations[0];
		ctx.ReportDiagnostic(Diagnostic.Create(descriptor, location, messageArgs));
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
