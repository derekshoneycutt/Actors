# Actors

Actors is an extremely lightweight .NET library implementing a take on the actor model,
using just the Task and Channel standard libraries. It runs on .NET 10.

This is not trying to be the big frameworks including network comms and all that jazz that
other projects solve for. This project is probably closer to DataFlow, but lower level.
The major difference aside from just being a light, low level actors implementation is the
absolute insistence that everything should be an actor. Even the hosted supervision
capabilities is another actor to us in this project, and only special in its relation and
function to other actors, really. **Everything** worth discussing is an actor.

A sample is present in `ActorMicroHttp` that does a basic static file GET/HEAD only HTTP
server. It is not even trying to be a proper HTTP server, just a demo proof of concept
for the actor model present in this library.

To be honest, this started as just playing with the actor model and using some basic text
parsing to play with it. I kept getting into the actor model as I explored different
programming languages, and also realizing that I had been leaning increasingly into a very
similar pattern with C# and Channels in a prior project that required a lot of concurrency
management. The `Machine` is largely the result of this experience wherein trying to make
an asynchronous state machine led to a remarkably actor-like structure almost exactly
matching the `Machine` present in this library.

This is basically a lil side project for me to get thinking about it out of my system. I
promise nothing, but maybe it is useful. Sure.

## Table Of Contents

