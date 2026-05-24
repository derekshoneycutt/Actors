using Actors.Errors;
using Actors.Mailbox;
using Actors.Policies.RestartPolicies;

namespace Actors.Routing;

/// <summary>
/// Router actor used to route messages to spawned child actors based on a consistent hash function
/// </summary>
/// <typeparam name="TMessage">The type of message to broadcast</typeparam>
public sealed class SpawningConsistentHashRouter<TActor, TMessage>
    : SpawningActorPool<TActor, TMessage>
    where TActor : class, IActor<TMessage>
{
    /// <summary>
    /// The function used to get a consistent hash
    /// </summary>
    private readonly Func<TMessage, int> _keySelector;

    /// <summary>
    /// Construct a new consistent hash router actor.
    /// </summary>
    /// <param name="size">The size of the pool; the number of actors to spawn</param>
    /// <param name="keySelector">The function used to get a consistent hash</param>
    /// <param name="host">The host supervisor to spawn actors on</param>
    /// <param name="childRestartPolicy">The policy used to handle child actor restarts</param>
    /// <param name="errorActor">The actor to send errors to for any further action</param>
    /// <param name="mailboxProvider">The provider used to construct the router's mailbox</param>
    public SpawningConsistentHashRouter(
        int size,
        Func<TMessage, int> keySelector,
        ISupervisor host,
        IRestartPolicy? childRestartPolicy = null,
        IActorRef<StandardError>? errorActor = null,
        IMailboxProvider? mailboxProvider = null)
        : base(size, host, childRestartPolicy, errorActor, mailboxProvider)
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
