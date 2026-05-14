using Actors.Supervising;
using Microsoft.Extensions.Hosting;

namespace Actors.Policies.HostLifetimePolicy;

/// <summary>
/// Policy describing to simply ignore when actors are shutdown and don't do any host lifetime action
/// </summary>
public sealed class IgnoreHostLifetimePolicy
    : IHostLifetimePolicy
{
    /// <summary>
    /// Get a static instance of the Ignore Host Lifetime Policy type
    /// </summary>
    public static IgnoreHostLifetimePolicy Instance { get; } = new();

    /// <summary>
    /// Method called when an actor is shutdown, offering opportunity to interact with host lifetime
    /// </summary>
    /// <param name="actorState">The state of the actor that is shutting down</param>
    /// <param name="hostLifetime">The host lifetime object</param>
    /// <returns>A Task representing the asynchronous operation</returns>
    public Task OnActorShutdownAsync(
        SupervisedRunningActorState actorState,
        IHostApplicationLifetime hostLifetime)
    {
        return Task.CompletedTask;
    }
}
