# Visual Pinball Engine - MPF Gamelogic Engine

[![UPM Package](https://img.shields.io/npm/v/org.visualpinball.engine.missionpinball?label=org.visualpinball.engine.missionpinball&registry_uri=https://registry.visualpinball.org&color=%2333cf57&logo=unity&style=flat)](https://registry.visualpinball.org/-/web/detail/org.visualpinball.engine.missionpinball)

*Enables the Mission Pinball Framework to drive VPE*

## Overview

[MPF](https://missionpinball.org/latest/about/)  is an open-source framework 
written in Python to drive real pinball machines. It has a "configuration over
code" approach, meaning that 90% of what you'd do in a pinball game can be 
achieved through configuration (YAML files) rather than implementing it in code.

When you read MPF's [Getting Started](https://missionpinball.org/latest/start/)
page, you'll notice a banner stating that "MPF is not a simulator." Well, 
you've found the simulator. ;)

This project lets you use MPF to drive game logic in [VPE](https://github.com/freezy/VisualPinball.Engine), 
a pinball simulator based on Unity. It does this by spawning a Python process running
MPF and communicating with VPE through [gRPC](https://grpc.io/).

### Installation

This project is available as a Unity package at `registry.visualpinball.org`. 
To install it, make sure the scoped registry is added in your Package Manager 
settings. Then open the Package Manager in Unity and add 
`org.visualpinball.engine.missionpinball` under *Install package by name*.

The Unity package is build and published to our registry on every merge to master.

## Setup

You currently need Python and MPF installed locally.

1. Install Python 3
2. `pip install --pre mpf mpf-mc`

Or, if you already have it:

`pip install mpf mpf-mc --pre --upgrade`

After that, `mpf --version` should return at least **MPF v0.55.0-dev.37**.

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

## License

[MIT](LICENSE)