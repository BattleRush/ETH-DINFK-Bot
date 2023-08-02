// Root myDeserializedClass = JsonConvert.DeserializeObject<Root>(myJsonResponse);
using System;
using System.Collections.Generic;

public class StatClimateRatingFromDegrees
    {
        public double HIGHMinDegCelsius { get; set; }
        public double MEDIUMMinDegCelsius { get; set; }
        public string __typename { get; set; }
    }

    public class StatData
    {
        public StatLocation location { get; set; }
        public StatClimateRatingFromDegrees climateRatingFromDegrees { get; set; }
    }

    public class StatKitchen
    {
        public string id { get; set; }
        public string publicLabel { get; set; }
        public List<StatsPerMinute> statsPerMinute { get; set; }
        public string __typename { get; set; }
    }

    public class StatLocation
    {
        public string id { get; set; }
        public StatKitchen kitchen { get; set; }
        public string __typename { get; set; }
    }

    public class Food2050StatPerMinute
    {
        public StatData data { get; set; }
    }

    public class StatsPerMinute
    {
        public DateTime timestamp { get; set; }
        public int co2EmissionsGramsDelta { get; set; }
        public int co2EmissionsGramsTotal { get; set; }
        public TemperatureChangeStats temperatureChangeStats { get; set; }
        public string __typename { get; set; }
    }

    public class TemperatureChangeStats
    {
        public double temperatureChange { get; set; }
        public int temperatureChangeDelta { get; set; }
        public string __typename { get; set; }
    }
