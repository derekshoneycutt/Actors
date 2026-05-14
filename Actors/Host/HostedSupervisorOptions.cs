using Actors.Policies.HostLifetimePolicy;
using Actors.Supervising;

namespace Actors.Host;

/// <summary>
/// Options class describing the behavior of the supervisor actor
/// </summary>
public class HostedSupervisorOptions
    : SupervisorOptions
{
    /// <summary>
    /// Gets or Sets the policy that defines how to handle host lifetime
    /// </summary>
    public IHostLifetimePolicy HostLifetimePolicy { get; set; }
        = IgnoreHostLifetimePolicy.Instance;
}
