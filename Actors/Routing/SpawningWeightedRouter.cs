using Actors.Errors;
using Actors.Mailbox;
using Actors.Policies.RestartPolicies;

namespace Actors.Routing;

/// <summary>
/// Router actor used to route messages to spawned child actors based on weights
/// </summary>
/// <typeparam name="TMessage">The type of message to broadcast</typeparam>
public sealed class SpawningWeightedRouter<TActor, TMessage>
    : SpawningActorPool<TActor, TMessage>
    where TActor : class, IActor<TMessage>
{
    /// <summary>
    /// The expanded list of indices representing the weights
    /// </summary>
    private readonly int[] _expandedIndices;

    /// <summary>
    /// The next target to get from the expanded indices
    /// </summary>
    private int _nextTarget = -1;

    /// <summary>
    /// Construct a new weighted router actor.
    /// </summary>
    /// <param name="weights">The list of weights; one actor is spawned per weight</param>
    /// <param name="host">The host supervisor to spawn actors on</param>
    /// <param name="childRestartPolicy">The policy used to handle child actor restarts</param>
    /// <param name="errorActor">The actor to send errors to for any further action</param>
    /// <param name="mailboxProvider">The provider used to construct the router's mailbox</param>
    public SpawningWeightedRouter(
        int[] weights,
        ISupervisor host,
        IRestartPolicy? childRestartPolicy = null,
        IActorRef<StandardError>? errorActor = null,
        IMailboxProvider? mailboxProvider = null)
        : base(weights.Length, host, childRestartPolicy, errorActor, mailboxProvider)
    {
        _expandedIndices = BuildExpandedIndices(weights);
    }

    /// <summary>
    /// Process the next message in the mailbox, sending it to the next target
    /// </summary>
    /// <param name="message">The message to process</param>
    /// <param name="cancellationToken">The token used to cancel the asynchronous operation</param>
    /// <returns>A Task that represents the asynchronous operation</returns>
    protected override async ValueTask ProcessMessageAsync(
        TMessage message, CancellationToken cancellationToken)
    {
        if (Actors.Count < 1)
        {
            return;
        }

        int expandedIndex =
            Interlocked.Increment(ref _nextTarget) % _expandedIndices.Length;
        int index = _expandedIndices[expandedIndex];

        IActorRef<TMessage> actor = Actors[index];
        await actor.SendAsync(message, cancellationToken);
    }

    /// <summary>
    /// Builds an expanded index array from the weight array.
    /// Target i appears weight[i] times in the result.
    /// </summary>
    /// <param name="weights">Weight values in target order.</param>
    /// <returns>Expanded index array for weighted round-robin selection.</returns>
    private static int[] BuildExpandedIndices(int[] weights)
    {
        int totalSlots = 0;
        foreach (int w in weights)
        {
            totalSlots += w;
        }

        int[] indices = new int[totalSlots];
        int pos = 0;
        for (int i = 0; i < weights.Length; i++)
        {
            for (int j = 0; j < weights[i]; j++)
            {
                indices[pos++] = i;
            }
        }

        return indices;
    }
}
