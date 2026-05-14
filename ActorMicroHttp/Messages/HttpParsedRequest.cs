using System.Net.Sockets;

namespace ActorMicroHttp.Messages;

public sealed record HttpParsedRequest(
    NetworkStream Stream,
    HttpRequestType RequestType,
    string Path,
    string Protocol,
    Dictionary<string, string> Headers);
