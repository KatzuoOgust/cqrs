using Microsoft.CodeAnalysis;

namespace KatzuoOgust.Cqrs.Analyzer;

public sealed class CqrsMiddlewareAnalyzerTests
{
	private static readonly CqrsMiddlewareAnalyzer Analyzer = new();

	// ── CQRS020 ──────────────────────────────────────────────────────────────

	[Fact]
	public async Task Cqrs020_MiddlewareNeverCallsNext_ReportsDiagnostic()
	{
		const string source = """
		                      using System;
		                      using System.Threading;
		                      using System.Threading.Tasks;
		                      using KatzuoOgust.Cqrs;
		                      using KatzuoOgust.Cqrs.Pipeline.Middlewares;
		                      class MyQuery : IQuery<int> { }
		                      class MyMiddleware : IRequestMiddleware<MyQuery, int>
		                      {
		                          public Task<int> HandleAsync(MyQuery request, CancellationToken ct,
		                              Func<CancellationToken, Task<int>> next)
		                              => Task.FromResult(42);
		                      }
		                      """;

		var diagnostics = await TestHelper.GetDiagnosticsAsync(Analyzer, source);

		var d = Assert.Single(diagnostics, d => d.Id == "CQRS020");
		Assert.Equal(DiagnosticSeverity.Warning, d.Severity);
		Assert.Contains("MyMiddleware", d.GetMessage());
	}

	[Fact]
	public async Task Cqrs020_MiddlewareCallsNext_NoDiagnostic()
	{
		const string source = """
		                      using System;
		                      using System.Threading;
		                      using System.Threading.Tasks;
		                      using KatzuoOgust.Cqrs;
		                      using KatzuoOgust.Cqrs.Pipeline.Middlewares;
		                      class MyQuery : IQuery<int> { }
		                      class MyMiddleware : IRequestMiddleware<MyQuery, int>
		                      {
		                          public async Task<int> HandleAsync(MyQuery request, CancellationToken ct,
		                              Func<CancellationToken, Task<int>> next)
		                          {
		                              var result = await next(ct);
		                              return result;
		                          }
		                      }
		                      """;

		var diagnostics = await TestHelper.GetDiagnosticsAsync(Analyzer, source);

		Assert.DoesNotContain(diagnostics, d => d.Id == "CQRS020");
	}

	[Fact]
	public async Task Cqrs020_MiddlewareCallsNextInExpressionBody_NoDiagnostic()
	{
		const string source = """
		                      using System;
		                      using System.Threading;
		                      using System.Threading.Tasks;
		                      using KatzuoOgust.Cqrs;
		                      using KatzuoOgust.Cqrs.Pipeline.Middlewares;
		                      class MyQuery : IQuery<int> { }
		                      class MyMiddleware : IRequestMiddleware<MyQuery, int>
		                      {
		                          public Task<int> HandleAsync(MyQuery request, CancellationToken ct,
		                              Func<CancellationToken, Task<int>> next) => next(ct);
		                      }
		                      """;

		var diagnostics = await TestHelper.GetDiagnosticsAsync(Analyzer, source);

		Assert.DoesNotContain(diagnostics, d => d.Id == "CQRS020");
	}

	[Fact]
	public async Task Cqrs020_MiddlewareCallsDelegateWithDifferentParameterName_NoDiagnostic()
	{
		const string source = """
		                      using System;
		                      using System.Threading;
		                      using System.Threading.Tasks;
		                      using KatzuoOgust.Cqrs;
		                      using KatzuoOgust.Cqrs.Pipeline.Middlewares;
		                      class MyQuery : IQuery<int> { }
		                      class MyMiddleware : IRequestMiddleware<MyQuery, int>
		                      {
		                          public Task<int> HandleAsync(MyQuery request, CancellationToken ct,
		                              Func<CancellationToken, Task<int>> continuePipeline) => continuePipeline(ct);
		                      }
		                      """;

		var diagnostics = await TestHelper.GetDiagnosticsAsync(Analyzer, source);

		Assert.DoesNotContain(diagnostics, d => d.Id == "CQRS020");
	}

	// ── CQRS021 ──────────────────────────────────────────────────────────────

