using Discord;
using Discord.Commands;
using Discord.Rest;
using Discord.Webhook;
using Discord.WebSocket;
using ETHBot.DataLayer.Data;
using ETHBot.DataLayer.Data.Discord;
using ETHBot.DataLayer.Data.Enums;
using ETHDINFKBot.Data;
using ETHDINFKBot.Helpers;
using SkiaSharp;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace ETHDINFKBot.Handlers
{
    public class MessageHandler
    {
        private readonly ulong AdministratorRoleId = 747753814723002500;

        private readonly Dictionary<string, ulong> DiscordChannels = DiscordHelper.DiscordChannels;
        private readonly Dictionary<string, ulong> DiscordEmotes = DiscordHelper.DiscordEmotes;

        private SocketUserMessage SocketMessage;
        private SocketGuildUser SocketGuildUser;
        private SocketCategoryChannel SocketCategoryChannel;
        private SocketTextChannel SocketTextChannel;
        private SocketThreadChannel SocketThreadChannel;
        private SocketGuildChannel SocketGuildChannel;
        private SocketGuild SocketGuild;

        private DatabaseManager DatabaseManager;
        private BotChannelSetting ChannelSettings;
        private List<string> CommandInfos;

        public MessageHandler(SocketUserMessage socketMessage, List<string> commandList, BotChannelSetting channelSettings = null)
        {
            SocketMessage = socketMessage;

            // verify what to do when these 2 cant be cast
            SocketGuildUser = socketMessage.Author as SocketGuildUser;
            SocketTextChannel = socketMessage.Channel as SocketTextChannel;
            SocketCategoryChannel = SocketTextChannel.Category as SocketCategoryChannel;
            SocketThreadChannel = socketMessage.Channel as SocketThreadChannel;
            SocketGuildChannel = socketMessage.Channel as SocketGuildChannel;

            if (socketMessage.Channel is SocketThreadChannel)
            {
                // The message if from a thread -> Replace the SocketChannel to the parent channel
                if (SocketThreadChannel.ParentChannel is SocketTextChannel)
                    SocketTextChannel = SocketThreadChannel.ParentChannel as SocketTextChannel;

                SocketGuildChannel = SocketThreadChannel.ParentChannel;

                // TODO Fix the correct setting from the calling method
                channelSettings = CommonHelper.GetChannelSettingByThreadId(SocketThreadChannel.Id).Setting;
            }

            // Dont handle DM's
            if (SocketGuildChannel == null)
                return;

            SocketGuild = SocketGuildChannel.Guild;

            ChannelSettings = channelSettings;
            CommandInfos = commandList;

            DatabaseManager = DatabaseManager.Instance();
        }

        // TODO do it in pararel 
        public async Task<bool> Run()
        {
            if (SocketMessage.Author.IsWebhook || SocketGuildChannel == null || SocketThreadChannel?.Id == 996746797236105236)
                return false; // slash commands are webhooks ???

            //AdministratorBait();
            try
            {
                EmoteDetection();
                Autoreact();
                LiveInBestCanton();
            }
            catch (Exception ex)
            {
                // TODO log
            }

            // Log to DB
            await CreateDiscordServerDBEntry();
            await CreateDiscordChannelDBEntry();
            await CreateOrUpdateDBUser();
            await CreateDiscordMessageDBEntry();

            return true; // kinda useless
        }

        private async Task<bool> CreateDiscordServerDBEntry()
        {
            // NO DM Tracking
            if (SocketGuildChannel == null)
                return false;

            var discordServer = DatabaseManager.GetDiscordServerById(SocketGuild.Id);

            if (discordServer == null)
            {
                var newDiscordServerEntry = DatabaseManager.CreateDiscordServer(new DiscordServer()
                {
                    DiscordServerId = SocketGuild.Id,
                    ServerName = SocketGuild.Name
                });

                return newDiscordServerEntry != null;
            }

            return true;
        }

        private async Task<bool> CreateDiscordChannelCategoryDBEntry()
        {
            if (SocketCategoryChannel == null)
                return false;

            var discordChannelCategory = new DiscordChannel()
            {
                DiscordChannelId = SocketCategoryChannel.Id,
                ChannelName = SocketCategoryChannel.Name,
                DiscordServerId = SocketGuild.Id,
                IsCategory = true,
                ParentDiscordChannel = null,
                Position = SocketCategoryChannel.Position
            };

            var dbDiscordChannel = DatabaseManager.GetDiscordChannel(SocketCategoryChannel.Id);
            if (dbDiscordChannel == null)
            {
                var newDiscordChannelEntry = DatabaseManager.CreateDiscordChannel(discordChannelCategory);
                return newDiscordChannelEntry != null;
            }
            else
            {
                DatabaseManager.UpdateDiscordChannel(discordChannelCategory);
            }

            return true;
        }
        private async Task<bool> CreateDiscordChannelDBEntry()
        {
            // NO DM Tracking
            if (SocketGuildChannel == null)
                return false;

            if (SocketGuildChannel is SocketForumChannel socketForumChannel)
            {
                // Forum group
                //1019663716804997192
            }


            // Ensure CategoryChannel is created
            await CreateDiscordChannelCategoryDBEntry();

            var discordChannel = new DiscordChannel()
            {
                DiscordChannelId = SocketGuildChannel.Id,
                ChannelName = SocketGuildChannel.Name,
                DiscordServerId = SocketGuild.Id,
                ParentDiscordChannelId = SocketCategoryChannel?.Id,
                IsCategory = false,
                Position = SocketGuildChannel.Position
            };

            var dbDiscordChannel = DatabaseManager.GetDiscordChannel(SocketGuildChannel.Id);
            if (dbDiscordChannel == null)
            {
                var newDiscordChannelEntry = DatabaseManager.CreateDiscordChannel(discordChannel);
                return newDiscordChannelEntry != null;
            }
            else
            {
                DatabaseManager.UpdateDiscordChannel(discordChannel);
            }

            // Create Thread
            if (SocketThreadChannel != null)
            {
                var discordThread = new DiscordThread()
                {
                    DiscordChannelId = SocketThreadChannel.ParentChannel.Id,
                    ThreadName = SocketThreadChannel.Name,
                    DiscordThreadId = SocketThreadChannel.Id,
                    IsArchived = SocketThreadChannel.IsArchived,
                    IsLocked = SocketThreadChannel.IsLocked,
                    IsPrivateThread = SocketThreadChannel.IsPrivateThread,
                    IsNsfw = SocketThreadChannel.IsNsfw,
                    MemberCount = SocketThreadChannel.MemberCount,
                    ThreadType = (int)SocketThreadChannel.Type
                };

                var dbDiscordThread = DatabaseManager.GetDiscordThread(SocketThreadChannel.Id);
                if (dbDiscordThread == null)
                {
                    var newDiscordThreadEntry = DatabaseManager.CreateDiscordThread(discordThread);
                    return newDiscordThreadEntry != null;
                }
                else
                {
                    DatabaseManager.UpdateDiscordThread(discordThread);
                }
            }

            return true;
        }

        private async Task<bool> CreateOrUpdateDBUser()
        {
            var dbAuthor = DatabaseManager.GetDiscordUserById(SocketGuildUser.Id);

            var discordUser = new DiscordUser()
            {
                DiscordUserId = SocketGuildUser.Id,
                DiscriminatorValue = SocketGuildUser.DiscriminatorValue,
                AvatarUrl = SocketGuildUser.GetAvatarUrl() ?? SocketGuildUser.GetDefaultAvatarUrl(), // If user has no custom avatar load the url for the default avatar
                IsBot = SocketGuildUser.IsBot,
                IsWebhook = SocketGuildUser.IsWebhook,
                Nickname = SocketGuildUser.Nickname,
                Username = SocketGuildUser.Username,
                JoinedAt = SocketGuildUser.JoinedAt,
                FirstDailyPostCount = dbAuthor?.FirstDailyPostCount ?? 0
            };

            if (dbAuthor == null)
            {
                // todo check non socket user
                dbAuthor = DatabaseManager.CreateDiscordUser(discordUser);
                //dbAuthor = DatabaseManager.GetDiscordUserById(SocketGuildUser.Id);
            }
            else
            {
                DatabaseManager.UpdateDiscordUser(discordUser);
            }

            return true;
        }

        private async Task<bool> CreateDiscordMessageDBEntry()
        {
            if (ChannelSettings != null && ((BotPermissionType)ChannelSettings?.ChannelPermissionFlags).HasFlag(BotPermissionType.Read))
            {
                ulong? referenceMessageId = null;
                if (SocketMessage.Reference != null)
                {
                    referenceMessageId = (ulong?)SocketMessage.Reference.MessageId;

                    if (DatabaseManager.GetDiscordMessageById(referenceMessageId) == null)
                        referenceMessageId = null; // original message is not in the db therefore do not link
                }

                var newDiscordMessageEntryCreated = DatabaseManager.CreateDiscordMessage(new DiscordMessage()
                {
                    DiscordChannelId = SocketGuildChannel.Id,
                    DiscordUserId = SocketGuildUser.Id,
                    DiscordMessageId = SocketMessage.Id,
                    Content = SocketMessage.Content,
                    ReplyMessageId = referenceMessageId,
                    DiscordThreadId = SocketThreadChannel?.Id
                }, true);

                if (SocketMessage.Reference != null && newDiscordMessageEntryCreated)
                {
                    // This message is a reply to some message -> Create PingHistory
                    // TODO if the reply contains a ping then we create a double entry
                    DatabaseManager.CreatePingHistory(new PingHistory()
                    {
                        DiscordMessageId = SocketMessage.Id,
                        DiscordRoleId = null,
                        DiscordUserId = null,
                        FromDiscordUserId = SocketGuildUser.Id,
                        IsReply = true
                    });
                }

                return newDiscordMessageEntryCreated;
            }
            else
            {
                // NO DM Tracking
                return false;
            }
        }

        private async void Autoreact()
        {
            try
            {
                // For now disable for threads
                if (SocketThreadChannel != null)
                    return;

                // Bot has permission to react in this channel
                if (ChannelSettings != null && ((BotPermissionType)ChannelSettings?.ChannelPermissionFlags).HasFlag(BotPermissionType.React))
                {
                    // this that reaction
                    List<ulong> upvoteChannels = new List<ulong>()
                {
                    DiscordChannels["pullrequest"],
                    DiscordChannels["serversuggestions"],
                    DiscordChannels["memes"],
                    DiscordChannels["ethmemes"],
                    DiscordChannels["uzhmemes"]
                };

                    if (upvoteChannels.Contains(SocketGuildChannel.Id))
                    {
                        await SocketMessage.AddReactionAsync(Emote.Parse($"<:this:{DiscordEmotes["this"]}>"));
                        await SocketMessage.AddReactionAsync(Emote.Parse($"<:that:{DiscordEmotes["that"]}>"));
                    }

                    // this that reaction
                    List<ulong> pikaChannels = new List<ulong>()
                {
                    DiscordChannels["pullrequest"]
                };

                    if (pikaChannels.Contains(SocketGuildChannel.Id))
                    {
                        await SocketMessage.AddReactionAsync(Emote.Parse($"<:pikashrugA:{DiscordEmotes["pikashrugA"]}>"));
                    }

                    // awww reaction
                    List<ulong> awwChannels = new List<ulong>()
                {
                    DiscordChannels["serotonin"]
                };

                    if (awwChannels.Contains(SocketGuildChannel.Id) && SocketMessage.Attachments.Count > 0)
                    {
                        await SocketMessage.AddReactionAsync(Emote.Parse($"<:awww:{DiscordEmotes["awww"]}>"));
                    }
                }
            }
            catch (Exception ex)
            {
                // If the message is quickly deleted an exception will be thrown -> Ignore
            }
        }

        private async void EmoteDetection()
        {
            if (SocketGuildChannel != null)
            {
                // this emote cant be send because its occupied by a command
                if (CommandInfos.Any(i => i.ToLower() == SocketMessage.Content.ToLower().Replace(Program.CurrentPrefix, "")))
                    return;

                if (SocketMessage.Content.StartsWith(Program.CurrentPrefix))
                {
                    // check if the emoji exists and if the emojis is animated
                    string name = SocketMessage.Content.Substring(Program.CurrentPrefix.Length, SocketMessage.Content.Length - Program.CurrentPrefix.Length);
                    int index = -1;

                    if (name.Contains('-'))
                    {
                        int.TryParse(name.Substring(name.IndexOf('-') + 1, name.Length - name.IndexOf('-') - 1), out index);
                        name = name.Substring(0, name.IndexOf('-')); // take only the emote name
                    }

                    var favEmote = DatabaseManager.EmoteDatabaseManager.GetFavouriteEmote(SocketGuildUser.Id, name);
                    DiscordEmote emote = null;


                    if (favEmote != null)
                    {
                        // Load the emote info
                        emote = DatabaseManager.EmoteDatabaseManager.GetDiscordEmoteById(favEmote.DiscordEmoteId);
                    }
                    else
                    {
                        var emotes = DatabaseManager.EmoteDatabaseManager.GetEmotesByDirectName(name);

                        if (index < 1)
                        {
                            emote = emotes?.FirstOrDefault();
                        }
                        else
                        {
                            emote = emotes?.Skip(index - 1)?.FirstOrDefault();

                            // prevent null ref error
                            if (emote == null)
                                return;

                            emote.EmoteName += $"-{index}";
                        }
                    }

                    if (emote != null)
                    {
                        RestWebhook webhook = null;
                        DiscordWebhookClient webhookClient = null;

                        try
                        {
                            var channelWebhooks = await SocketTextChannel.GetWebhooksAsync();
                            webhook = channelWebhooks.SingleOrDefault(i => i.Name == "BattleRush's Helper"); // TODO Do over ApplicationId

                            if (webhook == null)
                            {
                                FileStream file = new FileStream(Path.Combine(Program.ApplicationSetting.BasePath, "Images", "BRH_Logo.png"), FileMode.Open);

                                // Config name
                                await SocketTextChannel.CreateWebhookAsync("BattleRush's Helper", file);

                                channelWebhooks = await SocketTextChannel.GetWebhooksAsync();
                            }

                            webhook = channelWebhooks.SingleOrDefault(i => i.Name == "BattleRush's Helper"); // TODO Do over ApplicationId

                            //if (SocketThreadChannel == null)
                            webhookClient = new DiscordWebhookClient(webhook.Id, webhook.Token);
                            //else
                            //    webhookClient = new DiscordWebhookClient($"https://discord.com/api/webhooks/{webhook.Id}/{webhook.Token}?thread_id={SocketThreadChannel.Id}");

                        }
                        catch (Exception ex)
                        {
                            // likeky no webhook perms -> skip
                        }

                        await SocketMessage.DeleteAsync();

                        // TODO Keep relevant webhook infos in cache


                        string avatarUrl = SocketGuildUser.GetGuildAvatarUrl();
                        if (avatarUrl == null)
                            avatarUrl = SocketGuildUser.GetAvatarUrl() ?? SocketGuildUser.GetDefaultAvatarUrl();

                        if (SocketGuild.Emotes.Any(i => i.Id == emote.DiscordEmoteId))
                        {
                            var emoteString = $"<{(emote.Animated ? "a" : "")}:{SocketGuild.Emotes.First(i => i.Id == emote.DiscordEmoteId).Name}:{emote.DiscordEmoteId}>";

                            // we can post the emote as it will be rendered out
                            //await SocketTextChannel.SendMessageAsync(emoteString);
                            if (webhookClient != null)
                                await webhookClient.SendMessageAsync(emoteString, false, null, SocketGuildUser.Nickname ?? SocketGuildUser.Username, avatarUrl, threadId: SocketThreadChannel?.Id);
                            else
                                await SocketMessage.Channel.SendMessageAsync(emoteString, false, null, null, null, new MessageReference(SocketMessage.ReferencedMessage?.Id));
                        }
                        else
                        {
                            if (!File.Exists(emote.LocalPath))
                            {
                                using (var webClient = new WebClient())
                                {
                                    byte[] bytes = webClient.DownloadData(emote.Url);
                                    string filePath = EmoteDBManager.MoveEmoteToDisk(emote, bytes);
                                }
                            }

                            FileAttachment fileAttachment = new FileAttachment(emote.LocalPath, null, name, false);

                            // TODO store resized images in db for faster reuse
                            // TODO use images from filesystem -> no web call
                            if (emote.Animated)
                            {
                                // TODO gif resize
                                //await SocketTextChannel.SendMessageAsync(emote.Url);

                                //
                                if (webhookClient != null)
                                    //await webhookClient.SendFileAsync(emote.LocalPath, "", false, null, SocketGuildUser.Nickname ?? SocketGuildUser.Username, avatarUrl);
                                    await webhookClient.SendFileAsync(fileAttachment, "", false, null, SocketGuildUser.Nickname ?? SocketGuildUser.Username, avatarUrl, threadId: SocketThreadChannel?.Id);
                                else
                                    await SocketMessage.Channel.SendFileAsync(emote.LocalPath, "", false, null, null, false, null, new MessageReference(SocketMessage.ReferencedMessage?.Id));
                            }
                            else
                            {

                                SKBitmap bmp;
                                using (var ms = new MemoryStream(File.ReadAllBytes(emote.LocalPath)))
                                    bmp = SKBitmap.Decode(ms);

                                var resImage = CommonHelper.ResizeImage(bmp, Math.Min(bmp.Height, 48));
                                var stream = CommonHelper.GetStream(resImage);

                                fileAttachment = new FileAttachment(stream, $"{emote.EmoteName}.png", name, false);

                                if (webhookClient != null)
                                    //await webhookClient.SendFileAsync(stream, $"{emote.EmoteName}.png", "", false, null, SocketGuildUser.Nickname ?? SocketGuildUser.Username, avatarUrl);
                                    await webhookClient.SendFileAsync(fileAttachment, "", false, null, SocketGuildUser.Nickname ?? SocketGuildUser.Username, avatarUrl, threadId: SocketThreadChannel?.Id);
                                else
                                    await SocketMessage.Channel.SendFileAsync(stream, $"{emote.EmoteName}.png", "", false, null, null, false, null, new MessageReference(SocketMessage.ReferencedMessage?.Id));
                            }
                        }

                        // In case the webhook could not be created
                        if (webhookClient == null)
                            await SocketMessage.Channel.SendMessageAsync($"({Program.CurrentPrefix}{emote.EmoteName}) by <@{SocketGuildUser.Id}>");
                    }
                }
            }
        }

        private async void LiveInBestCanton()
        {
            try
            {
                if (SocketMessage.Content.ToLower() == "I live in the best Canton of Switzerland".ToLower() && !SocketGuildUser.IsBot)
                {
                    ulong bestCantonRoleId = 937025006997737485; // TODO const
                    var bestCantonRole = SocketGuild.Roles.FirstOrDefault(i => i.Id == bestCantonRoleId);

                    // check if the user has the role -> if not then assign
                    if (!SocketGuildUser.Roles.Any(i => i.Id == bestCantonRoleId))
                    {
                        // add role to user
                        await SocketGuildUser.AddRoleAsync(bestCantonRole);

                        // send in spam that they are free
                        await SocketTextChannel.SendMessageAsync($"<@{SocketGuildUser.Id}> declared that they indeed live in the best Canton of Switzerland.");
                    }
                }


                if (SocketMessage.Content.ToLower() == "I dont live in the best Canton of Switzerland".ToLower() && !SocketGuildUser.IsBot)
                {
                    // check if the user has the role -> if yes then remove
                    ulong bestCantonRoleId = 937025006997737485; // TODO const
                    var bestCantonRole = SocketGuild.Roles.FirstOrDefault(i => i.Id == bestCantonRoleId);

                    if (SocketGuildUser.Roles.Any(i => i.Id == bestCantonRoleId))
                    {
                        // remove the role from user
                        await SocketGuildUser.RemoveRoleAsync(bestCantonRole);

                        // send in spam that they are free
                        await SocketTextChannel.SendMessageAsync($"<@{SocketGuildUser.Id}> declared they don't live in the best Canton of Switzerland.");
                    }
                }
            }
            catch (Exception ex)
            {

            }
        }

        private async void AdministratorBait()
        {
            if (SocketMessage.Content.ToLower() == "-role Administration".ToLower() && !SocketGuildUser.IsBot)
            {
                string username = SocketGuildUser.Nickname ?? SocketGuildUser.Username;

                if (SocketGuildUser.Roles.Any(i => i.Id == AdministratorRoleId))
                {
                    // user is admin
                    await SocketTextChannel.SendMessageAsync($"Hello {SocketGuildUser.Nickname ?? SocketGuildUser.Username}. Granted you full Administrator permissions role permissions!");
                }
                else
                {
                    try
                    {
                        await SocketMessage.DeleteAsync(); // hide it from others

                        // *bonk* go to muted jail
                        var shameMessage = await SocketTextChannel.SendMessageAsync($"What did you expect to happen <@{SocketGuildUser.Id}>? Message an Admin in full shame and tell them what you just tried." + Environment.NewLine +
                            $"https://tenor.com/view/shame-go-t-game-of-thrones-walk-of-shame-shameful-gif-4949558");

                        var adminChannel = SocketGuild.GetTextChannel(DiscordChannels["staff"]);

                        var caveBobEmoteId = DiscordEmotes["cavebob"];

                        EmbedBuilder builder = new EmbedBuilder();

                        builder.WithTitle($"{username} just got muted because they were curious :P");
                        builder.WithColor(255, 0, 0);
                        builder.WithDescription($"{username} tried to be sneaky and requested the admin role <:cavebob:{caveBobEmoteId}> You might get messaged soon xD");

                        builder.WithAuthor(SocketGuildUser);
                        builder.WithCurrentTimestamp();

                        await adminChannel.SendMessageAsync("", false, builder.Build());

                        DiscordHelper.DeleteMessage(shameMessage, TimeSpan.FromSeconds(30), $"{username} has been a little bit too curious...");
                    }
                    catch (Exception ex)
                    {
                        // can cause an exception if the message gets deleted
                    }
                }
            }
        }
    }
}
