using System.Net.Sockets;
using Actors;

namespace ActorMicroHttp.Messages;

public record TcpClientDataReceivedMessage(
    IActorRef<HttpTextRequest> ParserActor,
    IActorRef<HttpParsedRequest> ResponderActor,
    NetworkStream Stream,
    TcpClientDataType DataType,
    byte[] Data);
