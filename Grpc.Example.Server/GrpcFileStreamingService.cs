using Grpc.Core;
using Grpc.Example.Server.Protos;

namespace Grpc.Example.Server;

public class GrpcFileStreamingService(IWebHostEnvironment environment, ILogger<GrpcFileStreamingService> logger)
    : FileStreamingService.FileStreamingServiceBase
{
    public override async Task StreamFile(IAsyncStreamReader<FileContent> requestStream,
        IServerStreamWriter<Result> responseStream, ServerCallContext context)
    {
        logger.LogInformation("Receiving File...");

        if (!await requestStream.MoveNext())
        {
            logger.LogWarning("No file content received.");
            return;
        }

        var fileName = environment.ContentRootPath + "/Files/" + requestStream.Current.FileName;
        var fileSize = requestStream.Current.FileSize;

        long totalBytesWritten = 0;

        await using var fs = new FileStream(fileName, FileMode.Create, FileAccess.Write);

        await fs.WriteAsync(requestStream.Current.File.ToArray());
        totalBytesWritten += requestStream.Current.File.Length;

        try
        {
            await foreach (var file in requestStream.ReadAllAsync())
            {
                await fs.WriteAsync(file.File.ToArray());

                totalBytesWritten += file.File.Length;

                await Task.Delay(20);

                var streamResult = new Result { Percent = GetPercent(totalBytesWritten, fileSize) };

                await responseStream.WriteAsync(streamResult);
            }
        }
        finally
        {
            fs.Close();
            logger.LogInformation("Saving File Completed");
        }
    }

    private static double GetPercent(long written, double total)
    {
        if (total == 0) return 100;
        return Math.Round(written / total * 100, 2);
    }
}