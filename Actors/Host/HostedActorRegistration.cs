using Actors.Supervising;

namespace Actors.Host;

/// <summary>
/// Registration record describing an actor instance in hosting DI
/// </summary>
/// <param name="Address">The address to reference the actor by.</param>
/// <param name="Actor">The instance of the actor saved in the DI host.</param>
/// <param name="ActorReference">The reference of the actor.</param>
/// <param name="Options">Options used to describe the actor's expected behavior.</param>
public sealed record HostedActorRegistration(
    string Address,
    IActor Actor,
    ISupervisedActorRef ActorReference,
    SupervisedActorOptions Options);
