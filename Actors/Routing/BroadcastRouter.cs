using Actors.Errors;
using Actors.Mailbox;

namespace Actors.Routing;

/// <summary>
/// Router actor that broadcasts messages to a collection of existing actors
/// </summary>
/// <typeparam name="TMessage">The type of message to broadcast</typeparam>
public sealed class BroadcastRouter<TMessage>
    : ActorPool<TMessage>
{
    /// <summary>
    /// Construct a new instance of the broadcast router
    /// </summary>
    /// <param name="actors">The actors to broadcast messages to</param>
    /// <param name="errorActor">The actor to send errors to for any further action</param>
    /// <param name="mailboxProvider">The provider used to create the mailbox for the broadcast router</param>
    public BroadcastRouter(
        IEnumerable<IActorRef<TMessage>> actors,
        IActorRef<StandardError>? errorActor = null,
        IMailboxProvider? mailboxProvider = null)
        : base(actors, errorActor, mailboxProvider)
    {
    }

    /// <summary>
    /// Process the next message in the mailbox
    /// </summary>
    /// <param name="message">The message to process</param>
    /// <param name="cancellationToken">The token used to cancel the asynchronous operation</param>
    /// <returns>A Task representing the asynchronous operation.</returns>
    protected override async ValueTask ProcessMessageAsync(
        TMessage message, CancellationToken cancellationToken)
    {
        if (Actors.Count < 1)
        {
            return;
        }

        var tasks = new Task[Actors.Count];
        for (int i = 0; i < Actors.Count; ++i)
        {
            IActorRef<TMessage> actor = Actors[i];
            tasks[i] = actor.SendAsync(message, cancellationToken).AsTask();
        }
        await Task.WhenAll(tasks).ConfigureAwait(false);
    }
}
