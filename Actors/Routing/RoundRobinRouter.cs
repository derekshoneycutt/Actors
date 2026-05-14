using Actors.Errors;
using Actors.Mailbox;

namespace Actors.Routing;

/// <summary>
/// Router actor used to route messages to other actors in a round robin fashion
/// </summary>
/// <typeparam name="TMessage">The type of message to broadcast</typeparam>
public sealed class RoundRobinRouter<TMessage>
    : ActorPool<TMessage>
{
    /// <summary>
    /// The next target to hit in the round robin
    /// </summary>
    private int _nextTarget = -1;

    /// <summary>
    /// Construct a new round robin router actor.
    /// </summary>
    /// <param name="actors">The actors that the router should route messages to</param>
    /// <param name="errorActor">The actor to send errors to for any further action</param>
    /// <param name="mailboxProvider">The provider used to construct the router's mailbox</param>
    public RoundRobinRouter(
        IEnumerable<IActorRef<TMessage>> actors,
        IActorRef<StandardError>? errorActor = null,
        IMailboxProvider? mailboxProvider = null)
        : base(actors, errorActor, mailboxProvider)
    {
    }

    /// <summary>
    /// Process the next message in the mailbox, sending it to the next target
    /// </summary>
    /// <param name="message">The message to process</param>
    /// <param name="cancellationToken">The token used to cancel the asynchronous operation</param>
    /// <returns>A Task that represents the asynchronous operation</returns>
    protected override async Task ProcessMessageAsync(
        TMessage message, CancellationToken cancellationToken)
    {
        if (Actors.Count < 1)
        {
            return;
        }

        int ticked = Interlocked.Increment(ref _nextTarget) % Actors.Count;

        IActorRef<TMessage> actor = Actors[ticked];
        await actor.SendAsync(message, cancellationToken);
    }
}
