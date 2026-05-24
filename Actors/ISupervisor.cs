using Actors.Supervising;
using Actors.Mailbox;

namespace Actors;

/// <summary>
/// Interface describing a supervising type receiver actor.
/// This watches over a hierarchy of actors, coordinating how they
/// act upon failure, cancellation, and success.
/// This is ideally used directly within and from IHost DI, letting it be the
/// major manager of Actors in the application.
/// </summary>
public interface ISupervisor
    : IReceiver<SupervisorMessage>
{
    /// <summary>
    /// Get the reference handle for the current actor, by the actor's actual reference, asynchronously
    /// </summary>
    /// <typeparam name="TActor">The type of actor to get the reference for</typeparam>
    /// <typeparam name="TMessage">The type of message the actor listens for</typeparam>
    /// <param name="forThis">The actor to find the reference handle for.</param>
    /// <returns>The handle for the given "this" actor.</returns>

    IActorRef<TMessage> This<TActor, TMessage>(
        TActor forThis)
        where TActor : IActor<TMessage>;

    /// <summary>
    /// Find an actor reference according to its address.
    /// </summary>
    /// <typeparam name="TMessage">The message type that actor looking for receives</typeparam>
    /// <param name="address">The address of the actor to search for</param>
    /// <returns>The reference to the matching actor.</returns>
    IActorRef<TMessage> FindActor<TMessage>(
        string address);

    /// <summary>
    /// Spawn a new actor.
    /// </summary>
    /// <typeparam name="TActor">The type of the actor to spawn.</typeparam>
    /// <typeparam name="TMessage">The message type that the actor receives.</typeparam>
    /// <param name="address">The address of the new actor to spawn</param>
    /// <param name="constructor">The constructor method to create the actor.</param>
    /// <returns>The reference handle to the new actor when it is spawned successfully.</returns>
    IActorRef<TMessage> Spawn<TActor, TMessage>(
        string address,
        Func<ISupervisor, IServiceProvider, TActor> constructor)
        where TActor : class, IActor<TMessage>;

    /// <summary>
    /// Spawn a new actor.
    /// </summary>
    /// <typeparam name="TActor">The type of the actor to spawn.</typeparam>
    /// <typeparam name="TMessage">The message type that the actor receives.</typeparam>
    /// <param name="address">The address of the new actor to spawn</param>
    /// <param name="constructor">The constructor method to create the actor.</param>
    /// <param name="options">The options used to control the actor once it is spawned.</param>
    /// <returns>The reference handle to the new actor when it is spawned successfully.</returns>
    IActorRef<TMessage> Spawn<TActor, TMessage>(
        string address,
        Func<ISupervisor, IServiceProvider, TActor> constructor,
        SupervisedActorOptions options)
        where TActor : class, IActor<TMessage>;

    /// <summary>
    /// Spawn a new actor.
    /// </summary>
    /// <typeparam name="TActor">The type of the actor to spawn.</typeparam>
    /// <typeparam name="TMessage">The message type that the actor receives.</typeparam>
    /// <param name="address">The address of the new actor to spawn</param>
    /// <param name="args">The arguments to pass to the constructor of the actor</param>
    /// <returns>The reference handle to the new actor when it is spawned successfully.</returns>
    IActorRef<TMessage> Spawn<TActor, TMessage>(
        string address,
        params object[] args)
        where TActor : class, IActor<TMessage>;

    /// <summary>
    /// Spawn a new actor.
    /// </summary>
    /// <typeparam name="TActor">The type of the actor to spawn.</typeparam>
    /// <typeparam name="TMessage">The message type that the actor receives.</typeparam>
    /// <param name="address">The address of the new actor to spawn</param>
    /// <param name="options">The options used to control the actor once it is spawned.</param>
    /// <param name="args">The arguments to pass to the constructor of the actor</param>
    /// <returns>The reference handle to the new actor when it is spawned successfully.</returns>
    IActorRef<TMessage> Spawn<TActor, TMessage>(
        string address,
        SupervisedActorOptions options,
        params object[] args)
        where TActor : class, IActor<TMessage>;

    /// <summary>
    /// Spawn a new actor.
    /// </summary>
    /// <typeparam name="TActor">The type of the actor to spawn.</typeparam>
    /// <typeparam name="TMessage">The message type that the actor receives.</typeparam>
    /// <param name="parent">The handle to the parent actor the spawned actor</param>
    /// <param name="address">The address of the new actor to spawn</param>
    /// <param name="constructor">The constructor method to create the actor.</param>
    /// <returns>The reference handle to the new actor when it is spawned successfully.</returns>
    IActorRef<TMessage> Spawn<TActor, TMessage>(
        IActorRef parent,
        string address,
        Func<ISupervisor, IServiceProvider, TActor> constructor)
        where TActor : class, IActor<TMessage>;

    /// <summary>
    /// Spawn a new actor.
    /// </summary>
    /// <typeparam name="TActor">The type of the actor to spawn.</typeparam>
    /// <typeparam name="TMessage">The message type that the actor receives.</typeparam>
    /// <param name="parent">The handle to the parent actor the spawned actor</param>
    /// <param name="address">The address of the new actor to spawn</param>
    /// <param name="constructor">The constructor method to create the actor.</param>
    /// <param name="options">The options used to control the actor once it is spawned.</param>
    /// <returns>The reference handle to the new actor when it is spawned successfully.</returns>
    IActorRef<TMessage> Spawn<TActor, TMessage>(
        IActorRef parent,
        string address,
        Func<ISupervisor, IServiceProvider, TActor> constructor,
        SupervisedActorOptions options)
        where TActor : class, IActor<TMessage>;

    /// <summary>
    /// Spawn a new actor.
    /// </summary>
    /// <typeparam name="TActor">The type of the actor to spawn.</typeparam>
    /// <typeparam name="TMessage">The message type that the actor receives.</typeparam>
    /// <param name="parentAddress">The address of the parent actor the spawned actor</param>
    /// <param name="address">The address of the new actor to spawn</param>
    /// <param name="constructor">The constructor method to create the actor.</param>
    /// <param name="options">The options used to control the actor once it is spawned.</param>
    /// <returns>The reference handle to the new actor when it is spawned successfully.</returns>
    IActorRef<TMessage> Spawn<TActor, TMessage>(
        string parentAddress,
        string address,
        Func<ISupervisor, IServiceProvider, TActor> constructor,
        SupervisedActorOptions options)
        where TActor : class, IActor<TMessage>;

    /// <summary>
    /// Spawn a new actor.
    /// </summary>
    /// <typeparam name="TActor">The type of the actor to spawn.</typeparam>
    /// <typeparam name="TMessage">The message type that the actor receives.</typeparam>
    /// <param name="parentAddress">The address of the parent actor the spawned actor</param>
    /// <param name="address">The address of the new actor to spawn</param>
    /// <param name="constructor">The constructor method to create the actor.</param>
    /// <returns>The reference handle to the new actor when it is spawned successfully.</returns>
    IActorRef<TMessage> Spawn<TActor, TMessage>(
        string parentAddress,
        string address,
        Func<ISupervisor, IServiceProvider, TActor> constructor)
        where TActor : class, IActor<TMessage>;

    /// <summary>
    /// Spawn a new actor.
    /// </summary>
    /// <typeparam name="TActor">The type of the actor to spawn.</typeparam>
    /// <typeparam name="TMessage">The message type that the actor receives.</typeparam>
    /// <param name="parentAddress">The address of the parent actor the spawned actor</param>
    /// <param name="address">The address of the new actor to spawn</param>
    /// <param name="args">The arguments to pass to the constructor of the actor</param>
    /// <returns>The reference handle to the new actor when it is spawned successfully.</returns>
    IActorRef<TMessage> Spawn<TActor, TMessage>(
        string parentAddress,
        string address,
        params object[] args)
        where TActor : class, IActor<TMessage>;

    /// <summary>
    /// Spawn a new actor.
    /// </summary>
    /// <typeparam name="TActor">The type of the actor to spawn.</typeparam>
    /// <typeparam name="TMessage">The message type that the actor receives.</typeparam>
    /// <param name="parentAddress">The address of the parent actor the spawned actor</param>
    /// <param name="address">The address of the new actor to spawn</param>
    /// <param name="options">The options used to control the actor once it is spawned.</param>
    /// <param name="args">The arguments to pass to the constructor of the actor</param>
    /// <returns>The reference handle to the new actor when it is spawned successfully.</returns>
    IActorRef<TMessage> Spawn<TActor, TMessage>(
        string parentAddress,
        string address,
        SupervisedActorOptions options,
        params object[] args)
        where TActor : class, IActor<TMessage>;

    /// <summary>
    /// Spawn a new actor.
    /// </summary>
    /// <typeparam name="TActor">The type of the actor to spawn.</typeparam>
    /// <typeparam name="TMessage">The message type that the actor receives.</typeparam>
    /// <param name="parent">The handle to the parent actor the spawned actor</param>
    /// <param name="address">The address of the new actor to spawn</param>
    /// <param name="args">The arguments to pass to the constructor of the actor</param>
    /// <returns>The reference handle to the new actor when it is spawned successfully.</returns>
    IActorRef<TMessage> Spawn<TActor, TMessage>(
        IActorRef parent,
        string address,
        params object[] args)
        where TActor : class, IActor<TMessage>;

    /// <summary>
    /// Spawn a new actor.
    /// </summary>
    /// <typeparam name="TActor">The type of the actor to spawn.</typeparam>
    /// <typeparam name="TMessage">The message type that the actor receives.</typeparam>
    /// <param name="parent">The handle to the parent actor the spawned actor</param>
    /// <param name="address">The address of the new actor to spawn</param>
    /// <param name="options">The options used to control the actor once it is spawned.</param>
    /// <param name="args">The arguments to pass to the constructor of the actor</param>
    /// <returns>The reference handle to the new actor when it is spawned successfully.</returns>
    IActorRef<TMessage> Spawn<TActor, TMessage>(
        IActorRef parent,
        string address,
        SupervisedActorOptions options,
        params object[] args)
        where TActor : class, IActor<TMessage>;

    /// <summary>
    /// Spawn a new actor.
    /// </summary>
    /// <typeparam name="TMessage">The message type that the actor receives.</typeparam>
    /// <param name="address">The address of the new actor to spawn</param>
    /// <param name="lambda">The action to take when the actor receives a message.</param>
    /// <param name="mailboxProvider">Optional provider used to create the actor mailbox channel.</param>
    /// <returns>The reference handle to the new actor when it is spawned successfully.</returns>
    IActorRef<TMessage> Spawn<TMessage>(
        string address,
        Func<LambdaActor<TMessage>, TMessage, ISupervisor, CancellationToken, ValueTask> lambda,
        IMailboxProvider? mailboxProvider = null);

    /// <summary>
    /// Spawn a new actor.
    /// </summary>
    /// <typeparam name="TMessage">The message type that the actor receives.</typeparam>
    /// <param name="address">The address of the new actor to spawn</param>
    /// <param name="lambda">The action to take when the actor receives a message.</param>
    /// <param name="options">The options used to control the actor once it is spawned.</param>
    /// <param name="mailboxProvider">Optional provider used to create the actor mailbox channel.</param>
    /// <returns>The reference handle to the new actor when it is spawned successfully.</returns>
    IActorRef<TMessage> Spawn<TMessage>(
        string address,
        Func<LambdaActor<TMessage>, TMessage, ISupervisor, CancellationToken, ValueTask> lambda,
        SupervisedActorOptions options,
        IMailboxProvider? mailboxProvider = null);

    /// <summary>
    /// Spawn a new actor.
    /// </summary>
    /// <typeparam name="TMessage">The message type that the actor receives.</typeparam>
    /// <param name="parentAddress">The address of the parent actor the spawned actor</param>
    /// <param name="address">The address of the new actor to spawn</param>
    /// <param name="lambda">The action to take when the actor receives a message.</param>
    /// <param name="mailboxProvider">Optional provider used to create the actor mailbox channel.</param>
    /// <returns>The reference handle to the new actor when it is spawned successfully.</returns>
    IActorRef<TMessage> Spawn<TMessage>(
        string parentAddress,
        string address,
        Func<LambdaActor<TMessage>, TMessage, ISupervisor, CancellationToken, ValueTask> lambda,
        IMailboxProvider? mailboxProvider = null);

    /// <summary>
    /// Spawn a new actor.
    /// </summary>
    /// <typeparam name="TMessage">The message type that the actor receives.</typeparam>
    /// <param name="parentAddress">The address of the parent actor the spawned actor</param>
    /// <param name="address">The address of the new actor to spawn</param>
    /// <param name="lambda">The action to take when the actor receives a message.</param>
    /// <param name="options">The options used to control the actor once it is spawned.</param>
    /// <param name="mailboxProvider">Optional provider used to create the actor mailbox channel.</param>
    /// <returns>The reference handle to the new actor when it is spawned successfully.</returns>
    IActorRef<TMessage> Spawn<TMessage>(
        string parentAddress,
        string address,
        Func<LambdaActor<TMessage>, TMessage, ISupervisor, CancellationToken, ValueTask> lambda,
        SupervisedActorOptions options,
        IMailboxProvider? mailboxProvider = null);

    /// <summary>
    /// Spawn a new actor.
    /// </summary>
    /// <typeparam name="TMessage">The message type that the actor receives.</typeparam>
    /// <param name="parent">The handle to the parent actor the spawned actor</param>
    /// <param name="address">The address of the new actor to spawn</param>
    /// <param name="lambda">The action to take when the actor receives a message.</param>
    /// <param name="mailboxProvider">Optional provider used to create the actor mailbox channel.</param>
    /// <returns>The reference handle to the new actor when it is spawned successfully.</returns>
    IActorRef<TMessage> Spawn<TMessage>(
        IActorRef parent,
        string address,
        Func<LambdaActor<TMessage>, TMessage, ISupervisor, CancellationToken, ValueTask> lambda,
        IMailboxProvider? mailboxProvider = null);

    /// <summary>
    /// Spawn a new actor.
    /// </summary>
    /// <typeparam name="TMessage">The message type that the actor receives.</typeparam>
    /// <param name="parent">The handle to the parent actor the spawned actor</param>
    /// <param name="address">The address of the new actor to spawn</param>
    /// <param name="lambda">The action to take when the actor receives a message.</param>
    /// <param name="options">The options used to control the actor once it is spawned.</param>
    /// <param name="mailboxProvider">Optional provider used to create the actor mailbox channel.</param>
    /// <returns>The reference handle to the new actor when it is spawned successfully.</returns>
    IActorRef<TMessage> Spawn<TMessage>(
        IActorRef parent,
        string address,
        Func<LambdaActor<TMessage>, TMessage, ISupervisor, CancellationToken, ValueTask> lambda,
        SupervisedActorOptions options,
        IMailboxProvider? mailboxProvider = null);

    /// <summary>
    /// Spawn a new actor.
    /// </summary>
    /// <typeparam name="TMessage">The message type that the actor receives.</typeparam>
    /// <param name="address">The address of the new actor to spawn</param>
    /// <param name="initialState">The initial state stored inside the machine actor.</param>
    /// <param name="lambda">The action to take when the actor receives a message.</param>
    /// <param name="mailboxProvider">Optional provider used to create the actor mailbox channel.</param>
    /// <returns>The reference handle to the new actor when it is spawned successfully.</returns>
    IActorRef<TMessage> Spawn<TMessage, TState>(
        string address,
        TState initialState,
        Func<LambdaMachine<TMessage, TState>, TMessage, TState, ISupervisor,
            CancellationToken, ValueTask<TState?>> lambda,
        IMailboxProvider? mailboxProvider = null)
        where TState : MachineState;

    /// <summary>
    /// Spawn a new actor.
    /// </summary>
    /// <typeparam name="TMessage">The message type that the actor receives.</typeparam>
    /// <param name="address">The address of the new actor to spawn</param>
    /// <param name="initialState">The initial state stored inside the machine actor.</param>
    /// <param name="lambda">The action to take when the actor receives a message.</param>
    /// <param name="options">The options used to control the actor once it is spawned.</param>
    /// <param name="mailboxProvider">Optional provider used to create the actor mailbox channel.</param>
    /// <returns>The reference handle to the new actor when it is spawned successfully.</returns>
    IActorRef<TMessage> Spawn<TMessage, TState>(
        string address,
        TState initialState,
        Func<LambdaMachine<TMessage, TState>, TMessage, TState, ISupervisor,
            CancellationToken, ValueTask<TState?>> lambda,
        SupervisedActorOptions options,
        IMailboxProvider? mailboxProvider = null)
        where TState : MachineState;

    /// <summary>
    /// Spawn a new actor.
    /// </summary>
    /// <typeparam name="TMessage">The message type that the actor receives.</typeparam>
    /// <param name="parentAddress">The address of the parent actor the spawned actor</param>
    /// <param name="address">The address of the new actor to spawn</param>
    /// <param name="initialState">The initial state stored inside the machine actor.</param>
    /// <param name="lambda">The action to take when the actor receives a message.</param>
    /// <param name="mailboxProvider">Optional provider used to create the actor mailbox channel.</param>
    /// <returns>The reference handle to the new actor when it is spawned successfully.</returns>
    IActorRef<TMessage> Spawn<TMessage, TState>(
        string parentAddress,
        string address,
        TState initialState,
        Func<LambdaMachine<TMessage, TState>, TMessage, TState, ISupervisor,
            CancellationToken, ValueTask<TState?>> lambda,
        IMailboxProvider? mailboxProvider = null)
        where TState : MachineState;

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
    /// <returns>The reference handle to the new actor when it is spawned successfully.</returns>
    IActorRef<TMessage> Spawn<TMessage, TState>(
        string parentAddress,
        string address,
        TState initialState,
        Func<LambdaMachine<TMessage, TState>, TMessage, TState, ISupervisor,
            CancellationToken, ValueTask<TState?>> lambda,
        SupervisedActorOptions options,
        IMailboxProvider? mailboxProvider = null)
        where TState : MachineState;

    /// <summary>
    /// Spawn a new actor.
    /// </summary>
    /// <typeparam name="TMessage">The message type that the actor receives.</typeparam>
    /// <param name="parent">The handle to the parent actor the spawned actor</param>
    /// <param name="address">The address of the new actor to spawn</param>
    /// <param name="initialState">The initial state stored inside the machine actor.</param>
    /// <param name="lambda">The action to take when the actor receives a message.</param>
    /// <param name="mailboxProvider">Optional provider used to create the actor mailbox channel.</param>
    /// <returns>The reference handle to the new actor when it is spawned successfully.</returns>
    IActorRef<TMessage> Spawn<TMessage, TState>(
        IActorRef parent,
        string address,
        TState initialState,
        Func<LambdaMachine<TMessage, TState>, TMessage, TState, ISupervisor,
            CancellationToken, ValueTask<TState?>> lambda,
        IMailboxProvider? mailboxProvider = null)
        where TState : MachineState;

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
    /// <returns>The reference handle to the new actor when it is spawned successfully.</returns>
    IActorRef<TMessage> Spawn<TMessage, TState>(
        IActorRef parent,
        string address,
        TState initialState,
        Func<LambdaMachine<TMessage, TState>, TMessage, TState, ISupervisor,
            CancellationToken, ValueTask<TState?>> lambda,
        SupervisedActorOptions options,
        IMailboxProvider? mailboxProvider = null)
        where TState : MachineState;

    /// <summary>
    /// Watch the state of an actor that has been spawned.
    /// </summary>
    /// <param name="address">The address of the actor to get a watch handle for.</param>
    /// <param name="cancellationToken">The cancellation token for processes to signal cancellation.</param>
    /// <returns>A Task that completes when the actor completes, or uniquely cancels when the passed cancellation token cancels.</returns>
    /// <remarks>The CancellationToken provided will not cancel the actual Actor's Task but only the
    ///     waiting Task provided here. The Actor may continue to run after this token's cancellation.</remarks>
    Task WatchAsync(
        string address,
        CancellationToken cancellationToken);

    /// <summary>
    /// Watch the state of an actor that has been spawned.
    /// </summary>
    /// <param name="actor">The reference to the actor to get a watch handle for.</param>
    /// <param name="cancellationToken">The cancellation token for processes to signal cancellation.</param>
    /// <returns>A Task that completes when the actor completes, or uniquely cancels when the passed cancellation token cancels.</returns>
    /// <remarks>The CancellationToken provided will not cancel the actual Actor's Task but only the
    ///     waiting Task provided here. The Actor may continue to run after this token's cancellation.</remarks>
    Task WatchAsync(
        IActorRef actor,
        CancellationToken cancellationToken);

    /// <summary>
    /// Send a Dispose signal to a given actor, telling it to cancel operations and shut down.
    /// The actor is subsequently removed from the Supervisor once it has completed operations.
    /// </summary>
    /// <param name="actor">The actor to shutdown and dispose.</param>
    /// <param name="cancellationToken">The cancellation token for processes to signal cancellation.</param>
    /// <returns>A Task that completes when the Dispose signal has been sent to the actor. The actor may still be operating when this Task completes.</returns>
    Task DisposeActorAsync(
        IActorRef actor,
        CancellationToken cancellationToken);
}
