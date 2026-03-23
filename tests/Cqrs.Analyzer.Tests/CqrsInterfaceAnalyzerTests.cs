using Microsoft.CodeAnalysis;

namespace KatzuoOgust.Cqrs.Analyzer;

public sealed class CqrsInterfaceAnalyzerTests
{
	private static readonly CqrsInterfaceAnalyzer Analyzer = new();

	// ── CQRS001 ──────────────────────────────────────────────────────────────

	[Fact]
	public async Task Cqrs001_DirectIRequestImplementation_ReportsDiagnostic()
	{
		const string source = """
		                      using System;
		                      using KatzuoOgust.Cqrs;
		                      class MyRequest : IRequest<Guid> { }
		                      """;

		var diagnostics = await TestHelper.GetDiagnosticsAsync(Analyzer, source);

		var d = Assert.Single(diagnostics, d => d.Id == "CQRS001");
		Assert.Equal(DiagnosticSeverity.Warning, d.Severity);
		Assert.Contains("MyRequest", d.GetMessage());
		Assert.Contains("Guid", d.GetMessage());
	}

	[Fact]
	public async Task Cqrs001_ImplementsICommandOfT_NoDiagnostic()
	{
		const string source = """
		                      using System;
		                      using KatzuoOgust.Cqrs;
		                      class MyCmd : ICommand<Guid> { }
		                      """;

		var diagnostics = await TestHelper.GetDiagnosticsAsync(Analyzer, source);

		Assert.DoesNotContain(diagnostics, d => d.Id == "CQRS001");
	}

	[Fact]
	public async Task Cqrs001_ImplementsIQueryOfT_NoDiagnostic()
	{
		const string source = """
		                      using System;
		                      using KatzuoOgust.Cqrs;
		                      class MyQuery : IQuery<Guid> { }
		                      """;

		var diagnostics = await TestHelper.GetDiagnosticsAsync(Analyzer, source);

		Assert.DoesNotContain(diagnostics, d => d.Id == "CQRS001");
	}

	[Fact]
	public async Task Cqrs001_ImplementsICommandVoid_NoDiagnostic()
	{
		const string source = """
		                      using KatzuoOgust.Cqrs;
		                      class MyCmd : ICommand { }
		                      """;

		var diagnostics = await TestHelper.GetDiagnosticsAsync(Analyzer, source);

		Assert.DoesNotContain(diagnostics, d => d.Id == "CQRS001");
	}

	// ── CQRS002 ──────────────────────────────────────────────────────────────

	[Fact]
	public async Task Cqrs002_IQueryOfUnit_ReportsDiagnostic()
	{
		const string source = """
		                      using KatzuoOgust.Cqrs;
		                      class MyQuery : IQuery<Unit> { }
		                      """;

		var diagnostics = await TestHelper.GetDiagnosticsAsync(Analyzer, source);

		var d = Assert.Single(diagnostics, d => d.Id == "CQRS002");
		Assert.Equal(DiagnosticSeverity.Warning, d.Severity);
		Assert.Contains("MyQuery", d.GetMessage());
	}

	[Fact]
	public async Task Cqrs002_IQueryOfInt_NoDiagnostic()
	{
		const string source = """
		                      using KatzuoOgust.Cqrs;
		                      class MyQuery : IQuery<int> { }
		                      """;

		var diagnostics = await TestHelper.GetDiagnosticsAsync(Analyzer, source);

		Assert.DoesNotContain(diagnostics, d => d.Id == "CQRS002");
	}

	[Fact]
	public async Task Cqrs002_ICommand_NoDiagnostic()
	{
		const string source = """
		                      using KatzuoOgust.Cqrs;
		                      class MyCmd : ICommand { }
		                      """;

		var diagnostics = await TestHelper.GetDiagnosticsAsync(Analyzer, source);

		Assert.DoesNotContain(diagnostics, d => d.Id == "CQRS002");
	}

	// ── CQRS003 ──────────────────────────────────────────────────────────────

	[Fact]
	public async Task Cqrs003_ICommandHandlerWithUnitResponse_ReportsDiagnostic()
	{
		const string source = """
		                      using System.Threading;
		                      using System.Threading.Tasks;
		                      using KatzuoOgust.Cqrs;
		                      class MyCmd : ICommand { }
		                      class MyHandler : ICommandHandler<MyCmd, Unit>
		                      {
		                          public Task<Unit> HandleAsync(MyCmd command, CancellationToken cancellationToken = default)
		                              => Task.FromResult(Unit.Value);
		                      }
		                      """;

		var diagnostics = await TestHelper.GetDiagnosticsAsync(Analyzer, source);

		var d = Assert.Single(diagnostics, d => d.Id == "CQRS003");
		Assert.Equal(DiagnosticSeverity.Warning, d.Severity);
		Assert.Contains("MyHandler", d.GetMessage());
		Assert.Contains("MyCmd", d.GetMessage());
	}

	[Fact]
	public async Task Cqrs003_ICommandHandlerVoid_NoDiagnostic()
	{
		const string source = """
		                      using System.Threading;
		                      using System.Threading.Tasks;
		                      using KatzuoOgust.Cqrs;
		                      class MyCmd : ICommand { }
		                      class MyHandler : ICommandHandler<MyCmd>
		                      {
		                          public Task HandleAsync(MyCmd command, CancellationToken cancellationToken = default)
		                              => Task.CompletedTask;
		                      }
		                      """;

		var diagnostics = await TestHelper.GetDiagnosticsAsync(Analyzer, source);

		Assert.DoesNotContain(diagnostics, d => d.Id == "CQRS003");
	}

	[Fact]
	public async Task Cqrs003_ICommandHandlerWithNonUnitResponse_NoDiagnostic()
	{
		const string source = """
		                      using System;
		                      using System.Threading;
		                      using System.Threading.Tasks;
		                      using KatzuoOgust.Cqrs;
		                      class MyCmd : ICommand<Guid> { }
		                      class MyHandler : ICommandHandler<MyCmd, Guid>
		                      {
		                          public Task<Guid> HandleAsync(MyCmd command, CancellationToken cancellationToken = default)
		                              => Task.FromResult(Guid.NewGuid());
		                      }
		                      """;

		var diagnostics = await TestHelper.GetDiagnosticsAsync(Analyzer, source);

		Assert.DoesNotContain(diagnostics, d => d.Id == "CQRS003");
	}
}
