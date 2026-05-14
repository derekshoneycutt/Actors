namespace Actors.Supervising;

/// <summary>
/// Message describing a request to kill an actor.
/// </summary>
/// <param name="Actor">The actor being requested to be killed</param>
public sealed record KillActorMessage(IActorRef Actor)
    : SupervisorMessage;
