using System.CommandLine;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Actors;
using Actors.Policies.HostLifetimePolicy;
using ActorMicroHttp;
using ActorMicroHttp.Messages;

Option<int> portOption = new("--port", ["-p"])
{
    Description = "The HTTP port to listen to clients on.",
    Arity = ArgumentArity.ZeroOrOne,
    DefaultValueFactory = _ => 80
};

Option<string> filesDirectoryOption = new("--filesdir", ["-f"])
{
    Description = "The directory to read static files from",
    Arity = ArgumentArity.ZeroOrOne,
    DefaultValueFactory = _ => "./Files/"
};

var rootCommand = new RootCommand("Actor-based Micro HTTP Server");
rootCommand.Options.Add(portOption);
rootCommand.Options.Add(filesDirectoryOption);

rootCommand.SetAction(async (ParseResult) =>
{
    int port = ParseResult.GetValue(portOption);
    string filesDirectory = ParseResult.GetValue(filesDirectoryOption) ?? "./Files/";

    CommandLineOptions options = new(port, filesDirectory);

    await new HostBuilder()
        .ConfigureServices(services =>
        {
            _ = services
                .AddSingleton(options)
                .AddActor<TcpHostActor, EmptyMessage>("actor://tcp-host")
                .AddActorSupervision(config =>
                {
                    config.HostLifetimePolicy =
                        new ShutdownListHostLifetimePolicy(["actor://tcp-host"]);
                });
        })
        .Build()
        .RunAsync();

    return 0;
});

return await rootCommand.Parse(args).InvokeAsync();
