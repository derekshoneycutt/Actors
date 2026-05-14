using Actors.Host;
using Actors.Supervising;
using Actors.Mailbox;
using Microsoft.Extensions.DependencyInjection;
using Actors.Errors;

namespace Actors;

/// <summary>
/// Service-collection extensions for keyed actor registration and hosted runtime wiring.
/// </summary>
public static class ActorServiceCollectionExtensions
{
    /// <summary>
    /// Add a hosted actor to dependency injection
    /// </summary>
    /// <typeparam name="TActor">The type of actor to add</typeparam>
    /// <typeparam name="TMessage">The message type that the actor receives</typeparam>
    /// <param name="services">The service collection to add the actor to.</param>
    /// <param name="address">The address to use for the new actor.</param>
    /// <param name="constructor">The constructor to use in building the actor.</param>
    /// <param name="configureOptions">Optional delegate used to configure the settings of the
    ///     actor at construction time.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddActor<TActor, TMessage>(
        this IServiceCollection services,
        string address,
        Func<IServiceProvider, TActor> constructor,
        Action<SupervisedActorOptions>? configureOptions = null)
        where TActor : class, IActor<TMessage>
    {
        string pureActorKey = $"pure://actor?{address}";
        string registrationKey = $"registration://actor?{address}";
        return services
            .AddKeyedSingleton<IActor<TMessage>, TActor>(
                pureActorKey, (sp, _) => constructor(sp))
            .AddKeyedSingleton<IActorRef<TMessage>>(address, (sp, _) =>
            {
                IActor<TMessage> actor = sp.GetRequiredKeyedService<IActor<TMessage>>(pureActorKey);
                return new SupervisedActorRef<TMessage>(address, actor);
            })
            .AddSingleton(sp =>
            {
                IActor<TMessage> actor =
                    sp.GetRequiredKeyedService<IActor<TMessage>>(pureActorKey);
                SupervisedActorRef<TMessage> actorRef =
                    sp.GetRequiredKeyedService<IActorRef<TMessage>>(address)
                        as SupervisedActorRef<TMessage>
                        ?? throw new InvalidOperationException("Actor Reference Construction Failed.");
                var options = new SupervisedActorOptions();
                configureOptions?.Invoke(options);
                return new HostedActorRegistration(address, actor, actorRef, options);
            });
    }

    /// <summary>
    /// Add a hosted actor to dependency injection
    /// </summary>
    /// <typeparam name="TActor">The type of actor to add</typeparam>
    /// <typeparam name="TMessage">The message type that the actor receives</typeparam>
    /// <param name="services">The service collection to add the actor to.</param>
    /// <param name="address">The address to use for the new actor.</param>
    /// <param name="configureOptions">Optional delegate used to configure the settings of the
    ///     actor at construction time.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddActor<TActor, TMessage>(
        this IServiceCollection services,
        string address,
        Action<SupervisedActorOptions>? configureOptions = null)
        where TActor : class, IActor<TMessage>
    {
        return services.AddActor<TActor, TMessage>(
            address,
            sp => ActivatorUtilities.CreateInstance<TActor>(sp),
            configureOptions);
    }

    /// <summary>
    /// Add a hosted actor to dependency injection
    /// </summary>
    /// <typeparam name="TMessage">The message type that the actor receives</typeparam>
    /// <param name="services">The service collection to add the actor to.</param>
    /// <param name="address">The address to use for the new actor.</param>
    /// <param name="handler">The delegate to run each time that the actor receives a message.</param>
    /// <param name="mailboxChannelProvider">Optional provider for mailbox construction.</param>
    /// <param name="configureOptions">Optional delegate used to configure the settings of the
    ///     actor at construction time.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddActor<TMessage>(
        this IServiceCollection services,
        string address,
        Func<LambdaActor<TMessage>, TMessage, ISupervisor, CancellationToken, Task> handler,
        IMailboxProvider? mailboxChannelProvider = null,
        Action<SupervisedActorOptions>? configureOptions = null)
    {
        return services.AddActor<LambdaActor<TMessage>, TMessage>(
            address,
            sp => new LambdaActor<TMessage>(
                handler,
                sp.GetRequiredService<ISupervisor>(),
                sp.GetKeyedService<IActorRef<StandardError>>("actor://error"),
                mailboxChannelProvider),
            configureOptions);
    }

    /// <summary>
    /// Add a hosted actor to dependency injection
    /// </summary>
    /// <typeparam name="TMessage">The message type that the actor receives</typeparam>
    /// <param name="services">The service collection to add the actor to.</param>
    /// <param name="address">The address to use for the new actor.</param>
    /// <param name="handler">The delegate to run each time that the actor receives a message.</param>
    /// <param name="mailboxChannelProvider">Optional provider for mailbox construction.</param>
    /// <param name="configureOptions">Optional delegate used to configure the settings of the
    ///     actor at construction time.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddActor<TMessage, TState>(
        this IServiceCollection services,
        string address,
        TState initialState,
        Func<LambdaMachine<TMessage, TState>, TMessage, TState, ISupervisor,
            CancellationToken, Task<TState?>> handler,
        IMailboxProvider? mailboxChannelProvider = null,
        Action<SupervisedActorOptions>? configureOptions = null)
        where TState : MachineState
    {
        return services.AddActor<LambdaMachine<TMessage, TState>, TMessage>(
            address,
            sp => new LambdaMachine<TMessage, TState>(
                initialState, handler,
                sp.GetRequiredService<ISupervisor>(),
                sp.GetKeyedService<IActorRef<StandardError>>("actor://error"),
                mailboxChannelProvider),
            configureOptions);
    }

    /// <summary>
    /// Add actor supervision to the IHost Dependency Injection, allowing supervision of actors via a Supervisor actor.
    /// </summary>
    /// <typeparam name="TErrorActor">The type of error actor to add to the host</typeparam>
    /// <param name="services">The service collection to add the actor to.</param>
    /// <param name="configureOptions">Optional delegate used to configure the settings of the
    ///     actor at construction time.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddActorSupervision<TErrorActor>(
        this IServiceCollection services,
        Action<HostedSupervisorOptions>? configureOptions = null)
        where TErrorActor : StandardErrorActor
    {
        HostedSupervisorOptions options = new();
        configureOptions?.Invoke(options);
        return services
            .AddActor<TErrorActor, StandardError>("actor://error")
            .AddSingleton(options)
            .AddSingleton<IActorHostRegistry, HostedSupervisor>()
            .AddSingleton(sp =>
            {
                IActorHostRegistry registry =
                    sp.GetRequiredService<IActorHostRegistry>();
                ISupervisor supervisor = registry as ISupervisor
                    ?? throw new InvalidOperationException(
                        "Failed to resolve ISupervisor from IActorHostRegistry.");
                return supervisor;
            })
            .AddHostedService<HostedActorService>();
    }

    /// <summary>
    /// Add actor supervision to the IHost Dependency Injection, allowing supervision of actors via a Supervisor actor.
    /// </summary>
    /// <param name="services">The service collection to add the actor to.</param>
    /// <param name="configureOptions">Optional delegate used to configure the settings of the
    ///     actor at construction time.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddActorSupervision(
        this IServiceCollection services,
        Action<HostedSupervisorOptions>? configureOptions = null)
    {
        return services.AddActorSupervision<StandardErrorActor>(configureOptions);
    }
}
