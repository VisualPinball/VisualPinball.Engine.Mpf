// <auto-generated>
//     Generated by the protocol buffer compiler.  DO NOT EDIT!
//     source: platform.proto
// </auto-generated>
#pragma warning disable 0414, 1591
#region Designer generated code

using grpc = global::Grpc.Core;

namespace Mpf.Vpe {
  public static partial class MpfHardwareService
  {
    static readonly string __ServiceName = "mpf.vpe.MpfHardwareService";

    static void __Helper_SerializeMessage(global::Google.Protobuf.IMessage message, grpc::SerializationContext context)
    {
      #if !GRPC_DISABLE_PROTOBUF_BUFFER_SERIALIZATION
      if (message is global::Google.Protobuf.IBufferMessage)
      {
        context.SetPayloadLength(message.CalculateSize());
        global::Google.Protobuf.MessageExtensions.WriteTo(message, context.GetBufferWriter());
        context.Complete();
        return;
      }
      #endif
      context.Complete(global::Google.Protobuf.MessageExtensions.ToByteArray(message));
    }

    static class __Helper_MessageCache<T>
    {
      public static readonly bool IsBufferMessage = global::System.Reflection.IntrospectionExtensions.GetTypeInfo(typeof(global::Google.Protobuf.IBufferMessage)).IsAssignableFrom(typeof(T));
    }

    static T __Helper_DeserializeMessage<T>(grpc::DeserializationContext context, global::Google.Protobuf.MessageParser<T> parser) where T : global::Google.Protobuf.IMessage<T>
    {
      #if !GRPC_DISABLE_PROTOBUF_BUFFER_SERIALIZATION
      if (__Helper_MessageCache<T>.IsBufferMessage)
      {
        return parser.ParseFrom(context.PayloadAsReadOnlySequence());
      }
      #endif
      return parser.ParseFrom(context.PayloadAsNewBuffer());
    }

    static readonly grpc::Marshaller<global::Mpf.Vpe.MachineState> __Marshaller_mpf_vpe_MachineState = grpc::Marshallers.Create(__Helper_SerializeMessage, context => __Helper_DeserializeMessage(context, global::Mpf.Vpe.MachineState.Parser));
    static readonly grpc::Marshaller<global::Mpf.Vpe.Commands> __Marshaller_mpf_vpe_Commands = grpc::Marshallers.Create(__Helper_SerializeMessage, context => __Helper_DeserializeMessage(context, global::Mpf.Vpe.Commands.Parser));
    static readonly grpc::Marshaller<global::Mpf.Vpe.EmptyRequest> __Marshaller_mpf_vpe_EmptyRequest = grpc::Marshallers.Create(__Helper_SerializeMessage, context => __Helper_DeserializeMessage(context, global::Mpf.Vpe.EmptyRequest.Parser));
    static readonly grpc::Marshaller<global::Mpf.Vpe.MachineDescription> __Marshaller_mpf_vpe_MachineDescription = grpc::Marshallers.Create(__Helper_SerializeMessage, context => __Helper_DeserializeMessage(context, global::Mpf.Vpe.MachineDescription.Parser));
    static readonly grpc::Marshaller<global::Mpf.Vpe.SwitchChanges> __Marshaller_mpf_vpe_SwitchChanges = grpc::Marshallers.Create(__Helper_SerializeMessage, context => __Helper_DeserializeMessage(context, global::Mpf.Vpe.SwitchChanges.Parser));
    static readonly grpc::Marshaller<global::Mpf.Vpe.EmptyResponse> __Marshaller_mpf_vpe_EmptyResponse = grpc::Marshallers.Create(__Helper_SerializeMessage, context => __Helper_DeserializeMessage(context, global::Mpf.Vpe.EmptyResponse.Parser));
    static readonly grpc::Marshaller<global::Mpf.Vpe.QuitRequest> __Marshaller_mpf_vpe_QuitRequest = grpc::Marshallers.Create(__Helper_SerializeMessage, context => __Helper_DeserializeMessage(context, global::Mpf.Vpe.QuitRequest.Parser));

