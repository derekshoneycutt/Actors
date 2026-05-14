using System.Threading.Channels;

namespace Actors.Mailbox;

/// <summary>
/// Creates mailbox channels for actor input processing.
/// </summary>
public interface IMailboxProvider
{
    /// <summary>
    /// Creates a mailbox channel for the given message type.
    /// </summary>
    /// <typeparam name="TMessage">Message type written to and read from the channel.</typeparam>
    /// <param name="options">Mailbox channel options used by the provider.</param>
    /// <returns>A channel instance used as an actor mailbox.</returns>
    Channel<TMessage> Create<TMessage>(MailboxOptions options);
}
