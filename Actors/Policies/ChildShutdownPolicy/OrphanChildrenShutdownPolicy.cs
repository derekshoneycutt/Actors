using Actors.Supervising;

namespace Actors.Policies.ChildShutdownPolicy;

/// <summary>
/// Child shutdown policy specifying to simply orphan the children
/// </summary>
public sealed class OrphanChildrenShutdownPolicy
    : IChildShutdownPolicy
{
    /// <summary>
    /// Get a static instance of the Orphan Children Shutdown Policy type
    /// </summary>
    public static OrphanChildrenShutdownPolicy Instance { get; } = new();

    /// <summary>
    /// Handle children when their parents are shutdown
    /// </summary>
    /// <param name="children">The children to handle</param>
    /// <returns>A Task representing the asynchronous operation</returns>
    public Task OnParentShutdownAsync(IEnumerable<SupervisedRunningActorState> children)
    {
        foreach (SupervisedRunningActorState child in children)
        {
            child.ParentReference = null;
        }
        return Task.CompletedTask;
    }
}
