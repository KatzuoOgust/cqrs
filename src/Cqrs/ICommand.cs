namespace KatzuoOgust.Cqrs;

/// <summary>Marker interface for a command that produces no result.</summary>
public interface ICommand : IRequest<Unit> { }

/// <summary>Marker interface for a command that produces <typeparamref name="TResponse"/>.</summary>
public interface ICommand<out TResponse> : IRequest<TResponse> { }