    static readonly grpc::Method<global::Mpf.Vpe.MachineState, global::Mpf.Vpe.Commands> __Method_Start = new grpc::Method<global::Mpf.Vpe.MachineState, global::Mpf.Vpe.Commands>(
        grpc::MethodType.ServerStreaming,
        __ServiceName,
        "Start",
        __Marshaller_mpf_vpe_MachineState,
        __Marshaller_mpf_vpe_Commands);

    static readonly grpc::Method<global::Mpf.Vpe.EmptyRequest, global::Mpf.Vpe.MachineDescription> __Method_GetMachineDescription = new grpc::Method<global::Mpf.Vpe.EmptyRequest, global::Mpf.Vpe.MachineDescription>(
        grpc::MethodType.Unary,
        __ServiceName,
        "GetMachineDescription",
        __Marshaller_mpf_vpe_EmptyRequest,
        __Marshaller_mpf_vpe_MachineDescription);

    static readonly grpc::Method<global::Mpf.Vpe.SwitchChanges, global::Mpf.Vpe.EmptyResponse> __Method_SendSwitchChanges = new grpc::Method<global::Mpf.Vpe.SwitchChanges, global::Mpf.Vpe.EmptyResponse>(
        grpc::MethodType.ClientStreaming,
        __ServiceName,
        "SendSwitchChanges",
        __Marshaller_mpf_vpe_SwitchChanges,
        __Marshaller_mpf_vpe_EmptyResponse);

    static readonly grpc::Method<global::Mpf.Vpe.QuitRequest, global::Mpf.Vpe.EmptyResponse> __Method_Quit = new grpc::Method<global::Mpf.Vpe.QuitRequest, global::Mpf.Vpe.EmptyResponse>(
        grpc::MethodType.Unary,
        __ServiceName,
        "Quit",
        __Marshaller_mpf_vpe_QuitRequest,
        __Marshaller_mpf_vpe_EmptyResponse);

    /// <summary>Service descriptor</summary>
    public static global::Google.Protobuf.Reflection.ServiceDescriptor Descriptor
    {
      get { return global::Mpf.Vpe.PlatformReflection.Descriptor.Services[0]; }
    }

    /// <summary>Base class for server-side implementations of MpfHardwareService</summary>
    [grpc::BindServiceMethod(typeof(MpfHardwareService), "BindService")]
    public abstract partial class MpfHardwareServiceBase
    {
      public virtual global::System.Threading.Tasks.Task Start(global::Mpf.Vpe.MachineState request, grpc::IServerStreamWriter<global::Mpf.Vpe.Commands> responseStream, grpc::ServerCallContext context)
      {
        throw new grpc::RpcException(new grpc::Status(grpc::StatusCode.Unimplemented, ""));
      }

      public virtual global::System.Threading.Tasks.Task<global::Mpf.Vpe.MachineDescription> GetMachineDescription(global::Mpf.Vpe.EmptyRequest request, grpc::ServerCallContext context)
      {
        throw new grpc::RpcException(new grpc::Status(grpc::StatusCode.Unimplemented, ""));
      }

      public virtual global::System.Threading.Tasks.Task<global::Mpf.Vpe.EmptyResponse> SendSwitchChanges(grpc::IAsyncStreamReader<global::Mpf.Vpe.SwitchChanges> requestStream, grpc::ServerCallContext context)
      {
        throw new grpc::RpcException(new grpc::Status(grpc::StatusCode.Unimplemented, ""));
      }

      public virtual global::System.Threading.Tasks.Task<global::Mpf.Vpe.EmptyResponse> Quit(global::Mpf.Vpe.QuitRequest request, grpc::ServerCallContext context)
      {
        throw new grpc::RpcException(new grpc::Status(grpc::StatusCode.Unimplemented, ""));
      }

    }

