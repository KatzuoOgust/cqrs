namespace KatzuoOgust.Cqrs.Pipeline.Middlewares;

/// <summary>Represents the next step in an event middleware pipeline.</summary>
public delegate Task EventMiddlewareDelegate(CancellationToken ct);

/// <summary>Represents the next step in a request middleware pipeline.</summary>
public delegate Task<TResult> RequestMiddlewareDelegate<TResult>(CancellationToken ct);
