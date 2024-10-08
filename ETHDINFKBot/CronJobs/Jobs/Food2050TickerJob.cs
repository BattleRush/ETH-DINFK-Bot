using CSharpMath.Rendering.FrontEnd;
using Discord;
using Discord.WebSocket;
using ETHDINFKBot.Data;
using ETHDINFKBot.Helpers;
using ETHDINFKBot.Helpers.Food;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Reactive.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using static System.Net.WebRequestMethods;

namespace ETHDINFKBot.CronJobs.Jobs
{
    public class Food2050TickerJob : CronJobService
    {
        private readonly ulong ServerSuggestion = 816776685407043614; // todo config?
        private readonly ILogger<Food2050TickerJob> _logger;
        private readonly string Name = "Food2050TickerJob";

        public Food2050TickerJob(IScheduleConfig<Food2050TickerJob> config, ILogger<Food2050TickerJob> logger)
            : base(config.CronExpression, config.TimeZoneInfo)
        {
            _logger = logger;
        }

        public override Task StartAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation($"{Name} starts.");
            return base.StartAsync(cancellationToken);
        }

        public async void GetTickerData()
        {
            HttpClient client = new HttpClient();
            var foodDBManager = new FoodDBManager();
            var restaurants = foodDBManager.GetAllFood2050Restaurants();

            Dictionary<string, int> restaurantCO2Added = new Dictionary<string, int>();

            List<string> processedMensas = new List<string>();
            string errorLog = "";
            foreach (var restaurant in restaurants)
            {
                try
                {
                    // not used for now
                    string mensaKey = restaurant.InternalName + "_" + restaurant.AdditionalInternalName;
                    if(processedMensas.Contains(mensaKey))
                    {
                        //continue;
                    }

                    int count = 0;


                    // {"operationName":"KitchenStatsPerMinute","variables":{"kitchenSlug":"untere-mensa","locationSlug":"uzh-zentrum","timestamp":"2023-07-27T14:33:14.628Z"},"query":"query KitchenStatsPerMinute($locationSlug: String!, $kitchenSlug: String!, $timestamp: DateTime!) {\n  location(id: $locationSlug) {\n    id\n    kitchen(slug: $kitchenSlug) {\n      id\n      publicLabel\n      statsPerMinute(\n        where: {timestamp: {lte: $timestamp}}\n        orderBy: {timestamp: desc}\n        take: 1\n      ) {\n        co2EmissionsGramsDelta\n        co2EmissionsGramsTotal\n        temperatureChangeStats {\n          temperatureChange\n          temperatureChangeDelta\n          __typename\n        }\n        __typename\n      }\n      __typename\n    }\n    __typename\n  }\n  climateRatingFromDegrees {\n    HIGHMinDegCelsius\n    MEDIUMMinDegCelsius\n    __typename\n  }\n}"}

                    var url = $"https://api.app.food2050.ch/";

                    // get utc time now in 2023-07-28T09:56:21.562Z format
                    var utcNow = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ss.fffZ");

                    int take = 5000;

                    var payload = new
                    {
                        operationName = "KitchenStatsPerMinute",
                        variables = new
                        {
                            kitchenSlug = restaurant.AdditionalInternalName,
                            locationSlug = restaurant.InternalName,
                            timestamp = utcNow
                        },
                        query = "query KitchenStatsPerMinute($locationSlug: String!, $kitchenSlug: String!) {\n  location(id: $locationSlug) {\n    id\n    kitchen(slug: $kitchenSlug) {\n      id\n      statsPerMinute(\n        where: {co2EmissionsGramsDelta: {gt: 0}}\n        orderBy: {timestamp: desc}\n        take: " + take + "\n      ) {\n        timestamp\n        co2EmissionsGramsDelta\n        co2EmissionsGramsTotal\n        temperatureChangeStats {\n          temperatureChange\n          temperatureChangeDelta\n          __typename\n        }\n        __typename\n      }\n      __typename\n    }\n    __typename\n  }\n  climateRatingFromDegrees {\n    HIGHMinDegCelsius\n    MEDIUMMinDegCelsius\n    __typename\n  }\n}"
                    };

                    processedMensas.Add(mensaKey);

                    var json = JsonConvert.SerializeObject(payload, Formatting.Indented);

                    //System.IO.File.WriteAllText("food2050.json", json);

                    var data = new StringContent(json, Encoding.UTF8, "application/json");

                    var response = await client.PostAsync(url, data);
                    string result = response.Content.ReadAsStringAsync().Result;

                    //System.IO.File.WriteAllText("food2050result.json", result);

                    if (result == "Service Unavailable")
                    {
                        // todo handle properly
                        errorLog += $"Service Unavailable for {restaurant.Name}" + Environment.NewLine;
                        continue;
                    }

                    var food2050Response = JsonConvert.DeserializeObject<Food2050StatPerMinute>(result);

                    // enfore ascending sort order
                    foreach (var stat in food2050Response.data.location.kitchen.statsPerMinute.OrderBy(x => x.timestamp))
                    {
                        if (stat.temperatureChangeStats == null)
                            continue;

                        var temperatureChange = stat.temperatureChangeStats.temperatureChange;
                        var temperatureChangeDelta = stat.temperatureChangeStats.temperatureChangeDelta;
                        var co2EmissionsGramsDelta = stat.co2EmissionsGramsDelta;
                        var co2EmissionsGramsTotal = stat.co2EmissionsGramsTotal;
                        var dateTime = stat.timestamp;

                        /*var message = $"Restaurant: {restaurant.Name}\n" +
                            $"Temperatur: {temperatureChange}°C\n" +
                            $"Temperatur Delta: {temperatureChangeDelta}°C\n" +
                            $"CO2 Delta: {co2EmissionsGramsDelta}g\n" +
                            $"CO2 Total: {co2EmissionsGramsTotal}g\n";*/

                        //var channel = Program.Client.GetGuild(747752542741725244).GetTextChannel(768600365602963496);
                        //await channel.SendMessageAsync(message);

                        var added = foodDBManager.AddFood2050CO2Entry(new ETHBot.DataLayer.Data.ETH.Food.Food2050CO2Entry()
                        {
                            DateTime = dateTime,
                            RestaurantId = restaurant.RestaurantId,
                            CO2Delta = co2EmissionsGramsDelta,
                            CO2Total = co2EmissionsGramsTotal,
                            TemperatureChange = temperatureChange,
                            TemperatureChangeDelta = temperatureChangeDelta,
                        });

                        if (added)
                            count++;
                    }

                    if (count > 0)
                        restaurantCO2Added.Add(restaurant.Name, count);
                }
                catch (Exception ex)
                {
                    errorLog += $"Error for {restaurant.Name}: {ex.Message}" + Environment.NewLine;
                }
            }

            // send to spam how many records added for restaurant
            var channel = Program.Client.GetGuild(747752542741725244).GetTextChannel(768600365602963496);

            string message = $"Food2050 CO2 Data added:\n";
            foreach (var item in restaurantCO2Added)
            {
                message += $"{item.Key}: {item.Value}\n";

                if(message.Length > 1800)
                {
                    //await channel.SendMessageAsync(message.Substring(0, 2000));
                    message = "";
                }
            }

            //if (!string.IsNullOrEmpty(message))
                //await channel.SendMessageAsync(message);

            if (!string.IsNullOrEmpty(errorLog))
            {
                // ensure error log is not too long
                if (errorLog.Length > 1800)
                {
                    while (errorLog.Length > 1800)
                    {
                        // split by new line
                        var split = errorLog.Split(Environment.NewLine);
                        var messagePart = split.Take(10);
                        errorLog = string.Join(Environment.NewLine, split.Skip(10));
                        await channel.SendMessageAsync(string.Join(Environment.NewLine, messagePart).Substring(0, 2000));
                    }
                }
                else
                {
                    await channel.SendMessageAsync(errorLog);
                }
            }
        }

        public override Task DoWork(CancellationToken cancellationToken)
        {
            _logger.LogInformation($"{DateTime.Now:hh:mm:ss} {Name} is working.");

            try
            {
                GetTickerData();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message);
                // send to spam
                var channel = Program.Client.GetGuild(747752542741725244).GetTextChannel(768600365602963496);
                channel.SendMessageAsync($"Error in {Name}: {ex.Message}");
            }

            return Task.CompletedTask;
        }

        public override Task StopAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation($"{Name}  is stopping.");
            return base.StopAsync(cancellationToken);
        }
    }
}
