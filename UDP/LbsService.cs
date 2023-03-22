using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace UDP;

public class LbsService
{
    private static readonly Lazy<LbsService> _instance = new Lazy<LbsService>(() => new LbsService());
    private readonly Dictionary<LBS, LbsCoordinates> _towers;

    private LbsService()
    {
        _towers = new Dictionary<LBS, LbsCoordinates>();
        ReadFile();
    }

    public static LbsService Instance()
    {
        return _instance.Value;
    }

    public LbsCoordinates Find (double longitude, double lat)
    {
        var min = double.MaxValue;
        var result = new LbsCoordinates();
        foreach (var tower in _towers)
        {
            var towerLat = tower.Value.Point.Lat;
            var towerLong = tower.Value.Point.Long;
            if ((towerLat - lat) * (towerLat - lat) + (towerLong - longitude) * (towerLong - longitude) < min)
            {
                min = (towerLat - lat) * (towerLat - lat) + (towerLong - longitude) * (towerLong - longitude);
                result.Point = new PointD(){ Long = towerLong, Lat = towerLat};
                result.Lbs = new LBS(){ Cid = tower.Key.Cid, Lac = tower.Key.Lac, Mcc = tower.Key.Mcc, Mnc = tower.Key.Mnc };
            }
        }

        return result;
    }

    public PointD Get(LBS lbs)
    {
        return _towers[lbs].Point;
    }

    private void ReadFile()
    {
        //using var httpClient = new HttpClient();
        const string patternInt = @"^\d+$";
        const string patternFloat = @"-?\d{1,3}\.\d+";
        try
        {
            //await using var responseStream = await httpClient.GetStreamAsync("https://opencellid.org/ocid/downloads?token=pk.12f9e5e3b7159e65187288bb821440cb&type=mcc&file=257.csv.gz");
            //await using var decompressed = new GZipStream(responseStream, CompressionMode.Decompress);

            using var reader = new StreamReader(Environment.GetFolderPath(Environment.SpecialFolder.Desktop) + "\\257.csv");
            //await using var writer = new StreamWriter(Environment.GetFolderPath(Environment.SpecialFolder.Desktop) + "\\Data.csv");
            while (!reader.EndOfStream)
            {
                var amountElements = 1;
                var startIndex = 0;
                var textLine = reader.ReadLine();
                var index = textLine.IndexOf(',', startIndex);
                if (index != -1)
                {
                    if (textLine.AsSpan().Slice(startIndex, index - startIndex).ToString() != "GSM")
                    {
                        continue;
                    }

                    amountElements++;
                    startIndex = index + 1;
                }

                var line = new StringBuilder();
                var check = true;
                while (textLine.IndexOf(',', startIndex) != -1 && check)
                {
                    index = textLine.IndexOf(',', startIndex);
                    if (amountElements == 2 || amountElements == 3 || amountElements == 4 || amountElements == 5)
                    {
                        var world = textLine.AsSpan().Slice(startIndex, index - startIndex).ToString();
                        if (!Regex.IsMatch(world, patternInt))
                        {
                            check = false;
                            break;
                        }
                    }
                    else if (amountElements == 7 || amountElements == 8)
                    {
                        var world = textLine.AsSpan().Slice(startIndex, index - startIndex).ToString();
                        if (!Regex.IsMatch(world, patternFloat))
                        {
                            check = false;
                            break;
                        }
                    }

                    amountElements++;
                    startIndex = index + 1;
                }

                if (check)
                {
                    var words = textLine.Split(",");
                    var mcc = int.Parse(words[1]);
                    var mnc = int.Parse(words[2]);
                    var lac = int.Parse(words[3]);
                    var cid = int.Parse(words[4]);
                    var longitude = double.Parse(words[6], new NumberFormatInfo { NumberDecimalSeparator = "." });
                    var lat = double.Parse(words[7], new NumberFormatInfo { NumberDecimalSeparator = "." });
                    var lbs = new LBS { Cid = cid, Lac = lac, Mcc = mcc, Mnc = mnc };
                    var point = new PointD { Long = longitude, Lat = lat };
                    _towers.Add(lbs, new LbsCoordinates { Lbs = lbs, Point = point});
                }
            }

        }
        catch (IOException ex)
        {
            Console.WriteLine(ex.Message);
            return;
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.Message);
            return;
        }
    }
}