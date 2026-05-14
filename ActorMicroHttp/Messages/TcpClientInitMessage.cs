using System.Net.Sockets;

namespace ActorMicroHttp.Messages;

public sealed record TcpClientInitMessage(
    TcpClient Client);
