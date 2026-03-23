using Microsoft.CodeAnalysis;

namespace KatzuoOgust.Cqrs.Analyzer;

public sealed class CqrsNullObjectAnalyzerTests
{
	private static readonly CqrsNullObjectAnalyzer Analyzer = new();

	// ── CQRS030 ──────────────────────────────────────────────────────────────

	[Fact]
	public async Task Cqrs030_VoidHandlerReturnsCompletedTask_ExpressionBody_ReportsDiagnostic()
	{
		const string source = """
		                      using System.Threading;
		                      using System.Threading.Tasks;
		                      using KatzuoOgust.Cqrs;
		                      class MyCmd : ICommand { }
		                      class MyHandler : ICommandHandler<MyCmd>
		                      {
		                          public Task HandleAsync(MyCmd command, CancellationToken ct = default)
		                              => Task.CompletedTask;
		                      }
		                      """;

		var diagnostics = await TestHelper.GetDiagnosticsAsync(Analyzer, source);

		var d = Assert.Single(diagnostics, d => d.Id == "CQRS030");
		Assert.Equal(DiagnosticSeverity.Info, d.Severity);
		Assert.Contains("MyHandler", d.GetMessage());
		Assert.Contains("MyCmd", d.GetMessage());
	}

	[Fact]
	public async Task Cqrs030_VoidHandlerReturnsCompletedTask_BlockBody_ReportsDiagnostic()
	{
		const string source = """
		                      using System.Threading;
		                      using System.Threading.Tasks;
		                      using KatzuoOgust.Cqrs;
		                      class MyCmd : ICommand { }
		                      class MyHandler : ICommandHandler<MyCmd>
		                      {
		                          public Task HandleAsync(MyCmd command, CancellationToken ct = default)
		                          {
		                              return Task.CompletedTask;
		                          }
		                      }
		                      """;

		var diagnostics = await TestHelper.GetDiagnosticsAsync(Analyzer, source);

		Assert.Single(diagnostics, d => d.Id == "CQRS030");
	}

	[Fact]
	public async Task Cqrs030_VoidHandlerEmptyAsyncBody_ReportsDiagnostic()
	{
		const string source = """
		                      using System.Threading;
		                      using System.Threading.Tasks;
		                      using KatzuoOgust.Cqrs;
		                      class MyCmd : ICommand { }
		                      class MyHandler : ICommandHandler<MyCmd>
		                      {
		                          public async Task HandleAsync(MyCmd command, CancellationToken ct = default)
		                          {
		                          }
		                      }
		                      """;

		var diagnostics = await TestHelper.GetDiagnosticsAsync(Analyzer, source);

		Assert.Single(diagnostics, d => d.Id == "CQRS030");
	}

	[Fact]
	public async Task Cqrs030_VoidHandlerWithActualWork_NoDiagnostic()
	{
		const string source = """
		                      using System;
		                      using System.Threading;
		                      using System.Threading.Tasks;
		                      using KatzuoOgust.Cqrs;
		                      class MyCmd : ICommand { }
		                      class MyHandler : ICommandHandler<MyCmd>
		                      {
		                          public Task HandleAsync(MyCmd command, CancellationToken ct = default)
		                          {
		                              Console.WriteLine("doing work");
		                              return Task.CompletedTask;
		                          }
		                      }
		                      """;

		var diagnostics = await TestHelper.GetDiagnosticsAsync(Analyzer, source);

		Assert.DoesNotContain(diagnostics, d => d.Id == "CQRS030");
	}

	[Fact]
	public async Task Cqrs030_ClassNameStartsWithNull_NoDiagnostic()
	{
		const string source = """
		                      using System.Threading;
		                      using System.Threading.Tasks;
		                      using KatzuoOgust.Cqrs;
		                      class MyCmd : ICommand { }
		                      class NullMyHandler : ICommandHandler<MyCmd>
		                      {
		                          public Task HandleAsync(MyCmd command, CancellationToken ct = default)
		                              => Task.CompletedTask;
		                      }
		                      """;

		var diagnostics = await TestHelper.GetDiagnosticsAsync(Analyzer, source);

		Assert.DoesNotContain(diagnostics, d => d.Id == "CQRS030");
	}

