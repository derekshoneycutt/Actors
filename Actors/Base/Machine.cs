using Actors.Errors;
using Actors.Mailbox;

namespace Actors.Base;

/// <summary>
/// Generic base class for stateful actors that own their input mailbox.
/// Extends <see cref="Receiver{TInput}"/>.
/// </summary>
/// <typeparam name="TMessage">Input message type consumed from the actor's mailbox.</typeparam>
/// <typeparam name="TState">State type; must inherit from MachineState.</typeparam>
public abstract class Machine<TMessage, TState>
    : Receiver<TMessage>, IMachine<TMessage, TState>
    where TState : MachineState
{
    /// <summary>
    /// The internal state known only to the Machine
    /// </summary>
    private TState _state;

    /// <summary>
    /// Initializes a mailbox machine with an initial state and error routing reference.
    /// </summary>
    /// <param name="initialState">The initial machine state; must not be null.</param>
    /// <param name="errorActor">The actor to send errors to for any further action</param>
    /// <param name="mailboxChannelProvider">
    /// Optional provider used to create the machine mailbox channel.
    /// Uses the default unbounded provider when omitted.
    /// </param>
    protected Machine(
        TState initialState,
        IActorRef<StandardError>? errorActor = null,
        IMailboxProvider? mailboxChannelProvider = null)
        : base(errorActor, mailboxChannelProvider)
    {
        _state = initialState;
    }

    /// <summary>
    /// Sealed override that captures the state snapshot, delegates to
    /// ProcessMessageWithStateAsync, and atomically applies returned state.
    /// Exceptions during processing leave state unchanged (implicit rollback).
    /// </summary>
    /// <param name="message">The input message to process.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>A task that completes when message processing is done.</returns>
    protected sealed override async ValueTask ProcessMessageAsync(
        TMessage message,
        CancellationToken cancellationToken)
    {
        TState stateSnapshot = _state;
        TState? newState = await ProcessMessageWithStateAsync(
            message,
            stateSnapshot,
            cancellationToken).ConfigureAwait(false);

        if (newState != null)
        {
            _state = newState;
            await OnStateUpdatedAsync(newState, cancellationToken).ConfigureAwait(false);
        }
    }

    /// <summary>
    /// Processes a single message with access to the current state snapshot.
    /// Return null to indicate no state change; return a new state to apply the update.
    /// Exceptions implicitly roll back state (no partial update is applied).
    /// </summary>
    /// <param name="message">The input message to process.</param>
    /// <param name="currentState">Snapshot of the current state at message arrival time.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>New state to apply, or null for no state change.</returns>
    protected abstract ValueTask<TState?> ProcessMessageWithStateAsync(
        TMessage message,
        TState currentState,
        CancellationToken cancellationToken);

    /// <summary>
    /// Runs after a new state has been committed to the machine.
    /// The default implementation does nothing.
    /// </summary>
    /// <param name="state">The state value that was just committed.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>A task that completes when post-commit work is done.</returns>
    protected virtual ValueTask OnStateUpdatedAsync(TState state, CancellationToken cancellationToken)
    {
        _ = state;
        _ = cancellationToken;
        return ValueTask.CompletedTask;
    }

}
