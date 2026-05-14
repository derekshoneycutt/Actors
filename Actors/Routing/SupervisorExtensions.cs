using Actors.Errors;
using Actors.Mailbox;
using Actors.Policies.RestartPolicies;
using Microsoft.Extensions.DependencyInjection;

namespace Actors.Routing;

/// <summary>
/// Extension class to the Supervisor actors to spawn actor pools
/// </summary>
public static class SupervisorExtensions
{
    /// <summary>
    /// Spawn a new broadcast actor pool
    /// </summary>
    /// <typeparam name="TActor">The type of actors to spawn in the pool</typeparam>
    /// <typeparam name="TMessage">The type of message that the pooled actors receive</typeparam>
    /// <param name="supervisor">The supervisor to spawn the pool on</param>
    /// <param name="address">The address to use for the new pool actor</param>
    /// <param name="size">The size of the pool to spawn</param>
    /// <param name="childRestartPolicy">The policy to use for restarting children in the pool</param>
    /// <param name="mailboxProvider">The provider used to create the pool mailbox</param>
    /// <returns>The spawned actor's reference</returns>
    public static IActorRef<TMessage> SpawnBroadcastActorPool<TActor, TMessage>(
        this ISupervisor supervisor,
        string address,
        int size,
        IRestartPolicy? childRestartPolicy = null,
        IMailboxProvider? mailboxProvider = null)
        where TActor : class, IActor<TMessage>
    {
        return supervisor.Spawn<SpawningBroadcastRouter<TActor, TMessage>, TMessage>(
            address,
            (sup, sp) => new SpawningBroadcastRouter<TActor, TMessage>(
                size, sup, childRestartPolicy,
                sp.GetKeyedService<IActorRef<StandardError>>("actor://error"),
                mailboxProvider));
    }

    /// <summary>
    /// Spawn a new broadcast actor pool
    /// </summary>
    /// <typeparam name="TMessage">The type of message that the pooled actors receive</typeparam>
    /// <param name="supervisor">The supervisor to spawn the pool on</param>
    /// <param name="address">The address to use for the new pool actor</param>
    /// <param name="actors">The actors to send messages to from the actor pool</param>
    /// <param name="mailboxProvider">The provider used to create the pool mailbox</param>
    /// <returns>The spawned actor's reference</returns>
    public static IActorRef<TMessage> SpawnBroadcastActorPool<TMessage>(
        this ISupervisor supervisor,
        string address,
        IEnumerable<IActorRef<TMessage>> actors,
        IMailboxProvider? mailboxProvider = null)
    {
        return supervisor.Spawn<BroadcastRouter<TMessage>, TMessage>(
            address,
            (sup, sp) => new BroadcastRouter<TMessage>(
                actors,
                sp.GetKeyedService<IActorRef<StandardError>>("actor://error"),
                mailboxProvider));
    }

    /// <summary>
    /// Spawn a new round robin actor pool
    /// </summary>
    /// <typeparam name="TActor">The type of actors to spawn in the pool</typeparam>
    /// <typeparam name="TMessage">The type of message that the pooled actors receive</typeparam>
    /// <param name="supervisor">The supervisor to spawn the pool on</param>
    /// <param name="address">The address to use for the new pool actor</param>
    /// <param name="size">The size of the pool to spawn</param>
    /// <param name="childRestartPolicy">The policy to use for restarting children in the pool</param>
    /// <param name="mailboxProvider">The provider used to create the pool mailbox</param>
    /// <returns>The spawned actor's reference</returns>
    public static IActorRef<TMessage> SpawnRoundRobinActorPool<TActor, TMessage>(
        this ISupervisor supervisor,
        string address,
        int size,
        IRestartPolicy? childRestartPolicy = null,
        IMailboxProvider? mailboxProvider = null)
        where TActor : class, IActor<TMessage>
    {
        return supervisor.Spawn<SpawningRoundRobinRouter<TActor, TMessage>, TMessage>(
            address,
            (sup, sp) => new SpawningRoundRobinRouter<TActor, TMessage>(
                size, sup, childRestartPolicy,
                sp.GetKeyedService<IActorRef<StandardError>>("actor://error"),
                mailboxProvider));
    }

    /// <summary>
    /// Spawn a new round robin actor pool
    /// </summary>
    /// <typeparam name="TMessage">The type of message that the pooled actors receive</typeparam>
    /// <param name="supervisor">The supervisor to spawn the pool on</param>
    /// <param name="address">The address to use for the new pool actor</param>
    /// <param name="actors">The actors to send messages to from the actor pool</param>
    /// <param name="mailboxProvider">The provider used to create the pool mailbox</param>
    /// <returns>The spawned actor's reference</returns>
    public static IActorRef<TMessage> SpawnRoundRobinActorPool<TMessage>(
        this ISupervisor supervisor,
        string address,
        IEnumerable<IActorRef<TMessage>> actors,
        IMailboxProvider? mailboxProvider = null)
    {
        return supervisor.Spawn<RoundRobinRouter<TMessage>, TMessage>(
            address,
            (sup, sp) => new RoundRobinRouter<TMessage>(
                actors,
                sp.GetKeyedService<IActorRef<StandardError>>("actor://error"),
                mailboxProvider));
    }

