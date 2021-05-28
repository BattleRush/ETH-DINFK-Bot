using Discord;
using Discord.Commands;
using Discord.WebSocket;
using ETHBot.DataLayer.Data;
using ETHBot.DataLayer.Data.Discord;
using ETHBot.DataLayer.Data.Enums;
using ETHDINFKBot.Helpers;
using System;
using System.Collections.Generic;
using System.Drawing;
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
        private SocketTextChannel SocketTextChannel;
        private SocketGuildChannel SocketGuildChannel;
        private SocketGuild SocketGuild;

        private DatabaseManager DatabaseManager;
        private BotChannelSetting ChannelSettings;
        private List<CommandInfo> CommandInfos;

        public MessageHandler(SocketUserMessage socketMessage, List<CommandInfo> commandList, BotChannelSetting channelSettings = null)
        {
            SocketMessage = socketMessage;

            // verify what to do when these 2 cant be cast
            SocketGuildUser = socketMessage.Author as SocketGuildUser;
            SocketTextChannel = socketMessage.Channel as SocketTextChannel;
            SocketGuildChannel = socketMessage.Channel as SocketGuildChannel;
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
            if (SocketMessage.Author.IsWebhook || SocketGuildChannel == null)
                return false; // slash commands are webhooks ???

            AdministratorBait();
            EmojiDetection();
            Autoreact();

            // Log to DB
            await CreateDiscordServerDBEntry();
            await CreateDiscordChannelDBEntry();
            await CreateOrUpdateDBUser();
            await CreateDiscordMessageDBEntry();

            return true; // kinda useless
        }


        private async Task<bool> CreateDiscordServerDBEntry()
        {
            if (SocketGuildChannel != null)
            {
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
            else
            {
                // NO DM Tracking
                return false;
            }
        }


        private async Task<bool> CreateDiscordChannelDBEntry()
        {
            // TODO UPDATE CHANNEL IF NEEDED
            if (SocketGuildChannel != null)
            {
                var discordChannel = new DiscordChannel()
                {
                    DiscordChannelId = SocketGuildChannel.Id,
                    ChannelName = SocketGuildChannel.Name,
                    DiscordServerId = SocketGuild.Id
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
                return true;
            }
            else
            {
                // NO DM Tracking
                return false;
            }

        }

        private async Task<bool> CreateOrUpdateDBUser()
        {
            var dbAuthor = DatabaseManager.GetDiscordUserById(SocketGuildUser.Id);

            var discordUser = new DiscordUser()
            {
                DiscordUserId = SocketGuildUser.Id,
                DiscriminatorValue = SocketGuildUser.DiscriminatorValue,
                AvatarUrl = SocketGuildUser.GetAvatarUrl(),
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

                var newDiscordMessageEntry = DatabaseManager.CreateDiscordMessage(new DiscordMessage()
                {
                    DiscordChannelId = SocketGuildChannel.Id,
                    DiscordUserId = SocketGuildUser.Id,
                    DiscordMessageId = SocketMessage.Id,
                    Content = SocketMessage.Content,
                    ReplyMessageId = referenceMessageId
                }, true);

                return newDiscordMessageEntry;
            }
            else
            {
                // NO DM Tracking
                return false;
            }
        }

        private async void Autoreact()
        {
            // Bot has permission to react in this channel
            if (ChannelSettings != null && ((BotPermissionType)ChannelSettings?.ChannelPermissionFlags).HasFlag(BotPermissionType.React))
            {
                // this that reaction
                List<ulong> upvoteChannels = new List<ulong>()
                {
                    DiscordChannels["pullrequest"],
                    DiscordChannels["serversuggestions"],
                    DiscordChannels["memes"],
                    DiscordChannels["ethmemes"]
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

        private async void EmojiDetection()
        {
            if (SocketGuildChannel != null)
            {
                // this emote cant be send because its occupied by a command
                if (CommandInfos.Any(i => i.Name.ToLower() == SocketMessage.Content.ToLower().Replace(".", "")))
                    return;

                if (SocketMessage.Content.StartsWith("."))
                {
                    // check if the emoji exists and if the emojis is animated
                    string name = SocketMessage.Content.Substring(1, SocketMessage.Content.Length - 1);
                    int index = -1;

                    if (name.Contains('-'))
                    {
                        string indexString = name.Substring(name.IndexOf('-') + 1, name.Length - name.IndexOf('-') - 1);

                        int.TryParse(indexString, out index);

                        name = name.Substring(0, name.IndexOf('-')); // take only the emote name
                    }

                    var emotes = DatabaseManager.GetEmotesByDirectName(name);
                    DiscordEmote emote = null;

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

                    if (emote != null)
                    {
                        SocketMessage.DeleteAsync();

                        if (SocketGuild.Emotes.Any(i => i.Id == emote.DiscordEmoteId))
                        {
                            var emoteString = $"<{(emote.Animated ? "a" : "")}:{SocketGuild.Emotes.First(i => i.Id == emote.DiscordEmoteId).Name}:{emote.DiscordEmoteId}>";

                            // we can post the emote as it will be rendered out
                            await SocketTextChannel.SendMessageAsync(emoteString);
                        }
                        else
                        {
                            // TODO store resized images in db for faster reuse
                            // TODO use images from filesystem -> no web call
                            if (emote.Animated)
                            {
                                // TODO gif resize
                                //await SocketTextChannel.SendMessageAsync(emote.Url);
                                try
                                {
                                    using (var stream = new MemoryStream(File.ReadAllBytes(emote.LocalPath)))
                                      await SocketMessage.Channel.SendFileAsync(stream, $"{emote.EmoteName}.gif", "", false, null, null, false, null, new Discord.MessageReference(SocketMessage.ReferencedMessage?.Id));
                                }
                                catch (Exception ex) { }
                            }
                            else
                            {
                                try
                                {
                                    Bitmap bmp;
                                    using (var ms = new MemoryStream(File.ReadAllBytes(emote.LocalPath)))
                                    {
                                        bmp = new Bitmap(ms);
                                    }
                                    var resImage = CommonHelper.ResizeImage(bmp, Math.Min(bmp.Height, 64));
                                    var stream = CommonHelper.GetStream(resImage);

                                    await SocketMessage.Channel.SendFileAsync(stream, $"{emote.EmoteName}.png", "", false, null, null, false, null, new Discord.MessageReference(SocketMessage.ReferencedMessage?.Id));
                                }
                                catch (Exception ex) 
                                { 
                                }
                            }
                        }

                        await SocketTextChannel.SendMessageAsync($"(.{emote.EmoteName}) by <@{SocketGuildUser.Id}>");
                    }
                }
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
