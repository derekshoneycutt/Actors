namespace Actors;

/// <summary>
/// Interface describing the core actor lifetime, without a reference to its mailbox.
/// </summary>
public interface IActor
    : IDisposable, IAsyncDisposable
{
    /// <summary>
    /// Runs the actor's mailbox processing loop until the mailbox is drained and completed.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token controlling actor shutdown.</param>
    /// <returns>A task that completes when all mailbox messages are processed.</returns>
    Task RunAsync(CancellationToken cancellationToken);
}

/// <summary>
/// Represents an actor that owns its input mailbox and processes messages asynchronously.
/// </summary>
/// <typeparam name="TMessage">Input message type consumed from the actor's mailbox.</typeparam>
public interface IActor<in TMessage>
    : IActor
{
    /// <summary>
    /// Enqueues an input message into this actor's mailbox.
    /// </summary>
    /// <param name="message">Message to enqueue for processing.</param>
    /// <param name="cancellationToken">Cancellation token controlling enqueue behavior.</param>
    /// <returns>A task that completes when the message is accepted.</returns>
    Task SendAsync(TMessage message, CancellationToken cancellationToken);
}
