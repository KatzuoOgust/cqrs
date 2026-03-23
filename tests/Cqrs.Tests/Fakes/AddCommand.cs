// ReSharper disable CheckNamespace
namespace KatzuoOgust.Cqrs;

internal sealed record AddCommand(int A, int B) : ICommand<int>;
