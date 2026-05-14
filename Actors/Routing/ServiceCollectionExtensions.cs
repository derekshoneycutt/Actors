using Actors.Supervising;
using Actors.Mailbox;
using Actors.Policies.RestartPolicies;
using Microsoft.Extensions.DependencyInjection;
using Actors.Errors;

namespace Actors.Routing;

/// <summary>
/// Service-collection extensions for keyed actor registration and hosted runtime wiring.
/// </summary>
public static class ActorServiceCollectionExtensions
{
    /// <summary>
    /// Add a new broadcasting actor pool in the service collection
    /// </summary>
    /// <typeparam name="TActor">The type of actor to create the pool for</typeparam>
    /// <typeparam name="TMessage">The type of message to send over the </typeparam>
    /// <param name="services">The service collection to add the actor pool to</param>
    /// <param name="address">The address to use for the new actor pool</param>
    /// <param name="size">The size of the pool to create</param>
    /// <param name="childRestartPolicy">The policy to apply to child restarts</param>
    /// <param name="mailboxProvider">The provider used to create the mailbox on the actor pool</param>
    /// <param name="configureOptions">Optional delegate used to configure the actor pool further</param>
    /// <returns>The service collection for chaining</returns>
    public static IServiceCollection AddBroadcastActorPool<TActor, TMessage>(
        this IServiceCollection services,
        string address,
        int size,
        IRestartPolicy? childRestartPolicy = null,
        IMailboxProvider? mailboxProvider = null,
        Action<SupervisedActorOptions>? configureOptions = null)
        where TActor : class, IActor<TMessage>
    {
        return services.AddActor<SpawningBroadcastRouter<TActor, TMessage>, TMessage>(
            address,
            sp => new SpawningBroadcastRouter<TActor, TMessage>(
                    size, sp.GetRequiredService<ISupervisor>(),
                    childRestartPolicy,
                    sp.GetKeyedService<IActorRef<StandardError>>("actor://error"),
                    mailboxProvider),
            configureOptions);
    }

    /// <summary>
    /// Add a new round robin actor pool in the service collection
    /// </summary>
    /// <typeparam name="TActor">The type of actor to create the pool for</typeparam>
    /// <typeparam name="TMessage">The type of message to send over the </typeparam>
    /// <param name="services">The service collection to add the actor pool to</param>
    /// <param name="address">The address to use for the new actor pool</param>
    /// <param name="size">The size of the pool to create</param>
    /// <param name="childRestartPolicy">The policy to apply to child restarts</param>
    /// <param name="mailboxProvider">The provider used to create the mailbox on the actor pool</param>
    /// <param name="configureOptions">Optional delegate used to configure the actor pool further</param>
    /// <returns>The service collection for chaining</returns>
    public static IServiceCollection AddRoundRobinActorPool<TActor, TMessage>(
        this IServiceCollection services,
        string address,
        int size,
        IRestartPolicy? childRestartPolicy = null,
        IMailboxProvider? mailboxProvider = null,
        Action<SupervisedActorOptions>? configureOptions = null)
        where TActor : class, IActor<TMessage>
    {
        return services.AddActor<SpawningRoundRobinRouter<TActor, TMessage>, TMessage>(
            address,
            sp => new SpawningRoundRobinRouter<TActor, TMessage>(
                    size, sp.GetRequiredService<ISupervisor>(),
                    childRestartPolicy,
                    sp.GetKeyedService<IActorRef<StandardError>>("actor://error"),
                    mailboxProvider),
            configureOptions);
    }

    /// <summary>
    /// Add a new weighted actor pool in the service collection
    /// </summary>
    /// <typeparam name="TActor">The type of actor to create the pool for</typeparam>
    /// <typeparam name="TMessage">The type of message to send over the </typeparam>
    /// <param name="services">The service collection to add the actor pool to</param>
    /// <param name="address">The address to use for the new actor pool</param>
    /// <param name="weights">The weights of the pool to create; one actor is created per weight</param>
    /// <param name="childRestartPolicy">The policy to apply to child restarts</param>
    /// <param name="mailboxProvider">The provider used to create the mailbox on the actor pool</param>
    /// <param name="configureOptions">Optional delegate used to configure the actor pool further</param>
    /// <returns>The service collection for chaining</returns>
    public static IServiceCollection AddWeightedActorPool<TActor, TMessage>(
        this IServiceCollection services,
        string address,
        int[] weights,
        IRestartPolicy? childRestartPolicy = null,
        IMailboxProvider? mailboxProvider = null,
        Action<SupervisedActorOptions>? configureOptions = null)
        where TActor : class, IActor<TMessage>
    {
        return services.AddActor<SpawningWeightedRouter<TActor, TMessage>, TMessage>(
            address,
            sp => new SpawningWeightedRouter<TActor, TMessage>(
                    weights, sp.GetRequiredService<ISupervisor>(),
                    childRestartPolicy,
                    sp.GetKeyedService<IActorRef<StandardError>>("actor://error"),
                    mailboxProvider),
            configureOptions);
    }

    /// <summary>
    /// Add a new consistent hash actor pool in the service collection
    /// </summary>
    /// <typeparam name="TActor">The type of actor to create the pool for</typeparam>
    /// <typeparam name="TMessage">The type of message to send over the </typeparam>
    /// <param name="services">The service collection to add the actor pool to</param>
    /// <param name="address">The address to use for the new actor pool</param>
    /// <param name="size">The size of the pool to create</param>
    /// <param name="keySelector">The </param>
    /// <param name="childRestartPolicy">The policy to apply to child restarts</param>
    /// <param name="mailboxProvider">The provider used to create the mailbox on the actor pool</param>
    /// <param name="configureOptions">Optional delegate used to configure the actor pool further</param>
    /// <returns>The service collection for chaining</returns>
    public static IServiceCollection AddConsistentHashActorPool<TActor, TMessage>(
        this IServiceCollection services,
        string address,
        int size,
        Func<TMessage, int> keySelector,
        IRestartPolicy? childRestartPolicy = null,
        IMailboxProvider? mailboxProvider = null,
        Action<SupervisedActorOptions>? configureOptions = null)
        where TActor : class, IActor<TMessage>
    {
        return services.AddActor<SpawningConsistentHashRouter<TActor, TMessage>, TMessage>(
            address,
            sp => new SpawningConsistentHashRouter<TActor, TMessage>(
                    size, keySelector, sp.GetRequiredService<ISupervisor>(),
                    childRestartPolicy,
                    sp.GetKeyedService<IActorRef<StandardError>>("actor://error"),
                    mailboxProvider),
            configureOptions);
    }
}
