using Grpc.StreamingDemo.Api;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddGrpc();

var app = builder.Build();

app.UseRouting();

app.MapGrpcService<GrpcFileStreamingService>();

app.Run();