	[Fact]
	public async Task Cqrs021_BehaviourCastsToConcreteRequest_ReportsDiagnostic()
	{
		const string source = """
		                      using System;
		                      using System.Threading;
		                      using System.Threading.Tasks;
		                      using KatzuoOgust.Cqrs;
		                      using KatzuoOgust.Cqrs.Pipeline.Behaviours;
		                      class MyQuery : IQuery<int> { }
		                      class MyBehaviour : IRequestPipelineBehaviour
		                      {
		                          public async Task<object?> HandleAsync(IRequest request, CancellationToken ct,
		                              Func<CancellationToken, Task<object?>> next)
		                          {
		                              var q = (MyQuery)request;
		                              return await next(ct);
		                          }
		                      }
		                      """;

		var diagnostics = await TestHelper.GetDiagnosticsAsync(Analyzer, source);

		var d = Assert.Single(diagnostics, d => d.Id == "CQRS021");
		Assert.Equal(DiagnosticSeverity.Warning, d.Severity);
		Assert.Contains("MyBehaviour", d.GetMessage());
		Assert.Contains("MyQuery", d.GetMessage());
	}

	[Fact]
	public async Task Cqrs021_BehaviourUsesAsPatternOnRequest_ReportsDiagnostic()
	{
		const string source = """
		                      using System;
		                      using System.Threading;
		                      using System.Threading.Tasks;
		                      using KatzuoOgust.Cqrs;
		                      using KatzuoOgust.Cqrs.Pipeline.Behaviours;
		                      class MyQuery : IQuery<int> { }
		                      class MyBehaviour : IRequestPipelineBehaviour
		                      {
		                          public async Task<object?> HandleAsync(IRequest request, CancellationToken ct,
		                              Func<CancellationToken, Task<object?>> next)
		                          {
		                              var q = request as MyQuery;
		                              return await next(ct);
		                          }
		                      }
		                      """;

		var diagnostics = await TestHelper.GetDiagnosticsAsync(Analyzer, source);

		var d = Assert.Single(diagnostics, d => d.Id == "CQRS021");
		Assert.Contains("MyQuery", d.GetMessage());
	}

	[Fact]
	public async Task Cqrs021_BehaviourUsesIsPatternOnRequest_ReportsDiagnostic()
	{
		const string source = """
		                      using System;
		                      using System.Threading;
		                      using System.Threading.Tasks;
		                      using KatzuoOgust.Cqrs;
		                      using KatzuoOgust.Cqrs.Pipeline.Behaviours;
		                      class MyQuery : IQuery<int> { }
		                      class MyBehaviour : IRequestPipelineBehaviour
		                      {
		                          public async Task<object?> HandleAsync(IRequest request, CancellationToken ct,
		                              Func<CancellationToken, Task<object?>> next)
		                          {
		                              if (request is MyQuery q) _ = q;
		                              return await next(ct);
		                          }
		                      }
		                      """;

		var diagnostics = await TestHelper.GetDiagnosticsAsync(Analyzer, source);

		var d = Assert.Single(diagnostics, d => d.Id == "CQRS021");
		Assert.Contains("MyQuery", d.GetMessage());
	}

	[Fact]
	public async Task Cqrs021_BehaviourDoesNotCast_NoDiagnostic()
	{
		const string source = """
		                      using System;
		                      using System.Threading;
		                      using System.Threading.Tasks;
		                      using KatzuoOgust.Cqrs;
		                      using KatzuoOgust.Cqrs.Pipeline.Behaviours;
		                      class MyBehaviour : IRequestPipelineBehaviour
		                      {
		                          public Task<object?> HandleAsync(IRequest request, CancellationToken ct,
		                              Func<CancellationToken, Task<object?>> next) => next(ct);
		                      }
		                      """;

		var diagnostics = await TestHelper.GetDiagnosticsAsync(Analyzer, source);

		Assert.DoesNotContain(diagnostics, d => d.Id == "CQRS021");
	}

	[Fact]
	public async Task Cqrs021_ClassWithMiddlewareAndBehaviour_AnalyzesBehaviourHandleAsync()
	{
		const string source = """
		                      using System;
		                      using System.Threading;
		                      using System.Threading.Tasks;
		                      using KatzuoOgust.Cqrs;
		                      using KatzuoOgust.Cqrs.Pipeline.Behaviours;
		                      using KatzuoOgust.Cqrs.Pipeline.Middlewares;
		                      class MyQuery : IQuery<int> { }
		                      class MyHandler : IRequestMiddleware<MyQuery, int>, IRequestPipelineBehaviour
		                      {
		                          public Task<int> HandleAsync(MyQuery request, CancellationToken ct,
		                              Func<CancellationToken, Task<int>> next) => next(ct);

		                          public async Task<object?> HandleAsync(IRequest request, CancellationToken ct,
		                              Func<CancellationToken, Task<object?>> next)
		                          {
		                              var q = (MyQuery)request;
		                              return await next(ct);
		                          }
		                      }
		                      """;

		var diagnostics = await TestHelper.GetDiagnosticsAsync(Analyzer, source);

		var d = Assert.Single(diagnostics, d => d.Id == "CQRS021");
		Assert.Contains("MyQuery", d.GetMessage());
	}
}
