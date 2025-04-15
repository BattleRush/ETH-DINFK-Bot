using Discord;
using Discord.Net;
using Discord.WebSocket;
using ETHBot.DataLayer.Data.Discord;
using ETHDINFKBot.Data;
using ETHDINFKBot.Handlers;
using ETHDINFKBot.Helpers;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace ETHDINFKBot.CronJobs.Jobs
{
    public class DownloadImagesJob : CronJobService
    {
        private readonly ILogger<DownloadImagesJob> _logger;
        private readonly string Name = "DownloadImagesJob";

        public DownloadImagesJob(IScheduleConfig<DownloadImagesJob> config, ILogger<DownloadImagesJob> logger)
            : base(config.CronExpression, config.TimeZoneInfo)
        {
            _logger = logger;
        }

        public override Task StartAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation($"{Name} starts.");
            return base.StartAsync(cancellationToken);
        }
        public void BackupDB(string sourceConnectionString, string targetConnectionString)
        {
            try
            {
                // TODO job for maria db
                /*
                using (var location = new SqliteConnection(sourceConnectionString))
                using (var destination = new SqliteConnection(targetConnectionString))
                {
                    location.Open();
                    destination.Open();
                    location.BackupDatabase(destination);
                }*/
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed Backup");
                //textChannel.SendMessageAsync("Failed DB Backup: " + ex.Message);
            }
        }
        public override Task DoWork(CancellationToken cancellationToken)
        {
            try
            {
                ProcessChannels();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in ProcessChannels");
            }

            return Task.CompletedTask;
        }

        private async void ProcessChannels()
        {
            ///////////////
            /// Commands to run to setupo the key value db
            /// .admin keyval add ImageScrapeBasePath "<path>"
            /// .admin keyval add MessageScrapePerRun 1000 Int32 // optional else it will default to 10_000
            /// .admin keyval add ImageScrapeChannelIds "<channelid>,<channelid>"
            /// .admin keyval add LastScapedMessageForChannel_<channelid> 0 // for each channel optionally
            ///////////////





            // todo config
            ulong guildId = 747752542741725244;
            //var guild = Program.Client.GetGuild(guildId);
            //var textChannel = guild.GetTextChannel(spamChannel);

            var keyValueDBManager = DatabaseManager.KeyValueManager;

            string basePath = keyValueDBManager.Get<string>("ImageScrapeBasePath");

            int scrapePerRun = keyValueDBManager.Get<int>("MessageScrapePerRun");
            string imageScrapeChannelIdsString = keyValueDBManager.Get<string>("ImageScrapeChannelIds");

            if (string.IsNullOrWhiteSpace(basePath))
            {
                _logger.LogError("ImageScrapeBasePath not set");
                return;
            }

            if (scrapePerRun == 0)
                scrapePerRun = 10_000;

            if (imageScrapeChannelIdsString == null)
            {
                _logger.LogError("ImageScrapeChannelIds not set");
                return;
            }

            var imageScrapeChannelIds = imageScrapeChannelIdsString.Split(',').Select(x => ulong.Parse(x)).ToList();

            var guild = Program.Client.GetGuild(guildId);

            var channels = guild.Channels.Where(x => imageScrapeChannelIds.Contains(x.Id)).ToList();
            var client = new HttpClient();
            client.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (X11; Ubuntu; Linux x86_64; rv:122.0) Gecko/20100101 Firefox/122.0");

            FileDBManager fileDBManager = FileDBManager.Instance();

            foreach (var channel in channels)
            {
                List<IMessage> messages = new List<IMessage>();
                try
                {
                    var textChannel = channel as SocketTextChannel;
                    ulong lastMessageForChannel = keyValueDBManager.Get<ulong>($"LastScapedMessageForChannel_{channel.Id}");

                    if (lastMessageForChannel == 0)
                        lastMessageForChannel = SnowflakeUtils.ToSnowflake(DateTimeOffset.UtcNow);

                    _logger.LogInformation($"Last message for channel {channel.Name} is {lastMessageForChannel}");

                    messages = textChannel.GetMessagesAsync(lastMessageForChannel, Direction.Before, scrapePerRun).FlattenAsync().Result.ToList();

                    if (messages.Count == 0)
                    {
                        _logger.LogInformation($"No new messages left for channel {channel.Name}");
                        //continue;
                    }

                    _logger.LogInformation($"Found {messages.Count} new messages for channel {channel.Name}");

                    int botCount = 0;
                    int noUrlCount = 0;

                    foreach (var message in messages)
                    {
                        if (message.Author.IsBot)
                        {
                            botCount++;
                            continue;
                        }

                        if (message.Attachments.Count == 0 && message.Embeds.Count == 0)
                        {
                            noUrlCount++;
                            continue;
                        }

                        List<string> urls = new List<string>();

                        foreach (var attachment in message.Attachments)
                        {
                            urls.Add(attachment.Url);
                        }

                        foreach (var embed in message.Embeds)
                        {
                            if (embed.Type == EmbedType.Image)
                            {
                                urls.Add(embed.Url);
                            }

                            if (embed.Type == EmbedType.Video)
                            {
                                urls.Add(embed.Url);
                            }

                            if (embed.Type == EmbedType.Rich)
                            {
                                urls.Add(embed.Url);
                            }
                        }

                        SaveMessageIfNotFound(message, basePath, channel.Name);

                        foreach (var url in urls)
                        {
                            try
                            {
                                var result = await DiscordHelper.DownloadFile(client, message, message.Id, url, urls.IndexOf(url), basePath, "");

                                if (result != null)
                                {
                                    if (string.IsNullOrWhiteSpace(result.FullPath))
                                    {
                                        _logger.LogError($"Failed to download file as it has empty full path {url} with result {JsonConvert.SerializeObject(result)}");
                                        continue;
                                    }

                                    fileDBManager.SaveDiscordFile(result);
                                }
                                else
                                {
                                    _logger.LogError($"Failed to download file {url}");
                                }
                            }
                            catch (Exception ex)
                            {
                                _logger.LogError(ex, $"Error to download file {url}");
                            }

                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"Error in ProcessChannels for channel {channel.Name}");
                }

                if (messages.Count > 0)
                {
                    // update last message id
                    keyValueDBManager.Update($"LastScapedMessageForChannel_{channel.Id}", messages.Min(x => x.Id));
                }
                else
                {
                    int emptyCount = keyValueDBManager.Get<int>($"EmptyCount_{channel.Id}");
                    emptyCount++;
                    keyValueDBManager.Update($"EmptyCount_{channel.Id}", emptyCount);

                    // if empty count bigger than 10 we should stop this channel TODO
                }
            }
        }

        private void SaveMessageIfNotFound(IMessage message, string basePath, string path)
        {
            try
            {
                var dbManager = DatabaseManager.Instance();
                var dbUser = dbManager.GetDiscordUserById(message.Author.Id);

                if (dbUser == null)
                {
                    var user = message.Author;
                    var socketGuildUser = user as SocketGuildUser;

                    var dbUserNew = dbManager.CreateDiscordUser(new ETHBot.DataLayer.Data.Discord.DiscordUser()
                    {
                        DiscordUserId = user.Id,
                        DiscriminatorValue = user.DiscriminatorValue,
                        AvatarUrl = user.GetAvatarUrl() ?? user.GetDefaultAvatarUrl(), // If user has no custom avatar load the url for the default avatar
                        IsBot = user.IsBot,
                        IsWebhook = user.IsWebhook,
                        Nickname = socketGuildUser?.Nickname,
                        Username = user.Username,
                        JoinedAt = socketGuildUser?.JoinedAt
                    });
                }

                var dbMessage = dbManager.GetMessage(message.Id);
                if (dbMessage != null)
                {
                    return;
                }

                var newMessage = dbManager.CreateDiscordMessage(new ETHBot.DataLayer.Data.Discord.DiscordMessage()
                {
                    //Channel = discordChannel,
                    DiscordChannelId = message.Channel.Id,
                    //DiscordUser = dbAuthor,
                    DiscordUserId = message.Author.Id,
                    DiscordMessageId = message.Id,
                    Content = message.Content,
                    //ReplyMessageId = message.Reference.MessageId,
                    Preloaded = true
                }, true);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to save message");
            }
        }

        public override Task StopAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation($"{Name} is stopping.");
            return base.StopAsync(cancellationToken);
        }
    }
}
