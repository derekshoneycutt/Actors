namespace Actors.Policies.RestartPolicies;

/// <summary>
/// A restart policy describing to always fail immediately, instead of any restarts
/// </summary>
public sealed class FailFastRestartPolicy
    : IRestartPolicy
{
    /// <summary>
    /// Gets a static instance of the fail fast restart policy
    /// </summary>
    public static FailFastRestartPolicy Instance { get; } = new();

    /// <summary>
    /// Performed when an actor has completed without fault
    /// </summary>
    /// <param name="attempt">The attempt index for the actor</param>
    /// <param name="cancellationToken">The token used to cancel asynchronous operations</param>
    /// <returns>A Task representing the operation, returning how to proceed with restart or not</returns>
    public Task<RestartPolicyResult> OnActorCompletionAsync(
        int attempt, CancellationToken cancellationToken)
    {
        return Task.FromResult(RestartPolicyResult.AbandonActor);
    }

    /// <summary>
    /// Performed when an actor has completed as cancelled
    /// </summary>
    /// <param name="attempt">The attempt index for the actor</param>
    /// <param name="ex">The exception describing the cancellation</param>
    /// <returns>A Task representing the operation, returning how to proceed with restart or not</returns>
    public Task<RestartPolicyResult> OnActorCancelledAsync(
        int attempt, OperationCanceledException ex)
    {
        return Task.FromResult(RestartPolicyResult.AbandonActor);
    }

    /// <summary>
    /// Performed when an actor has completed with fault
    /// </summary>
    /// <param name="attempt">The attempt index for the actor</param>
    /// <param name="ex">The exception describing the fault</param>
    /// <param name="cancellationToken">The token used to cancel asynchronous operations</param>
    /// <returns>A Task representing the operation, returning how to proceed with restart or not</returns>
    public Task<RestartPolicyResult> OnActorFaultedAsync(
        int attempt, Exception ex,
        CancellationToken cancellationToken)
    {
        return Task.FromResult(RestartPolicyResult.AbandonActor);
    }
}
