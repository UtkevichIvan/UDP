using UDP;

var host = Host.CreateDefaultBuilder(args)
    .ConfigureServices(services =>
    {
        services.AddHostedService<GetterService>();
        services.AddHostedService<SenderService>();
    })
    .Build();

host.Run();
