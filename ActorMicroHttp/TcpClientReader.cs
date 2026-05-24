using System.Net.Sockets;
using ActorMicroHttp.Messages;
using Actors;
using Actors.Base;
using Actors.Errors;
using Microsoft.Extensions.DependencyInjection;

namespace ActorMicroHttp;

public sealed class TcpClientReader
    : Receiver<TcpClientReaderInitMessage>
{
    public TcpClientReader(
        [FromKeyedServices("actor://error")]
        IActorRef<StandardError> errorActor)
        : base(errorActor)
    {
    }

    protected override async ValueTask ProcessMessageAsync(
        TcpClientReaderInitMessage message,
        CancellationToken cancellationToken)
    {
        using NetworkStream clientStream = message.Client.GetStream();

        await message.BuffererActor.SendAsync(
            new(message.ParserActor, message.ResponderActor,
                clientStream, TcpClientDataType.Initial, []),
            cancellationToken)
            .ConfigureAwait(false);

        try
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                byte[] buffer = new byte[1024];
                int readSize = await clientStream.ReadAsync(
                    buffer.AsMemory(),
                    cancellationToken)
                    .ConfigureAwait(false);
                if (readSize == 0)
                {
                    await message.BuffererActor.SendAsync(
                        new(message.ParserActor, message.ResponderActor, clientStream,
                            TcpClientDataType.EndOfStream, []),
                        cancellationToken)
                        .ConfigureAwait(false);
                    return;
                }

                await message.BuffererActor.SendAsync(
                    new(message.ParserActor, message.ResponderActor, clientStream,
                        TcpClientDataType.ReceivedData, buffer[..readSize]),
                    cancellationToken)
                    .ConfigureAwait(false);
            }
        }
        catch (IOException) when (cancellationToken.IsCancellationRequested)
        {
            // gtfo
        }
        finally
        {
            CloseMailbox();
        }
    }
}
