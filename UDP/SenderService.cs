using System.Globalization;
using System.Net;
using System.Net.Sockets;
using System.Text;
using Aspose.Gis;
using Aspose.Gis.Formats.Gpx;
using Aspose.Gis.Geometries;

namespace UDP;

public class SenderService : BackgroundService
{
    private readonly IHostApplicationLifetime _lifetime;

    private readonly List<PointD> _data = new List<PointD>();

    private readonly ILogger<SenderService> _logger;

    public SenderService(ILogger<SenderService> logger, IHostApplicationLifetime lifetime)
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
            CreateList();
            using var udpSocket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
            int i = 0;
            int amountCycle = 1;
            while (!stoppingToken.IsCancellationRequested)
            {
                if (i < _data.Count)
                {
                    if (amountCycle == 1)
                    {
                        var time = DateTime.Now;
                        time = time.AddSeconds(5.0 * i);
                        var data = _data[i].Lat + " " + _data[i].Long + " " + 5 + " " + time.ToLongTimeString();
                        var result = Encoding.UTF8.GetBytes(data);
                        EndPoint remotePoint = new IPEndPoint(IPAddress.Parse("127.0.0.1"), 22220);
                        await udpSocket.SendToAsync(result, remotePoint);
                    }
                    else
                    {
                        var time = DateTime.Now;
                        time = time.AddSeconds(5.0 * i);
                        var data = _data[i].Lat + " " + _data[i].Long + " " + 0 + " " + time.ToLongTimeString();
                        var result = Encoding.UTF8.GetBytes(data);
                        EndPoint remotePoint = new IPEndPoint(IPAddress.Parse("127.0.0.1"), 22220);
                        await udpSocket.SendToAsync(result, remotePoint);
                    }

                    i++;
                }
                else if(amountCycle == 1)
                {
                    i = 0;
                    amountCycle++;
                }

                //await Task.Delay(200, stoppingToken);
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

    private void CreateList()
    {
        var options = new GpxOptions()
        {
            ReadNestedAttributes = true
        };

        using var layer = Drivers.Gpx.OpenLayer("TestPoints.gpx", options);
        foreach (var feature in layer)
        {
            if (feature.Geometry.GeometryType == GeometryType.MultiLineString)
            {
                var lines = (MultiLineString)feature.Geometry;
                for (int i = 0; i < lines.Count; i++)
                {
                    var segment = (LineString)lines[i];
                    for (int j = 0; j < segment.Count; j++)
                    {
                        var words = segment[j].AsText().Split(' ', '(' , ')');
                        _data.Add(new PointD()
                        {
                            Long = double.Parse(words[3], new NumberFormatInfo { NumberDecimalSeparator = "." }),
                            Lat = double.Parse(words[4], new NumberFormatInfo { NumberDecimalSeparator = "." })
                        });
                    }
                }
            }
        }
    }

    private static async Task<bool> WaitForAppStartup(IHostApplicationLifetime lifetime, CancellationToken stoppingToken)
    {
        var startedSource = new TaskCompletionSource();
        await using var reg1 = lifetime.ApplicationStarted.Register(() => startedSource.SetResult());

        var cancelledSource = new TaskCompletionSource();
        await using var reg2 = stoppingToken.Register(() => cancelledSource.SetResult());

        var completedTask = await Task.WhenAny(startedSource.Task, cancelledSource.Task).ConfigureAwait(false);

        return completedTask == startedSource.Task;
    }
}