    /// <summary>Client for MpfHardwareService</summary>
    public partial class MpfHardwareServiceClient : grpc::ClientBase<MpfHardwareServiceClient>
    {
      /// <summary>Creates a new client for MpfHardwareService</summary>
      /// <param name="channel">The channel to use to make remote calls.</param>
      public MpfHardwareServiceClient(grpc::ChannelBase channel) : base(channel)
      {
      }
      /// <summary>Creates a new client for MpfHardwareService that uses a custom <c>CallInvoker</c>.</summary>
      /// <param name="callInvoker">The callInvoker to use to make remote calls.</param>
      public MpfHardwareServiceClient(grpc::CallInvoker callInvoker) : base(callInvoker)
      {
      }
      /// <summary>Protected parameterless constructor to allow creation of test doubles.</summary>
      protected MpfHardwareServiceClient() : base()
      {
      }
      /// <summary>Protected constructor to allow creation of configured clients.</summary>
      /// <param name="configuration">The client configuration.</param>
      protected MpfHardwareServiceClient(ClientBaseConfiguration configuration) : base(configuration)
      {
      }

      public virtual grpc::AsyncServerStreamingCall<global::Mpf.Vpe.Commands> Start(global::Mpf.Vpe.MachineState request, grpc::Metadata headers = null, global::System.DateTime? deadline = null, global::System.Threading.CancellationToken cancellationToken = default(global::System.Threading.CancellationToken))
      {
        return Start(request, new grpc::CallOptions(headers, deadline, cancellationToken));
      }
      public virtual grpc::AsyncServerStreamingCall<global::Mpf.Vpe.Commands> Start(global::Mpf.Vpe.MachineState request, grpc::CallOptions options)
      {
        return CallInvoker.AsyncServerStreamingCall(__Method_Start, null, options, request);
      }
      public virtual global::Mpf.Vpe.MachineDescription GetMachineDescription(global::Mpf.Vpe.EmptyRequest request, grpc::Metadata headers = null, global::System.DateTime? deadline = null, global::System.Threading.CancellationToken cancellationToken = default(global::System.Threading.CancellationToken))
      {
        return GetMachineDescription(request, new grpc::CallOptions(headers, deadline, cancellationToken));
      }
      public virtual global::Mpf.Vpe.MachineDescription GetMachineDescription(global::Mpf.Vpe.EmptyRequest request, grpc::CallOptions options)
      {
        return CallInvoker.BlockingUnaryCall(__Method_GetMachineDescription, null, options, request);
      }
      public virtual grpc::AsyncUnaryCall<global::Mpf.Vpe.MachineDescription> GetMachineDescriptionAsync(global::Mpf.Vpe.EmptyRequest request, grpc::Metadata headers = null, global::System.DateTime? deadline = null, global::System.Threading.CancellationToken cancellationToken = default(global::System.Threading.CancellationToken))
      {
        return GetMachineDescriptionAsync(request, new grpc::CallOptions(headers, deadline, cancellationToken));
      }
      public virtual grpc::AsyncUnaryCall<global::Mpf.Vpe.MachineDescription> GetMachineDescriptionAsync(global::Mpf.Vpe.EmptyRequest request, grpc::CallOptions options)
      {
        return CallInvoker.AsyncUnaryCall(__Method_GetMachineDescription, null, options, request);
      }
      public virtual grpc::AsyncClientStreamingCall<global::Mpf.Vpe.SwitchChanges, global::Mpf.Vpe.EmptyResponse> SendSwitchChanges(grpc::Metadata headers = null, global::System.DateTime? deadline = null, global::System.Threading.CancellationToken cancellationToken = default(global::System.Threading.CancellationToken))
      {
        return SendSwitchChanges(new grpc::CallOptions(headers, deadline, cancellationToken));
      }
      public virtual grpc::AsyncClientStreamingCall<global::Mpf.Vpe.SwitchChanges, global::Mpf.Vpe.EmptyResponse> SendSwitchChanges(grpc::CallOptions options)
      {
        return CallInvoker.AsyncClientStreamingCall(__Method_SendSwitchChanges, null, options);
      }
      public virtual global::Mpf.Vpe.EmptyResponse Quit(global::Mpf.Vpe.QuitRequest request, grpc::Metadata headers = null, global::System.DateTime? deadline = null, global::System.Threading.CancellationToken cancellationToken = default(global::System.Threading.CancellationToken))
      {
        return Quit(request, new grpc::CallOptions(headers, deadline, cancellationToken));
      }
      public virtual global::Mpf.Vpe.EmptyResponse Quit(global::Mpf.Vpe.QuitRequest request, grpc::CallOptions options)
      {
        return CallInvoker.BlockingUnaryCall(__Method_Quit, null, options, request);
      }
      public virtual grpc::AsyncUnaryCall<global::Mpf.Vpe.EmptyResponse> QuitAsync(global::Mpf.Vpe.QuitRequest request, grpc::Metadata headers = null, global::System.DateTime? deadline = null, global::System.Threading.CancellationToken cancellationToken = default(global::System.Threading.CancellationToken))
      {
        return QuitAsync(request, new grpc::CallOptions(headers, deadline, cancellationToken));
      }
      public virtual grpc::AsyncUnaryCall<global::Mpf.Vpe.EmptyResponse> QuitAsync(global::Mpf.Vpe.QuitRequest request, grpc::CallOptions options)
      {
        return CallInvoker.AsyncUnaryCall(__Method_Quit, null, options, request);
      }
      /// <summary>Creates a new instance of client from given <c>ClientBaseConfiguration</c>.</summary>
      protected override MpfHardwareServiceClient NewInstance(ClientBaseConfiguration configuration)
      {
        return new MpfHardwareServiceClient(configuration);
      }
    }

