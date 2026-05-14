using Actors.Mailbox;

namespace Actors.Supervising;

/// <summary>
/// Options class describing the behavior of the supervisor actor
/// </summary>
public class SupervisorOptions
{
    /// <summary>
    /// Gets or Sets the provider for the supervisor's mailbox
    /// </summary>
    public IMailboxProvider HostMailboxProvider { get; set; }
        = MailboxProviders.Unbounded;
}
