using Actors.Supervising;
using Microsoft.Extensions.Hosting;

namespace Actors.Policies.HostLifetimePolicy;

/// <summary>
/// Interface describing a policy to control host lifetimes when actor shutdown occurs
/// </summary>
public interface IHostLifetimePolicy
{
    /// <summary>
    /// Method called when an actor is shutdown, offering opportunity to interact with host lifetime
    /// </summary>
    /// <param name="actorState">The state of the actor that is shutting down</param>
    /// <param name="hostLifetime">The host lifetime object</param>
    /// <returns>A Task representing the asynchronous operation</returns>
    Task OnActorShutdownAsync(
        SupervisedRunningActorState actorState,
        IHostApplicationLifetime hostLifetime);
}
