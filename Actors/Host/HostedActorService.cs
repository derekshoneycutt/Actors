using Microsoft.Extensions.Hosting;

namespace Actors.Host;

/// <summary>
/// Internal service used to start and stop actors from the DI host
/// </summary>
internal sealed class HostedActorService
    : IHostedService
{
    /// <summary>
    /// The supervisor registry used to manage actor lifetimes
    /// </summary>
    private readonly IActorHostRegistry _actorHostRegistry;

    /// <summary>
    /// Construct a new instance of the hosted actor service
    /// </summary>
    /// <param name="actorHostRegistry">The supervisor registry to manage actor lifetime with</param>
    /// <param name="registrations">The registrations of actors added to DI</param>
    public HostedActorService(
        IActorHostRegistry actorHostRegistry,
        IEnumerable<HostedActorRegistration> registrations)
    {
        _actorHostRegistry = actorHostRegistry;
        _actorHostRegistry.Initialize(registrations);
    }

    /// <summary>
    /// Start the hosted actor services, requesting all DI actors to run via the supervisor.
    /// </summary>
    /// <param name="cancellationToken">The token used to signal operation cancellation.</param>
    /// <returns>A Task that completes at the end of the asynchronous operation</returns>
    public Task StartAsync(CancellationToken cancellationToken)
    {
        _ = _actorHostRegistry.RunAsync(cancellationToken);
        return Task.CompletedTask;
    }

    /// <summary>
    /// Stop the hosted actor services, requesting all DI actors to halt via the supervisor.
    /// </summary>
    /// <param name="cancellationToken">The token used to signal operation cancellation.</param>
    /// <returns>A Task that completes at the end of the asynchronous operation</returns>
    public async Task StopAsync(CancellationToken cancellationToken)
    {
        await _actorHostRegistry.StopAllRegisteredAsync(cancellationToken)
            .ConfigureAwait(false);
    }
}
