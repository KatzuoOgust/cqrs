using Microsoft.CodeAnalysis;

namespace KatzuoOgust.Cqrs.Analyzer;

internal static class CompilationExtensions
{
	public static INamedTypeSymbol? GetIRequest(this Compilation compilation) =>
		compilation.GetTypeByMetadataName("KatzuoOgust.Cqrs.IRequest");

	public static INamedTypeSymbol? GetIRequestOfT(this Compilation compilation) =>
		compilation.GetTypeByMetadataName("KatzuoOgust.Cqrs.IRequest`1");

	public static INamedTypeSymbol? GetIQueryOfT(this Compilation compilation) =>
		compilation.GetTypeByMetadataName("KatzuoOgust.Cqrs.IQuery`1");

	public static INamedTypeSymbol? GetICommandHandlerT1(this Compilation compilation) =>
		compilation.GetTypeByMetadataName("KatzuoOgust.Cqrs.ICommandHandler`1");

	public static INamedTypeSymbol? GetICommandHandlerT2(this Compilation compilation) =>
		compilation.GetTypeByMetadataName("KatzuoOgust.Cqrs.ICommandHandler`2");

	public static INamedTypeSymbol? GetIQueryHandlerT2(this Compilation compilation) =>
		compilation.GetTypeByMetadataName("KatzuoOgust.Cqrs.IQueryHandler`2");

	public static INamedTypeSymbol? GetUnit(this Compilation compilation) =>
		compilation.GetTypeByMetadataName("KatzuoOgust.Cqrs.Unit");

	public static INamedTypeSymbol? GetIRequestMiddlewareT2(this Compilation compilation) =>
		compilation.GetTypeByMetadataName("KatzuoOgust.Cqrs.Pipeline.Middlewares.IRequestMiddleware`2");

	public static INamedTypeSymbol? GetIRequestPipelineBehaviour(this Compilation compilation) =>
		compilation.GetTypeByMetadataName("KatzuoOgust.Cqrs.Pipeline.Behaviours.IRequestPipelineBehaviour");
}
