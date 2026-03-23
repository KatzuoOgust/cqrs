namespace KatzuoOgust.Cqrs.Analyzer;

/// <summary>Minimal stub source for the KatzuoOgust.Cqrs interfaces used in analyzer tests.</summary>
internal static class CqrsStubs
{
	public const string CoreInterfaces = """
	                                     using System.Threading;
	                                     using System.Threading.Tasks;
	                                     namespace KatzuoOgust.Cqrs
	                                     {
	                                         public interface IRequest { }
	                                         public interface IRequest<out TResponse> : IRequest { }
	                                         public interface ICommand : IRequest<Unit> { }
	                                         public interface ICommand<out TResponse> : IRequest<TResponse> { }
	                                         public interface IQuery<out TResponse> : IRequest<TResponse> { }
	                                         public interface IEvent { }
	                                         public struct Unit
	                                         {
	                                             public static readonly Unit Value = default;
	                                         }
	                                         public interface ICommandHandler<in TCommand>
	                                             where TCommand : ICommand
	                                         {
	                                             Task HandleAsync(TCommand command, CancellationToken cancellationToken = default);
	                                         }
	                                         public interface ICommandHandler<in TCommand, TResponse>
	                                             where TCommand : ICommand<TResponse>
	                                         {
	                                             Task<TResponse> HandleAsync(TCommand command, CancellationToken cancellationToken = default);
	                                         }
	                                         public interface IQueryHandler<in TQuery, TResponse>
	                                             where TQuery : IQuery<TResponse>
	                                         {
	                                             Task<TResponse> HandleAsync(TQuery query, CancellationToken cancellationToken = default);
	                                         }
	                                         public sealed class NullCommandHandler<TCommand> : ICommandHandler<TCommand>
	                                             where TCommand : ICommand
	                                         {
	                                             public static readonly NullCommandHandler<TCommand> Instance = new NullCommandHandler<TCommand>();
	                                             private NullCommandHandler() { }
	                                             public Task HandleAsync(TCommand command, CancellationToken cancellationToken = default)
	                                                 => Task.CompletedTask;
	                                         }
	                                         public sealed class NullQueryHandler<TQuery, TResponse> : IQueryHandler<TQuery, TResponse>
	                                             where TQuery : IQuery<TResponse>
	                                         {
	                                             public static readonly NullQueryHandler<TQuery, TResponse> Instance = new NullQueryHandler<TQuery, TResponse>();
	                                             private NullQueryHandler() { }
	                                             public Task<TResponse> HandleAsync(TQuery query, CancellationToken cancellationToken = default)
	                                                 => Task.FromResult<TResponse>(default!);
	                                         }
	                                     }
	                                     """;

	public const string PipelineInterfaces = """
	                                         using System;
	                                         using System.Threading;
	                                         using System.Threading.Tasks;
	                                         namespace KatzuoOgust.Cqrs.Pipeline.Middlewares
	                                         {
	                                             public interface IRequestMiddleware<TRequest, TResult>
	                                                 where TRequest : KatzuoOgust.Cqrs.IRequest<TResult>
	                                             {
	                                                 Task<TResult> HandleAsync(
	                                                     TRequest request,
	                                                     CancellationToken ct,
	                                                     Func<CancellationToken, Task<TResult>> next);
	                                             }
	                                         }
	                                         namespace KatzuoOgust.Cqrs.Pipeline.Behaviours
	                                         {
	                                             public interface IRequestPipelineBehaviour
	                                             {
	                                                 Task<object?> HandleAsync(
	                                                     KatzuoOgust.Cqrs.IRequest request,
	                                                     CancellationToken ct,
	                                                     Func<CancellationToken, Task<object?>> next);
	                                             }
	                                         }
	                                         """;
}
