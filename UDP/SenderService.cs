using System.Net;
using System.Net.Sockets;
using System.Text;

namespace UDP;

public class SenderService : BackgroundService
{
    private readonly ILogger<SenderService> _logger;

    public SenderService(ILogger<SenderService> logger)
    {
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            using var udpSocket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);

            while (!stoppingToken.IsCancellationRequested)
            {
                while (!stoppingToken.IsCancellationRequested)
                {
                    var data = Encoding.UTF8.GetBytes(DateTimeOffset.Now.ToString());
                    EndPoint remotePoint = new IPEndPoint(IPAddress.Parse("127.0.0.1"), 22220);
                    await udpSocket.SendToAsync(data, remotePoint);
                    await Task.Delay(200, stoppingToken);
                }
            }

            await Task.CompletedTask;
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
}
