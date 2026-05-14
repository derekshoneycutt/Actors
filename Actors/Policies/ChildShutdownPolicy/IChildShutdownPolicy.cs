using Actors.Supervising;

namespace Actors.Policies.ChildShutdownPolicy;

/// <summary>
/// Interface describing policies for how to handle child shutdown procedures
/// </summary>
public interface IChildShutdownPolicy
{
    /// <summary>
    /// Handle children when their parents are shutdown
    /// </summary>
    /// <param name="children">The children to handle</param>
    /// <returns>A Task representing the asynchronous operation</returns>
    Task OnParentShutdownAsync(IEnumerable<SupervisedRunningActorState> children);
}