    /// <summary>Creates service definition that can be registered with a server</summary>
    /// <param name="serviceImpl">An object implementing the server-side handling logic.</param>
    public static grpc::ServerServiceDefinition BindService(MpfHardwareServiceBase serviceImpl)
    {
      return grpc::ServerServiceDefinition.CreateBuilder()
          .AddMethod(__Method_Start, serviceImpl.Start)
          .AddMethod(__Method_GetMachineDescription, serviceImpl.GetMachineDescription)
          .AddMethod(__Method_SendSwitchChanges, serviceImpl.SendSwitchChanges)
          .AddMethod(__Method_Quit, serviceImpl.Quit).Build();
    }

    /// <summary>Register service method with a service binder with or without implementation. Useful when customizing the  service binding logic.
    /// Note: this method is part of an experimental API that can change or be removed without any prior notice.</summary>
    /// <param name="serviceBinder">Service methods will be bound by calling <c>AddMethod</c> on this object.</param>
    /// <param name="serviceImpl">An object implementing the server-side handling logic.</param>
    public static void BindService(grpc::ServiceBinderBase serviceBinder, MpfHardwareServiceBase serviceImpl)
    {
      serviceBinder.AddMethod(__Method_Start, serviceImpl == null ? null : new grpc::ServerStreamingServerMethod<global::Mpf.Vpe.MachineState, global::Mpf.Vpe.Commands>(serviceImpl.Start));
      serviceBinder.AddMethod(__Method_GetMachineDescription, serviceImpl == null ? null : new grpc::UnaryServerMethod<global::Mpf.Vpe.EmptyRequest, global::Mpf.Vpe.MachineDescription>(serviceImpl.GetMachineDescription));
      serviceBinder.AddMethod(__Method_SendSwitchChanges, serviceImpl == null ? null : new grpc::ClientStreamingServerMethod<global::Mpf.Vpe.SwitchChanges, global::Mpf.Vpe.EmptyResponse>(serviceImpl.SendSwitchChanges));
      serviceBinder.AddMethod(__Method_Quit, serviceImpl == null ? null : new grpc::UnaryServerMethod<global::Mpf.Vpe.QuitRequest, global::Mpf.Vpe.EmptyResponse>(serviceImpl.Quit));
    }

  }
}
#endregion