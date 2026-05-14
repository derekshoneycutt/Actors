using System.Threading.Channels;

namespace Actors.Mailbox;

/// <summary>
/// Configures mailbox channel construction behavior for actors.
/// </summary>
public sealed class MailboxOptions
{
    /// <summary>
    /// Gets or sets whether the mailbox channel has a single logical reader.
    /// </summary>
    public bool SingleReader { get; set; } = true;

    /// <summary>
    /// Gets or sets whether the mailbox channel has a single logical writer.
    /// </summary>
    public bool SingleWriter { get; set; } = false;

    /// <summary>
    /// Gets or sets bounded channel capacity when using bounded providers.
    /// Null delegates capacity decisions to the provider.
    /// </summary>
    public int? Capacity { get; set; }

    /// <summary>
    /// Gets or sets bounded channel full-mode behavior.
    /// </summary>
    public BoundedChannelFullMode BoundedFullMode { get; set; } = BoundedChannelFullMode.Wait;
}
