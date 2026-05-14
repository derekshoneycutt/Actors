namespace Actors.Supervising;

/// <summary>
/// Interface describing an actor reference with internal metadata for hosted actors
/// </summary>
public interface ISupervisedActorRef : IActorRef
{
    /// <summary>
    /// Change the actor instance that is actually under the reference
    /// </summary>
    /// <param name="supervisor">The supervisor that oversees the actor.</param>
    /// <param name="serviceProvider">The service provider used to get DI services.</param>
    /// <param name="constructor">The delegate used to construct the new actor instance.</param>
    /// <returns>A Task that returns the new actor instance when the operation has completed.</returns>
    Task<IActor> ChangeActorAsync(
        ISupervisor supervisor,
        IServiceProvider serviceProvider,
        Func<ISupervisor, IServiceProvider, IActor> constructor);
}
