using Discord;
using Discord.WebSocket;
using ETHBot.DataLayer.Data.Enums;
using ETHDINFKBot.Log;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace ETHDINFKBot.CronJobs.Jobs
{
    public class PreloadResponse
    {
        public int SuccessCount { get; set; }
        public int DuplicateCount { get; set; }
        public int EmotesAdded { get; set; }
        public int NewUsers { get; set; }
        public ulong OldestMessageId { get; set; }
        public ulong NewestMessageId { get; set; }
        public long ElapsedMilliseconds { get; set; }
        public bool ReachedEndOfChannel { get; set; }
    }



    public class PreloadJob : CronJobService
    {
        private readonly ulong ServerSuggestion = 747752542741725247; // todo config?
        private readonly ILogger<PreloadJob> _logger;
        private readonly string Name = "PreloadJob";

        public PreloadJob(IScheduleConfig<PreloadJob> config, ILogger<PreloadJob> logger)
            : base(config.CronExpression, config.TimeZoneInfo)
        {
            _logger = logger;
        }

        public override Task StartAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation($"{Name} starts.");
            return base.StartAsync(cancellationToken);
        }






        // this one needs some reworking
        private async Task<PreloadResponse> ProcessLoadedMessages(ISocketMessageChannel textChannel, ulong messageIdFrom, Direction direction, int count = 50_000)
        {
            PreloadResponse response = new PreloadResponse();
            response.OldestMessageId = UInt64.MaxValue;

            Stopwatch watch = new Stopwatch();
            watch.Start();
            // new column preloaded


            var dbManager = DatabaseManager.Instance();


            //var messages = channel.GetMessagesAsync(100000).FlattenAsync(); //defualt is 100

            var messagesFromMsg = await textChannel.GetMessagesAsync(messageIdFrom, direction, count).FlattenAsync();

            if (messagesFromMsg.Count() == 0 && direction == Direction.Before)
            {
                response.ReachedEndOfChannel = true;
                return response;
            }

            LogManager logManager = new LogManager(dbManager); // rework
            //int success = 0;
            //int tags = 0;
            //int newUsers = 0;
            //int duplicates = 0;
            //ulong oldestMessage = UInt64.MaxValue;
            //ulong newestMessage = UInt64.MaxValue;

            try
            {
                foreach (var message in messagesFromMsg)
                {
                    var dbUser = dbManager.GetDiscordUserById(message.Author.Id);

                    if (dbUser == null)
                    {
                        var user = message.Author;
                        var socketGuildUser = user as SocketGuildUser;


                        var dbUserNew = dbManager.CreateDiscordUser(new ETHBot.DataLayer.Data.Discord.DiscordUser()
                        {
                            DiscordUserId = user.Id,
                            DiscriminatorValue = user.DiscriminatorValue,
                            AvatarUrl = user.GetAvatarUrl(),
                            IsBot = user.IsBot,
                            IsWebhook = user.IsWebhook,
                            Nickname = socketGuildUser?.Nickname,
                            Username = user.Username,
                            JoinedAt = socketGuildUser?.JoinedAt
                        });

                        if (dbUserNew != null)
                            response.NewUsers++;

                    }

                    var newMessage = dbManager.CreateDiscordMessage(new ETHBot.DataLayer.Data.Discord.DiscordMessage()
                    {
                        //Channel = discordChannel,
                        DiscordChannelId = textChannel.Id,
                        //DiscordUser = dbAuthor,
                        DiscordUserId = message.Author.Id,
                        MessageId = message.Id,
                        Content = message.Content,
                        //ReplyMessageId = message.Reference.MessageId,
                        Preloaded = true
                    }, true);


                    if (newMessage)
                    {
                        response.SuccessCount++;
                    }
                    else
                    {
                        response.DuplicateCount++;
                    }

                    if (message.Id < response.OldestMessageId)
                        response.OldestMessageId = message.Id;

                    if (message.Id > response.NewestMessageId)
                        response.NewestMessageId = message.Id;

                    if (message.Reactions.Count > 0 && newMessage)
                    {
                        if (newMessage && message.Tags.Count > 0)
                        {
                            response.EmotesAdded += message.Tags.Count;
                            await logManager.ProcessEmojisAndPings(message.Tags, message.Author.Id, message.Id, message.Author as SocketGuildUser, true);
                        }
                    }
                }
                watch.Stop();

                response.ElapsedMilliseconds = watch.ElapsedMilliseconds;

                // todo return object
                return response;

                //Context.Channel.SendMessageAsync($"Processed {messagesFromMsg.Count()} Added: {success} TagsCount: {tags} From: {SnowflakeUtils.FromSnowflake(messagesFromMsg.First()?.Id ?? 1)} To: {SnowflakeUtils.FromSnowflake(messagesFromMsg.Last()?.Id ?? 1)}" +
                //    $" New Users: {newUsers} In: {watch.ElapsedMilliseconds}ms", false);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message);
                return null;
            }
        }


        public override async Task DoWork(CancellationToken cancellationToken)
        {
            _logger.LogInformation($"{DateTime.Now:hh:mm:ss} {Name} is working.");
            // get settings per channel
            var botChannelSettings = DatabaseManager.Instance().GetAllChannelSettings(); // TODO filter for guild

            string result = "";
            foreach (var item in Program.Client.Guilds)
            {
                if (item.Id != 747752542741725244)
                    continue; // only sync eth dinfk for now

                foreach (var botChannelSetting in botChannelSettings)
                {
                    var channel = item.GetTextChannel(botChannelSetting.DiscordChannelId);


                    if (botChannelSetting != null && ((BotPermissionType)botChannelSetting?.ChannelPermissionFlags).HasFlag(BotPermissionType.Read))
                    {
                        if (channel != null)
                        {
                            // TODO alot of dupe code clean up and generalize


                            if (!botChannelSetting.ReachedOldestPreload)
                            {
                                // should be 50 calls -> around 1 min per channel
                                int amount = 10_000; // we do only 50k a day to not overload the requests + db

                                // we still need to go back
                                DateTimeOffset fromDate = DateTimeOffset.Now;

                                if(botChannelSetting.OldestPostTimePreloaded.HasValue)
                                    fromDate = botChannelSetting.OldestPostTimePreloaded.Value;

                                ulong fromSnowflake = SnowflakeUtils.ToSnowflake(fromDate);

                                var processResponse = await ProcessLoadedMessages(channel, fromSnowflake, Direction.Before, amount);


                                if(processResponse?.SuccessCount > 0 && !processResponse.ReachedEndOfChannel)
                                {
                                    // we synced some successfully
                                    result += $"{channel.Name} synced. Msg: {processResponse.SuccessCount}/{processResponse.DuplicateCount} Users: {processResponse.NewUsers} Emotes: {processResponse.EmotesAdded} " +
                                        $"Time: {processResponse.ElapsedMilliseconds/1000}s Time: {SnowflakeUtils.FromSnowflake(processResponse.OldestMessageId)}/{SnowflakeUtils.FromSnowflake(processResponse.NewestMessageId)}" + Environment.NewLine;

                                    DatabaseManager.Instance().UpdateChannelSetting(botChannelSetting.DiscordChannelId, -1, processResponse.OldestMessageId, processResponse.NewestMessageId);
                                }
                                else if (processResponse.ReachedEndOfChannel)
                                {
                                    result += $"{channel.Name} no new messages. Reached the end..." + Environment.NewLine;

                                    // reached the end
                                    DatabaseManager.Instance().UpdateChannelSetting(botChannelSetting.DiscordChannelId, -1, processResponse.OldestMessageId, processResponse.NewestMessageId);
                                }
                                else
                                {
                                    // error which "hopefully got logged and we just ignore this case xD
                                }

                                //var messagesFromMsg = await channel.GetMessagesAsync(fromSnowflake, Direction.Before, amount).FlattenAsync();

                            }
                            else
                            {
                                // should be 50 calls -> around 1 min per channel
                                int amount = 20_000; // we do only 50k a day to not overload the requests + db

                                // we still need to go back
                                DateTimeOffset fromDate = DateTimeOffset.Now;

                                if (botChannelSetting.NewestPostTimePreloaded.HasValue)
                                    fromDate = botChannelSetting.NewestPostTimePreloaded.Value;

                                ulong fromSnowflake = SnowflakeUtils.ToSnowflake(fromDate);

                                var processResponse = await ProcessLoadedMessages(channel, fromSnowflake, Direction.After, amount);

                                if (processResponse?.SuccessCount > 0 || processResponse?.DuplicateCount > 0)
                                {
                                    // we synced some successfully
                                    result += $"{channel.Name} synced (new). Msg: {processResponse.SuccessCount}/{processResponse.DuplicateCount} Users: {processResponse.NewUsers} Emotes: {processResponse.EmotesAdded} " +
                                        $"Time: {processResponse.ElapsedMilliseconds / 1000}s Time: {SnowflakeUtils.FromSnowflake(processResponse.OldestMessageId)}/{SnowflakeUtils.FromSnowflake(processResponse.NewestMessageId)}" + Environment.NewLine;

                                    DatabaseManager.Instance().UpdateChannelSetting(botChannelSetting.DiscordChannelId, -1, processResponse.OldestMessageId, processResponse.NewestMessageId);
                                }
                                else
                                {
                                    // error which "hopefully got logged and we just ignore this case xD
                                }
                            }



                            // read all channels that we can read from the current guild
                            // see preload status
                            // do preload

                            // <- 25k msg at a time
                            // -> from newest to now compare delta

                        }
                        else
                        {
                            result += $"Ignored {channel?.Name ?? "Channel deleted but has active settings"}" + Environment.NewLine;
                        }
                    }
                    else
                    {
                        result += $"Ignored {channel?.Name ?? "Channel not found"}" + Environment.NewLine;
                    }
                }
            }

            if(result.Length > 0)
            {
                ulong guildId = 747752542741725244;
                ulong spamChannel = 768600365602963496;
                var guild = Program.Client.GetGuild(guildId);
                var textChannel = guild.GetTextChannel(spamChannel);

                textChannel.SendMessageAsync(result.Substring(0, Math.Min(2000, result.Length)));
            }


            // todo if result > 2k chars


            //return Task.CompletedTask;
        }

        public override Task StopAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation($"{Name} is stopping.");
            return base.StopAsync(cancellationToken);
        }
    }
}
