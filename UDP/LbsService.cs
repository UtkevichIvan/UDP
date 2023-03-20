using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace UDP;

public class LbsService
{
    private static LbsService? _instance;
    private readonly Dictionary<LBS, PointD> _towers;

    private LbsService()
    {
        _towers = new Dictionary<LBS, PointD>();
        ReadFile();
    }

    public static LbsService GetInstance()
    {
        _instance ??= new LbsService();

        return _instance;
    }

    public LBSData Find (double longitude, double lat)
    {
        var min = double.MaxValue;
        var resMcc = 0;
        var resMnc = 0;
        var resLac = 0;
        var resCid = 0;
        var resLong = 0.0;
        var resLat = 0.0;
        foreach (var tower in _towers)
        {
            if ((tower.Value.Lat - lat) * (tower.Value.Lat - lat) +
                (tower.Value.Long - longitude) * (tower.Value.Long - longitude) < min)
            {
                min = (tower.Value.Lat - lat) * (tower.Value.Lat - lat) +
                      (tower.Value.Long - longitude) * (tower.Value.Long - longitude);
                resCid = tower.Key.CID;
                resLac = tower.Key.LAC;
                resMnc = tower.Key.MNC;
                resMcc = tower.Key.MCC;
                resLong = tower.Value.Long;
                resLat = tower.Value.Lat;
            }
        }

        return new LBSData{ MCC = resMcc, MNC = resMnc, LAC = resLac, CID = resCid, Long = resLong, Lat = resLat};
    }

    public PointD Get(LBS lbs)
    {
        return _towers[lbs];
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
                    _towers.Add(new LBS { CID = cid, LAC = lac, MCC = mcc, MNC = mnc}, new PointD{Long = longitude, Lat = lat});
                }
            }

        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.Message);
            return;
        }
    }
}