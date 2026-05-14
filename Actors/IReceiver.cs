namespace Actors;

/// <summary>
/// Represents a message receiving actor that owns its input mailbox
/// Extends <see cref="IActor{TInput}"/> with message receiving idiomatic semantics
/// </summary>
/// <typeparam name="TMessage">Input message type consumed from the actor's mailbox.</typeparam>
public interface IReceiver<TMessage>
    : IActor<TMessage>
{
}
