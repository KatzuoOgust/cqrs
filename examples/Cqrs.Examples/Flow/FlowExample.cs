using KatzuoOgust.Cqrs.DependencyInjection;
using KatzuoOgust.Cqrs.Flow;

namespace KatzuoOgust.Cqrs.Examples.Flow;

#region Domain

/// <summary>
/// Workflow commands that trigger subsequent commands.
/// </summary>
internal sealed record OrderCreatedCommand(Guid OrderId, string ProductName) : ICommand;
internal sealed record ChargePaymentCommand(Guid OrderId, decimal Amount) : ICommand;
internal sealed record SendConfirmationCommand(Guid OrderId, string Email) : ICommand;
internal sealed record UpdateInventoryCommand(Guid OrderId, string ProductName, int Quantity) : ICommand;

#endregion

#region Flow Handlers — Commands that produce follow-up commands

internal sealed class OrderCreatedFlowHandler : IFlowCommandHandler<OrderCreatedCommand>
{
    public Task<IEnumerable<ICommand>> ExecuteAsync(OrderCreatedCommand command, CancellationToken ct = default)
    {
        Console.WriteLine($"    [Flow] Processing order: {command.OrderId}");
        Console.WriteLine($"           Product: {command.ProductName}");

        ICommand[] followUp = new ICommand[]
        {
            new ChargePaymentCommand(command.OrderId, 99.99m),
            new UpdateInventoryCommand(command.OrderId, command.ProductName, 1),
        };

        Console.WriteLine($"           Enqueued {followUp.Length} follow-up commands");

        return Task.FromResult(followUp.AsEnumerable());
    }
}

internal sealed class ChargePaymentFlowHandler : IFlowCommandHandler<ChargePaymentCommand>
{
    public Task<IEnumerable<ICommand>> ExecuteAsync(ChargePaymentCommand command, CancellationToken ct = default)
    {
        Console.WriteLine($"    [Flow] Charged ${command.Amount:F2} for order {command.OrderId}");

        ICommand[] followUp = new ICommand[]
        {
            new SendConfirmationCommand(command.OrderId, "customer@example.com"),
        };

        Console.WriteLine($"           Enqueued 1 follow-up command");

        return Task.FromResult(followUp.AsEnumerable());
    }
}

internal sealed class UpdateInventoryFlowHandler : IFlowCommandHandler<UpdateInventoryCommand>
{
    public Task<IEnumerable<ICommand>> ExecuteAsync(UpdateInventoryCommand command, CancellationToken ct = default)
    {
        Console.WriteLine($"    [Flow] Updated inventory: -{command.Quantity} {command.ProductName}");
        
        // Terminal command — no follow-up
        return Task.FromResult(Enumerable.Empty<ICommand>());
    }
}

internal sealed class SendConfirmationFlowHandler : IFlowCommandHandler<SendConfirmationCommand>
{
    public Task<IEnumerable<ICommand>> ExecuteAsync(SendConfirmationCommand command, CancellationToken ct = default)
    {
        Console.WriteLine($"    [Flow] Sent confirmation email to {command.Email}");
        
        // Terminal command — no follow-up
        return Task.FromResult(Enumerable.Empty<ICommand>());
    }
}

#endregion

#region Command Queue Implementation

/// <summary>
/// Command queue that dispatches commands sequentially via the dispatcher.
/// </summary>
internal sealed class DispatchingCommandQueue : ICommandQueue
{
    private readonly IDispatcher _dispatcher;

    public DispatchingCommandQueue(IDispatcher dispatcher)
    {
        ArgumentNullException.ThrowIfNull(dispatcher);
        _dispatcher = dispatcher;
    }

    public async Task EnqueueAsync<TCommand>(TCommand command, CancellationToken cancellationToken = default)
        where TCommand : ICommand
    {
        ArgumentNullException.ThrowIfNull(command);
        await _dispatcher.InvokeAsync(command, cancellationToken);
    }
}

#endregion

#region Example Runner

internal static class FlowExample
{
    public static async Task RunAsync()
    {
        Console.WriteLine("=== Flow — Command Chaining ===\n");

        // Create service provider and command queue first
        var sp = new SimpleServiceProvider();
        var commandQueue = new DispatchingCommandQueue(new Dispatcher(sp));

        // Register all flow handlers wrapped as command handlers
        sp.Register<ICommandHandler<OrderCreatedCommand>>(
               new FlowCommandHandlerWrapper<OrderCreatedCommand>(
                   new OrderCreatedFlowHandler(),
                   commandQueue))
             .Register<ICommandHandler<ChargePaymentCommand>>(
               new FlowCommandHandlerWrapper<ChargePaymentCommand>(
                   new ChargePaymentFlowHandler(),
                   commandQueue))
             .Register<ICommandHandler<UpdateInventoryCommand>>(
               new FlowCommandHandlerWrapper<UpdateInventoryCommand>(
                   new UpdateInventoryFlowHandler(),
                   commandQueue))
             .Register<ICommandHandler<SendConfirmationCommand>>(
               new FlowCommandHandlerWrapper<SendConfirmationCommand>(
                   new SendConfirmationFlowHandler(),
                   commandQueue));

        // Create dispatcher with registered handlers
        var dispatcher = new Dispatcher(sp);

        Console.WriteLine("-- Initiating workflow: OrderCreated command --\n");
        await dispatcher.InvokeAsync(new OrderCreatedCommand(Guid.NewGuid(), "Premium Widget"));

        Console.WriteLine("\n-- Workflow complete: All chained commands executed --\n");

        await Task.CompletedTask;
    }
}

#endregion
