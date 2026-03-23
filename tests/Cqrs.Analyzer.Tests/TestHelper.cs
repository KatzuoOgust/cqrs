using System.Collections.Immutable;
using System.Runtime.InteropServices;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Text;

namespace KatzuoOgust.Cqrs.Analyzer;

/// <summary>Runs a <see cref="DiagnosticAnalyzer" /> against in-memory source and returns its diagnostics.</summary>
internal static class TestHelper
{
	private static readonly IReadOnlyList<MetadataReference> RuntimeRefs = BuildRuntimeRefs();

	/// <summary>
	///     Compiles <paramref name="sources" /> together with the CQRS stubs and returns
	///     only the diagnostics produced by <paramref name="analyzer" />.
	/// </summary>
	public static async Task<ImmutableArray<Diagnostic>> GetDiagnosticsAsync(
		DiagnosticAnalyzer analyzer,
		params string[] sources)
	{
		return await GetDiagnosticsAsync(analyzer,
			new[] { CqrsStubs.CoreInterfaces, CqrsStubs.PipelineInterfaces },
			sources);
	}

	/// <summary>
	///     Compiles <paramref name="sources" /> together with explicit <paramref name="stubs" /> and returns
	///     only the diagnostics produced by <paramref name="analyzer" />.
	/// </summary>
	public static async Task<ImmutableArray<Diagnostic>> GetDiagnosticsAsync(
		DiagnosticAnalyzer analyzer,
		string[] stubs,
		string[] sources)
	{
		var allSources = stubs.Concat(sources).Select((s, i) =>
			CSharpSyntaxTree.ParseText(
				SourceText.From(s),
				CSharpParseOptions.Default.WithLanguageVersion(LanguageVersion.Latest),
				$"Source{i}.cs"));

		var compilation = CSharpCompilation.Create(
			"AnalyzerTest",
			allSources,
			RuntimeRefs,
			new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary)
				.WithNullableContextOptions(NullableContextOptions.Enable));

		var withAnalyzers = compilation.WithAnalyzers(ImmutableArray.Create(analyzer));
		return await withAnalyzers.GetAnalyzerDiagnosticsAsync();
	}

	private static IReadOnlyList<MetadataReference> BuildRuntimeRefs()
	{
		// Gather all trusted platform assemblies (available on .NET 5+)
		var trustedPlatformAssemblies =
			AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES") as string ?? string.Empty;

		var paths = trustedPlatformAssemblies
			.Split(RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? ';' : ':')
			.Where(p => !string.IsNullOrEmpty(p))
			.ToList();

		return paths
			.Select(p => (MetadataReference)MetadataReference.CreateFromFile(p))
			.ToList();
	}
}
