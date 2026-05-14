namespace Actors.Errors;

/// <summary>
/// Standard actor error payload.
/// </summary>
/// <param name="ActorKind">Actor kind or type name where the failure occurred.</param>
/// <param name="Message">Human-readable error summary.</param>
/// <param name="Exception">Original processing exception.</param>
/// <param name="OccurredAtUtc">UTC timestamp when the failure was captured.</param>
public record StandardError(
    string ActorKind,
    string Message,
    Exception Exception,
    DateTimeOffset OccurredAtUtc);
