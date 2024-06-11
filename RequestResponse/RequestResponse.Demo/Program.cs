using Microsoft.Extensions.Http.Diagnostics;
using Serilog;

Serilog.Debugging.SelfLog.Enable(Console.Error);

Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .CreateBootstrapLogger();

var builder = WebApplication.CreateBuilder(args);

try
{
    Log.Information("Configuring web host ({Application})", builder.Environment.ApplicationName);

    builder.Services.AddEndpointsApiExplorer();
    builder.Services.AddSwaggerGen();

    builder.Host.UseSerilog((hostContext, loggerBuilder) =>
    {
        loggerBuilder
        .ReadFrom.Configuration(hostContext.Configuration);
    });

    builder.Services.AddHttpClient<PostClient>()
        .ConfigureHttpClient(c =>
        {
            c.BaseAddress = new Uri("https://jsonplaceholder.typicode.com");
        })
        .AddExtendedHttpClientLogging(options =>
        {
            options.LogBody = true;
            options.RequestBodyContentTypes.Add("application/json");
            options.RequestPathParameterRedactionMode = HttpRouteParameterRedactionMode.None;
            options.ResponseBodyContentTypes.Add("application/json");
        });

    builder.Services.AddRedaction();

    builder.Services.AddLatencyContext();

    builder.Services.AddHttpClientLatencyTelemetry();

    var app = builder.Build();

    app.UseSwagger();
    app.UseSwaggerUI();

    app.MapGet("/posts/{id}", async (ILoggerFactory loggerFactory, PostClient client, int id) =>
    {
        var logger = loggerFactory.CreateLogger("GetPosts");
        logger.LogInformation("fetching post {PostId}", id);

        var post = await client.GetPostAsync(id);

        return TypedResults.Ok(post);
    });

    app.Run();

    return 0;
}
catch (Exception ex)
{
    Log.Fatal(ex, "Program terminated unexpectedly ({Application})", builder.Environment.ApplicationName);
    return 1;
}
finally
{
    Log.CloseAndFlush();
}


public class PostClient
{
    private readonly HttpClient _client;

    public PostClient(HttpClient client)
    {
        _client = client;
    }

    public async Task<Post?> GetPostAsync(int id)
    {
        return await _client.GetFromJsonAsync<Post>($"posts/{id}");
    }
}

public record Post
{
    public int userId { get; init; }
    public int id { get; init; }
    public string title { get; init; }
    public string body { get; init; }
}