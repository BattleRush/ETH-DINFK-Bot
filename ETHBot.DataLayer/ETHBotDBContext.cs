using Microsoft.EntityFrameworkCore;
using System;
using ETHBot.DataLayer.Data.Discord;
using ETHBot.DataLayer.Data.Reddit;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Console;

namespace ETHBot.DataLayer
{
    public class ETHBotDBContext : DbContext
    {
        private static bool _created = false;
        //static LoggerFactory object
        public static readonly ILoggerFactory loggerFactory
    = LoggerFactory.Create(builder => { builder.AddConsole(); });

        public ETHBotDBContext()
        {
            if (!_created)
            {
                _created = true;
                //Database.EnsureDeleted();
                Database.EnsureCreated();
            }
        }
        protected override void OnConfiguring(DbContextOptionsBuilder optionbuilder)
        {
            optionbuilder.UseLoggerFactory(loggerFactory).UseSqlite(@"Data Source=I:\ETHBot\ETHBot.db").EnableSensitiveDataLogging();
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



        public DbSet<SubredditInfo> SubredditInfos { get; set; }
        public DbSet<RedditPost> RedditPosts { get; set; }
        public DbSet<RedditImage> RedditImages { get; set; }


        
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
                 //modelBuilder.Configurations.Add(new Student.StudentMapping());

        }
       
    }
}
