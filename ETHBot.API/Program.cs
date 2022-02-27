using Discord;
using Discord.WebSocket;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

var summaries = new[]
{
    "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
};

app.MapGet("/weatherforecast", () =>
{
    var forecast = Enumerable.Range(1, 5).Select(index =>
       new WeatherForecast
       (
           DateTime.Now.AddDays(index),
           Random.Shared.Next(-20, 55),
           summaries[Random.Shared.Next(summaries.Length)]
       ))
        .ToArray();
    return forecast;
})
.WithName("GetWeatherForecast");

app.MapGet("/channels", async () =>
{
    return "test";
});

app.MapGet("/auth", async (string code) =>
{
    var config = new DiscordSocketConfig
    {
        MessageCacheSize = 250,
        AlwaysDownloadUsers = true,
        GatewayIntents = GatewayIntents.All
    };
    var Client = new DiscordSocketClient(config);

    await Client.LoginAsync(TokenType.Bearer, code);


    return "test";
});


app.Run();

class MessageCount
{
    public long[] Channels { get; set; }
    public int Days { get; set; }
    public int GroupByHours { get; set; }
}

internal record WeatherForecast(DateTime Date, int TemperatureC, string? Summary)
{
    public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);
}