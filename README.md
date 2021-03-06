# Visual Pinball Engine - MPF Gamelogic Engine
*Enables the Mission Pinball Framework to drive VPE*

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

## Setup

You currently need Python and MPF installed locally.

1. Install Python 3
2. `pip install --pre mpf mpf-mc`

Or, if you already have it:

`pip install mpf mpf-mc --pre --upgrade`

After that, `mpf --version` should return at least **MPF v0.55.0-dev.12**.

## Usage

Test code in the repo. Init:

```cs
var mpfApi = new MpfApi(@"path\to\machine\folder");

// spawn MPF and connect to it
await mpfApi.Launch();

// start MPF
mpfApi.Start();

// retrieve machine configuration
var descr = await mpfApi.GetMachineDescription();

Console.WriteLine($"Description: {descr}");
```

## License

[MIT](LICENSE)