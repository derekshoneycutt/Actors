using Actors.Base;
using Actors.Errors;
using Actors.Mailbox;

namespace Actors.Routing;

/// <summary>
/// Base class for an actor that manages sending messages to a pool of actors
/// </summary>
/// <typeparam name="TMessage">The type of message sent over the pool of actors</typeparam>
public abstract class ActorPool<TMessage>
    : Receiver<TMessage>
{
    /// <summary>
    /// The actors that messages are to be forwarded to
    /// </summary>
    private readonly List<IActorRef<TMessage>> _actors;

    /// <summary>
    /// Gets the actors that messages are to be forwarded to
    /// </summary>
    protected IReadOnlyList<IActorRef<TMessage>> Actors => _actors;

    /// <summary>
    /// Abstract constructor for a new instance of the actor pool
    /// </summary>
    /// <param name="actors">The actors to use in the pool</param>
    /// <param name="errorActor">The actor to send errors to for any further action</param>
    /// <param name="mailboxChannelProvider">The provider used to create the mailbox for the actor pool</param>
    public ActorPool(
        IEnumerable<IActorRef<TMessage>> actors,
        IActorRef<StandardError>? errorActor = null,
        IMailboxProvider? mailboxChannelProvider = null)
        : base(errorActor, mailboxChannelProvider)
    {
        _actors = [.. actors];
        if (_actors.Count <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(actors), "Size must be greater than zero.");
        }
    }

    /// <summary>
    /// Disposes the actor asynchronously.
    /// </summary>
    /// <returns>A value task that completes when disposal is finished.</returns>
    protected override async ValueTask DisposeAsyncCore()
    {
        if (!IsDisposed)
        {
            _actors.Clear();
        }
        await base.DisposeAsyncCore();
    }

    /// <summary>
    /// Disposes the actor.
    /// </summary>
    /// <param name="disposing">Indicates whether the method is called from Dispose (true) or from a finalizer (false).</param>
    protected override void Dispose(bool disposing)
    {
        if (!IsDisposed)
        {
            if (disposing)
            {
                _actors.Clear();
            }
            base.Dispose(disposing);
        }
    }
}
