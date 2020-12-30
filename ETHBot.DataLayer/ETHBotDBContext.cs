using Microsoft.EntityFrameworkCore;
using System;
using ETHBot.DataLayer.Data.Discord;
using ETHBot.DataLayer.Data.Reddit;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Console;
using ETHBot.DataLayer.Data;

namespace ETHBot.DataLayer
{
    public class ETHBotDBContext : DbContext
    {
        private static bool _created = false;

        public static readonly ILoggerFactory loggerFactory = LoggerFactory.Create(builder =>
        {
            builder.AddConsole().AddFilter((provider, category, logLevel) => { if (logLevel >= LogLevel.Warning) return true; return false; });
        });

        public ETHBotDBContext()
        {
            //dotnet ef migrations add AddRantTables --project ETHBot.DataLayer/  --startup-project ETHDINFKBot/

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
            // TODO Setting
#if DEBUG
            optionbuilder.UseLoggerFactory(loggerFactory).UseSqlite(@"Data Source=I:\ETHBot\ETHBot.db").EnableSensitiveDataLogging();
#else
            optionbuilder.UseLoggerFactory(loggerFactory).UseSqlite(@"Data Source=/usr/local/bin/ETHBot/Database/ETHBot.db").EnableSensitiveDataLogging();
#endif

        }

        public DbSet<BannedLink> BannedLinks { get; set; }
        public DbSet<CommandStatistic> CommandStatistics { get; set; }
        public DbSet<CommandType> CommandTypes { get; set; }
        public DbSet<DiscordChannel> DiscordChannels { get; set; }
        public DbSet<DiscordMessage> DiscordMessages { get; set; }
        public DbSet<DiscordServer> DiscordServers { get; set; }
        public DbSet<DiscordUser> DiscordUsers { get; set; }
        public DbSet<EmojiHistory> EmojiHistory { get; set; }
        public DbSet<EmojiStatistic> EmojiStatistics { get; set; }
        public DbSet<PingStatistic> PingStatistics { get; set; }
        public DbSet<SavedMessage> SavedMessages { get; set; }
        public DbSet<BotChannelSetting> BotChannelSettings { get; set; }
        public DbSet<SubredditInfo> SubredditInfos { get; set; }
        public DbSet<RedditPost> RedditPosts { get; set; }
        public DbSet<RedditImage> RedditImages { get; set; }


        public DbSet<RantType> RantTypes { get; set; }
        public DbSet<RantMessage> RantMessages { get; set; }


        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {

        }
    }
}
