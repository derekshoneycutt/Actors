namespace Actors;

/// <summary>
/// Represents a stateful actor that owns its input mailbox and manages internal state
/// across successive message-processing cycles.
/// Extends <see cref="IReceiver{TInput}"/> with disposable lifecycle semantics
/// covering state resources.
/// </summary>
/// <typeparam name="TMessage">Input message type consumed from the actor's mailbox.</typeparam>
/// <typeparam name="TState">State type maintained across processing cycles.</typeparam>
public interface IMachine<TMessage, TState>
    : IReceiver<TMessage>
    where TState : MachineState
{
}
