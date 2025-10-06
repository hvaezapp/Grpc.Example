using Google.Protobuf;
using Grpc.Core;
using Grpc.Example.Client.Protos;
using Grpc.Net.Client;
using Spectre.Console;

var filePath = @"D:\movies\fury.mp4";

AnsiConsole.Console.WriteLine($"Uploading file: {filePath}", new Style(Color.Green, Color.Black, Decoration.Bold));

using var channel = GrpcChannel.ForAddress("https://localhost:7143");

var client = new FileStreamingService.FileStreamingServiceClient(channel);

var call = client.StreamFile();

var file = File.OpenRead(filePath);

var buffer = new byte[file.Length / 1000];

int bytesRead = 0;

var requestStreamTask = Task.Run(async () =>
{
    while ((bytesRead = await file.ReadAsync(buffer, 0, buffer.Length)) > 0)
    {
        await call.RequestStream.WriteAsync(new FileContent()
        { FileName = Path.GetFileName(filePath), File = ByteString.CopyFrom(buffer), FileSize = file.Length });

        await Task.Delay(10);
    }

    await call.RequestStream.CompleteAsync();
});


var responseStreamTask = Task.Run(async () =>
{
    await AnsiConsole.Progress().StartAsync(async context =>
    {
        var task = context.AddTask("[yellow]Uploading file[/]");

        while (!context.IsFinished)
        {
            await foreach (var percentage in call.ResponseStream.ReadAllAsync())
            {
                task.Value = percentage.Percent;
            }
        }
    });
    AnsiConsole.Console.WriteLine("Upload Finished!", new Style(Color.Yellow, Color.Black, Decoration.Bold));

});


await Task.WhenAll(requestStreamTask, responseStreamTask);

