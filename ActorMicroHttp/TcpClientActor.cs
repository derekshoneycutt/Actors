using System.Net.Sockets;
using ActorMicroHttp.Messages;
using Actors;
using Actors.Base;
using Actors.Errors;
using Microsoft.Extensions.DependencyInjection;

namespace ActorMicroHttp;

public sealed class TcpClientActor
    : Receiver<TcpClientInitMessage>
{
    private readonly ISupervisor _supervisor;

    public TcpClientActor(
        ISupervisor supervisor,
        [FromKeyedServices("actor://error")]
        IActorRef<StandardError> errorActor)
        : base(errorActor)
    {
        _supervisor = supervisor;
    }

    protected override async ValueTask ProcessMessageAsync(
        TcpClientInitMessage message,
        CancellationToken cancellationToken)
    {
        using TcpClient client = message.Client;

        // Create a pipeline of children that will handle parts of the pipeline and send it to
        //  the next actor; then we just send an init to the first one
        IActorRef<TcpClientInitMessage> thisRef =
            _supervisor.This<TcpClientActor, TcpClientInitMessage>(this);

        IActorRef<TcpClientReaderInitMessage> tcpReaderActor =
            _supervisor.Spawn<TcpClientReader, TcpClientReaderInitMessage>(
                thisRef, $"{thisRef.Address}/tcp-reader");

        IActorRef<TcpClientDataReceivedMessage> tcpBufferer =
            _supervisor.Spawn<TcpMessageBufferer, TcpClientDataReceivedMessage>(
                thisRef, $"{thisRef.Address}/tcp-bufferer");

        IActorRef<HttpTextRequest> httpParser =
            _supervisor.Spawn<HttpParser, HttpTextRequest>(
                thisRef, $"{thisRef.Address}/http-parser");

        IActorRef<HttpParsedRequest> httpFileResponder =
            _supervisor.Spawn<HttpFileResponder, HttpParsedRequest>(
                thisRef, $"{thisRef.Address}/http-responder");

        await tcpReaderActor.SendAsync(
            new(tcpBufferer, httpParser, httpFileResponder, client),
            cancellationToken)
            .ConfigureAwait(false);

        await _supervisor.WatchAsync(httpFileResponder, cancellationToken)
            .ConfigureAwait(false);

        CloseMailbox();
    }
}
