namespace Actors.Mailbox;

/// <summary>
/// Common mailbox channel provider instances for actor construction.
/// </summary>
public static class MailboxProviders
{
    /// <summary>
    /// Gets the default unbounded mailbox channel provider.
    /// </summary>
    public static IMailboxProvider Unbounded { get; } =
        new UnboundedMailboxProvider();

    /// <summary>
    /// Gets the default unbounded prioritized mailbox channel provider.
    /// </summary>
    public static IMailboxProvider UnboundedPrioritized { get; } =
        new UnboundedPrioritizedMailboxProvider();

    /// <summary>
    /// Creates a bounded mailbox channel provider with the specified default capacity.
    /// </summary>
    /// <param name="defaultCapacity">Default mailbox capacity used by the provider.</param>
    /// <returns>A bounded mailbox channel provider.</returns>
    public static IMailboxProvider Bounded(int defaultCapacity = 64)
    {
        return new BoundedMailboxProvider(defaultCapacity);
    }
}