1. [A (not so) Quick Overview](#a-not-so-quick-overview)
2. [Getting Started](#getting-started)
3. [Hosting Dependency Injection](#hosting-dependency-injection)
4. [Children and Spawning Actors](#children-and-spawning-actors)

## A (not so) Quick Overview

### Everything is an actor

The core building block is `IActor` types, constructed as classes that sit on the
ThreadPool and (maybe) act upon received messages. Actors own their own mailboxes and
other Actors send them messages via a `IActorRef<TMessage>` reference including a unique
address. Actor bases included in this library are Actor, Receiver, Machine, and Supervisor.

### Mailboxes

`System.Threading.Channels` is used for message mailboxes. All channels are constructed
via `IMailboxProvider` implementations. The common Unbounded, Bounded, and
UnboundedPrioritized channel providers are included and can be used for backpressure
and some degrees of memory management. It is simple to also provide a custom provider to
handle complex, custom messaging behaviors. The default channel provider is Unbounded,
providing no real backpressure protection.

### Receivers

The default Actors do not require that messages are ever responded to at all, although
mailboxes are inherently setup as part of their structure. A subset that only responds to
messages is the `Receiver` type actor. Instead of `RunAsync`, this requires implementers
to include responses to the message. This makes a message-oriented Actor. These are
particularly programmable for failure behavior. The default when an exception occurs on
some message is to fail for some owner to deal with. You can configure this to smoothly
continue receiving further messages or other behaviors by overriding methods while making
a subclass.

### State

The base `Actor` is essentially a stateless unit, but `Machine` is also included that
provides an idiomatic way to store immutable state across message boundaries within an
Actor. The state in these machines is not directly accessible to any other Actor, and is
designed to be used with immutable record types. C# has limited constraints on what can
be used for the state type, but this works best with immutable state updated only by
the standard interface and newer C# features like `currState with { Value = newValue }`.

### Supervision

When multiple actors are consistently performing standard actions with a common supervisor
pattern desired, a `Supervisor` Actor can be constructed. This is a special kind of
Actor, which along with comprehensive policy structures, allows responding to child Actor
failures and exceptions. This can run supervision capabilities over single and multiple
actors in a system.

### Routing

Routing options are also provided to manage broadcast and fan-out messaging behaviors and
other complex multi-actor management. The major part of this is represented through the
`ActorPool` collecting multiple similar Actors. `Router` classes route messages according
to varied algorithms. Included routing algorithms are Broadcast, Round-Robin, Weighted,
and Consistent-Hash.

### Error Handling

This library has a strong error handling support. Although the default effectively leans
into just throwing errors away to stderr, active error handling is directly available to
every actor via default references to an error handling actor. We are opinionated that
error handling should be explicitly available, but we are not particularly opinionated
about what the implementer does from that explicit standpoint.

## Getting Started

The easiest way to just have an actor and use it is to just create a subclass of the
`Receiver<TMessage>` type, create an instance, call `RunAsync`, and send it some messages.

```csharp
public sealed record WorkItem(string Payload);

public class Worker : Receiver<WorkItem>
{
    protected override async Task ProcessMessageAsync(
        WorkItem message, CancellationToken cancellationToken)
    {
        await DoWorkAsync(message.Payload, cancellationToken);
    }
}


await using Worker worker = new();

_ = worker.RunAsync(CancellationToken.None);

await worker.SendAsync(new("hello"), CancellationToken.None);
await worker.SendAsync(new("world"), CancellationToken.None);
await worker.SendAsync(new("and"), CancellationToken.None);
await worker.SendAsync(new("aliens"), CancellationToken.None);
await worker.SendAsync(new("too"), CancellationToken.None);
```

You can always use C# pattern matching to match overloaded types and deal with multiple,
complex message types on a single actor, even on this simple route.

Actors need to be started with `RunAsync` if not spawned via a supervisor. This returns a
Task that completes when the Run operation has completed. For Receiver actors, this is an
infinite loop until cancelled. In this example, the reception loop is fire-and-forget,
which is fine if you are okay with letting the actor potentially go until application
close. It may not do clean up operations you expect doing so, however, so use cautiously.
Calling `DisposeAsync` or using a CancellationToken can help add some determinism for
these actor types, even with fire-and-forget runs as it ends the loop. These examples
just use `await using`.

## Hosting Dependency Injection

Using the IHost based dependency injection is certainly the preferred route of using this
library. This allows adding actors into the dependency injection as keyed services that
can be referenced by their addresses. It adds a specialized `Supervisor` that starts the
`RunAsync` for all DI registered actors and enables spawning children, etc.

The default policy is to let some other mechanism in the host control the hosting lifetime,
but the `ShutdownListHostLifetimePolicy` allows creating a list of actors that once all of
the actors in the list have shutdown, the application host should also shut down. This can
be useful in console apps and the like, especially.

```csharp
await new HostBuilder()
    .ConfigureServices(services =>
    {
        _ = services
            .AddActor<WorkSender, EmptyMessage>("actor://sender")
            .AddActor<Worker, WorkItem>("actor://worker")
            .AddActorSupervision(config =>
            {
                config.HostLifetimePolicy =
                    new ShutdownListHostLifetimePolicy(["actor://sender"]);
            });
    })
    .Build()
    .RunAsync();

public sealed record EmptyMessage;
public sealed record WorkItem(string Payload);

public class WorkSender : Actor<EmptyMessage>
{
    private readonly IActorRef<WorkItem> _worker;

    public WorkSender(
        [FromKeyedServices("actor://worker")]
        IActorRef<WorkItem> worker)
    {
        _worker = worker;
    }

    public override async Task RunAsync(CancellationToken cancellationToken)
    {
        await _worker.SendAsync(new("hello"), CancellationToken.None);
        await _worker.SendAsync(new("world"), CancellationToken.None);
        await _worker.SendAsync(new("and"), CancellationToken.None);
        await _worker.SendAsync(new("aliens"), CancellationToken.None);
        await _worker.SendAsync(new("too"), CancellationToken.None);
    }
}

public class Worker : Receiver<WorkItem>
{
    protected override async Task ProcessMessageAsync(
        WorkItem message, CancellationToken cancellationToken)
    {
        await DoWorkAsync(message.Payload, cancellationToken);
    }
}
```

## Children and Spawning Actors

Sometimes, we want to spawn actors with the benefits of the DI supervision capabilities,
but we need to spawn them at run time in response to some event. For example, when a new
client connects to a server, we might spawn a new actor for each client connection. We
can just take an instance of `ISupervisor` from DI and use the Spawn methods.

We can also use `WatchAsync` to get the Task of a currently running actor. This allows us
to wait for its completion, and respond to exceptions and cancellation in standard C#
async/await syntax.

```csharp
public sealed class TcpHostActor
    : Actor<EmptyMessage>
{
    private readonly int _port;

    private readonly ISupervisor _supervisor;

    private int _clientIndex = 0;

    public TcpHostActor(
        CommandLineOptions options,
        ISupervisor supervisor,
        IMailboxProvider? mailboxChannelProvider = null)
        : base(mailboxChannelProvider)
    {
        _port = options.Port;
        _supervisor = supervisor;
    }

    public override async Task RunAsync(CancellationToken cancellationToken)
    {
        IActorRef<EmptyMessage> thisRef =
            _supervisor.This<TcpHostActor, EmptyMessage>(this);

        using TcpListener listener = new(IPAddress.Any, _port);

        listener.Start();

        while (!cancellationToken.IsCancellationRequested)
        {
            TcpClient client = await listener
                .AcceptTcpClientAsync(cancellationToken)
                .ConfigureAwait(false);

            int useIndex = Interlocked.Increment(ref _clientIndex);
            IActorRef<TcpClientInitMessage> clientActor =
                _supervisor.Spawn<TcpClientActor, TcpClientInitMessage>(
                    thisRef, $"std://tcp-clients/{useIndex}");
            await clientActor.SendAsync(new(client), cancellationToken)
                .ConfigureAwait(false);
        }
    }
}
```
