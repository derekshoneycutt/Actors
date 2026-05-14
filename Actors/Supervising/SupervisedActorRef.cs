namespace Actors.Supervising;

/// <summary>
/// Internal actor reference utilized to track the actor instance for an actor
/// </summary>
/// <typeparam name="TMessage">The message type the referenced actor receives.</typeparam>
/// <param name="address">The address of the actor</param>
/// <param name="actor">The initial actor instance</param>
internal sealed class SupervisedActorRef<TMessage>(
    string address, IActor<TMessage> actor)
    : IActorRef<TMessage>, ISupervisedActorRef
{
    /// <summary>
    /// The actor that this reference currently points to
    /// </summary>
    private IActor<TMessage> _actor = actor;

    /// <summary>
    /// Gets the address referencing the actor for message delivery and diagnostics.
    /// </summary>
    public string Address { get; } = address;

    /// <summary>
    /// Enqueues a message in the target actor's mailbox.
    /// Returns when the message is accepted; processing is asynchronous.
    /// </summary>
    /// <param name="message">Message to deliver to the target actor.</param>
    /// <param name="cancellationToken">
    /// Cancellation token controlling the enqueue operation.
    /// </param>
    /// <returns>A task that completes when the message is enqueued.</returns>
    public Task SendAsync(TMessage message, CancellationToken cancellationToken)
    {
        return _actor.SendAsync(message, cancellationToken);
    }

    /// <summary>
    /// Create a new instance of the actor and point this reference to it.
    /// </summary>
    /// <param name="supervisor">The supervisor that will watch over the actor.</param>
    /// <param name="serviceProvider">The hosting service provider to pull DI services from.</param>
    /// <param name="constructor">The delegate used to construct the new instance of the actor.</param>
    /// <returns>A Task that returns the new instance of the actor.</returns>
    public async Task<IActor> ChangeActorAsync(
        ISupervisor supervisor, IServiceProvider serviceProvider,
        Func<ISupervisor, IServiceProvider, IActor> constructor)
    {
        IActor newActor = constructor(supervisor, serviceProvider);
        if (newActor is IActor<TMessage> typedActor)
        {
            _actor = typedActor;
        }
        else
        {
            await newActor.DisposeAsync().ConfigureAwait(false);
            throw new InvalidOperationException(
                "Attempted to set incorrect Actor type to Actor Referen");
        }
        return newActor;
    }
}
