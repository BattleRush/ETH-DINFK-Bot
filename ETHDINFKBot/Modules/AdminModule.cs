using Discord;
using Discord.Commands;
using Discord.WebSocket;
using ETHBot.DataLayer;
using ETHBot.DataLayer.Data.Enums;
using ETHDINFKBot.Drawing;
using ETHDINFKBot.Helpers;
using ETHDINFKBot.Log;
using Newtonsoft.Json;
using Reddit;
using Reddit.Controllers;
using RedditScrapper;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace ETHDINFKBot.Modules
{

    public class Class1
    {
        public ulong id { get; set; }
        public string nick { get; set; }
        public string top_role_name { get; set; }
        public ulong top_role_id { get; set; }
    }



    [Group("admin")]
    public class AdminModule : ModuleBase<SocketCommandContext>
    {
        [Command("renameback")]
        public async Task Test()
        {
            return; // disable again
            var author = Context.Message.Author;
            if (author.Id != ETHDINFKBot.Program.Owner)
            {
                Context.Channel.SendMessageAsync("You aren't allowed to run this command", false);
                return;
            }
            try
            {
                var allUsers = await Context.Guild.GetUsersAsync().FlattenAsync();
                Context.Channel.SendMessageAsync("users " + allUsers.Count().ToString(), false);

                Random r = new Random();

                var jsonString = File.ReadAllText("");

                var jsonUsers = JsonConvert.DeserializeObject<Class1[]>(jsonString).ToList();

                Context.Channel.SendMessageAsync("json " + jsonUsers.Count.ToString(), false);


                foreach (SocketGuildUser user in allUsers)
                {
                    var targerUser = jsonUsers.SingleOrDefault(i => i.id == user.Id);

                    if (targerUser == null || targerUser.nick == user.Nickname)
                        continue;

                    try
                    {
                        await user.ModifyAsync(i =>
                        {
                            i.Nickname = targerUser.nick;
                        });

                        Context.Channel.SendMessageAsync("Fixing " + user.Username, false);

                    }
                    catch (Exception ex)
                    {
                        Context.Channel.SendMessageAsync(ex.Message + " on " + user.Username, false);
                    }
                }
            }
            catch (Exception ex)
            {
                Context.Channel.SendMessageAsync(ex.Message, false);
            }
        }

        [Command("help")]
        public async Task AdminHelp()
        {
            var author = Context.Message.Author;
            if (author.Id != ETHDINFKBot.Program.Owner)
            {
                Context.Channel.SendMessageAsync("You aren't allowed to run this command", false);
                return;
            }

            EmbedBuilder builder = new EmbedBuilder();

            builder.WithTitle("Admin Help (Admin only)");

            builder.WithColor(0, 0, 255);

            builder.WithThumbnailUrl(Program.Client.CurrentUser.GetAvatarUrl());

            builder.WithCurrentTimestamp();
            builder.AddField("admin help", "This message :)");
            builder.AddField("admin channel help", "Help for channel command");
            builder.AddField("admin reddit help", "Help for reddit command");
            builder.AddField("admin rant help", "Help for rant command");
            builder.AddField("admin place help", "Help for place command");
            builder.AddField("admin kill", "Do I really need to explain this one");
            builder.AddField("admin blockemote <id> <block>", "Block an emote from being selectable");

            Context.Channel.SendMessageAsync("", false, builder.Build());
        }

        [Command("kill")]
        public async Task AdminKill()
        {
            var author = Context.Message.Author;
            if (author.Id != ETHDINFKBot.Program.Owner)
            {
                Context.Channel.SendMessageAsync("You aren't allowed to run this command", false);
                return;
            }
            Context.Channel.SendMessageAsync("I'll be back!", false);
            Process.GetCurrentProcess().Kill();
        }

        [Command("blockemote")]
        public async Task BlockEmote(ulong emoteId, bool blockStatus)
        {
            var author = Context.Message.Author;
            if (author.Id != ETHDINFKBot.Program.Owner)
            {
                Context.Channel.SendMessageAsync("You aren't allowed to run this command", false);
                return;
            }

            bool success = DatabaseManager.Instance().SetEmoteBlockStatus(emoteId, blockStatus);

            if (success)
            {
                Context.Channel.SendMessageAsync($"Successfully set block status of emote {emoteId} to: {blockStatus}", false);
            }
            else
            {
                Context.Channel.SendMessageAsync($"Failed to set block status of emote {emoteId}", false);
            }
        }


        [Group("rant")]
        public class RantAdminModule : ModuleBase<SocketCommandContext>
        {
            [Command("help")]
            public async Task AdminHelp()
            {
                var author = Context.Message.Author;
                if (author.Id != ETHDINFKBot.Program.Owner)
                {
                    Context.Channel.SendMessageAsync("You aren't allowed to run this command", false);
                    return;
                }

                EmbedBuilder builder = new EmbedBuilder();

                builder.WithTitle("Admin Help (Admin only)");

                builder.WithColor(0, 0, 255);
                builder.WithThumbnailUrl(Program.Client.CurrentUser.GetAvatarUrl());

                builder.WithCurrentTimestamp();
                builder.AddField("admin rant help", "This message :)");
                builder.AddField("admin rant all", "List all types");
                builder.AddField("admin rant add <type>", "Add new type (open for all)");
                builder.AddField("admin rant dt <type id>", "Delete type");
                builder.AddField("admin rant dr <rant id>", "Delete rant");

                Context.Channel.SendMessageAsync("", false, builder.Build());
            }

            [Command("all")]
            public async Task AdminAllRantTypes()
            {
                // todo a bit of a duplicate from DiscordModule
                var typeList = DatabaseManager.Instance().GetAllRantTypes();
                string allTypes = "```" + string.Join(", ", typeList) + "```";

                EmbedBuilder builder = new EmbedBuilder();

                builder.WithTitle("All Rant types");

                builder.WithColor(0, 0, 255);
                builder.WithThumbnailUrl(Program.Client.CurrentUser.GetAvatarUrl());

                builder.WithCurrentTimestamp();
                builder.AddField("Types [Id, Name]", allTypes);

                Context.Channel.SendMessageAsync("", false, builder.Build());
            }

            [Command("add")]
            public async Task AddRantType(string type)
            {
                /*var author = Context.Message.Author;
                if (author.Id != ETHDINFKBot.Program.Owner)
                {
                    Context.Channel.SendMessageAsync("You aren't allowed to run this command", false);
                    return;
                }*/

                bool success = DatabaseManager.Instance().AddRantType(type);
                Context.Channel.SendMessageAsync($"Added {type} Success: {success}", false);
            }

            [Command("dt")]
            public async Task DeleteRantType(int typeId)
            {
                var author = Context.Message.Author;
                if (author.Id != ETHDINFKBot.Program.Owner)
                {
                    Context.Channel.SendMessageAsync("You aren't allowed to run this command", false);
                    return;
                }

                bool success = DatabaseManager.Instance().DeleteRantType(typeId);
                Context.Channel.SendMessageAsync("Delete success: " + success, false);
            }


            [Command("dr")]
            public async Task DeleteRantMessage(int typeId)
            {
                var author = Context.Message.Author;
                if (author.Id != ETHDINFKBot.Program.Owner)
                {
                    Context.Channel.SendMessageAsync("You aren't allowed to run this command", false);
                    return;
                }

                bool success = DatabaseManager.Instance().DeleteRantMessage(typeId);
                Context.Channel.SendMessageAsync("Delete success: " + success, false);
            }
        }


        [Group("channel")]
        public class ChannelAdminModule : ModuleBase<SocketCommandContext>
        {
            [Command("help")]
            public async Task ChannelAdminHelp()
            {
                var author = Context.Message.Author;
                if (author.Id != ETHDINFKBot.Program.Owner)
                {
                    Context.Channel.SendMessageAsync("You aren't allowed to run this command", false);
                    return;
                }

                EmbedBuilder builder = new EmbedBuilder();

                builder.WithTitle("Reddit Admin Help (Admin only)");

                builder.WithColor(0, 0, 255);
                builder.WithThumbnailUrl(Program.Client.CurrentUser.GetAvatarUrl());

                builder.WithCurrentTimestamp();
                builder.AddField("admin channel help", "This message :)");
                builder.AddField("admin channel info", "Returns info about the current channel settings and global channel order info");
                builder.AddField("admin channel lock <true|false>", "Locks the ordering of all channels and reverts any order changes when active");
                builder.AddField("admin channel preload <channelId> <amount>", "Loads old messages into the DB");
                builder.AddField("admin channel set <permission>", "Set permissions for the current channel");
                builder.AddField("admin channel all <permission>", "Set the MINIMUM permissions for ALL channels");
                builder.AddField("admin channel flags", "Returns help with the flag infos");

                Context.Channel.SendMessageAsync("", false, builder.Build());
            }

            /* public static IEnumerable<Enum> GetAllFlags(Enum e)
             {
                 return Enum.GetValues(e.GetType()).Cast<Enum>();
             }

             // TODO move to somewhere common
             static IEnumerable<Enum> GetFlags(Enum input)
             {
                 foreach (Enum value in Enum.GetValues(input.GetType()))
                     if (input.HasFlag(value))
                         yield return value;
             }*/


            [Command("lockinfo")]
            [RequireUserPermission(GuildPermission.ManageChannels)]
            public async Task GetLockInfo()
            {
                ulong guildId = 747752542741725244;

#if DEBUG
                guildId = 774286694794919986;
#endif

                var guild = Program.Client.GetGuild(guildId);


                var channels = guild.Channels;

                var sortedDict = from entry in Program.ChannelPositions orderby entry.Value ascending select entry;

                List<string> header = new List<string>()
                        {
                            "Order",
                            "Channel Name",
                            "Id"
                        };


                List<List<string>> data = new List<List<string>>();

                foreach (var item in sortedDict)
                {
                    var channel = channels.SingleOrDefault(i => i.Id == item.Key);
                    if (channel == null)
                        continue;

                    var currentRecord = new List<string>();

                    currentRecord.Add(item.Value.ToString());
                    currentRecord.Add(Regex.Replace(channel.Name, @"[^\u0000-\u007F]+", string.Empty));
                    currentRecord.Add(item.Key.ToString());


                    data.Add(currentRecord);

                }



                var drawTable = new DrawTable(header, data, "");

                var stream = await drawTable.GetImage();
                if (stream == null)
                    return;// todo some message

                await Context.Channel.SendFileAsync(stream, "graph.png", "", false, null, null, false, null, new Discord.MessageReference(Context.Message.Id));
                stream.Dispose();
            }

            [Command("lock")]
            [RequireUserPermission(GuildPermission.ManageChannels)]
            public async Task LockChannelOrdering(bool lockChannels)
            {
                // allow for people that can manage channels to lock the ordering

                var botSettings = DatabaseManager.Instance().GetBotSettings();
                botSettings.ChannelOrderLocked = lockChannels;
                botSettings = DatabaseManager.Instance().SetBotSettings(botSettings);

                Context.Message.Channel.SendMessageAsync($"Set Global Postion Lock to: {botSettings.ChannelOrderLocked}");

                if (botSettings.ChannelOrderLocked)
                {
                    // TODO Setting
                    ulong guildId = 747752542741725244;

#if DEBUG
                    guildId = 774286694794919986;
#endif

                    var guild = Program.Client.GetGuild(guildId);

                    Program.ChannelPositions = new Dictionary<ulong, int>();

                    // refresh the current order
                    foreach (var item in guild.Channels)
                        Program.ChannelPositions.Add(item.Id, item.Position);


                    Context.Message.Channel.SendMessageAsync($"Saved ordering for: {Program.ChannelPositions.Count}");
                }
                else
                {
                    // do nothing
                }
            }

            [Command("preload")]
            public async Task PreloadOldMessages(ulong channelId, int count = 1000)
            {
                Stopwatch watch = new Stopwatch();
                watch.Start();
                // new column preloaded
                var author = Context.Message.Author;
                if (author.Id != ETHDINFKBot.Program.Owner)
                {
                    Context.Channel.SendMessageAsync("You aren't allowed to run this command", false);
                    return;
                }
                var dbManager = DatabaseManager.Instance();
                var channel = Program.Client.GetChannel(channelId) as ISocketMessageChannel;
                var oldestMessage = dbManager.GetOldestMessageAvailablePerChannel(channelId);

                if (oldestMessage == null)
                    return;

                //var messages = channel.GetMessagesAsync(100000).FlattenAsync(); //default is 100

                var messagesFromMsg = await channel.GetMessagesAsync(oldestMessage.Value, Direction.Before, count).FlattenAsync();

                LogManager logManager = new LogManager(dbManager);
                int success = 0;
                int tags = 0;
                int newUsers = 0;
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
                                newUsers++;

                        }

                        var newMessage = dbManager.CreateDiscordMessage(new ETHBot.DataLayer.Data.Discord.DiscordMessage()
                        {
                            //Channel = discordChannel,
                            DiscordChannelId = channelId,
                            //DiscordUser = dbAuthor,
                            DiscordUserId = message.Author.Id,
                            DiscordMessageId = message.Id,
                            Content = message.Content,
                            //ReplyMessageId = message.Reference.MessageId,
                            Preloaded = true
                        });


                        if (newMessage)
                        {
                            success++;
                        }
                        if (message.Reactions.Count > 0)
                        {


                            if (newMessage && message.Tags.Count > 0)
                            {
                                tags += message.Tags.Count;
                                logManager.ProcessEmojisAndPings(message.Tags, message.Author.Id, message.Id, message.Author as SocketGuildUser);
                            }
                        }
                    }
                    watch.Stop();

                    Context.Channel.SendMessageAsync($"Processed {messagesFromMsg.Count()} Added: {success} TagsCount: {tags} From: {SnowflakeUtils.FromSnowflake(messagesFromMsg.First()?.Id ?? 1)} To: {SnowflakeUtils.FromSnowflake(messagesFromMsg.Last()?.Id ?? 1)}" +
                        $" New Users: {newUsers} In: {watch.ElapsedMilliseconds}ms", false);
                }
                catch (Exception ex)
                {

                }
            }

            [Command("info")]

            public async Task GetChannelInfoAsync(bool all = false)
            {
                var guildUser = Context.Message.Author as SocketGuildUser;
                var author = Context.Message.Author;
                if (!(author.Id == ETHDINFKBot.Program.Owner || guildUser.GuildPermissions.ManageChannels))
                {
                    Context.Channel.SendMessageAsync("You aren't allowed to run this command", false);
                    return;
                }

                if (Context.Message.Channel is SocketGuildChannel guildChannel)
                {
                    if (!all)
                    {
                        var channelInfo = DatabaseManager.Instance().GetChannelSetting(guildChannel.Id);
                        var botSettings = DatabaseManager.Instance().GetBotSettings();

                        if (channelInfo == null)
                        {
                            Context.Channel.SendMessageAsync("channelInfo is null bad admin", false);
                            return;
                        }

                        EmbedBuilder builder = new EmbedBuilder();
                        builder.WithTitle($"Channel Info for {guildChannel.Name}");
                        builder.WithDescription($"Global Channel position lock active: {botSettings.ChannelOrderLocked} for " +
                            $"{(botSettings.ChannelOrderLocked ? Program.ChannelPositions.Count : -1)} channels");
                        builder.WithColor(255, 0, 0);
                        builder.WithThumbnailUrl(Program.Client.CurrentUser.GetAvatarUrl());

                        builder.WithCurrentTimestamp();

                        builder.AddField("Permission flag", channelInfo.ChannelPermissionFlags);

                        foreach (BotPermissionType flag in Enum.GetValues(typeof(BotPermissionType)))
                        {
                            var hasFlag = ((BotPermissionType)channelInfo.ChannelPermissionFlags).HasFlag(flag);
                            builder.AddField(flag.ToString() + $" ({(int)flag})", $"```diff\r\n{(hasFlag ? "+ YES" : "- NO")}```", true);
                        }


                        Context.Channel.SendMessageAsync("", false, builder.Build());

                    }
                    else
                    {
                        var botChannelSettings = DatabaseManager.Instance().GetAllChannelSettings();

                        List<string> header = new List<string>()
                        {
                            "Channel Id",
                            "Channel Name",
                            "Permission value",
                            "Permission string",
                            "Preload old",
                            "Preload new",
                            "Reached oldest"
                        };


                        List<List<string>> data = new List<List<string>>();


                        foreach (var channelSetting in botChannelSettings)
                        {
                            List<string> channelInfoRow = new List<string>();

                            var discordChannel = DatabaseManager.Instance().GetDiscordChannel(channelSetting.DiscordChannelId);

                            if (discordChannel.DiscordServerId != guildChannel.Guild.Id)
                                break; // dont show other server

                            channelInfoRow.Add(discordChannel.DiscordChannelId.ToString());
                            channelInfoRow.Add(discordChannel.ChannelName);
                            channelInfoRow.Add(channelSetting.ChannelPermissionFlags.ToString());
                            List<string> permissionFlagNames = new List<string>();
                            foreach (BotPermissionType flag in Enum.GetValues(typeof(BotPermissionType)))
                            {
                                var hasFlag = ((BotPermissionType)(channelSetting.ChannelPermissionFlags)).HasFlag(flag);

                                if (hasFlag)
                                    permissionFlagNames.Add($"{flag} ({(int)flag})");
                            }

                            channelInfoRow.Add(string.Join(", " + Environment.NewLine, permissionFlagNames));
                            channelInfoRow.Add(channelSetting.OldestPostTimePreloaded?.ToString());
                            channelInfoRow.Add(channelSetting.NewestPostTimePreloaded?.ToString());
                            channelInfoRow.Add(channelSetting.ReachedOldestPreload.ToString());
                            data.Add(channelInfoRow);
                        }

                        var drawTable = new DrawTable(header, data, "");

                        var stream = await drawTable.GetImage();
                        if (stream == null)
                            return;// todo some message

                        await Context.Channel.SendFileAsync(stream, "graph.png", "", false, null, null, false, null, new Discord.MessageReference(Context.Message.Id));
                        stream.Dispose();
                    }
                }

            }

            [Command("set")]
            public async Task SetChannelInfoAsync(int flag, ulong? channelId = null)
            {
                var author = Context.Message.Author;
                if (author.Id != ETHDINFKBot.Program.Owner)
                {
                    Context.Channel.SendMessageAsync("You aren't allowed to run this command", false);
                    return;
                }
                if (Context.Message.Channel is SocketGuildChannel guildChannel)
                {
                    if (!channelId.HasValue)
                    {

                        DatabaseManager.Instance().UpdateChannelSetting(guildChannel.Id, flag);
                        Context.Channel.SendMessageAsync($"Set flag {flag} for channel {guildChannel.Name}", false);
                    }
                    else
                    {
                        var channel = guildChannel.Guild.GetTextChannel(channelId.Value);

                        DatabaseManager.Instance().UpdateChannelSetting(channel.Id, flag);
                        Context.Channel.SendMessageAsync($"Set flag {flag} for channel {channel.Name}", false);
                    }
                }
            }

            [Command("all")]
            public async Task SetAllChannelInfoAsync(int flag)
            {
                return; // this one is a bit too risky xD
                var author = Context.Message.Author;
                if (author.Id != ETHDINFKBot.Program.Owner)
                {
                    Context.Channel.SendMessageAsync("You aren't allowed to run this command", false);
                    return;
                }

                if (Context.Message.Channel is SocketGuildChannel guildChannel)
                {
                    var channels = DatabaseManager.Instance().GetDiscordAllChannels(guildChannel.Guild.Id);

                    foreach (var item in channels)
                    {
                        DatabaseManager.Instance().UpdateChannelSetting(item.DiscordChannelId, flag, 0, 0, true);
                        Context.Channel.SendMessageAsync($"Set flag {flag} for channel {item.ChannelName}", false);
                    }
                }
            }


            [Command("flags")]
            public async Task GetChannelInfoFlagsAsync()
            {
                var author = Context.Message.Author;
                if (author.Id != ETHDINFKBot.Program.Owner)
                {
                    Context.Channel.SendMessageAsync("You aren't allowed to run this command", false);
                    return;
                }

                EmbedBuilder builder = new EmbedBuilder();

                builder.WithTitle($"Available flags");

                builder.WithColor(255, 0, 0);

                builder.WithThumbnailUrl(Program.Client.CurrentUser.GetAvatarUrl());
                builder.WithCurrentTimestamp();
                string inlineString = "```";
                foreach (BotPermissionType flag in Enum.GetValues(typeof(BotPermissionType)))
                {
                    inlineString += $"{flag} ({(int)(flag)})\r\n";
                }

                inlineString = inlineString.Trim() + "```";
                builder.AddField("BotPermissionType", inlineString);

                Context.Channel.SendMessageAsync("", false, builder.Build());
            }
        }

        [Group("place")]
        public class PlaceAdminModule : ModuleBase<SocketCommandContext>
        {
            [Command("help")]
            public async Task PlaceAdminHelp()
            {
                var author = Context.Message.Author;
                if (author.Id != ETHDINFKBot.Program.Owner)
                {
                    Context.Channel.SendMessageAsync("You aren't allowed to run this command", false);
                    return;
                }

                EmbedBuilder builder = new EmbedBuilder();

                builder.WithTitle("PLace Admin Help");

                builder.WithColor(0, 0, 255);


                builder.WithThumbnailUrl(Program.Client.CurrentUser.GetAvatarUrl());
                builder.WithCurrentTimestamp();
                builder.AddField("admin place help", "This message :)");
                builder.AddField("admin place verify <user> <true|false>", "Used to verify user for multipixel feature");

                Context.Channel.SendMessageAsync("", false, builder.Build());
            }

            [Command("verify")]
            public async Task VerifyPlaceUser(SocketUser user, bool verified)
            {
                var author = Context.Message.Author;
                if (author.Id != ETHDINFKBot.Program.Owner)
                {
                    Context.Channel.SendMessageAsync("You aren't allowed to run this command", false);
                    return;
                }

                var success = DatabaseManager.Instance().VerifyDiscordUserForPlace(user.Id, verified);

                await Context.Channel.SendMessageAsync($"Set <@{user.Id}> to {verified} Success: {success}", false);
            }
        }

        [Group("reddit")]
        public class RedditAdminModule : ModuleBase<SocketCommandContext>
        {
            [Command("help")]
            public async Task RedditAdminHelp()
            {
                var author = Context.Message.Author;
                if (author.Id != ETHDINFKBot.Program.Owner)
                {
                    Context.Channel.SendMessageAsync("You aren't allowed to run this command", false);
                    return;
                }

                EmbedBuilder builder = new EmbedBuilder();

                builder.WithTitle("Reddit Admin Help (Admin only)");

                builder.WithColor(0, 0, 255);

                builder.WithThumbnailUrl(Program.Client.CurrentUser.GetAvatarUrl());
                builder.WithCurrentTimestamp();
                builder.AddField("admin reddit help", "This message :)");
                builder.AddField("admin reddit status", "Returns if there are currently active scrapers");
                builder.AddField("admin reddit add <name>", "Add Subreddit to SubredditInfos");
                builder.AddField("admin reddit ban <name>", "Manually ban");
                builder.AddField("admin reddit start <name>", "Starts the scraper for a specific subreddit if no scraper is currently running");

                Context.Channel.SendMessageAsync("", false, builder.Build());
            }

            [Command("status")]
            public async Task CheckStatusAsync()
            {
                var author = Context.Message.Author;
                if (author.Id != ETHDINFKBot.Program.Owner)
                {
                    Context.Channel.SendMessageAsync("You aren't allowed to run this command", false);
                    return;
                }
                CheckReddit();
            }

            [Command("add")]
            public async Task AddSubredditAsync(string subredditName)
            {
                subredditName = subredditName.ToLower();

                var author = Context.Message.Author;
                if (author.Id != ETHDINFKBot.Program.Owner)
                {
                    Context.Channel.SendMessageAsync("Ping the owner and he will add it for you", false);
                    return;
                }

                AddSubreddit(subredditName);
            }

            [Command("ban")]
            public async Task BanSubredditAsync(string subredditName)
            {
                subredditName = subredditName.ToLower();

                var author = Context.Message.Author;
                if (author.Id != ETHDINFKBot.Program.Owner)
                {
                    Context.Channel.SendMessageAsync("You aren't allowed to run this command", false);
                    return;
                }

                DatabaseManager.Instance().BanSubreddit(subredditName);
                Context.Channel.SendMessageAsync("Banned " + subredditName, false);

            }

            [Command("start")]
            public async Task StartScraperAsync(string subredditName)
            {
                var author = Context.Message.Author;
                if (author.Id != ETHDINFKBot.Program.Owner)
                {
                    Context.Channel.SendMessageAsync("You aren't allowed to run this command", false);
                    return;
                }

                var subreddits = DatabaseManager.Instance().GetSubredditsByStatus();
                if (subreddits.Count > 0)
                {
                    Context.Channel.SendMessageAsync($"{subreddits.First().SubredditName} is currently running. Try again later", false);
                    return;
                }
                // TODO check if no scraper is active
                Context.Channel.SendMessageAsync($"Started {subredditName} please wait :)", false);

                if (subredditName.ToLower() == "all")
                {
                    var allSubreddits = DatabaseManager.Instance().GetSubredditsByStatus(false);

                    var allNames = allSubreddits.Select(i => i.SubredditName).ToList();

                    Context.Channel.SendMessageAsync($"Starting", false);

                    for (int i = 0; i < allNames.Count; i += 100)
                    {
                        var items = allNames.Skip(i).Take(100);
                        Context.Channel.SendMessageAsync($"{string.Join(", ", items)}", false);
                        // Do something with 100 or remaining items
                    }

                    Context.Channel.SendMessageAsync($"Please wait :)", false);


                    Task.Factory.StartNew(() => CommonHelper.ScrapReddit(allNames, Context.Channel));
                }
                else
                {
                    Task.Factory.StartNew(() => CommonHelper.ScrapReddit(subredditName, Context.Channel));
                }
            }


            private async void CheckReddit()
            {
                var subreddits = DatabaseManager.Instance().GetSubredditsByStatus();

                foreach (var subreddit in subreddits)
                {
                    Context.Channel.SendMessageAsync($"{subreddit.SubredditName} is active", false);
                }

                if (subreddits.Count == 0)
                {
                    Context.Channel.SendMessageAsync($"No subreddits are currently active", false);
                }
            }

            private async void AddSubreddit(string subredditName)
            {
                var reddit = new RedditClient(Program.RedditAppId, Program.RedditRefreshToken, Program.RedditAppSecret);
                using (ETHBotDBContext context = new ETHBotDBContext())
                {
                    SubredditManager subManager = new SubredditManager(subredditName, reddit, context);
                    Context.Channel.SendMessageAsync($"{subManager.SubredditName} was added to the list", false); // NSFW: {subManager.SubredditInfo.IsNSFW}
                }
            }


            // TODO cleanup this mess


        }
    }
}