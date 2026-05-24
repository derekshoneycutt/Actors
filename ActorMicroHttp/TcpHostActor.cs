using System.Net;
using System.Net.Sockets;
using ActorMicroHttp.Messages;
using Actors;
using Actors.Base;
using Actors.Mailbox;

namespace ActorMicroHttp;

public sealed class TcpHostActor
    : Actor<EmptyMessage>
{
    private readonly int _port;

    private readonly ISupervisor _supervisor;

    private int _clientIndex = 0;

    public TcpHostActor(
        CommandLineOptions options,
        ISupervisor supervisor,
        IMailboxProvider? mailboxChannelProvider = null)
        : base(mailboxChannelProvider)
    {
        _port = options.Port;
        _supervisor = supervisor;
    }

    public override async ValueTask RunAsync(CancellationToken cancellationToken)
    {
        IActorRef<EmptyMessage> thisRef =
            _supervisor.This<TcpHostActor, EmptyMessage>(this);

        using TcpListener listener = new(IPAddress.Any, _port);

        try
        {
            listener.Start();

            while (!cancellationToken.IsCancellationRequested)
            {
                TcpClient client = await listener
                    .AcceptTcpClientAsync(cancellationToken)
                    .ConfigureAwait(false);

                int useIndex = Interlocked.Increment(ref _clientIndex);
                IActorRef<TcpClientInitMessage> clientActor =
                    _supervisor.Spawn<TcpClientActor, TcpClientInitMessage>(
                        thisRef, $"std://tcp-clients/{useIndex}");
                await clientActor.SendAsync(new(client), cancellationToken)
                    .ConfigureAwait(false);
            }
        }
        catch (OperationCanceledException)
        {
            // stfu
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Error listening: {ex}");
            return;
        }
    }
}
