using ActorMicroHttp.Messages;
using Actors;
using Actors.Base;
using Actors.Errors;
using Microsoft.Extensions.DependencyInjection;

namespace ActorMicroHttp;

public sealed class HttpParser
    : Receiver<HttpTextRequest>
{
    public HttpParser(
        [FromKeyedServices("actor://error")]
        IActorRef<StandardError> errorActor)
        : base(errorActor)
    {
    }

    protected override async ValueTask ProcessMessageAsync(
        HttpTextRequest message,
        CancellationToken cancellationToken)
    {
        try
        {
            string[] lines = message.BufferedRequest.Split("\r\n",
                StringSplitOptions.RemoveEmptyEntries);
            if (lines.Length < 1)
            {
                return;
            }

            string[] requestLine = lines[0].Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (requestLine.Length < 3)
            {
                return;
            }

            (string requestTypeString, string path, string protocol)
                = (requestLine[0], requestLine[1], requestLine[2]);
            Dictionary<string, string> headers = [];
            for (int i = 1; i < lines.Length; ++i)
            {
                int sep = lines[i].IndexOf(':');
                if (sep > -1)
                {
                    headers[lines[i][..sep]] = lines[i][(sep + 1)..];
                }
            }

            HttpRequestType requestType = requestTypeString.ToUpper() switch
            {
                "GET" => HttpRequestType.Get,
                "HEAD" => HttpRequestType.Head,
                "POST" => HttpRequestType.Post,
                "PUT" => HttpRequestType.Put,
                "PATCH" => HttpRequestType.Patch,
                "DELETE" => HttpRequestType.Delete,
                _ => HttpRequestType.Unknown
            };

            if (path.EndsWith('/'))
            {
                path += "index.html";
            }
            while (path.StartsWith('/'))
            {
                path = path[1..];
            }

            HttpParsedRequest parsedRequest =
                new(message.Stream, requestType, path, protocol, headers);

            if (requestType is not HttpRequestType.Get and not HttpRequestType.Head)
            {
                return;
            }

            await message.ResponderActor.SendAsync(
                parsedRequest, cancellationToken)
                .ConfigureAwait(false);
        }
        finally
        {
            CloseMailbox();
        }
    }
}
