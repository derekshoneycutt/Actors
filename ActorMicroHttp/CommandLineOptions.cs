namespace ActorMicroHttp;

public sealed record CommandLineOptions(
    int Port,
    string FilesDirectory);
