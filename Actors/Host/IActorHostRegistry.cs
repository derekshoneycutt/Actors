namespace Actors.Host;

/// <summary>
/// Internal interface describing the internal interface of the supervisor in DI hosting
/// </summary>
internal interface IActorHostRegistry
{
    /// <summary>
    /// Initialize the supervisor host registry with a set of actor registrations.
    /// </summary>
    /// <param name="registrations">The set of registrations to initialize in the supervisor.</param>
    void Initialize(IEnumerable<HostedActorRegistration> registrations);

    /// <summary>
    /// Run the primary loop of the Supervisor process
    /// </summary>
    /// <param name="cancellationToken">The cancellation token for processes to signal cancellation.</param>
    /// <returns>A Task that completes when the operation has finished.</returns>
    Task RunAsync(CancellationToken cancellationToken);

    /// <summary>
    /// Stop all registered actors and shut down the supervision process.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token for processes to signal cancellation.</param>
    /// <returns>A Task that completes when the operation has finished.</returns>
    Task StopAllRegisteredAsync(CancellationToken cancellationToken);
}
