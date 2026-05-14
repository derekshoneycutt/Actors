
using Actors.Base;
using Actors.Supervising;
using Actors.Mailbox;
using Actors.Policies.ChildShutdownPolicy;
using Actors.Policies.RestartPolicies;
using Actors.Errors;

namespace Actors.Routing;

/// <summary>
/// Base class for an actor that manages sending messages to a pool of actors that it spawns as children
/// </summary>
/// <typeparam name="TMessage">The type of message sent over the pool of actors</typeparam>
public abstract class SpawningActorPool<TActor, TMessage>
    : Receiver<TMessage>
    where TActor : class, IActor<TMessage>
{
    /// <summary>
    /// The size of pool; the number of actors to spawn
    /// </summary>
    private readonly int _size;

    /// <summary>
    /// The host supervisor to spawn child actors on
    /// </summary>
    private readonly ISupervisor _host;

    /// <summary>
    /// The restart policy for how to handle children actors
    /// </summary>
    private readonly IRestartPolicy _childRestartPolicy;

    /// <summary>
    /// The references to the spawned actors
    /// </summary>
    private readonly List<IActorRef<TMessage>> _actors;

    /// <summary>
    /// Gets the current list of references to the child actors
    /// </summary>
    protected IReadOnlyList<IActorRef<TMessage>> Actors => _actors;

    /// <summary>
    /// Abstract constructor for a new spawning actor pool instance
    /// </summary>
    /// <param name="size">The size of the pool to construct</param>
    /// <param name="host">The supervisor to spawn child actors via</param>
    /// <param name="childRestartPolicy">The restart policy to enforce on child actors</param>
    /// <param name="errorActor">The actor to send errors to for any further action</param>
    /// <param name="mailboxChannelProvider">The provider used to create the mailbox for the actor pool</param>
    public SpawningActorPool(
        int size,
        ISupervisor host,
        IRestartPolicy? childRestartPolicy = null,
        IActorRef<StandardError>? errorActor = null,
        IMailboxProvider? mailboxChannelProvider = null)
        : base(errorActor, mailboxChannelProvider)
    {
        if (size <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(size), "Size must be greater than zero.");
        }

        _size = size;
        _actors = new List<IActorRef<TMessage>>(size);
        _host = host;
        _childRestartPolicy = childRestartPolicy
            ?? FailFastRestartPolicy.Instance;
    }

    /// <summary>
    /// Initialize a new run of the actor pool, spawning the children actors
    /// </summary>
    /// <param name="cancellationToken">Token used to signal cancellation</param>
    /// <returns>A Task that completes when the operation is complete.</returns>
    protected override Task InitializeRunAsync(CancellationToken cancellationToken)
    {
        if (Actors.Count > 0)
        {
            return Task.CompletedTask;
        }

        IActorRef<TMessage> self =
            _host.This<SpawningActorPool<TActor, TMessage>, TMessage>(this);

        for (int i = 0; i < _size; ++i)
        {
            _actors.Add(_host.Spawn<TActor, TMessage>(
                self, $"{self.Address}/{i}", new SupervisedActorOptions()
                {
                    RestartPolicy = _childRestartPolicy,
                    ChildShutdownPolicy = KillChildrenShutdownPolicy.Instance
                }));
        }

        return Task.CompletedTask;
    }

    /// <summary>
    /// Disposes the actor asynchronously.
    /// </summary>
    /// <returns>A value task that completes when disposal is finished.</returns>
    protected override async ValueTask DisposeAsyncCore()
    {
        if (!IsDisposed)
        {
            _actors.Clear();
        }
        await base.DisposeAsyncCore();
    }

    /// <summary>
    /// Disposes the actor.
    /// </summary>
    /// <param name="disposing">Indicates whether the method is called from Dispose (true) or from a finalizer (false).</param>
    protected override void Dispose(bool disposing)
    {
        if (!IsDisposed)
        {
            if (disposing)
            {
                _actors.Clear();
            }
            base.Dispose(disposing);
        }
    }
}
