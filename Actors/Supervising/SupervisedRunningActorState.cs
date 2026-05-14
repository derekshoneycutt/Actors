using System.Collections.Concurrent;
using Actors.Policies.ChildShutdownPolicy;
using Actors.Policies.RestartPolicies;

namespace Actors.Supervising;

/// <summary>
/// State class describing an actor hosted by a supervisor actor
/// </summary>
public sealed class SupervisedRunningActorState
{
    /// <summary>
    /// Gets the reference to the actor
    /// </summary>
    public required ISupervisedActorRef ActorReference { get; init; }

    /// <summary>
    /// Gets the children that the referenced actor is a parent to
    /// </summary>
    public ConcurrentDictionary<string, SupervisedRunningActorState> Children { get; init; }
        = [];

    /// <summary>
    /// Gets or sets the actual actor object reference
    /// </summary>
    public required IActor Actor { get; set; }

    /// <summary>
    /// Gets or sets the current running status of the actor
    /// </summary>
    public required ActorStatus Status { get; set; }

    /// <summary>
    /// Gets or sets the constructor delegate used to construct a new instance of the actor if needed
    /// </summary>
    public Func<ISupervisor, IServiceProvider, IActor>? Constructor { get; set; } = null;

    /// <summary>
    /// Gets or sets a reference to the parent actor, if the actor has a parent.
    /// </summary>
    public IActorRef? ParentReference { get; set; } = null;

    /// <summary>
    /// Gets or sets the task of the current actor run
    /// </summary>
    public Task? RunningTask { get; set; } = null;

    /// <summary>
    /// Gets or sets the last exception fault that occurred in the actor run
    /// </summary>
    public Exception? LastFault { get; set; } = null;

    /// <summary>
    /// Gets or sets the policy used to handle restarts
    /// </summary>
    public IRestartPolicy RestartPolicy { get; set; }
        = FailFastRestartPolicy.Instance;

    /// <summary>
    /// Gets or sets the policy used to shutdown children of the actor when the actor stops
    /// </summary>
    public IChildShutdownPolicy ChildShutdownPolicy { get; set; }
        = KillChildrenShutdownPolicy.Instance;
}
