using Actors.Supervising;

namespace Actors.Policies.ChildShutdownPolicy;

/// <summary>
/// Child shutdown policy specifying to also shutdown the children
/// </summary>
public sealed class KillChildrenShutdownPolicy
    : IChildShutdownPolicy
{
    /// <summary>
    /// Get a static instance of the Kill Children Shutdown Policy type
    /// </summary>
    public static KillChildrenShutdownPolicy Instance { get; } = new();

    /// <summary>
    /// Handle children when their parents are shutdown
    /// </summary>
    /// <param name="children">The children to handle</param>
    /// <returns>A Task representing the asynchronous operation</returns>
    public async Task OnParentShutdownAsync(IEnumerable<SupervisedRunningActorState> children)
    {
        foreach (SupervisedRunningActorState child in children)
        {
            await child.Actor.DisposeAsync().ConfigureAwait(false);
        }
    }
}
