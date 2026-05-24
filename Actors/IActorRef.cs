namespace Actors;

/// <summary>
/// Core interface describing a reference to an actor, given primarily its address.
/// </summary>
public interface IActorRef
{
    /// <summary>
    /// Gets the address referencing the actor for message delivery and diagnostics.
    /// </summary>
    string Address { get; }
}

/// <summary>
/// Typed actor reference that accepts messages of type
/// <typeparamref name="TMessage"/>.
/// Enables type-safe message delivery to an actor's mailbox without
/// exposing the mailbox channel directly.
/// </summary>
/// <typeparam name="TMessage">Type of messages accepted by the target actor.</typeparam>
public interface IActorRef<in TMessage>
    : IActorRef
{
    /// <summary>
    /// Enqueues a message in the target actor's mailbox.
    /// Returns when the message is accepted; processing is asynchronous.
    /// </summary>
    /// <param name="message">Message to deliver to the target actor.</param>
    /// <param name="cancellationToken">
    /// Cancellation token controlling the enqueue operation.
    /// </param>
    /// <returns>A task that completes when the message is enqueued.</returns>
    ValueTask SendAsync(TMessage message, CancellationToken cancellationToken);
}
