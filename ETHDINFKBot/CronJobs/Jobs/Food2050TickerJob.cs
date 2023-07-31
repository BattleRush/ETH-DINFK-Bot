using CSharpMath.Rendering.FrontEnd;
using Discord;
using Discord.WebSocket;
using ETHDINFKBot.Data;
using ETHDINFKBot.Helpers;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
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

            foreach (var restaurant in restaurants)
            {
                if(restaurant.RestaurantId == 10)
                {
                    // continue for this one as its dinner for lower which is captured by the lunch case
                    // todo handle properly
                    continue;
                }

                // {"operationName":"KitchenStatsPerMinute","variables":{"kitchenSlug":"untere-mensa","locationSlug":"uzh-zentrum","timestamp":"2023-07-27T14:33:14.628Z"},"query":"query KitchenStatsPerMinute($locationSlug: String!, $kitchenSlug: String!, $timestamp: DateTime!) {\n  location(id: $locationSlug) {\n    id\n    kitchen(slug: $kitchenSlug) {\n      id\n      publicLabel\n      statsPerMinute(\n        where: {timestamp: {lte: $timestamp}}\n        orderBy: {timestamp: desc}\n        take: 1\n      ) {\n        co2EmissionsGramsDelta\n        co2EmissionsGramsTotal\n        temperatureChangeStats {\n          temperatureChange\n          temperatureChangeDelta\n          __typename\n        }\n        __typename\n      }\n      __typename\n    }\n    __typename\n  }\n  climateRatingFromDegrees {\n    HIGHMinDegCelsius\n    MEDIUMMinDegCelsius\n    __typename\n  }\n}"}

                var url = $"https://api.app.food2050.ch/";

                var payload = new
                {
                    operationName = "KitchenStatsPerMinute",
                    variables = new
                    {
                        kitchenSlug = restaurant.AdditionalInternalName,
                        locationSlug = restaurant.InternalName,
                        timestamp = DateTime.Now
                    },
                    query = "query KitchenStatsPerMinute($locationSlug: String!, $kitchenSlug: String!, $timestamp: DateTime!) {\n  location(id: $locationSlug) {\n    id\n    kitchen(slug: $kitchenSlug) {\n      id\n      publicLabel\n      statsPerMinute(\n        where: {timestamp: {lte: $timestamp}}\n        orderBy: {timestamp: desc}\n        take: 1\n      ) {\n        co2EmissionsGramsDelta\n        co2EmissionsGramsTotal\n        temperatureChangeStats {\n          temperatureChange\n          temperatureChangeDelta\n          __typename\n        }\n        __typename\n      }\n      __typename\n    }\n    __typename\n  }\n  climateRatingFromDegrees {\n    HIGHMinDegCelsius\n    MEDIUMMinDegCelsius\n    __typename\n  }\n}"
                };

                var json = JsonConvert.SerializeObject(payload);
                var data = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await client.PostAsync(url, data);
                string result = response.Content.ReadAsStringAsync().Result;

                var food2050Response = JsonConvert.DeserializeObject<Food2050StatPerMinute>(result);

                if(food2050Response.data.location.kitchen.statsPerMinute.Count > 0)
                {
                    var stats = food2050Response.data.location.kitchen.statsPerMinute[0];

                    var temperatureChange = stats.temperatureChangeStats.temperatureChange;
                    var temperatureChangeDelta = stats.temperatureChangeStats.temperatureChangeDelta;
                    var co2EmissionsGramsDelta = stats.co2EmissionsGramsDelta;
                    var co2EmissionsGramsTotal = stats.co2EmissionsGramsTotal;

                    var message = $"Restaurant: {restaurant.Name}\n" +
                        $"Temperatur: {temperatureChange}°C\n" +
                        $"Temperatur Delta: {temperatureChangeDelta}°C\n" +
                        $"CO2 Delta: {co2EmissionsGramsDelta}g\n" +
                        $"CO2 Total: {co2EmissionsGramsTotal}g\n";

                    var channel = Program.Client.GetGuild(747752542741725244).GetTextChannel(768600365602963496);
                    await channel.SendMessageAsync(message);
                    
                    foodDBManager.AddFood2050CO2Entry(new ETHBot.DataLayer.Data.ETH.Food.Food2050CO2Entry()
                    {
                        DateTime = DateTime.UtcNow.AddHours(Program.TimeZoneInfo.IsDaylightSavingTime(DateTime.Now) ? 2 : 1), // CEST CONVERSION
                        RestaurantId = restaurant.RestaurantId,
                        CO2Delta = co2EmissionsGramsDelta,
                        CO2Total = co2EmissionsGramsTotal,
                        TemperatureChange = temperatureChange,
                        TemperatureChangeDelta = temperatureChangeDelta,
                    });
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
