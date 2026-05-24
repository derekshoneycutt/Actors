using Actors.Errors;
using Actors.Mailbox;
namespace Actors.Routing;

/// <summary>
/// Router actor used to route messages to other actors based on a consistent hash function
/// </summary>
/// <typeparam name="TMessage">The type of message to broadcast</typeparam>
public sealed class ConsistentHashRouter<TMessage>
    : ActorPool<TMessage>
{
    /// <summary>
    /// The function used to get a consistent hash
    /// </summary>
    private readonly Func<TMessage, int> _keySelector;

    /// <summary>
    /// Construct a new consistent hash router actor.
    /// </summary>
    /// <param name="actors">The actors that the router should route messages to</param>
    /// <param name="keySelector">The function used to get a consistent hash</param>
    /// <param name="errorActor">The actor to send errors to for any further action</param>
    /// <param name="mailboxProvider">The provider used to construct the router's mailbox</param>
    public ConsistentHashRouter(
        IEnumerable<IActorRef<TMessage>> actors,
        Func<TMessage, int> keySelector,
        IActorRef<StandardError>? errorActor = null,
        IMailboxProvider? mailboxProvider = null)
        : base(actors, errorActor, mailboxProvider)
    {
        _keySelector = keySelector;
    }

    /// <summary>
    /// Process a new message, routing it to the appropriate actor
    /// </summary>
    /// <param name="message">The message to process</param>
    /// <param name="cancellationToken">The token used to signal cancellation in the process</param>
    /// <returns>A Task representing the asynchronous operation.</returns>
    protected override async ValueTask ProcessMessageAsync(
        TMessage message, CancellationToken cancellationToken)
    {
        if (Actors.Count < 1)
        {
            return;
        }

        int hashKey = Math.Abs(_keySelector?.Invoke(message) ?? 0) % Actors.Count;

        IActorRef<TMessage> actor = Actors[hashKey];
        await actor.SendAsync(message, cancellationToken);
    }
}
