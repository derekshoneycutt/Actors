using System.Threading.Channels;

namespace Actors.Mailbox;

/// <summary>
/// Creates unbounded mailbox channels for actor message processing.
/// </summary>
public sealed class UnboundedMailboxProvider
    : IMailboxProvider
{
    /// <summary>
    /// Creates a mailbox channel for the requested message type.
    /// </summary>
    /// <typeparam name="TMessage">Message type written to and read from the mailbox channel.</typeparam>
    /// <param name="options">Mailbox creation options used by the provider.</param>
    /// <returns>A configured mailbox channel instance.</returns>
    public Channel<TMessage> Create<TMessage>(MailboxOptions options)
    {
        return Channel.CreateUnbounded<TMessage>(
            new()
            {
                SingleReader = options.SingleReader,
                SingleWriter = options.SingleWriter,
            });
    }
}
