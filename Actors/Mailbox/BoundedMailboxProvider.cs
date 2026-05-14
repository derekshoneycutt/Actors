using System.Threading.Channels;

namespace Actors.Mailbox;

/// <summary>
/// Creates bounded mailbox channels for actor message processing.
/// </summary>
public sealed class BoundedMailboxProvider
    : IMailboxProvider
{
    /// <summary>
    /// Default capacity for bounded mailbox channels.
    /// </summary>
    private readonly int _defaultCapacity;

    /// <summary>
    /// Initializes a bounded provider with a default capacity.
    /// </summary>
    /// <param name="defaultCapacity">Default mailbox capacity used when options do not specify one.</param>
    public BoundedMailboxProvider(int defaultCapacity = 64)
    {
        if (defaultCapacity <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(defaultCapacity),
                "Default bounded mailbox capacity must be greater than zero.");
        }

        _defaultCapacity = defaultCapacity;
    }

    /// <summary>
    /// Creates a mailbox channel for the requested message type.
    /// </summary>
    /// <typeparam name="TMessage">Message type written to and read from the mailbox channel.</typeparam>
    /// <param name="options">Mailbox creation options used by the provider.</param>
    /// <returns>A configured mailbox channel instance.</returns>
    public Channel<TMessage> Create<TMessage>(MailboxOptions options)
    {
        int capacity = options.Capacity ?? _defaultCapacity;
        return Channel.CreateBounded<TMessage>(
            new BoundedChannelOptions(capacity)
            {
                SingleReader = options.SingleReader,
                SingleWriter = options.SingleWriter,
                FullMode = options.BoundedFullMode,
            });
    }
}
