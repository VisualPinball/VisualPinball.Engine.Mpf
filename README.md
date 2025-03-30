# Visual Pinball Engine - MPF Gamelogic Engine

[![UPM Package](https://img.shields.io/npm/v/org.visualpinball.engine.missionpinball?label=org.visualpinball.engine.missionpinball&registry_uri=https://registry.visualpinball.org&color=%2333cf57&logo=unity&style=flat)](https://registry.visualpinball.org/-/web/detail/org.visualpinball.engine.missionpinball)

_Enables the Mission Pinball Framework to drive VPE_

Documentation at <https://docs.visualpinball.org/plugins/mpf/>

## Overview

[MPF](https://missionpinball.org/latest/about/) is an open-source framework
written in Python to drive real pinball machines. It has a "configuration over
code" approach, meaning that 90% of what you'd do in a pinball game can be
achieved through configuration (YAML files) rather than implementing it in code.

When you read MPF's [Getting Started](https://missionpinball.org/latest/start/)
page, you'll notice a banner stating that "MPF is not a simulator." Well, you've
found the simulator. ;)

This project lets you use MPF to drive game logic in
[VPE](https://github.com/freezy/VisualPinball.Engine), a pinball simulator based
on Unity. It does this by spawning a Python process running MPF and
communicating with VPE through [gRPC](https://grpc.io/).

## User setup

This project is available as a Unity package at `registry.visualpinball.org`. To
install it, make sure the scoped registry is added in your Package Manager
settings. Then open the Package Manager in Unity and add
`org.visualpinball.engine.missionpinball` under _Install package by name_.

The Unity package is built and published to our registry on every merge to
master.

# Use

1. Add the `MpfGamelogicEngine` component to the root object of your table
2. Click on 'Get Machine Desciption' in its inspector
3. Click on 'Populate Hardware' to bring the coils switches and lamps from MPFs
   machine description into VPEs respective manager windows
4. Assign the coils, switches and lamps to items on your playfield using the
   coil, switch and lamp manager windows

## Development Setup

In order to import the package locally instead from our registry, clone and
compile it. This will copy the necessary binaries into the Unity folder. Only
then, import the project into Unity.

Since the Unity folder contains `.meta` files of the binaries, but not the
actual binaries, `.meta` files of uncompiled platforms are cleaned up by Unity.
In order to not accidentally commit those files, we recommend to ignore them:

```bash
git update-index --assume-unchanged Plugins/Google.Protobuf.dll.meta
git update-index --assume-unchanged Plugins/Grpc.Core.Api.dll.meta
git update-index --assume-unchanged Plugins/Grpc.Net.Client.dll.meta
git update-index --assume-unchanged Plugins/Grpc.Net.Common.dll.meta
git update-index --assume-unchanged Plugins/Microsoft.Extensions.Logging.Abstractions.dll.meta
git update-index --assume-unchanged Plugins/System.Buffers.dll.meta
git update-index --assume-unchanged Plugins/System.Diagnostics.DiagnosticSource.dll.meta
git update-index --assume-unchanged Plugins/System.IO.Pipelines.dll.meta
git update-index --assume-unchanged Plugins/System.Memory.dll.meta
git update-index --assume-unchanged Plugins/System.Numerics.Vectors.dll.meta
git update-index --assume-unchanged Plugins/System.Runtime.CompilerServices.Unsafe.dll.meta
git update-index --assume-unchanged Plugins/System.Threading.Tasks.Extensions.dll.meta
git update-index --assume-unchanged Plugins/VisualPinball.Engine.Mpf.deps.json.meta
git update-index --assume-unchanged Plugins/VisualPinball.Engine.Mpf.dll.meta
git update-index --assume-unchanged Plugins/VisualPinball.Engine.Mpf.pdb.meta
```

## Design

MPF is a standalone Python application and not a library. Despite this, we want
to make its use with VPE as seamless as possible, so we include precompiled MPF
binaries and start them together with VPE automatically. The process of starting
up and communicating with MPF via gRPC while respecting the many configuration
options that are available is quite complex and managed in a single class:
`MpfWrangler`. It could be worthwhile to split it up into one class that manages
the MPF process and another that manages the gRPC connection.

MPF has two APIs: One to control hardware on the playfield, and one to play
media in the backbox. The former is the main, essential part of this MPF
integration and the latter was added later. The hardware controller is
responsible for sending switch changes to MPF and applying its commands to
correct items on the playfield. This is done by the class `MpfGamelogicEngine`.
It also retrieves and stores the machine description from MPF at design time,
which allows the table author to create the mapping between MPF and VPE coils,
lamps and switches that is used at runtime to control the playfield.

### MPF Binaries

The precompiled binaries included here are slightly different from the official
version of MPF. They are based on `0.80.0.dev6`, with the addition of a _Ping_
RPC that allows VPE to check whether MPF is ready without any side effects (like
starting the game). This way, the game can start as soon as MPF is ready,
regardless of how long MPF takes to start up. I have
[proposed this change](https://github.com/missionpinball/mpf/pull/1865) to the
MPF developers, but as of February 2025 they have not yet gotten around to
including it in the official version. The binaries were made using
[PyInstaller](https://pyinstaller.org). I'm not completely sure anymore what
parameters I used, but this should be pretty close:
`pyinstaller --collect-all mpf --exclude-module mpf.tests --name mpf --noconfirm mpf/**main**.py`.

### Media controller

This repository includes a basic media controller implementation that is fully
compliant with the
[BCP spec](https://missionpinball.org/latest/code/BCP_Protocol/) and can parse
all documented message types. Without writing code, table authors can trigger
sounds, toggle game objects and display variables in text fields, but its
capabilities are not comparable to those of the official Kivvy and Godot based
media controllers made by the MPF developers.

#### Connection

When the included media controller is selected in the game logic engine
inspector, the `MpfWrangler` creates the `BcpInterface` in its constructor,
which in turn creates the `BcpServer` that ultimately starts a `TcpListener` to
send and receive messages from MPF via TCP. Messages are sent and received
through a message queue, but the server does not run on a separate thread.

#### Parsing

To communicate with the various available media controller implementations, the
MPF developers invented their own little message format called
'[Backbox Control Protocol](https://missionpinball.org/latest/code/BCP_Protocol/),'
or BCP for short. The downside compared to something like gRPC is that there is
no parser. All we get is strings. Much of the code that comprises VPE's media
controller is dedicated to parsing those message strings into strongly typed C#
objects and back. For each message type, there is a class that parses and
represents instances of it.

#### Reacting to messages

There is also a message handler class for each type of message that fires an
event every time a message of the associated type is received. Some message
types must be requested by the media controller using the
[`monitor_start`](https://missionpinball.org/latest/code/BCP_Protocol/monitor_start/)
command. By the number of listeners to the events of each message handler, we
can tell when a message type is actually of interest and monitor only those
message types.

#### Monitoring

Many BCP message types are sent when some variable in MPF changes. For easy
access to these values in VPE, there are monitoring classes for these types of
messages that share a common base class and provide the most recently sent value
and an event that is triggered when a new value is received. For example, using
the `CurrentPlayerPlayerMonitor` class, other code can easily keep track of
whose turn it is without having to go through the
`PlayerTurnStartMessageHandler`.

#### Limitations

To the table author, who is not really allowed to write C# code because it
cannot be shipped with the table file, we offer a few Unity components that can
make sounds and toggle objects based on events and modes from MPF and display
player and machine variables in text fields. The sad part is that this severely
limits what would otherwise be possible with this media controller
implementation. Many message types are not accessible and some fairly basic
functionality like displaying player variables of a player other than the
currently active one is not supported. To fix this, the selection of components
and their feature-set should grow and there should be a visual scripting
interface for less common use-cases.

The most important missing feature is support for slides or more generally
support for displaying stuff on the backbox display. The first obstacle here is
that the way slides work in MPF is
[not officially defined or documented](https://github.com/missionpinball/mpf-gmc/issues/26).

## License

[MIT](LICENSE)