    /// <summary>
    /// Spawn a new weighted actor pool
    /// </summary>
    /// <typeparam name="TActor">The type of actors to spawn in the pool</typeparam>
    /// <typeparam name="TMessage">The type of message that the pooled actors receive</typeparam>
    /// <param name="supervisor">The supervisor to spawn the pool on</param>
    /// <param name="address">The address to use for the new pool actor</param>
    /// <param name="weights">The weights of the pool to spawn; one actor is spawned per weight</param>
    /// <param name="childRestartPolicy">The policy to use for restarting children in the pool</param>
    /// <param name="mailboxProvider">The provider used to create the pool mailbox</param>
    /// <returns>The spawned actor's reference</returns>
    public static IActorRef<TMessage> SpawnWeightedActorPool<TActor, TMessage>(
        this ISupervisor supervisor,
        string address,
        int[] weights,
        IRestartPolicy? childRestartPolicy = null,
        IMailboxProvider? mailboxProvider = null)
        where TActor : class, IActor<TMessage>
    {
        return supervisor.Spawn<SpawningWeightedRouter<TActor, TMessage>, TMessage>(
            address,
            (sup, sp) => new SpawningWeightedRouter<TActor, TMessage>(
                weights, sup, childRestartPolicy,
                sp.GetKeyedService<IActorRef<StandardError>>("actor://error"),
                mailboxProvider));
    }

    /// <summary>
    /// Spawn a new weighted actor pool
    /// </summary>
    /// <typeparam name="TMessage">The type of message that the pooled actors receive</typeparam>
    /// <param name="supervisor">The supervisor to spawn the pool on</param>
    /// <param name="address">The address to use for the new pool actor</param>
    /// <param name="actors">The actors to send messages to from the actor pool, and the associated weight for each</param>
    /// <param name="mailboxProvider">The provider used to create the pool mailbox</param>
    /// <returns>The spawned actor's reference</returns>
    public static IActorRef<TMessage> SpawnWeightedActorPool<TMessage>(
        this ISupervisor supervisor,
        string address,
        IEnumerable<(IActorRef<TMessage>, int)> actors,
        IMailboxProvider? mailboxProvider = null)
    {
        return supervisor.Spawn<WeightedRouter<TMessage>, TMessage>(
            address,
            (sup, sp) => new WeightedRouter<TMessage>(
                actors,
                sp.GetKeyedService<IActorRef<StandardError>>("actor://error"),
                mailboxProvider));
    }

    /// <summary>
    /// Spawn a new consistent hash actor pool
    /// </summary>
    /// <typeparam name="TActor">The type of actors to spawn in the pool</typeparam>
    /// <typeparam name="TMessage">The type of message that the pooled actors receive</typeparam>
    /// <param name="supervisor">The supervisor to spawn the pool on</param>
    /// <param name="address">The address to use for the new pool actor</param>
    /// <param name="size">The size of the pool to spawn</param>
    /// <param name="keySelector">Selector function used to get the hashed key from a message</param>
    /// <param name="childRestartPolicy">The policy to use for restarting children in the pool</param>
    /// <param name="mailboxProvider">The provider used to create the pool mailbox</param>
    /// <returns>The spawned actor's reference</returns>
    public static IActorRef<TMessage> SpawnConsistentHashActorPool<TActor, TMessage>(
        this ISupervisor supervisor,
        string address,
        int size,
        Func<TMessage, int> keySelector,
        IRestartPolicy? childRestartPolicy = null,
        IMailboxProvider? mailboxProvider = null)
        where TActor : class, IActor<TMessage>
    {
        return supervisor.Spawn<SpawningConsistentHashRouter<TActor, TMessage>, TMessage>(
            address,
            (sup, sp) => new SpawningConsistentHashRouter<TActor, TMessage>(
                size, keySelector, sup, childRestartPolicy,
                sp.GetKeyedService<IActorRef<StandardError>>("actor://error"),
                mailboxProvider));
    }

    /// <summary>
    /// Spawn a new consistent hash actor pool
    /// </summary>
    /// <typeparam name="TMessage">The type of message that the pooled actors receive</typeparam>
    /// <param name="supervisor">The supervisor to spawn the pool on</param>
    /// <param name="address">The address to use for the new pool actor</param>
    /// <param name="actors">The actors to send messages to from the actor pool, and the associated weight for each</param>
    /// <param name="keySelector">Selector function used to get the hashed key from a message</param>
    /// <param name="mailboxProvider">The provider used to create the pool mailbox</param>
    /// <returns>The spawned actor's reference</returns>
    public static IActorRef<TMessage> SpawnConsistentHashActorPool<TMessage>(
        this ISupervisor supervisor,
        string address,
        IEnumerable<IActorRef<TMessage>> actors,
        Func<TMessage, int> keySelector,
        IMailboxProvider? mailboxProvider = null)
    {
        return supervisor.Spawn<ConsistentHashRouter<TMessage>, TMessage>(
            address,
            (sup, sp) => new ConsistentHashRouter<TMessage>(
                actors, keySelector,
                sp.GetKeyedService<IActorRef<StandardError>>("actor://error"),
                mailboxProvider));
    }
}
