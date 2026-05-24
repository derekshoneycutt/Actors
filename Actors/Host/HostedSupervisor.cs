using Actors.Errors;
using Actors.Supervising;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Actors.Host;

/// <summary>
/// Implementation of the Supervisor class intended for use through IHost Dependency Injection
/// </summary>
public class HostedSupervisor
    : Supervisor, IActorHostRegistry
{
    /// <summary>
    /// The hosted supervisor options
    /// </summary>
    private readonly HostedSupervisorOptions _options;

    /// <summary>
    /// The host lifetime handle used to signal the host ready for shutdown
    /// </summary>
    private readonly IHostApplicationLifetime _hostLifetime;

    /// <summary>
    /// Construct a new instance of the Supervisor Actor.
    /// This is intended to be loaded with DI via the AddActorSupervision extension method
    /// </summary>
    /// <param name="options">The options to load the supervisor with.</param>
    /// <param name="hostLifetime">The lifetime handle for the current host instance.</param>
    /// <param name="serviceProvider">The service provider for the current host instance.</param>
    public HostedSupervisor(
        HostedSupervisorOptions options,
        IHostApplicationLifetime hostLifetime,
        IServiceProvider serviceProvider,
        [FromKeyedServices("actor://error")]
        IActorRef<StandardError> errorActor)
        : base(options, serviceProvider, errorActor)
    {
        _options = options;
        _hostLifetime = hostLifetime;
    }

    /// <summary>
    /// Virtual method that is called when a dead actor is removed from the supervisor
    /// </summary>
    /// <param name="state">The state of the actor that was removed</param>
    /// <returns>A Task that represents the asynchronous operation.</returns>
    protected override async Task OnDeadActorRemovedAsync(SupervisedRunningActorState state)
    {
        await _options.HostLifetimePolicy.OnActorShutdownAsync(state, _hostLifetime)
            .ConfigureAwait(false);
    }


    /// <summary>
    /// Initialize the supervisor host registry with a set of actor registrations.
    /// </summary>
    /// <param name="registrations">The set of registrations to initialize in the supervisor.</param>
    public void Initialize(
        IEnumerable<HostedActorRegistration> registrations)
    {
        foreach (HostedActorRegistration registration in registrations)
        {
            SupervisedRunningActorState factory()
            {
                return new SupervisedRunningActorState
                {
                    Actor = registration.Actor,
                    ActorReference = registration.ActorReference,
                    Status = ActorStatus.Registered,
                    RestartPolicy = registration.Options.RestartPolicy,
                    ChildShutdownPolicy = registration.Options.ChildShutdownPolicy
                };
            }

            _ = HostedStates.AddOrUpdate(registration.Address,
                factory(), (_, _) => factory());
        }
    }

    /// <summary>
    /// Stop all registered actors and shut down the supervision process.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token for processes to signal cancellation.</param>
    /// <returns>A Task that completes when the operation has finished.</returns>
    public async ValueTask StopAllRegisteredAsync(CancellationToken cancellationToken)
    {
        List<SupervisedRunningActorState> states = [.. HostedStates.Select(kvp => kvp.Value)];
        await CancelAllActorsAsync().ConfigureAwait(false);
        foreach (SupervisedRunningActorState state in states)
        {
            await state.Actor.DisposeAsync().ConfigureAwait(false);
            if (state.RunningTask is not null)
            {
                try
                {
                    await state.RunningTask;
                }
                catch (Exception ex)
                {
                    Console.Error.WriteLine($"Error while awaiting running task: {ex}");
                }
            }
        }
    }
}
