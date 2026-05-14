using System.Net.Sockets;
using Actors;

namespace ActorMicroHttp.Messages;

public sealed record HttpTextRequest(
    IActorRef<HttpParsedRequest> ResponderActor,
    NetworkStream Stream,
    string BufferedRequest);
