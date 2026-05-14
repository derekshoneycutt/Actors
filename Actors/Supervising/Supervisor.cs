using System.Collections.Concurrent;
using System.Runtime.ExceptionServices;
using Actors.Base;
using Actors.Errors;
using Actors.Mailbox;
using Actors.Policies.RestartPolicies;
using Microsoft.Extensions.DependencyInjection;

namespace Actors.Supervising;

/// <summary>
/// Implementation of the Supervisor class intended for use through IHost Dependency Injection
/// </summary>
public class Supervisor
    : Receiver<SupervisorMessage>, ISupervisor
{
    /// <summary>
    /// The service provider used to pull dependencies off the host
    /// </summary>
    private readonly IServiceProvider _serviceProvider;

    /// <summary>
    /// The Dictionary of currently hosted, running actors and associated state
    /// </summary>
    protected ConcurrentDictionary<string, SupervisedRunningActorState> HostedStates { get; init; }

    /// <summary>
    /// The cancellation source used to cancel all hosted actor operations on shut down
    /// </summary>
    private readonly CancellationTokenSource _hostedActorsCancellation = new();

    /// <summary>
    /// Gets the options used to control this supervisor instance.
    /// </summary>
    protected SupervisorOptions Options { get; init; }

    /// <summary>
    /// Construct a new instance of the Supervisor Actor.
    /// This is intended to be loaded with DI via the AddActorSupervision extension method
    /// </summary>
    /// <param name="options">The options to load the supervisor with.</param>
    /// <param name="serviceProvider">The service provider for the current host instance.</param>
    /// <param name="errorActor">The actor to send errors to for any further action</param>
    public Supervisor(
        SupervisorOptions options,
        IServiceProvider serviceProvider,
        IActorRef<StandardError>? errorActor = null)
        : base(errorActor, options.HostMailboxProvider)
    {
        _serviceProvider = serviceProvider;
        HostedStates = [];
        Options = options;
    }

    /// <summary>
    /// Virtual method that is called when a dead actor is removed from the supervisor
    /// </summary>
    /// <param name="state">The state of the actor that was removed</param>
    /// <returns>A Task that represents the asynchronous operation.</returns>
    protected virtual Task OnDeadActorRemovedAsync(SupervisedRunningActorState state)
        => Task.CompletedTask;

    /// <summary>
    /// Remove a now dead actor from the list of running actors.
    /// </summary>
    /// <param name="state">The state representing the actor to be removed.</param>
    /// <returns>A Task that completes when the removal operation has been completed.</returns>
    private async Task RemoveDeadActorAsync(
        SupervisedRunningActorState state)
    {
        _ = HostedStates.Remove(state.ActorReference.Address, out _);
        await state.Actor.DisposeAsync().ConfigureAwait(false);

        IEnumerable<SupervisedRunningActorState> children =
            [.. state.Children.Select(kvp => kvp.Value)];
        await state.ChildShutdownPolicy.OnParentShutdownAsync(children)
            .ConfigureAwait(false);

        await OnDeadActorRemovedAsync(state).ConfigureAwait(false);
    }

    /// <summary>
    /// Apply a single run of an actor
    /// </summary>
    /// <param name="state">The actor state to apply the single run</param>
    /// <param name="attempt">The attempt number this represents</param>
    /// <param name="cancellationToken">The cancellation token for processes to signal cancellation.</param>
    /// <returns>A Task that completes when the actor run has completed.</returns>
    private async Task<(RestartPolicyResult, Exception?)> ApplyRunAsync(
        SupervisedRunningActorState state,
        int attempt,
        CancellationToken cancellationToken)
    {
        try
        {
            Task runTask = state.Actor.RunAsync(cancellationToken);
            state.Status = ActorStatus.Running;
            await runTask.ConfigureAwait(false);
            state.Status = ActorStatus.Stopped;

            return await state.RestartPolicy
                .OnActorCompletionAsync(attempt, cancellationToken)
                .ConfigureAwait(false) switch
            {
                RestartPolicyResult.AbandonActor =>
                    (RestartPolicyResult.AbandonActor, null),
                RestartPolicyResult.RestartActor =>
                    (RestartPolicyResult.RestartActor, null),
                _ => (RestartPolicyResult.RestartActor, null)
            };
        }
        catch (OperationCanceledException ex)
        {
            state.LastFault = ex;
            state.Status = ActorStatus.Cancelled;

            if (_hostedActorsCancellation.IsCancellationRequested)
            {
                return (RestartPolicyResult.AbandonActor, null);
            }
            return await state.RestartPolicy
                .OnActorCancelledAsync(attempt, ex)
                .ConfigureAwait(false) switch
            {
                RestartPolicyResult.AbandonActor =>
                    (RestartPolicyResult.AbandonActor, ex),
                RestartPolicyResult.RestartActor =>
                    (RestartPolicyResult.RestartActor, null),
                _ => (RestartPolicyResult.RestartActor, null)
            };
        }
        catch (Exception ex)
        {
            state.LastFault = ex;
            state.Status = ActorStatus.Faulted;

            return await state.RestartPolicy
                .OnActorFaultedAsync(attempt, ex, cancellationToken)
                .ConfigureAwait(false) switch
            {
                RestartPolicyResult.AbandonActor =>
                    (RestartPolicyResult.AbandonActor, ex),
                RestartPolicyResult.RestartActor =>
                    (RestartPolicyResult.RestartActor, null),
                _ => (RestartPolicyResult.RestartActor, null)
            };
        }
    }

    /// <summary>
    /// Run an Actor's primary function, handling restarts and faults as desired by policy.
    /// </summary>
    /// <param name="state">The actor state to modify, including the actor to run.</param>
    /// <returns>A Task that completes when the actor has completed and is not being restarted.</returns>
    private async Task RunActorAsync(
        SupervisedRunningActorState state)
    {
        try
        {
            int attempt = 0;
            while (true)
            {
                ++attempt;
                (RestartPolicyResult result, Exception? ex)
                    = await ApplyRunAsync(state, attempt, _hostedActorsCancellation.Token)
                        .ConfigureAwait(false);
                switch (result)
                {
                    case RestartPolicyResult.AbandonActor:
                        if (ex is not null)
                        {
                            ExceptionDispatchInfo.Throw(ex);
                        }
                        return;
                    case RestartPolicyResult.RestartActor:
                    default:
                        break;
                }

                // We only restart actors that can be re-constructed; i.e. spawned actors
                if (state.Constructor is null)
                {
                    return;
                }
                else
                {
                    IActor oldActor = state.Actor;
                    state.Actor = await state.ActorReference.ChangeActorAsync(
                        this, _serviceProvider, state.Constructor)
                        .ConfigureAwait(false);
                    await oldActor.DisposeAsync().ConfigureAwait(false);
                }
            }
        }
        finally
        {
            await RemoveDeadActorAsync(state)
                .ConfigureAwait(false);
        }
    }

    /// <summary>
    /// Find an actor according to its address
    /// </summary>
    /// <param name="address">The address of the actor to find</param>
    /// <returns>The actor's reference handle.</returns>
    private IActorRef FindActor(string address)
    {
        return HostedStates.TryGetValue(address, out SupervisedRunningActorState? state)
            && (state is not null)
            ? state.ActorReference
            : throw new InvalidOperationException($"Actor with address {address} not found.");
    }

    /// <summary>
    /// Processes a single message from the mailbox.
    /// </summary>
    /// <param name="message">The message to process.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>A task that completes when processing is done.</returns>
    protected override async Task ProcessMessageAsync(
        SupervisorMessage message, CancellationToken cancellationToken)
    {
        switch (message)
        {
            case KillActorMessage killmsg:
                if (HostedStates.TryGetValue(killmsg.Actor.Address,
                    out SupervisedRunningActorState? state)
                    && (state is not null))
                {
                    await state.Actor.DisposeAsync().ConfigureAwait(false);
                }
                break;

            default:
                Console.Error.WriteLine($"Received host message: {message}");
                break;
        }
    }

    /// <summary>
    /// Initializes the actor before starting the run loop.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    protected override Task InitializeRunAsync(
        CancellationToken cancellationToken)
    {
        foreach (KeyValuePair<string, SupervisedRunningActorState> kvpState in HostedStates)
        {
            if (kvpState.Value.Status != ActorStatus.Running)
            {
                kvpState.Value.RunningTask = RunActorAsync(kvpState.Value);
            }
        }
        return Task.CompletedTask;
    }

    /// <summary>
    /// Get the reference handle for the current actor, by the actor's actual reference, asynchronously
    /// </summary>
    /// <typeparam name="TActor">The type of actor to get the reference for</typeparam>
    /// <typeparam name="TMessage">The type of message the actor listens for</typeparam>
    /// <param name="forThis">The actor to find the reference handle for.</param>
    /// <returns>The handle for the given "this" actor.</returns>
    public IActorRef<TMessage> This<TActor, TMessage>(
        TActor forThis)
        where TActor : IActor<TMessage>
    {
        foreach (SupervisedRunningActorState state in HostedStates.Select(kvp => kvp.Value))
        {
            if (ReferenceEquals(state.Actor, forThis))
            {
                IActorRef<TMessage> reference =
                    state.ActorReference as IActorRef<TMessage>
                    ?? throw new InvalidOperationException("Invalid actor reference corruption.");
                return reference;
            }
        }
        throw new InvalidOperationException("Actor not found.");
    }

    /// <summary>
    /// Find an actor reference according to its address.
    /// </summary>
    /// <typeparam name="TMessage">The message type that actor looking for receives</typeparam>
    /// <param name="address">The address of the actor to search for</param>
    /// <returns>The reference to the matching actor.</returns>
    public IActorRef<TMessage> FindActor<TMessage>(
        string address)
    {
        return HostedStates.TryGetValue(address, out SupervisedRunningActorState? state)
            && (state is not null)
            ? state.ActorReference as IActorRef<TMessage>
                ?? throw new InvalidOperationException(
                    $"Actor of type {typeof(TMessage)} not found.")
            : throw new InvalidOperationException("Actor not found.");
    }

    /// <summary>
    /// Spawn a new actor.
    /// </summary>
    /// <typeparam name="TActor">The type of the actor to spawn.</typeparam>
    /// <typeparam name="TMessage">The message type that the actor receives.</typeparam>
    /// <param name="address">The address of the new actor to spawn</param>
    /// <param name="constructor">The constructor method to create the actor.</param>
    /// <returns>A Task that returns the reference handle to the new actor when it is spawned successfully.</returns>
    public IActorRef<TMessage> Spawn<TActor, TMessage>(
        string address,
        Func<ISupervisor, IServiceProvider, TActor> constructor)
        where TActor : class, IActor<TMessage>
    {
        SupervisedRunningActorState Create(string addr)
        {
            TActor actor = constructor(this, _serviceProvider);
            SupervisedActorRef<TMessage> actorRef = new(addr, actor);
            SupervisedRunningActorState state = new()
            {
                Actor = actor,
                ActorReference = actorRef,
                Status = ActorStatus.Registered,
                Constructor = constructor
            };
            state.RunningTask = RunActorAsync(state);
            return state;
        }

        SupervisedRunningActorState state =
            HostedStates.AddOrUpdate(address, Create, (_, existing) => existing);
        return state.ActorReference as IActorRef<TMessage>
            ?? throw new InvalidOperationException("Failed to construct actor of specified type.");
    }

    /// <summary>
    /// Spawn a new actor.
    /// </summary>
    /// <typeparam name="TActor">The type of the actor to spawn.</typeparam>
    /// <typeparam name="TMessage">The message type that the actor receives.</typeparam>
    /// <param name="address">The address of the new actor to spawn</param>
    /// <param name="constructor">The constructor method to create the actor.</param>
    /// <param name="options">The options used to control the actor once it is spawned.</param>
    /// <returns>The reference handle to the new actor when it is spawned successfully.</returns>
    public IActorRef<TMessage> Spawn<TActor, TMessage>(
        string address,
        Func<ISupervisor, IServiceProvider, TActor> constructor,
        SupervisedActorOptions options)
        where TActor : class, IActor<TMessage>
    {
        SupervisedRunningActorState Create(string addr)
        {
            TActor actor = constructor(this, _serviceProvider);
            SupervisedActorRef<TMessage> actorRef = new(addr, actor);
            SupervisedRunningActorState state = new()
            {
                Actor = actor,
                ActorReference = actorRef,
                Status = ActorStatus.Registered,
                Constructor = constructor,
                RestartPolicy = options.RestartPolicy,
                ChildShutdownPolicy = options.ChildShutdownPolicy
            };
            state.RunningTask = RunActorAsync(state);
            return state;
        }

        SupervisedRunningActorState state =
            HostedStates.AddOrUpdate(address, Create, (_, existing) => existing);
        return state.ActorReference as IActorRef<TMessage>
            ?? throw new InvalidOperationException("Failed to construct actor of specified type.");
    }

    /// <summary>
    /// Spawn a new actor.
    /// </summary>
    /// <typeparam name="TActor">The type of the actor to spawn.</typeparam>
    /// <typeparam name="TMessage">The message type that the actor receives.</typeparam>
    /// <param name="address">The address of the new actor to spawn</param>
    /// <param name="args">The arguments to pass to the constructor of the actor</param>
    /// <returns>The reference handle to the new actor when it is spawned successfully.</returns>
    public IActorRef<TMessage> Spawn<TActor, TMessage>(
        string address,
        params object[] args)
        where TActor : class, IActor<TMessage>
    {
        return Spawn<TActor, TMessage>(
            address,
            (sup, sp) => ActivatorUtilities.CreateInstance<TActor>(sp, args));
    }

    /// <summary>
    /// Spawn a new actor.
    /// </summary>
    /// <typeparam name="TActor">The type of the actor to spawn.</typeparam>
    /// <typeparam name="TMessage">The message type that the actor receives.</typeparam>
    /// <param name="address">The address of the new actor to spawn</param>
    /// <param name="options">The options used to control the actor once it is spawned.</param>
    /// <param name="args">The arguments to pass to the constructor of the actor</param>
    /// <returns>The reference handle to the new actor when it is spawned successfully.</returns>
    public IActorRef<TMessage> Spawn<TActor, TMessage>(
        string address,
        SupervisedActorOptions options,
        params object[] args)
        where TActor : class, IActor<TMessage>
    {
        return Spawn<TActor, TMessage>(
            address,
            (sup, sp) => ActivatorUtilities.CreateInstance<TActor>(sp, args),
            options);
    }

    /// <summary>
    /// Spawn a new actor.
    /// </summary>
    /// <typeparam name="TActor">The type of the actor to spawn.</typeparam>
    /// <typeparam name="TMessage">The message type that the actor receives.</typeparam>
    /// <param name="parent">The handle to the parent actor the spawned actor</param>
    /// <param name="address">The address of the new actor to spawn</param>
    /// <param name="constructor">The constructor method to create the actor.</param>
    /// <returns>A Task that returns the reference handle to the new actor when it is spawned successfully.</returns>
    public IActorRef<TMessage> Spawn<TActor, TMessage>(
        IActorRef parent,
        string address,
        Func<ISupervisor, IServiceProvider, TActor> constructor)
        where TActor : class, IActor<TMessage>
    {
        SupervisedRunningActorState Create(string addr)
        {
            TActor actor = constructor(this, _serviceProvider);
            SupervisedActorRef<TMessage> actorRef = new(addr, actor);
            SupervisedRunningActorState state = new()
            {
                Actor = actor,
                ActorReference = actorRef,
                ParentReference = parent,
                Status = ActorStatus.Registered,
                Constructor = constructor
            };
            state.RunningTask = RunActorAsync(state);
            return state;
        }

        SupervisedRunningActorState state =
            HostedStates.AddOrUpdate(address, Create, (_, existing) => existing);
        if (HostedStates.TryGetValue(parent.Address, out SupervisedRunningActorState? parentState)
            && parentState is not null)
        {
            _ = parentState.Children.AddOrUpdate(address, state, (_, _) => state);
        }
        return state.ActorReference as IActorRef<TMessage>
            ?? throw new InvalidOperationException("Failed to construct actor of specified type.");
    }

    /// <summary>
    /// Spawn a new actor.
    /// </summary>
    /// <typeparam name="TActor">The type of the actor to spawn.</typeparam>
    /// <typeparam name="TMessage">The message type that the actor receives.</typeparam>
    /// <param name="parent">The handle to the parent actor the spawned actor</param>
    /// <param name="address">The address of the new actor to spawn</param>
    /// <param name="constructor">The constructor method to create the actor.</param>
    /// <param name="options">The options used to control the actor once it is spawned.</param>
    /// <returns>A Task that returns the reference handle to the new actor when it is spawned successfully.</returns>
    public IActorRef<TMessage> Spawn<TActor, TMessage>(
        IActorRef parent,
        string address,
        Func<ISupervisor, IServiceProvider, TActor> constructor,
        SupervisedActorOptions options)
        where TActor : class, IActor<TMessage>
    {
        SupervisedRunningActorState Create(string addr)
        {
            TActor actor = constructor(this, _serviceProvider);
            SupervisedActorRef<TMessage> actorRef = new(addr, actor);
            SupervisedRunningActorState state = new()
            {
                Actor = actor,
                ActorReference = actorRef,
                ParentReference = parent,
                Status = ActorStatus.Registered,
                Constructor = constructor,
                RestartPolicy = options.RestartPolicy,
                ChildShutdownPolicy = options.ChildShutdownPolicy
            };
            state.RunningTask = RunActorAsync(state);
            return state;
        }

        SupervisedRunningActorState state =
            HostedStates.AddOrUpdate(address, Create, (_, existing) => existing);
        if (HostedStates.TryGetValue(parent.Address, out SupervisedRunningActorState? parentState)
            && parentState is not null)
        {
            _ = parentState.Children.AddOrUpdate(address, state, (_, _) => state);
        }
        return state.ActorReference as IActorRef<TMessage>
            ?? throw new InvalidOperationException("Failed to construct actor of specified type.");
    }

    /// <summary>
    /// Spawn a new actor.
    /// </summary>
    /// <typeparam name="TActor">The type of the actor to spawn.</typeparam>
    /// <typeparam name="TMessage">The message type that the actor receives.</typeparam>
    /// <param name="parentAddress">The address of the parent actor the spawned actor</param>
    /// <param name="address">The address of the new actor to spawn</param>
    /// <param name="constructor">The constructor method to create the actor.</param>
    /// <returns>A Task that returns the reference handle to the new actor when it is spawned successfully.</returns>
    public IActorRef<TMessage> Spawn<TActor, TMessage>(
        string parentAddress,
        string address,
        Func<ISupervisor, IServiceProvider, TActor> constructor)
        where TActor : class, IActor<TMessage>
    {
        IActorRef parent = FindActor<TMessage>(parentAddress);
        return Spawn<TActor, TMessage>(parent, address, constructor);
    }

    /// <summary>
    /// Spawn a new actor.
    /// </summary>
    /// <typeparam name="TActor">The type of the actor to spawn.</typeparam>
    /// <typeparam name="TMessage">The message type that the actor receives.</typeparam>
    /// <param name="parentAddress">The address of the parent actor the spawned actor</param>
    /// <param name="address">The address of the new actor to spawn</param>
    /// <param name="constructor">The constructor method to create the actor.</param>
    /// <returns>A Task that returns the reference handle to the new actor when it is spawned successfully.</returns>
    public IActorRef<TMessage> Spawn<TActor, TMessage>(
        string parentAddress,
        string address,
        Func<ISupervisor, IServiceProvider, TActor> constructor,
        SupervisedActorOptions options)
        where TActor : class, IActor<TMessage>
    {
        IActorRef parent = FindActor(parentAddress);
        return Spawn<TActor, TMessage>(parent, address, constructor, options);
    }

    /// <summary>
    /// Spawn a new actor.
    /// </summary>
    /// <typeparam name="TActor">The type of the actor to spawn.</typeparam>
    /// <typeparam name="TMessage">The message type that the actor receives.</typeparam>
    /// <param name="parentAddress">The address of the parent actor the spawned actor</param>
    /// <param name="address">The address of the new actor to spawn</param>
    /// <param name="args">The arguments to pass to the constructor of the actor</param>
    /// <returns>A Task that returns the reference handle to the new actor when it is spawned successfully.</returns>
    public IActorRef<TMessage> Spawn<TActor, TMessage>(
        string parentAddress,
        string address,
        params object[] args)
        where TActor : class, IActor<TMessage>
    {
        IActorRef parent = FindActor(parentAddress);
        return Spawn<TActor, TMessage>(parent, address, args);
    }

    /// <summary>
    /// Spawn a new actor.
    /// </summary>
    /// <typeparam name="TActor">The type of the actor to spawn.</typeparam>
    /// <typeparam name="TMessage">The message type that the actor receives.</typeparam>
    /// <param name="parentAddress">The address of the parent actor the spawned actor</param>
    /// <param name="address">The address of the new actor to spawn</param>
    /// <param name="options">The options used to control the actor once it is spawned.</param>
    /// <param name="args">The arguments to pass to the constructor of the actor</param>
    /// <returns>A Task that returns the reference handle to the new actor when it is spawned successfully.</returns>
    public IActorRef<TMessage> Spawn<TActor, TMessage>(
        string parentAddress,
        string address,
        SupervisedActorOptions options,
        params object[] args)
        where TActor : class, IActor<TMessage>
    {
        IActorRef parent = FindActor(parentAddress);
        return Spawn<TActor, TMessage>(parent, address, options, args);
    }

    /// <summary>
    /// Spawn a new actor.
    /// </summary>
    /// <typeparam name="TActor">The type of the actor to spawn.</typeparam>
    /// <typeparam name="TMessage">The message type that the actor receives.</typeparam>
    /// <param name="parent">The handle to the parent actor the spawned actor</param>
    /// <param name="address">The address of the new actor to spawn</param>
    /// <param name="args">The arguments to pass to the constructor of the actor</param>
    /// <returns>A Task that returns the reference handle to the new actor when it is spawned successfully.</returns>
    public IActorRef<TMessage> Spawn<TActor, TMessage>(
        IActorRef parent,
        string address,
        params object[] args)
        where TActor : class, IActor<TMessage>
    {
        return Spawn<TActor, TMessage>(
            parent, address,
            (sup, sp) => ActivatorUtilities.CreateInstance<TActor>(
                _serviceProvider, args));
    }

    /// <summary>
    /// Spawn a new actor.
    /// </summary>
    /// <typeparam name="TActor">The type of the actor to spawn.</typeparam>
    /// <typeparam name="TMessage">The message type that the actor receives.</typeparam>
    /// <param name="parent">The handle to the parent actor the spawned actor</param>
    /// <param name="address">The address of the new actor to spawn</param>
    /// <param name="options">The options used to control the actor once it is spawned.</param>
    /// <param name="args">The arguments to pass to the constructor of the actor</param>
    /// <returns>A Task that returns the reference handle to the new actor when it is spawned successfully.</returns>
    public IActorRef<TMessage> Spawn<TActor, TMessage>(
        IActorRef parent,
        string address,
        SupervisedActorOptions options,
        params object[] args)
        where TActor : class, IActor<TMessage>
    {
        return Spawn<TActor, TMessage>(
            parent, address,
            (sup, sp) => ActivatorUtilities.CreateInstance<TActor>(
                _serviceProvider, args),
            options);
    }

    /// <summary>
    /// Spawn a new actor.
    /// </summary>
    /// <typeparam name="TMessage">The message type that the actor receives.</typeparam>
    /// <param name="address">The address of the new actor to spawn</param>
    /// <param name="lambda">The action to take when the actor receives a message.</param>
    /// <param name="mailboxProvider">Optional provider used to create the actor mailbox channel.</param>
    /// <returns>A Task that returns the reference handle to the new actor when it is spawned successfully.</returns>
    public IActorRef<TMessage> Spawn<TMessage>(
        string address,
        Func<LambdaActor<TMessage>, TMessage, ISupervisor, CancellationToken, Task> lambda,
        IMailboxProvider? mailboxProvider = null)
    {
        return Spawn<LambdaActor<TMessage>, TMessage>(
            address,
            (sup, sp) => new(lambda, this, ErrorActor, mailboxProvider));
    }

    /// <summary>
    /// Spawn a new actor.
    /// </summary>
    /// <typeparam name="TMessage">The message type that the actor receives.</typeparam>
    /// <param name="address">The address of the new actor to spawn</param>
    /// <param name="lambda">The action to take when the actor receives a message.</param>
    /// <param name="options">The options used to control the actor once it is spawned.</param>
    /// <param name="mailboxProvider">Optional provider used to create the actor mailbox channel.</param>
    /// <returns>A Task that returns the reference handle to the new actor when it is spawned successfully.</returns>
    public IActorRef<TMessage> Spawn<TMessage>(
        string address,
        Func<LambdaActor<TMessage>, TMessage, ISupervisor, CancellationToken, Task> lambda,
        SupervisedActorOptions options,
        IMailboxProvider? mailboxProvider = null)
    {
        return Spawn<LambdaActor<TMessage>, TMessage>(
            address,
            (sup, sp) => new(lambda, this, ErrorActor, mailboxProvider),
            options);
    }

    /// <summary>
    /// Spawn a new actor.
    /// </summary>
    /// <typeparam name="TMessage">The message type that the actor receives.</typeparam>
    /// <param name="parentAddress">The address of the parent actor the spawned actor</param>
    /// <param name="address">The address of the new actor to spawn</param>
    /// <param name="lambda">The action to take when the actor receives a message.</param>
    /// <param name="mailboxProvider">Optional provider used to create the actor mailbox channel.</param>
    /// <returns>A Task that returns the reference handle to the new actor when it is spawned successfully.</returns>
    public IActorRef<TMessage> Spawn<TMessage>(
        string parentAddress,
        string address,
        Func<LambdaActor<TMessage>, TMessage, ISupervisor, CancellationToken, Task> lambda,
        IMailboxProvider? mailboxProvider = null)
    {
        IActorRef parent = FindActor(parentAddress);
        return Spawn(parent, address, lambda, mailboxProvider);
    }

    /// <summary>
    /// Spawn a new actor.
    /// </summary>
    /// <typeparam name="TMessage">The message type that the actor receives.</typeparam>
    /// <param name="parentAddress">The address of the parent actor the spawned actor</param>
    /// <param name="address">The address of the new actor to spawn</param>
    /// <param name="lambda">The action to take when the actor receives a message.</param>
    /// <param name="options">The options used to control the actor once it is spawned.</param>
    /// <param name="mailboxProvider">Optional provider used to create the actor mailbox channel.</param>
    /// <returns>A Task that returns the reference handle to the new actor when it is spawned successfully.</returns>
    public IActorRef<TMessage> Spawn<TMessage>(
        string parentAddress,
        string address,
        Func<LambdaActor<TMessage>, TMessage, ISupervisor, CancellationToken, Task> lambda,
        SupervisedActorOptions options,
        IMailboxProvider? mailboxProvider = null)
    {
        IActorRef parent = FindActor(parentAddress);
        return Spawn(parent, address, lambda, options, mailboxProvider);
    }

    /// <summary>
    /// Spawn a new actor.
    /// </summary>
    /// <typeparam name="TMessage">The message type that the actor receives.</typeparam>
    /// <param name="parent">The handle to the parent actor the spawned actor</param>
    /// <param name="address">The address of the new actor to spawn</param>
    /// <param name="lambda">The action to take when the actor receives a message.</param>
    /// <param name="mailboxProvider">Optional provider used to create the actor mailbox channel.</param>
    /// <returns>A Task that returns the reference handle to the new actor when it is spawned successfully.</returns>
    public IActorRef<TMessage> Spawn<TMessage>(
        IActorRef parent,
        string address,
        Func<LambdaActor<TMessage>, TMessage, ISupervisor, CancellationToken, Task> lambda,
        IMailboxProvider? mailboxProvider = null)
    {
        return Spawn<LambdaActor<TMessage>, TMessage>(
            parent,
            address,
            (sup, sp) => new(lambda, this, ErrorActor, mailboxProvider));
    }

    /// <summary>
    /// Spawn a new actor.
    /// </summary>
    /// <typeparam name="TMessage">The message type that the actor receives.</typeparam>
    /// <param name="parent">The handle to the parent actor the spawned actor</param>
    /// <param name="address">The address of the new actor to spawn</param>
    /// <param name="lambda">The action to take when the actor receives a message.</param>
    /// <param name="options">The options used to control the actor once it is spawned.</param>
    /// <param name="mailboxProvider">Optional provider used to create the actor mailbox channel.</param>
    /// <returns>A Task that returns the reference handle to the new actor when it is spawned successfully.</returns>
    public IActorRef<TMessage> Spawn<TMessage>(
        IActorRef parent,
        string address,
        Func<LambdaActor<TMessage>, TMessage, ISupervisor, CancellationToken, Task> lambda,
        SupervisedActorOptions options,
        IMailboxProvider? mailboxProvider = null)
    {
        return Spawn<LambdaActor<TMessage>, TMessage>(
            parent,
            address,
            (sup, sp) => new(lambda, this, ErrorActor, mailboxProvider),
            options);

    }

    /// <summary>
    /// Spawn a new actor.
    /// </summary>
    /// <typeparam name="TMessage">The message type that the actor receives.</typeparam>
    /// <param name="address">The address of the new actor to spawn</param>
    /// <param name="initialState">The initial state stored inside the machine actor.</param>
    /// <param name="lambda">The action to take when the actor receives a message.</param>
    /// <param name="mailboxProvider">Optional provider used to create the actor mailbox channel.</param>
    /// <returns>A Task that returns the reference handle to the new actor when it is spawned successfully.</returns>
    public IActorRef<TMessage> Spawn<TMessage, TState>(
        string address,
        TState initialState,
        Func<LambdaMachine<TMessage, TState>, TMessage, TState, ISupervisor,
            CancellationToken, Task<TState?>> lambda,
        IMailboxProvider? mailboxProvider = null)
        where TState : MachineState
    {
        return Spawn<LambdaMachine<TMessage, TState>, TMessage>(
            address,
            (sup, sp) => new(initialState, lambda, this, ErrorActor, mailboxProvider));
    }

    /// <summary>
    /// Spawn a new actor.
    /// </summary>
    /// <typeparam name="TMessage">The message type that the actor receives.</typeparam>
    /// <param name="address">The address of the new actor to spawn</param>
    /// <param name="initialState">The initial state stored inside the machine actor.</param>
    /// <param name="lambda">The action to take when the actor receives a message.</param>
    /// <param name="options">The options used to control the actor once it is spawned.</param>
    /// <param name="mailboxProvider">Optional provider used to create the actor mailbox channel.</param>
    /// <returns>A Task that returns the reference handle to the new actor when it is spawned successfully.</returns>
    public IActorRef<TMessage> Spawn<TMessage, TState>(
        string address,
        TState initialState,
        Func<LambdaMachine<TMessage, TState>, TMessage, TState, ISupervisor,
            CancellationToken, Task<TState?>> lambda,
        SupervisedActorOptions options,
        IMailboxProvider? mailboxProvider = null)
        where TState : MachineState
    {
        return Spawn<LambdaMachine<TMessage, TState>, TMessage>(
            address,
            (sup, sp) => new(initialState, lambda, this, ErrorActor, mailboxProvider),
            options);
    }

    /// <summary>
    /// Spawn a new actor.
    /// </summary>
    /// <typeparam name="TMessage">The message type that the actor receives.</typeparam>
    /// <param name="parentAddress">The address of the parent actor the spawned actor</param>
    /// <param name="address">The address of the new actor to spawn</param>
    /// <param name="initialState">The initial state stored inside the machine actor.</param>
    /// <param name="lambda">The action to take when the actor receives a message.</param>
    /// <param name="mailboxProvider">Optional provider used to create the actor mailbox channel.</param>
    /// <returns>A Task that returns the reference handle to the new actor when it is spawned successfully.</returns>
    public IActorRef<TMessage> Spawn<TMessage, TState>(
        string parentAddress,
        string address,
        TState initialState,
        Func<LambdaMachine<TMessage, TState>, TMessage, TState, ISupervisor,
            CancellationToken, Task<TState?>> lambda,
        IMailboxProvider? mailboxProvider = null)
        where TState : MachineState
    {
        IActorRef parent = FindActor(parentAddress);
        return Spawn(parent, address, initialState, lambda, mailboxProvider);
    }

    /// <summary>
    /// Spawn a new actor.
    /// </summary>
    /// <typeparam name="TMessage">The message type that the actor receives.</typeparam>
    /// <param name="parentAddress">The address of the parent actor the spawned actor</param>
    /// <param name="address">The address of the new actor to spawn</param>
    /// <param name="initialState">The initial state stored inside the machine actor.</param>
    /// <param name="lambda">The action to take when the actor receives a message.</param>
    /// <param name="options">The options used to control the actor once it is spawned.</param>
    /// <param name="mailboxProvider">Optional provider used to create the actor mailbox channel.</param>
    /// <returns>A Task that returns the reference handle to the new actor when it is spawned successfully.</returns>
    public IActorRef<TMessage> Spawn<TMessage, TState>(
        string parentAddress,
        string address,
        TState initialState,
        Func<LambdaMachine<TMessage, TState>, TMessage, TState, ISupervisor,
            CancellationToken, Task<TState?>> lambda,
        SupervisedActorOptions options,
        IMailboxProvider? mailboxProvider = null)
        where TState : MachineState
    {
        IActorRef parent = FindActor(parentAddress);
        return Spawn(parent, address, initialState, lambda, options, mailboxProvider);
    }

    /// <summary>
    /// Spawn a new actor.
    /// </summary>
    /// <typeparam name="TMessage">The message type that the actor receives.</typeparam>
    /// <param name="parent">The handle to the parent actor the spawned actor</param>
    /// <param name="address">The address of the new actor to spawn</param>
    /// <param name="initialState">The initial state stored inside the machine actor.</param>
    /// <param name="lambda">The action to take when the actor receives a message.</param>
    /// <param name="mailboxProvider">Optional provider used to create the actor mailbox channel.</param>
    /// <returns>A Task that returns the reference handle to the new actor when it is spawned successfully.</returns>
    public IActorRef<TMessage> Spawn<TMessage, TState>(
        IActorRef parent,
        string address,
        TState initialState,
        Func<LambdaMachine<TMessage, TState>, TMessage, TState, ISupervisor,
            CancellationToken, Task<TState?>> lambda,
        IMailboxProvider? mailboxProvider = null)
        where TState : MachineState
    {
        return Spawn<LambdaMachine<TMessage, TState>, TMessage>(
            parent,
            address,
            (sup, sp) => new(initialState, lambda, this, ErrorActor, mailboxProvider));
    }

    /// <summary>
    /// Spawn a new actor.
    /// </summary>
    /// <typeparam name="TMessage">The message type that the actor receives.</typeparam>
    /// <param name="parent">The handle to the parent actor the spawned actor</param>
    /// <param name="address">The address of the new actor to spawn</param>
    /// <param name="initialState">The initial state stored inside the machine actor.</param>
    /// <param name="lambda">The action to take when the actor receives a message.</param>
    /// <param name="options">The options used to control the actor once it is spawned.</param>
    /// <param name="mailboxProvider">Optional provider used to create the actor mailbox channel.</param>
    /// <returns>A Task that returns the reference handle to the new actor when it is spawned successfully.</returns>
    public IActorRef<TMessage> Spawn<TMessage, TState>(
        IActorRef parent,
        string address,
        TState initialState,
        Func<LambdaMachine<TMessage, TState>, TMessage, TState, ISupervisor,
            CancellationToken, Task<TState?>> lambda,
        SupervisedActorOptions options,
        IMailboxProvider? mailboxProvider = null)
        where TState : MachineState
    {
        return Spawn<LambdaMachine<TMessage, TState>, TMessage>(
            parent,
            address,
            (sup, sp) => new(initialState, lambda, this, ErrorActor, mailboxProvider),
            options);
    }

    /// <summary>
    /// Watch the state of an actor that has been spawned.
    /// </summary>
    /// <param name="address">The address of the actor to get a watch handle for.</param>
    /// <param name="cancellationToken">The cancellation token for processes to signal cancellation.</param>
    /// <returns>A Task that completes when the actor completes, or uniquely cancels when the passed cancellation token cancels.</returns>
    /// <remarks>The CancellationToken provided will not cancel the actual Actor's Task but only the
    ///     waiting Task provided here. The Actor may continue to run after this token's cancellation.</remarks>
    public async Task WatchAsync(
        string address,
        CancellationToken cancellationToken)
    {
        if (HostedStates.TryGetValue(address, out SupervisedRunningActorState? state) &&
            (state is not null))
        {
            if (state.RunningTask is not null)
            {
                await state.RunningTask.WaitAsync(cancellationToken);
            }
        }
        else
        {
            throw new InvalidOperationException($"Actor with address {address} not found.");
        }
    }

    /// <summary>
    /// Watch the state of an actor that has been spawned.
    /// </summary>
    /// <param name="actor">The reference to the actor to get a watch handle for.</param>
    /// <param name="cancellationToken">The cancellation token for processes to signal cancellation.</param>
    /// <returns>A Task that completes when the actor completes, or uniquely cancels when the passed cancellation token cancels.</returns>
    /// <remarks>The CancellationToken provided will not cancel the actual Actor's Task but only the
    ///     waiting Task provided here. The Actor may continue to run after this token's cancellation.</remarks>
    public Task WatchAsync(
        IActorRef actor,
        CancellationToken cancellationToken)
    {
        return WatchAsync(actor.Address, cancellationToken);
    }

    /// <summary>
    /// Send a Dispose signal to a given actor, telling it to cancel operations and shut down.
    /// The actor is subsequently removed from the Supervisor once it has completed operations.
    /// </summary>
    /// <param name="actor">The actor to shutdown and dispose.</param>
    /// <param name="cancellationToken">The cancellation token for processes to signal cancellation.</param>
    /// <returns>A Task that completes when the Dispose signal has been sent to the actor. The actor may still be operating when this Task completes.</returns>
    public async Task DisposeActorAsync(
        IActorRef actor,
        CancellationToken cancellationToken)
    {
        if (HostedStates.TryGetValue(actor.Address, out SupervisedRunningActorState? state) &&
            (state is not null))
        {
            await state.Actor.DisposeAsync();
        }
        else
        {
            throw new InvalidOperationException($"Actor with address {actor.Address} not found.");
        }
    }

    /// <summary>
    /// Cancel all actors asynchronously
    /// </summary>
    /// <returns>A Task that represents the asynchronous operation</returns>
    protected Task CancelAllActorsAsync()
    {
        return _hostedActorsCancellation.CancelAsync();
    }


    /// <summary>
    /// Dispose resources that have been secured for the Supervisor
    /// </summary>
    protected override void Dispose(bool disposing)
    {
        if (disposing && !IsDisposed)
        {
            _hostedActorsCancellation.Dispose();
        }
        base.Dispose(disposing);
    }

    /// <summary>
    /// Dispose resources that have been secured for the Supervisor
    /// </summary>
    protected override async ValueTask DisposeAsyncCore()
    {
        if (!IsDisposed)
        {
            _hostedActorsCancellation.Dispose();
        }
        await base.DisposeAsyncCore();
    }
}
