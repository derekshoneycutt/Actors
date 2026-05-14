using Actors.Supervising;
using Microsoft.Extensions.Hosting;

namespace Actors.Policies.HostLifetimePolicy;

/// <summary>
/// Policy describing a list of actor addresses that should signal host shutdown when they all complete
/// </summary>
public sealed class ShutdownListHostLifetimePolicy
    : IHostLifetimePolicy
{
    /// <summary>
    /// Get a static instance of the Shutdown List Host Lifetime Policy type
    /// </summary>
    private readonly List<string> _actorAddresses;

    /// <summary>
    /// Construct a new instance of the shutdown list host lifetime policy
    /// </summary>
    /// <param name="actorAddresses">The addresses of the actors to shutdown the host once all complete</param>
    public ShutdownListHostLifetimePolicy(
        params IEnumerable<string> actorAddresses)
    {
        _actorAddresses = [.. actorAddresses];
    }

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
        _ = _actorAddresses.Remove(actorState.ActorReference.Address);
        if (_actorAddresses.Count < 1)
        {
            hostLifetime.StopApplication();
        }
        return Task.CompletedTask;
    }
}