	// ── CQRS031 ──────────────────────────────────────────────────────────────

	[Fact]
	public async Task Cqrs031_QueryHandlerReturnsDefaultBang_ExpressionBody_ReportsDiagnostic()
	{
		const string source = """
		                      using System.Threading;
		                      using System.Threading.Tasks;
		                      using KatzuoOgust.Cqrs;
		                      class MyQuery : IQuery<int> { }
		                      class MyHandler : IQueryHandler<MyQuery, int>
		                      {
		                          public Task<int> HandleAsync(MyQuery query, CancellationToken ct = default)
		                              => Task.FromResult<int>(default!);
		                      }
		                      """;

		var diagnostics = await TestHelper.GetDiagnosticsAsync(Analyzer, source);

		var d = Assert.Single(diagnostics, d => d.Id == "CQRS031");
		Assert.Equal(DiagnosticSeverity.Warning, d.Severity);
		Assert.Contains("MyHandler", d.GetMessage());
	}

	[Fact]
	public async Task Cqrs031_QueryHandlerReturnsDefaultBang_BlockBody_ReportsDiagnostic()
	{
		const string source = """
		                      using System.Threading;
		                      using System.Threading.Tasks;
		                      using KatzuoOgust.Cqrs;
		                      class MyQuery : IQuery<string> { }
		                      class MyHandler : IQueryHandler<MyQuery, string>
		                      {
		                          public Task<string> HandleAsync(MyQuery query, CancellationToken ct = default)
		                          {
		                              return Task.FromResult<string>(default!);
		                          }
		                      }
		                      """;

		var diagnostics = await TestHelper.GetDiagnosticsAsync(Analyzer, source);

		Assert.Single(diagnostics, d => d.Id == "CQRS031");
	}

	[Fact]
	public async Task Cqrs031_CommandHandlerWithUnitResponseReturnsDefaultBang_ReportsDiagnostic()
	{
		const string source = """
		                      using System;
		                      using System.Threading;
		                      using System.Threading.Tasks;
		                      using KatzuoOgust.Cqrs;
		                      class MyCmd : ICommand<Guid> { }
		                      class MyHandler : ICommandHandler<MyCmd, Guid>
		                      {
		                          public Task<Guid> HandleAsync(MyCmd command, CancellationToken ct = default)
		                              => Task.FromResult<Guid>(default!);
		                      }
		                      """;

		var diagnostics = await TestHelper.GetDiagnosticsAsync(Analyzer, source);

		Assert.Single(diagnostics, d => d.Id == "CQRS031");
	}

	[Fact]
	public async Task Cqrs031_ClassNameStartsWithNull_NoDiagnostic()
	{
		const string source = """
		                      using System.Threading;
		                      using System.Threading.Tasks;
		                      using KatzuoOgust.Cqrs;
		                      class MyQuery : IQuery<int> { }
		                      class NullMyHandler : IQueryHandler<MyQuery, int>
		                      {
		                          public Task<int> HandleAsync(MyQuery query, CancellationToken ct = default)
		                              => Task.FromResult<int>(default!);
		                      }
		                      """;

		var diagnostics = await TestHelper.GetDiagnosticsAsync(Analyzer, source);

		Assert.DoesNotContain(diagnostics, d => d.Id == "CQRS031");
	}

	[Fact]
	public async Task Cqrs031_QueryHandlerWithActualReturn_NoDiagnostic()
	{
		const string source = """
		                      using System.Threading;
		                      using System.Threading.Tasks;
		                      using KatzuoOgust.Cqrs;
		                      class MyQuery : IQuery<int> { }
		                      class MyHandler : IQueryHandler<MyQuery, int>
		                      {
		                          public Task<int> HandleAsync(MyQuery query, CancellationToken ct = default)
		                              => Task.FromResult(42);
		                      }
		                      """;

		var diagnostics = await TestHelper.GetDiagnosticsAsync(Analyzer, source);

		Assert.DoesNotContain(diagnostics, d => d.Id == "CQRS031");
	}
}
