using Actors.Base;
using Actors.Errors;
using Actors.Mailbox;

namespace Actors;

/// <summary>
/// A stateful actor whose behavior is defined by a delegate rather than inheritance.
/// </summary>
/// <typeparam name="TMessage">Input message type consumed from the actor's mailbox.</typeparam>
/// <typeparam name="TState">State type; must inherit from MachineState.</typeparam>
public sealed class LambdaMachine<TMessage, TState>
    : Machine<TMessage, TState>
    where TState : MachineState
{
    /// <summary>
    /// The delegate that defines the actor's behavior.
    /// </summary>
    private readonly Func<LambdaMachine<TMessage, TState>, TMessage, TState, ISupervisor,
        CancellationToken, Task<TState?>> _handler;

    /// <summary>
    /// The actor host to allow function with in the lambda
    /// </summary>
    private readonly ISupervisor _host;

    /// <summary>
    /// Initializes a lambda machine with an initial state, message handler, and error routing reference.
    /// </summary>
    /// <param name="initialState">The initial machine state; must not be null.</param>
    /// <param name="handler">Async delegate invoked for each message with the current state snapshot.</param>
    /// <param name="host">The actor host to allow funciton with in the lambda.</param>
    /// <param name="errorActor">The actor to send errors to for any further action</param>
    /// <param name="mailboxChannelProvider">
    /// Optional provider used to create the machine mailbox channel.
    /// Uses the default unbounded provider when omitted.
    /// </param>
    public LambdaMachine(
        TState initialState,
        Func<LambdaMachine<TMessage, TState>, TMessage, TState, ISupervisor,
            CancellationToken, Task<TState?>> handler,
        ISupervisor host,
        IActorRef<StandardError>? errorActor = null,
        IMailboxProvider? mailboxChannelProvider = null)
        : base(initialState, errorActor, mailboxChannelProvider)
    {
        ArgumentNullException.ThrowIfNull(handler);
        ArgumentNullException.ThrowIfNull(host);
        _handler = handler;
        _host = host;
    }

    /// <summary>
    /// Processes a message by invoking the user-provided handler delegate with the current state.
    /// </summary>
    /// <param name="message">The input message to process.</param>
    /// <param name="currentState">Snapshot of the current state at message arrival time.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>New state to apply, or null for no state change.</returns>
    protected override Task<TState?> ProcessMessageWithStateAsync(
        TMessage message,
        TState currentState,
        CancellationToken cancellationToken)
    {
        return _handler(this, message, currentState, _host, cancellationToken);
    }
}
