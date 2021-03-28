# Visual Pinball Engine - MPF Gamelogic Engine
*Enables the Mission Pinball Framework to drive VPE*

[![UPM Package](https://img.shields.io/npm/v/org.visualpinball.engine.missionpinball?label=org.visualpinball.engine.missionpinball&registry_uri=https://registry.visualpinball.org&color=%2333cf57&logo=unity&style=flat)](https://registry.visualpinball.org/-/web/detail/org.visualpinball.engine.missionpinball)

## Structure

This project contains three folders:

- `VisualPinball.Engine.Mpf` is a library which builds the gRPC protos and 
  wraps them into a nicer interface.
- `VisualPinball.Engine.Mpf.Test` is a command line tool that allows quick 
  testing without running Unity
- `VisualPinball.Engine.Mpf.Unity` is the Unity UPM package that plugs into 
  VPE and implements the gamelogic engine.
  
Currently, only the first two projects are contained in the provided VS 
solution. In the future we might add the Unity project with its dependencies,
but for now you'll need to open it through Unity.

### Binaries

Both gRPC and Protobuf come with dependencies that conflict with Unity's, namely
`System.Buffers`, `System.Memory` and `System.Runtime.CompilerServices`. To
solve this, we pack all dependencies into a single DLL and ship it to Unity as
a single binary. So, what Unity is getting is:

- `VisualPinball.Engine.Mpf.dll`, which is `VisualPinball.Engine.Mpf` including
  all its .NET dependencies
- `grpc_csharp_ext.dll`, which is the native gRCP library used by the C# wrapper.

### Unity Package

The goal of this repo is to use it within Unity. In order to do that, open the
Package Manager in Unity, and add `org.visualpinball.engine.missionpinball` under 
*Add package from git URL*.

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
git update-index --assume-unchanged VisualPinball.Engine.Mpf.Unity/Plugins/linux-x64/libgrpc_csharp_ext.so.meta
git update-index --assume-unchanged VisualPinball.Engine.Mpf.Unity/Plugins/osx-x64/VisualPinball.Engine.Mpf.dll.meta
git update-index --assume-unchanged VisualPinball.Engine.Mpf.Unity/Plugins/osx-x64/libgrpc_csharp_ext.dylib.meta
git update-index --assume-unchanged VisualPinball.Engine.Mpf.Unity/Plugins/win-x64/VisualPinball.Engine.Mpf.dll.meta
git update-index --assume-unchanged VisualPinball.Engine.Mpf.Unity/Plugins/win-x64/grpc_csharp_ext.dll.meta
git update-index --assume-unchanged VisualPinball.Engine.Mpf.Unity/Plugins/win-x86/VisualPinball.Engine.Mpf.dll.meta
git update-index --assume-unchanged VisualPinball.Engine.Mpf.Unity/Plugins/win-x86/grpc_csharp_ext.dll.meta
```

## License

[MIT](LICENSE)