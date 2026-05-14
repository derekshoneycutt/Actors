namespace Actors.Errors;

/// <summary>
/// Specifies how a receiver should proceed after handling a message-processing error.
/// </summary>
public enum ReceiverErrorAction
{
    /// <summary>
    /// Continue processing subsequent messages.
    /// </summary>
    Continue = 0,

    /// <summary>
    /// Fail the run loop immediately by rethrowing the original processing exception.
    /// </summary>
    Fail = 1,
}
