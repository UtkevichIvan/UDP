using UDP;

var a = LbsService.GetInstance().Get(new LBS{MCC = 257, MNC = 2, LAC = 84, CID = 55722 });
var b = LbsService.GetInstance().Find( 29.718291, 54.0918372);
//Console.WriteLine(a.Lat + " " + a.Long);

//var host = Host.CreateDefaultBuilder(args)
//    .ConfigureServices(services =>
//    {
//        services.AddHostedService<GetterService>();
//        services.AddHostedService<SenderService>();
//    })
//    .Build();

//host.Run();
