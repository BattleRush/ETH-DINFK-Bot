using Discord;
using Discord.Rest;
using Discord.WebSocket;
using ETHBot.DataLayer.Data;
using ETHBot.DataLayer.Data.Discord;
using ETHBot.DataLayer.Data.Enums;
using ETHDINFKBot.Helpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace ETHDINFKBot.Handlers
{
    public class ReactionHandler
    {
        private readonly Dictionary<string, ulong> DiscordChannels = DiscordHelper.DiscordChannels;
        private readonly Dictionary<string, ulong> DiscordEmotes = DiscordHelper.DiscordEmotes;

        private SocketGuild SocketGuild;
        private IMessage Message;
        private SocketGuildChannel SocketGuildChannel;
        private SocketReaction SocketReaction;
        private SocketGuildUser SocketGuildMessageUser;
        private SocketGuildUser SocketGuildReactionUser;
        private SocketTextChannel SocketTextChannel;

        private DatabaseManager DatabaseManager;
        private BotChannelSetting ChannelSettings;

        private bool AddedReaction;

        public ReactionHandler(IMessage message, SocketReaction socketReaction, BotChannelSetting channelSettings = null, bool addedReaction = true)
        {
            // TODO DM reactions handling

            Message = message;
            if (Message != null)
            {
                SocketReaction = socketReaction;
                SocketGuildMessageUser = message.Author as SocketGuildUser;
                SocketGuildReactionUser = socketReaction.User.Value as SocketGuildUser; // TODO make sure user is never null
                SocketGuildChannel = message.Channel as SocketGuildChannel;
                SocketTextChannel = SocketGuildChannel as SocketTextChannel;
                SocketGuild = SocketGuildChannel.Guild;

                ChannelSettings = channelSettings;
                DatabaseManager = DatabaseManager.Instance();

                AddedReaction = addedReaction;
            }
        }

        public async void Run()
        {
            if (Message == null)
                return; // ignore?

            EnsureDBMessageCreated();

            if (SocketReaction.Emote is Emote ReactionEmote)
            {
                await CreateOrUpdateDBUser();
                ProcessEmote(ReactionEmote);

                if (AddedReaction)
                {
                    SaveReaction(ReactionEmote);
                    UpvoteReactionToPullRequests(ReactionEmote);
                    PeopleWhoRefuseToPutFoodFav(ReactionEmote);
                    //PeopleUpvotingTheirOwnMessages(ReactionEmote);
                }
            }
        }

        private async void PeopleUpvotingTheirOwnMessages(Emote reactionEmote)
        {
            List<ulong> upvoteChannels = new List<ulong>()
            {
                DiscordChannels["memes"],
                DiscordChannels["ethmemes"]
            };
            
            try
            {
                if (upvoteChannels.Contains(SocketGuildChannel.Id) && SocketGuildMessageUser.Id == SocketGuildReactionUser.Id && reactionEmote.Id == DiscordEmotes["this"])
                {
                    // delete the the message
                    await Message.DeleteAsync();
                }
            }
            catch (Exception ex)
            {
                // Eh idk sometimes it errors out because null ref
                // TODO When not lazy find out why
            }
        }


        // DUPE From Message handler (since some like to react before typing xD)
        private async Task<bool> CreateOrUpdateDBUser()
        {
            var dbAuthor = DatabaseManager.GetDiscordUserById(SocketGuildReactionUser.Id);

            var discordUser = new DiscordUser()
            {
                DiscordUserId = SocketGuildReactionUser.Id,
                DiscriminatorValue = SocketGuildReactionUser.DiscriminatorValue,
                AvatarUrl = SocketGuildReactionUser.GetAvatarUrl() ?? SocketGuildReactionUser.GetDefaultAvatarUrl(), // If user has no custom avatar load the url for the default avatar
                IsBot = SocketGuildReactionUser.IsBot,
                IsWebhook = SocketGuildReactionUser.IsWebhook,
                Nickname = SocketGuildReactionUser.Nickname,
                Username = SocketGuildReactionUser.Username,
                JoinedAt = SocketGuildReactionUser.JoinedAt,
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

        public async void ProcessEmote(Emote emote)
        {
            try
            {
                var discordEmote = new DiscordEmote()
                {
                    Animated = emote.Animated,
                    DiscordEmoteId = emote.Id,
                    EmoteName = emote.Name,
                    Url = emote.Url,
                    CreatedAt = emote.CreatedAt,
                    Blocked = false,
                    LastUpdatedAt = DateTime.Now, // todo chech changes
                    LocalPath = null,
                    IsValid = true // TODO maybe set default as true from db?
                };

                await DatabaseManager.EmoteDatabaseManager.ProcessDiscordEmote(discordEmote, Message.Id, AddedReaction ? 1 : -1, true, SocketGuildReactionUser, false);
                //Program.GlobalStats.EmojiInfoUsage.Single(i => i.EmojiId == emote.Id).UsedAsReaction++;
            }
            catch (Exception ex)
            {

            }
        }
        private async void EnsureDBMessageCreated()
        {
            var dbMessage = DatabaseManager.GetDiscordMessageById(Message.Id);
            if (dbMessage == null)
            {
                // TODO call message handler to save this message -> Preload = true
            }
        }


        private async void SaveReaction(Emote reactionEmote)
        {
            if (reactionEmote.Id == DiscordEmotes["savethis"] && !SocketGuildReactionUser.IsBot)
            {
                try
                {
                    await DiscordHelper.SaveMessage(SocketTextChannel, SocketGuildReactionUser, Message, false);
                }
                catch (Exception ex)
                {
                    var ownerUser = Program.Client.GetUser(Program.ApplicationSetting.Owner);
                    if(ownerUser != null)
                        await ownerUser.SendMessageAsync($"{SocketGuildReactionUser.Username} tried to save {Message.Id} " + ex.ToString());

                    //Message.Channel.SendMessageAsync("Failed to save the message. Discord returned: " + ex.Message);
                }
            }
        }

        private async void PeopleWhoRefuseToPutFoodFav(Emote reactionEmote)
        {
            try
            {
                if(SocketGuildMessageUser.Id == Program.Client.CurrentUser.Id && reactionEmote.Id == DiscordEmotes["shut"] && new Random().Next(0, 100) < 1 /* 1% of the cases */)
                {
                    await SocketTextChannel.SendMessageAsync($"<@{SocketGuildReactionUser.Id}>");    
                    await SocketTextChannel.SendMessageAsync("https://tenor.com/view/ninja-rage-ninja-twitch-you-little-shit-the-fuck-you-say-the-fuck-you-said-gif-18318497");          
                }
            }
            catch (Exception ex)
            {
                // Eh idk sometimes it errors out because null ref
                // TODO When not lazy find out why
            }
        }

        private async void UpvoteReactionToPullRequests(Emote reactionEmote)
        {
            try
            {
                //LogManager.AddReaction(reactionEmote, SocketMessage.Id, SocketGuildReactionUser);
                /*
                // one time code
                if(Message.Content == "Process Initialization Check" && Message.Author.Id == Program.Client.CurrentUser.Id)
                {
                    var upvoteCount = Message.Reactions.Where(i => i.Key is Emote emote && emote.Id == DiscordEmotes["this"]).FirstOrDefault();

                    if(upvoteCount.Value.ReactionCount >= 15)
                    {
                        // Unlock channel
                        var user = SocketGuild.GetUser(123841216662994944);;
                        var testChannel = SocketGuild.GetTextChannel(843623001164742696);

                        testChannel.AddPermissionOverwriteAsync(user, OverwritePermissions.InheritAll.Modify(viewChannel: PermValue.Allow, readMessageHistory: PermValue.Allow));

                        Message.Channel.SendMessageAsync("Verification Success! <@123841216662994944> Proceed to the next stage. You are on your own now. Good luck!");
                        Message.DeleteAsync();

                        Message.Channel.SendMessageAsync("Elthision and Marc right now");
                        Message.Channel.SendMessageAsync("https://cdn140.picsart.com/329258174052201.gif?to=min&r=640");
                    }
                }
                */
                if (!SocketGuildReactionUser.IsBot && SocketGuildChannel.Id == DiscordChannels["serversuggestions"]
                    && (reactionEmote.Id == DiscordEmotes["this"] || reactionEmote.Id == DiscordEmotes["that"]))
                {
                    // this emote and that emote
                    // TODO save these ids somewhere global


                    var upvoteCount = Message.Reactions.Where(i => i.Key is Emote emote && emote.Id == DiscordEmotes["this"]).FirstOrDefault();
                    var downvoteCount = Message.Reactions.Where(i => i.Key is Emote emote && emote.Id == DiscordEmotes["that"]).FirstOrDefault();

                    // Reaction count over 15
                    // Only if opvote count is higher than downvotes
                    // Only if message is not older than 14 days
                    if (upvoteCount.Value.ReactionCount > 15 
                        && upvoteCount.Value.ReactionCount > downvoteCount.Value.ReactionCount
                        && Message.CreatedAt > DateTime.UtcNow.AddDays(-14))
                    {
                        // TODO not fixed ids
                        var adminSuggestionChannel = SocketGuild.GetTextChannel(DiscordChannels["pullrequest"]);

                        var oldMessages = await adminSuggestionChannel.GetMessagesAsync(100).FlattenAsync();

                        string title = $"Suggestion: {Message.Id}";

                        if (!oldMessages.Any(i => i.Content.Contains(title)))
                        {
                            EmbedBuilder builder = new EmbedBuilder();

                            builder.WithTitle($"{SocketGuildMessageUser.Username} has a suggestion");
                            builder.WithColor(0, 0, 255);

                            if (SocketGuildMessageUser != null)
                                builder.WithAuthor(SocketGuildMessageUser);

                            //add first attachment as thumbnail (if attachment is not picture, height == null) 
                            if (Message.Attachments != null && Message.Attachments.FirstOrDefault()?.Height != null)
                            {
                                builder.WithThumbnailUrl(Message.Attachments.First().Url);
                                builder.WithImageUrl(Message.Attachments.First().Url);
                            }

                            builder.WithCurrentTimestamp();
                            //if no content, add content
                            builder.AddField("Suggestion", (Message.Content.Length > 0 ? Message.Content : "No content provided."), true);
                            builder.AddField("Up/Downvotes", $"<:this:{DiscordEmotes["this"]}> {upvoteCount.Value.ReactionCount} / <:that:{DiscordEmotes["that"]}> {downvoteCount.Value.ReactionCount}", true);
                            var link = $"https://discord.com/channels/{SocketGuild.Id}/{SocketGuildChannel.Id}/{Message.Id}";

                            builder.AddField("Link", $"[Message]({link})");

                            var attachments = Message.Attachments;

                            HttpClient client = new HttpClient();
                            var attachmentsToSend = new List<FileAttachment>();
                            if (attachments != null)
                            {
                                builder.AddField("Attachments", attachments.Count.ToString(), true);

                                foreach (var attachment in attachments)
                                {
                                    // download attachments
                                    var attachmentStream = await client.GetStreamAsync(attachment.Url);
                                    var attachmentFileName = attachment.Filename;

                                    attachmentsToSend.Add(new FileAttachment(attachmentStream, attachmentFileName));
                                }
                            }

                            // download attachments

                            // attach attachments to message

                            // send message

                            RestUserMessage message;

                            if (attachmentsToSend.Count == 0)
                                message = await adminSuggestionChannel.SendMessageAsync(title, false, builder.Build());
                            else
                                message = await adminSuggestionChannel.SendFilesAsync(attachmentsToSend, title, false, builder.Build());

                            // create thread for this message
                            await adminSuggestionChannel.CreateThreadAsync("Discussion for suggestion: " + Message.Id, message: message);
                        }
                    }

                    //argMessageChannel.SendMessageAsync($"This post has {upvoteCount.Value.ReactionCount} upvotes and {downvoteCount.Value.ReactionCount} downvotes", false);
                }


            }
            catch (Exception ex)
            {

            }

            return;
        }
    }
}
