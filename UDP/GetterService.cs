using System.Net.Sockets;
using System.Text;

namespace UDP;

public class GetterService : BackgroundService
{
    private readonly IHostApplicationLifetime _lifetime;

    private readonly ILogger<GetterService> _logger;

    public GetterService(ILogger<GetterService> logger, IHostApplicationLifetime lifetime)
    {
        _logger = logger;
        _lifetime = lifetime;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!await WaitForAppStartup(_lifetime, stoppingToken))
            return;

        try
        {
            using var receiver = new UdpClient(22220);

            while (!stoppingToken.IsCancellationRequested)
            {
                var result = await receiver.ReceiveAsync(stoppingToken);
                var message = Encoding.UTF8.GetString(result.Buffer);
                Console.WriteLine(message);
            }
        }
        catch (OperationCanceledException ex)
        {
            _logger.LogError(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex.Message);
        }
    }

    private static async Task<bool> WaitForAppStartup(IHostApplicationLifetime lifetime, CancellationToken stoppingToken)
    {
        var startedSource = new TaskCompletionSource();
        await using var reg1 =  lifetime.ApplicationStarted.Register(() => startedSource.SetResult());

        var cancelledSource = new TaskCompletionSource();
        await using var reg2 = stoppingToken.Register(() => cancelledSource.SetResult());

        var completedTask = await Task.WhenAny(startedSource.Task, cancelledSource.Task).ConfigureAwait(false);

        return completedTask == startedSource.Task;
    }
}