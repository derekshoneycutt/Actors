namespace Actors.Policies.RestartPolicies;

/// <summary>
/// Enum describing the possible restart actions to take
/// </summary>
public enum RestartPolicyResult
{
    /// <summary>
    /// Indicates to restart the actor
    /// </summary>
    RestartActor,

    /// <summary>
    /// Indicates to abandon the actor and let it free all state resources
    /// </summary>
    AbandonActor
}
