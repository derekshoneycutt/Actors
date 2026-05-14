using Actors.Base;

namespace Actors.Errors;

/// <summary>
/// App-lifetime actor that writes <see cref="StandardError"/> payloads to
/// <see cref="Console.Error"/>.
/// </summary>
public class StandardErrorActor
    : Receiver<StandardError>
{
    /// <summary>
    /// Writes a formatted error record to <see cref="Console.Error"/>.
    /// </summary>
    /// <param name="message">Error payload emitted by actor components.</param>
    /// <param name="cancellationToken">Cancellation token controlling write behavior.</param>
    protected override Task ProcessMessageAsync(
        StandardError message,
        CancellationToken cancellationToken)
    {
        Console.Error.WriteLine(
            $"[{message.OccurredAtUtc:O}] {message.ActorKind}: {message.Message}{Environment.NewLine}{message.Exception}");
        return Task.CompletedTask;
    }
}
