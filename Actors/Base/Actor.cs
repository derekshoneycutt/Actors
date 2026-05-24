using System.Threading.Channels;
using Actors.Mailbox;

namespace Actors.Base;

/// <summary>
/// Generic base class for actors that own their input mailbox internally.
/// </summary>
/// <typeparam name="TMessage">Input message type consumed from the actor's mailbox.</typeparam>
public abstract class Actor<TMessage>
    : IActor<TMessage>
{
    /// <summary>
    /// The mailbox channel used to deliver messages to this actor.
    /// </summary>
    private readonly Channel<TMessage> _mailbox;

    /// <summary>
    /// Gets whether the actor has been disposed.
    /// </summary>
    public bool IsDisposed { get; private set; }

    /// <summary>
    /// Initializes a mailbox actor with an error routing reference.
    /// </summary>
    /// <param name="mailboxChannelProvider">
    /// Optional provider used to create the actor mailbox channel.
    /// Uses the default unbounded provider when omitted.
    /// </param>
    protected Actor(
        IMailboxProvider? mailboxChannelProvider = null)
    {
        IMailboxProvider provider =
            mailboxChannelProvider ?? MailboxProviders.Unbounded;
        _mailbox = provider.Create<TMessage>(
            new MailboxOptions
            {
                SingleReader = true,
                SingleWriter = false,
            });
    }

    /// <summary>
    /// Runs the actor's mailbox processing loop until the mailbox is
    /// drained and completed.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token controlling actor shutdown.</param>
    /// <returns>A task that completes when all mailbox messages are processed.</returns>
    public abstract ValueTask RunAsync(CancellationToken cancellationToken);

    /// <summary>
    /// Enqueues an input message into this actor's mailbox.
    /// </summary>
    /// <param name="message">Message to enqueue for processing.</param>
    /// <param name="cancellationToken">Cancellation token controlling enqueue behavior.</param>
    /// <returns>A task that completes when the message is accepted.</returns>
    public ValueTask SendAsync(TMessage message, CancellationToken cancellationToken)
    {
        return _mailbox.Writer.WriteAsync(message, cancellationToken);
    }

    /// <summary>
    /// Reads a single message from this actor's mailbox.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token controlling dequeue behavior.</param>
    /// <returns>A task that completes with the next message.</returns>
    protected Task<TMessage> ReceiveAsync(CancellationToken cancellationToken)
    {
        return _mailbox.Reader.ReadAsync(cancellationToken).AsTask();
    }

    /// <summary>
    /// Reads all messages from this actor's mailbox.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token controlling dequeue behavior.</param>
    /// <returns>An async enumerable that yields all messages.</returns>
    protected IAsyncEnumerable<TMessage> ReceiveAllAsync(CancellationToken cancellationToken)
    {
        return _mailbox.Reader.ReadAllAsync(cancellationToken);
    }

    /// <summary>
    /// Close this actor's mailbox
    /// </summary>
    protected void CloseMailbox()
    {
        _ = _mailbox.Writer.TryComplete();
    }

    /// <summary>
    /// Disposes the actor asynchronously.
    /// </summary>
    /// <returns>A value task that completes when disposal is finished.</returns>
    protected virtual ValueTask DisposeAsyncCore()
    {
        if (!IsDisposed)
        {
            _ = _mailbox.Writer.TryComplete();
        }
        Dispose(disposing: false);

        return ValueTask.CompletedTask;
    }

    /// <summary>
    /// Disposes the actor.
    /// </summary>
    /// <param name="disposing">Indicates whether the method is called from Dispose (true) or from a finalizer (false).</param>
    protected virtual void Dispose(bool disposing)
    {
        if (!IsDisposed)
        {
            if (disposing)
            {
                _ = _mailbox.Writer.TryComplete();
            }
            IsDisposed = true;
        }
    }

    /// <summary>
    /// Disposes the actor asynchronously.
    /// </summary>
    /// <returns>A value task that completes when disposal is finished.</returns>
    public async ValueTask DisposeAsync()
    {
        await DisposeAsyncCore();
        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// Disposes the actor synchronously.
    /// </summary>
    public void Dispose()
    {
        // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
        Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }
}
