.\protoc.exe -I ../Runtime ^
--plugin=protoc-gen-grpc=grpc_csharp_plugin.exe ^
--proto_path=protos ^
--csharp_out=../Runtime ^
--grpc_out=../Runtime ^
platform.proto

