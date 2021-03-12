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

        public async void Run()
        {
            EnsureDBMessageCreated();

            if (SocketReaction.Emote is Emote ReactionEmote)
            {
                ProcessEmote(ReactionEmote);

                SaveReaction(ReactionEmote);
                UpvoteReactionToPullRequests(ReactionEmote);
            }
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
                    LocalPath = null
                };
                
                DatabaseManager.ProcessDiscordEmote(discordEmote, Message.Id, AddedReaction ? 1: -1, true, SocketGuildReactionUser, false);
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
                // Save the post link

                /*          var user = DatabaseManager.GetDiscordUserById(arg1.Value.Author.Id); // Verify the user is created but should actually be available by this poitn
                var saveBy = DatabaseManager.GetDiscordUserById(arg3.User.Value.Id); // Verify the user is created but should actually be available by this poitn
                */

                if (DatabaseManager.IsSaveMessage(Message.Id, SocketGuildReactionUser.Id))
                {
                    // dont allow double saves
                    return;
                }


                string authorUsername = SocketGuildMessageUser.Nickname ?? SocketGuildMessageUser.Username;

                var link = $"https://discord.com/channels/{SocketGuild.Id}/{SocketGuildChannel.Id}/{Message.Id}";
                if (!string.IsNullOrWhiteSpace(Message.Content))
                {
                    DatabaseManager.SaveMessage(Message.Id, SocketGuildMessageUser.Id, SocketGuildReactionUser.Id, link, Message.Content);

                    // Send a DM to the user
                    await SocketGuildReactionUser.SendMessageAsync($"Saved post from {SocketGuildMessageUser.Username}:{Environment.NewLine}" +
                        $"{Message.Content} {Environment.NewLine}" +
                        $"Direct link: [{SocketGuild.Name}/{SocketGuildChannel.Name}/by {authorUsername}] <{link}>");
                }

                foreach (var item in Message.Embeds)
                {
                    DatabaseManager.SaveMessage(Message.Id, SocketGuildMessageUser.Id, SocketGuildReactionUser.Id, link, "Embed: " + item.ToString());
                    await SocketGuildReactionUser.SendMessageAsync("", false, (Embed)item);
                }



                foreach (var item in Message.Attachments)
                {
                    DatabaseManager.SaveMessage(Message.Id, SocketGuildMessageUser.Id, SocketGuildReactionUser.Id, link, item.Url);

                    await SocketGuildReactionUser.SendMessageAsync($"Saved post from {SocketGuildMessageUser.Username}:{Environment.NewLine}" +
                        $"{item.Url} {Environment.NewLine}" +
                        $"Direct link: [{SocketGuild.Name}/{SocketGuildChannel.Name}/by {authorUsername}] <{link}>");
                }


                if (((BotPermissionType)ChannelSettings?.ChannelPermissionFlags).HasFlag(BotPermissionType.SaveMessage))
                {
                    EmbedBuilder builder = new EmbedBuilder();

                    builder.WithTitle($"A message was saved");
                    builder.WithColor(0, 128, 255);

                    builder.AddField("Message Link", $"[Message]({link})", true);
                    builder.AddField("Message Author", $"{authorUsername}", true);

                    builder.AddField("Info", $"To save a message react with <:savethis:780179874656419880> to a message", false);

                    builder.WithAuthor(SocketGuildReactionUser);
                    builder.WithCurrentTimestamp();


                    var saveMessage = await SocketTextChannel.SendMessageAsync("", false, builder.Build());

                    DiscordHelper.DeleteMessage(saveMessage, TimeSpan.FromSeconds(45));
                }
            }
        }

        private async void UpvoteReactionToPullRequests(Emote reactionEmote)
        {
            try
            {
                //LogManager.AddReaction(reactionEmote, SocketMessage.Id, SocketGuildReactionUser);

                if (!SocketGuildReactionUser.IsBot && SocketGuildChannel.Id == DiscordChannels["serversuggestions"]
                    && (reactionEmote.Id == DiscordEmotes["this"] || reactionEmote.Id == DiscordEmotes["that"]))
                {
                    // this emote and that emote
                    // TODO save these ids somewhere global


                    var upvoteCount = Message.Reactions.Where(i => i.Key is Emote emote && emote.Id == DiscordEmotes["this"]).FirstOrDefault();
                    var downvoteCount = Message.Reactions.Where(i => i.Key is Emote emote && emote.Id == DiscordEmotes["that"]).FirstOrDefault();


                    if (upvoteCount.Value.ReactionCount > 10)
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

                            builder.WithCurrentTimestamp();
                            builder.AddField("Suggestion", Message.Content);

                            var link = $"https://discord.com/channels/{SocketGuild.Id}/{SocketGuildChannel.Id}/{Message.Id}";

                            builder.AddField("Link", $"[Message]({link})");


                            await adminSuggestionChannel.SendMessageAsync(title, false, builder.Build());
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
