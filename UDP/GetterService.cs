using System.Net.Sockets;
using System.Text;

namespace UDP;

public class GetterService : BackgroundService
{
    private readonly List<GpsLbs> _way = new List<GpsLbs>();
    private readonly ILogger<GetterService> _logger;

    public GetterService(ILogger<GetterService> logger)
    {
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            using var receiver = new UdpClient(22220);

            int i = 0;
            while (!stoppingToken.IsCancellationRequested)
            {
                var result = await receiver.ReceiveAsync(stoppingToken);
                if (result.Buffer.Length != 0)
                {
                    var message = Encoding.UTF8.GetString(result.Buffer);
                    var words = message.Split(' ');
                    if (int.Parse(words[2]) >= 3 )
                    {
                        _way.Add(new GpsLbs()
                        {
                            Time = words[3],
                            AmountSatellite = int.Parse(words[2]),
                            Point = new PointD(){Lat = double.Parse(words[0]) , Long = double.Parse(words[1])}
                        });
                        Console.WriteLine(_way[i].Point.Lat + " " + _way[i].Point.Long + 
                                          ' ' + _way[i].AmountSatellite + ' ' + _way[i].Time);
                        i++;
                    }
                    else
                    {
                        var lbs = LbsService.Instance();
                        var lbsCoord = lbs.Find(double.Parse(words[1]), double.Parse(words[0]));
                        _way.Add(new GpsLbs()
                        {
                            Time = words[3],
                            AmountSatellite = int.Parse(words[2]),
                            Point = lbsCoord.Point,
                            Lbs = lbsCoord.Lbs
                        });
                        Console.WriteLine(_way[i].Point.Lat + " " + _way[i].Point.Long + ' ' +
                                          _way[i].AmountSatellite + " " +_way[i].Lbs.Cid + " " +
                                          _way[i].Lbs.Lac + " " + _way[i].Lbs.Mcc + " " +
                                          _way[i].Lbs.Mnc + ' ' + _way[i].Time);
                        i++;
                    }
                }
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
}