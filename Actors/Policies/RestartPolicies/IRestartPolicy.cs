namespace Actors.Policies.RestartPolicies;

/// <summary>
/// Interface describing policies that control how actors are restarted
/// </summary>
public interface IRestartPolicy
{
    /// <summary>
    /// Performed when an actor has completed without fault
    /// </summary>
    /// <param name="attempt">The attempt index for the actor</param>
    /// <param name="cancellationToken">The token used to cancel asynchronous operations</param>
    /// <returns>A Task representing the operation, returning how to proceed with restart or not</returns>
    Task<RestartPolicyResult> OnActorCompletionAsync(
        int attempt, CancellationToken cancellationToken);

    /// <summary>
    /// Performed when an actor has completed as cancelled
    /// </summary>
    /// <param name="attempt">The attempt index for the actor</param>
    /// <param name="ex">The exception describing the cancellation</param>
    /// <returns>A Task representing the operation, returning how to proceed with restart or not</returns>
    Task<RestartPolicyResult> OnActorCancelledAsync(
        int attempt, OperationCanceledException ex);

    /// <summary>
    /// Performed when an actor has completed with fault
    /// </summary>
    /// <param name="attempt">The attempt index for the actor</param>
    /// <param name="ex">The exception describing the fault</param>
    /// <param name="cancellationToken">The token used to cancel asynchronous operations</param>
    /// <returns>A Task representing the operation, returning how to proceed with restart or not</returns>
    Task<RestartPolicyResult> OnActorFaultedAsync(
        int attempt, Exception ex,
        CancellationToken cancellationToken);
}
