using Microsoft.EntityFrameworkCore;
using System;
using ETHBot.DataLayer.Data.Discord;
using ETHBot.DataLayer.Data.Reddit;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Console;
using ETHBot.DataLayer.Data;
using ETHBot.DataLayer.Data.Study;
using ETHBot.DataLayer.Data.Fun;
using System.Linq;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Configuration.Json;

namespace ETHBot.DataLayer
{
    public class ETHBotDBContext : DbContext
    {
        private static bool _created = false;
        private string ConnectionString = "";
        public static readonly ILoggerFactory loggerFactory = LoggerFactory.Create(builder =>
        {
            builder.AddConsole().AddFilter((provider, category, logLevel) => { if (logLevel >= LogLevel.Warning) return true; return false; });
        });

        public ETHBotDBContext()
        {
            //dotnet ef migrations add InitialCreate --project ETHBot.DataLayer/  --startup-project ETHDINFKBot/ -v

            var configuration = new ConfigurationBuilder()
                  .AddJsonFile("appsettings.json", optional: false, reloadOnChange: false)
                  .Build();

            ConnectionString = configuration.GetConnectionString("ConnectionString_Full").ToString();

            if (!_created)
            {
                _created = true;
                //Database.EnsureDeleted();
                //Database.EnsureCreated();
                Database.Migrate();
            }
        }
        protected override void OnConfiguring(DbContextOptionsBuilder optionbuilder)
        {
            optionbuilder.UseLoggerFactory(loggerFactory)
            .UseMySql(
                ConnectionString,
                new MySqlServerVersion(new Version(10, 3, 31)))
            .EnableSensitiveDataLogging()
            .EnableDetailedErrors();
        }

        public DbSet<BannedLink> BannedLinks { get; set; }
        public DbSet<CommandStatistic> CommandStatistics { get; set; }
        public DbSet<CommandType> CommandTypes { get; set; }

        public DbSet<DiscordChannel> DiscordChannels { get; set; }
        public DbSet<DiscordThread> DiscordThreads { get; set; }
        public DbSet<DiscordMessage> DiscordMessages { get; set; }
        public DbSet<DiscordServer> DiscordServers { get; set; }
        public DbSet<DiscordUser> DiscordUsers { get; set; }
        public DbSet<DiscordRole> DiscordRoles { get; set; }

        public DbSet<PingStatistic> PingStatistics { get; set; }
        public DbSet<PingHistory> PingHistory { get; set; }
        public DbSet<SavedMessage> SavedMessages { get; set; }

        public DbSet<BotChannelSetting> BotChannelSettings { get; set; }

        public DbSet<SubredditInfo> SubredditInfos { get; set; }
        public DbSet<RedditPost> RedditPosts { get; set; }
        public DbSet<RedditImage> RedditImages { get; set; }

        public DbSet<BotSetting> BotSetting { get; set; }

        // migrate table
        public DbSet<DiscordEmote> DiscordEmotes { get; set; }
        public DbSet<DiscordEmoteHistory> DiscordEmoteHistory { get; set; }
        public DbSet<DiscordEmoteStatistic> DiscordEmoteStatistics { get; set; }


        public DbSet<RantType> RantTypes { get; set; }
        public DbSet<RantMessage> RantMessages { get; set; }


        // ETH Place
        public DbSet<PlaceBoardPixel> PlaceBoardPixels { get; set; }
        public DbSet<PlaceBoardHistory> PlaceBoardHistory { get; set; }
        public DbSet<PlacePerformanceInfo> PlacePerformanceInfos { get; set; }
        public DbSet<PlaceDiscordUser> PlaceDiscordUsers { get; set; }
        public DbSet<PlaceMultipixelJob> PlaceMultipixelJobs { get; set; }
        public DbSet<PlaceMultipixelPacket> PlaceMultipixelPackets { get; set; }


        public DbSet<FavouriteDiscordEmote> FavouriteDiscordEmotes { get; set; }

        public DbSet<BotStartUpTime> BotStartUpTimes { get; set; }
        public DbSet<StoredKeyValuePair> StoredKeyValuePairs { get; set; }

        /*

        // todo reconsider how to import them
        public DbSet<Subject> Subjects { get; set; }
        public DbSet<Exam> Exams { get; set; }
        public DbSet<Question> Questions { get; set; }
        */

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // https://github.com/dotnet/efcore/issues/11003#issuecomment-492333796
            // get all composite keys (entity decorated by more than 1 [Key] attribute
            foreach (var entity in modelBuilder.Model.GetEntityTypes()
                .Where(t =>
                    t.ClrType.GetProperties()
                        .Count(p => p.CustomAttributes.Any(a => a.AttributeType == typeof(KeyAttribute))) > 1))
            {
                // get the keys in the appropriate order
                var orderedKeys = entity.ClrType
                    .GetProperties()
                    .Where(p => p.CustomAttributes.Any(a => a.AttributeType == typeof(KeyAttribute)))
                    .OrderBy(p =>
                        p.CustomAttributes.Single(x => x.AttributeType == typeof(ColumnAttribute))?
                            .NamedArguments?.Single(y => y.MemberName == nameof(ColumnAttribute.Order))
                            .TypedValue.Value ?? 0)
                    .Select(x => x.Name)
                    .ToArray();

                // apply the keys to the model builder
                modelBuilder.Entity(entity.ClrType).HasKey(orderedKeys);

            }

            // Prevent PlaceUser table to have 2 discord users
            modelBuilder.Entity<PlaceDiscordUser>().HasIndex(c => c.DiscordUserId).IsUnique();
        }
    }
}
