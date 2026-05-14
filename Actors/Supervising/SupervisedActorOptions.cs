using Actors.Policies.ChildShutdownPolicy;
using Actors.Policies.RestartPolicies;

namespace Actors.Supervising;

/// <summary>
/// Options class describing how a hosted actor behaves
/// </summary>
public sealed class SupervisedActorOptions
{
    /// <summary>
    /// Gets or Sets the restart policy to use when an actor is stopped or faulted
    /// </summary>
    public IRestartPolicy RestartPolicy { get; set; }
        = FailFastRestartPolicy.Instance;

    /// <summary>
    /// Gets or Sets the shutdown policy for children of stopped actors
    /// </summary>
    public IChildShutdownPolicy ChildShutdownPolicy { get; set; }
        = KillChildrenShutdownPolicy.Instance;
}
