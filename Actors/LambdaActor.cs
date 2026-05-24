using Actors.Base;
using Actors.Errors;
using Actors.Mailbox;

namespace Actors;

/// <summary>
/// A stateless actor whose behavior is defined by a delegate rather than inheritance.
/// </summary>
/// <typeparam name="TMessage">Input message type consumed from the actor's mailbox.</typeparam>
public sealed class LambdaActor<TMessage>
    : Receiver<TMessage>
{
    /// <summary>
    /// The delegate invoked to handle each incoming message.
    /// </summary>
    private readonly Func<LambdaActor<TMessage>, TMessage, ISupervisor,
        CancellationToken, ValueTask> _handler;

    /// <summary>
    /// The actor host to allow function with in the lambda
    /// </summary>
    private readonly ISupervisor _host;

    /// <summary>
    /// Initializes a lambda actor with a message handler.
    /// </summary>
    /// <param name="handler">Async delegate invoked for each message in the mailbox.</param>
    /// <param name="host">The actor host to allow funciton with in the lambda.</param>
    /// <param name="errorActor">The actor to send errors to for any further action</param>
    /// <param name="mailboxChannelProvider">
    /// Optional provider used to create the actor mailbox channel.
    /// Uses the default unbounded provider when omitted.
    /// </param>
    public LambdaActor(
        Func<LambdaActor<TMessage>, TMessage, ISupervisor, CancellationToken, ValueTask> handler,
        ISupervisor host,
        IActorRef<StandardError>? errorActor = null,
        IMailboxProvider? mailboxChannelProvider = null)
        : base(errorActor, mailboxChannelProvider)
    {
        ArgumentNullException.ThrowIfNull(handler);
        ArgumentNullException.ThrowIfNull(host);
        _handler = handler;
        _host = host;
    }

    /// <summary>
    /// Processes a message by invoking the user-provided handler delegate.
    /// </summary>
    /// <param name="message">The input message to process.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>A task that completes when the handler completes.</returns>
    protected override ValueTask ProcessMessageAsync(
        TMessage message,
        CancellationToken cancellationToken)
    {
        return _handler(this, message, _host, cancellationToken);
    }
}
