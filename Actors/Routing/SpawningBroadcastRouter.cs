using Actors.Errors;
using Actors.Mailbox;
using Actors.Policies.RestartPolicies;

namespace Actors.Routing;

/// <summary>
/// Router actor that broadcasts messages to a collection of spawned child actors
/// </summary>
/// <typeparam name="TMessage">The type of message to broadcast</typeparam>
public sealed class SpawningBroadcastRouter<TActor, TMessage>
    : SpawningActorPool<TActor, TMessage>
    where TActor : class, IActor<TMessage>
{
    /// <summary>
    /// Construct a new instance of the spawning broadcast router
    /// </summary>
    /// <param name="size">The size of the actor pool to create</param>
    /// <param name="host">The host supervisor to spawn child actors with</param>
    /// <param name="childRestartPolicy">The policy used to handle child actor restarts</param>
    /// <param name="errorActor">The actor to send errors to for any further action</param>
    /// <param name="mailboxProvider">The provider used to create the router's mailbox</param>
    public SpawningBroadcastRouter(
        int size,
        ISupervisor host,
        IRestartPolicy? childRestartPolicy = null,
        IActorRef<StandardError>? errorActor = null,
        IMailboxProvider? mailboxProvider = null)
        : base(size, host, childRestartPolicy, errorActor, mailboxProvider)
    {
    }

    /// <summary>
    /// Process the next message in the mailbox
    /// </summary>
    /// <param name="message">The message to process</param>
    /// <param name="cancellationToken">The token used to cancel the asynchronous operation</param>
    /// <returns>A Task representing the asynchronous operation.</returns>
    protected override async Task ProcessMessageAsync(
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
            tasks[i] = actor.SendAsync(message, cancellationToken);
        }
        await Task.WhenAll(tasks).ConfigureAwait(false);
    }
}
