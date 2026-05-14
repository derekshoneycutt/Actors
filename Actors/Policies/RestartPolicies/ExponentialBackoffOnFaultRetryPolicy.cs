using System.Runtime.ExceptionServices;

namespace Actors.Policies.RestartPolicies;

/// <summary>
/// Policy describing to restart actors using an exponential backoff timing on fault
/// Abandons cancels and succesfully completed actors.
/// </summary>
public sealed class ExponentialBackoffOnFaultRetryPolicy
    : IRestartPolicy
{
    /// <summary>
    /// Gets a static instance of the exponential backoff restart policy
    /// </summary>
    public static ExponentialBackoffOnFaultRetryPolicy Instance { get; } = new();

    /// <summary>
    /// The timespan used to base the exponential backoff on
    /// </summary>
    public TimeSpan BaseBackoff { get; }

    /// <summary>
    /// The maximum number of attempts to make restarting the child
    /// </summary>
    public int MaxAttempts { get; }

    public ExponentialBackoffOnFaultRetryPolicy(
        TimeSpan? baseBackoff = null,
        int maxAttempts = 10)
    {
        if (baseBackoff < TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(
                nameof(baseBackoff),
                "Base backoff must be greater than or equal to zero.");
        }

        if (maxAttempts <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(maxAttempts),
                "Max attempts must be greater than zero.");
        }

        BaseBackoff = baseBackoff ?? TimeSpan.Zero;
        MaxAttempts = maxAttempts;
    }

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
    public async Task<RestartPolicyResult> OnActorFaultedAsync(
        int attempt, Exception ex,
        CancellationToken cancellationToken)
    {
        if (attempt <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(attempt),
                "Restart attempt must be greater than zero.");
        }
        if (attempt > MaxAttempts)
        {
            ExceptionDispatchInfo.Throw(ex);
        }

        if (BaseBackoff == TimeSpan.Zero)
        {
            ExceptionDispatchInfo.Throw(ex);
        }

        int exponent = Math.Min(attempt - 1, 30);
        long multiplier = 1L << exponent;
        long ticks = BaseBackoff.Ticks * multiplier;
        TimeSpan waitFor = ticks < 0 || ticks > TimeSpan.MaxValue.Ticks
            ? TimeSpan.MaxValue
            : TimeSpan.FromTicks(ticks);
        await Task.Delay(waitFor, cancellationToken).ConfigureAwait(false);
        return RestartPolicyResult.RestartActor;
    }
}
