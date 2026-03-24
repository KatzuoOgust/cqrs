using Microsoft.CodeAnalysis;

namespace KatzuoOgust.Cqrs.Analyzer;

internal static class CompilationExtensions
{
	extension(Compilation compilation)
	{
		public INamedTypeSymbol? GetIRequest() =>
			compilation.GetTypeByMetadataName("KatzuoOgust.Cqrs.IRequest");

		public INamedTypeSymbol? GetIRequestOfT() =>
			compilation.GetTypeByMetadataName("KatzuoOgust.Cqrs.IRequest`1");

		public INamedTypeSymbol? GetIQueryOfT() =>
			compilation.GetTypeByMetadataName("KatzuoOgust.Cqrs.IQuery`1");

		public INamedTypeSymbol? GetICommandHandlerT1() =>
			compilation.GetTypeByMetadataName("KatzuoOgust.Cqrs.ICommandHandler`1");

		public INamedTypeSymbol? GetICommandHandlerT2() =>
			compilation.GetTypeByMetadataName("KatzuoOgust.Cqrs.ICommandHandler`2");

		public INamedTypeSymbol? GetIQueryHandlerT2() =>
			compilation.GetTypeByMetadataName("KatzuoOgust.Cqrs.IQueryHandler`2");

		public INamedTypeSymbol? GetUnit() =>
			compilation.GetTypeByMetadataName("KatzuoOgust.Cqrs.Unit");

		public INamedTypeSymbol? GetIRequestMiddlewareT2() =>
			compilation.GetTypeByMetadataName("KatzuoOgust.Cqrs.Pipeline.Middlewares.IRequestMiddleware`2");

		public INamedTypeSymbol? GetIRequestPipelineBehaviour() =>
			compilation.GetTypeByMetadataName("KatzuoOgust.Cqrs.Pipeline.Behaviours.IRequestPipelineBehaviour");
	}
}
