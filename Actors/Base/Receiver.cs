using Actors.Errors;
using Actors.Mailbox;

namespace Actors.Base;

/// <summary>
/// Generic base class for actors that own their input mailbox internally.
/// </summary>
/// <typeparam name="TMessage">Input message type consumed from the actor's mailbox.</typeparam>
public abstract class Receiver<TMessage>
    : Actor<TMessage>, IReceiver<TMessage>
{
    /// <summary>
    /// The cancellation token source used to signal an end to the reception loop safely
    /// </summary>
    private CancellationTokenSource? _receptionLoopEndSignal;

    /// <summary>
    /// Gets the actor used to handle errors that occur in the reception
    /// </summary>
    protected IActorRef<StandardError>? ErrorActor { get; private init; }

    /// <summary>
    /// Initializes a receiver actor with an error routing reference.
    /// </summary>
    /// <param name="errorActor">The actor to send errors to for any further action</param>
    /// <param name="mailboxChannelProvider">
    /// Optional provider used to create the actor mailbox channel.
    /// Uses the default unbounded provider when omitted.
    /// </param>
    protected Receiver(
        IActorRef<StandardError>? errorActor = null,
        IMailboxProvider? mailboxChannelProvider = null)
        : base(mailboxChannelProvider)
    {
        ErrorActor = errorActor;
        _receptionLoopEndSignal = new();
    }

    /// <summary>
    /// Runs the actor's mailbox processing loop until the mailbox is
    /// drained and completed.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token controlling actor shutdown.</param>
    /// <returns>A task that completes when all mailbox messages are processed.</returns>
    public override async Task RunAsync(CancellationToken cancellationToken)
    {
        try
        {
            await InitializeRunAsync(cancellationToken).ConfigureAwait(false);

            await foreach (TMessage message in
                ReceiveAllAsync(cancellationToken).ConfigureAwait(false))
            {
                await HandleMessageProcessingAsync(message, cancellationToken)
                    .ConfigureAwait(false);
            }
        }
        finally
        {
            FinalizeRun(cancellationToken);
        }
    }

    /// <summary>
    /// Handles the processing of a single message, including telemetry and error handling.
    /// </summary>
    /// <param name="message">The message to process.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>A task that completes when processing is done.</returns>
    private async Task HandleMessageProcessingAsync(
        TMessage message,
        CancellationToken cancellationToken)
    {
        try
        {
            using var mergedToken = CancellationTokenSource.CreateLinkedTokenSource(
                cancellationToken, _receptionLoopEndSignal?.Token ?? CancellationToken.None);

            await ProcessMessageAsync(message, mergedToken.Token).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
            when (_receptionLoopEndSignal?.IsCancellationRequested ?? false)
        {
            // We just end safely when the reception loop signal is canceled
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            await HandleProcessingErrorAsync(message, ex, cancellationToken)
                .ConfigureAwait(false);

            ReceiverErrorAction action = DecideErrorAction(
                message, ex, cancellationToken);
            if (action == ReceiverErrorAction.Fail)
            {
                throw;
            }
        }
    }

    /// <summary>
    /// Processes a single message from the mailbox.
    /// Subclasses implement domain-specific message handling here.
    /// </summary>
    /// <param name="message">The message to process.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>A task that completes when processing is done.</returns>
    protected abstract Task ProcessMessageAsync(
        TMessage message,
        CancellationToken cancellationToken);

    /// <summary>
    /// Handles uncaught exceptions during message processing.
    /// </summary>
    /// <param name="message">The message being processed when the exception occurred.</param>
    /// <param name="exception">The exception that was thrown.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>A task that completes when error handling is done.</returns>
    protected virtual async Task HandleProcessingErrorAsync(
        TMessage message,
        Exception exception,
        CancellationToken cancellationToken)
    {
        _ = message;
        if (ErrorActor is not null)
        {
            StandardError error = new(
                ActorKind: GetType().Name,
                Message: $"Actor failed processing message type '{typeof(TMessage).Name}': {exception.Message}",
                Exception: exception,
                OccurredAtUtc: DateTimeOffset.UtcNow);
            await ErrorActor.SendAsync(error, cancellationToken).ConfigureAwait(false);
        }
    }

    /// <summary>
    /// Decides whether this receiver should continue processing after a handled
    /// message-processing exception.
    /// </summary>
    /// <param name="message">The message being processed when the exception occurred.</param>
    /// <param name="exception">The exception raised during message processing.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>
    /// <see cref="ReceiverErrorAction.Continue"/> to proceed with subsequent messages,
    /// or <see cref="ReceiverErrorAction.Fail"/> to fail the run loop.
    /// </returns>
    protected virtual ReceiverErrorAction DecideErrorAction(
        TMessage message,
        Exception exception,
        CancellationToken cancellationToken)
    {
        _ = message;
        _ = exception;
        _ = cancellationToken;
        return ReceiverErrorAction.Fail;
    }

    /// <summary>
    /// Initializes the actor before starting the run loop.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    protected virtual Task InitializeRunAsync(CancellationToken cancellationToken)
        => Task.CompletedTask;

    /// <summary>
    /// Finalizes the actor after the run loop has completed.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    protected virtual void FinalizeRun(CancellationToken cancellationToken) { }

    /// <summary>
    /// Disposes the actor asynchronously, releasing managed resources.
    /// </summary>
    /// <returns>A value task that completes when disposal is done.</returns>
    protected override async ValueTask DisposeAsyncCore()
    {
        CancellationTokenSource? dispose = _receptionLoopEndSignal;
        if (!IsDisposed && dispose is not null)
        {
            _receptionLoopEndSignal = null;
            await dispose.CancelAsync();
            dispose.Dispose();
        }
        await base.DisposeAsyncCore();
    }

    /// <summary>
    /// Disposes the actor, releasing managed resources if <paramref name="disposing"/> is <c>true</c>.
    /// </summary>
    /// <param name="disposing">Indicates whether to release managed resources.</param>
    protected override void Dispose(bool disposing)
    {
        CancellationTokenSource? dispose = _receptionLoopEndSignal;
        if (disposing && !IsDisposed && dispose is not null)
        {
            _receptionLoopEndSignal = null;
            dispose.Cancel();
            dispose.Dispose();
        }
        base.Dispose(disposing);
    }
}
