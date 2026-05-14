using System.Net.Sockets;
using Actors;

namespace ActorMicroHttp.Messages;

public sealed record TcpClientReaderInitMessage(
    IActorRef<TcpClientDataReceivedMessage> BuffererActor,
    IActorRef<HttpTextRequest> ParserActor,
    IActorRef<HttpParsedRequest> ResponderActor,
    TcpClient Client);
