using Elastic.Channels;
using Elastic.Ingest.Elasticsearch;
using Elastic.Ingest.Elasticsearch.DataStreams;
using Elastic.Serilog.Sinks;
using Elastic.Transport;
using OpenTelemetry;
using OpenTelemetry.Resources;
using Serilog;

try
{
    var builder = WebApplication.CreateBuilder(args);
    
    Log.Logger = new LoggerConfiguration()
        .WriteTo.Elasticsearch([new Uri("http://localhost:9200")], opts =>
        {
            opts.DataStream = new DataStreamName("logs", "telemetry-loggin", "demo");
            opts.BootstrapMethod = BootstrapMethod.Failure;
            opts.ConfigureChannel = channelOpts => 
                channelOpts.BufferOptions = new BufferOptions { ExportMaxConcurrency = 10 };
        }, transport =>
        {
            transport.Authentication(new BasicAuthentication("elastic", "changeme"));
            transport.OnRequestCompleted(d => Console.WriteLine($"es-req: {d.DebugInformation}"));
        })
        .Enrich.WithProperty("Environment", builder.Environment.EnvironmentName)
        .ReadFrom.Configuration(builder.Configuration)
        .CreateLogger();

    // Add services to the container.
    builder.Services.AddRazorPages();
    
    builder.Services.AddOpenTelemetry()
        .ConfigureResource(r => r.AddService("abobus"))
        .WithLogging()
        .UseOtlpExporter();
    
    builder.Services.AddSerilog(Log.Logger);

    var app = builder.Build();

    // Configure the HTTP request pipeline.
    if (!app.Environment.IsDevelopment())
    {
        app.UseExceptionHandler("/Error");
        // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
        app.UseHsts();
    }

    app.UseHttpsRedirection();
    app.UseStaticFiles();

    app.UseSerilogRequestLogging();

    app.UseRouting();

    app.UseAuthorization();

    app.MapRazorPages();

    app.Run();
}
catch (Exception ex)
{ 
    Log.Logger = new LoggerConfiguration()
        .WriteTo.Console()
        .WriteTo.File("./.logs/start-host-log-.txt", rollingInterval: RollingInterval.Day)
        .CreateLogger();

    Log.Fatal(ex, "Unhandled exception");
}
