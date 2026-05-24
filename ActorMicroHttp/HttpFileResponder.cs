using System.Text;
using ActorMicroHttp.Messages;
using Actors;
using Actors.Base;
using Actors.Errors;
using Microsoft.Extensions.DependencyInjection;

namespace ActorMicroHttp;

public sealed class HttpFileResponder
    : Receiver<HttpParsedRequest>
{
    private readonly CommandLineOptions _options;

    public HttpFileResponder(
        CommandLineOptions options,
        [FromKeyedServices("actor://error")]
        IActorRef<StandardError> errorActor)
        : base(errorActor)
    {
        _options = options;
    }

    protected override async ValueTask ProcessMessageAsync(
        HttpParsedRequest message,
        CancellationToken cancellationToken)
    {
        try
        {
            StringBuilder responseBuilder = new();
            string usePath = Path.Combine(_options.FilesDirectory, message.Path);
            if (File.Exists(usePath))
            {
                string fileText =
                    message.RequestType == HttpRequestType.Get
                    ? await File.ReadAllTextAsync(
                        usePath, cancellationToken)
                        .ConfigureAwait(false)
                    : string.Empty;

                _ = responseBuilder
                    .Append(message.Protocol)
                    .Append(" 200 OK\r\n")
                    .Append("Date: ")
                    .Append(File.GetLastWriteTime(usePath).ToString("r"))
                    .Append("\r\n")
                    .Append("Content-Type: text/html\r\n")
                    .Append("Content-Length: ")
                    .Append(fileText.Length)
                    .Append("\r\n\r\n")
                    .Append(fileText);
            }
            else
            {
                _ = responseBuilder
                    .Append(message.Protocol)
                    .Append(" 404 NOT FOUND\r\n")
                    .Append("Date: ")
                    .Append(DateTime.UtcNow.ToString("r"))
                    .Append("\r\n\r\n");
            }
            string response = responseBuilder.ToString();
            byte[] responseBytes = Encoding.UTF8.GetBytes(response);

            await message.Stream.WriteAsync(
                responseBytes, cancellationToken)
                .ConfigureAwait(false);
        }
        finally
        {
            CloseMailbox();
        }
    }
}
