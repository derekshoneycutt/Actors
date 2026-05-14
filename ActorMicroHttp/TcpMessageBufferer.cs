using System.Text;
using ActorMicroHttp.Messages;
using Actors;
using Actors.Base;
using Actors.Errors;
using Actors.Mailbox;
using Microsoft.Extensions.DependencyInjection;

namespace ActorMicroHttp;

public sealed record TcpMessageBuffererState(
    string BufferedRequest)
    : MachineState;

public sealed class TcpMessageBufferer
    : Machine<TcpClientDataReceivedMessage, TcpMessageBuffererState>
{
    public TcpMessageBufferer(
        [FromKeyedServices("actor://error")]
        IActorRef<StandardError> errorActor,
        IMailboxProvider? mailboxChannelProvider = null)
        : base(new(string.Empty), errorActor, mailboxChannelProvider)
    {
    }

    protected override async Task<TcpMessageBuffererState?> ProcessMessageWithStateAsync(
        TcpClientDataReceivedMessage message,
        TcpMessageBuffererState currentState,
        CancellationToken cancellationToken)
    {
        switch (message.DataType)
        {
            case TcpClientDataType.Initial:
                return currentState with { BufferedRequest = string.Empty };

            case TcpClientDataType.ReceivedData:
                string newBuffer = currentState.BufferedRequest +
                    Encoding.UTF8.GetString(message.Data);
                int endHeaderIndex = newBuffer.IndexOf("\r\n\r\n");
                if (endHeaderIndex > -1)
                {
                    string request = newBuffer[..endHeaderIndex];
                    newBuffer = newBuffer[(endHeaderIndex + 4)..];
                    await message.ParserActor.SendAsync(
                        new(message.ResponderActor, message.Stream, request), cancellationToken)
                        .ConfigureAwait(false);
                    CloseMailbox();
                }
                return currentState with { BufferedRequest = newBuffer };

            case TcpClientDataType.EndOfStream:
                CloseMailbox();
                return currentState with { BufferedRequest = string.Empty };

            default:
                return currentState;
        }
    }
}
