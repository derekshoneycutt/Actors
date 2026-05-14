namespace Actors.Supervising;

/// <summary>
/// Enum describing the status of an actor
/// </summary>
public enum ActorStatus
{
    /// <summary>
    /// The actor is registered but has not been started yet
    /// </summary>
    Registered,

    /// <summary>
    /// The actor is currently running
    /// </summary>
    Running,

    /// <summary>
    /// The actor has faulted and has not restarted
    /// </summary>
    Faulted,

    /// <summary>
    /// The actor has stopped running without fault
    /// </summary>
    Stopped,

    /// <summary>
    /// The actor has been cancelled
    /// </summary>
    Cancelled
}
