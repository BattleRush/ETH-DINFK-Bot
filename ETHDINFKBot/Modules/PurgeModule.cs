using Discord;
using Discord.Commands;
using Discord.Net;
using Discord.WebSocket;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ETHDINFKBot.Modules
{
    [Group("purge")]
    public class PurgeModule : ModuleBase<SocketCommandContext>
    {
        private static readonly HashSet<ulong> Whitelist = new HashSet<ulong>();

        // Users (besides the bot owner) allowed to use every purge command
        private static readonly HashSet<ulong> AdminUsers = new HashSet<ulong>
        {
            238388792132763648
        };

        // Set by the skip button to stop purging the channel currently being processed
        private volatile bool _skipRequested;

        // 2-hour safety buffer before the actual 14-day bulk-delete cutoff
        private static DateTimeOffset BulkDeleteCutoff =>
            DateTimeOffset.UtcNow.AddDays(-14).AddHours(2);

        private bool IsAdmin(ulong userId) =>
            userId == Program.ApplicationSetting.Owner || AdminUsers.Contains(userId);

        private bool IsWhitelisted(ulong userId) =>
            IsAdmin(userId) || Whitelist.Contains(userId);

        [Command("adduser")]
        public async Task AddUser(ulong userId)
        {
            if (!IsAdmin(Context.Message.Author.Id))
            {
                await Context.Channel.SendMessageAsync("Only the owner can manage the purge whitelist.");
                return;
            }
            Whitelist.Add(userId);
            await Context.Channel.SendMessageAsync($"Added <@{userId}> to the purge whitelist. ({Whitelist.Count} user(s) total)");
        }

        [Command("removeuser")]
        public async Task RemoveUser(ulong userId)
        {
            if (!IsAdmin(Context.Message.Author.Id))
            {
                await Context.Channel.SendMessageAsync("Only the owner can manage the purge whitelist.");
                return;
            }
            bool removed = Whitelist.Remove(userId);
            if (removed)
                await Context.Channel.SendMessageAsync($"Removed <@{userId}> from the purge whitelist.");
            else
                await Context.Channel.SendMessageAsync($"<@{userId}> is not on the purge whitelist.");
        }

        [Command("list")]
        public async Task ListWhitelist()
        {
            if (!IsAdmin(Context.Message.Author.Id))
            {
                await Context.Channel.SendMessageAsync("Only the owner can view the purge whitelist.");
                return;
            }
            if (Whitelist.Count == 0)
            {
                await Context.Channel.SendMessageAsync("The purge whitelist is empty. (The bot owner is always allowed.)");
                return;
            }
            string users = string.Join("\n", Whitelist.Select(id => $"<@{id}> (`{id}`)"));
            await Context.Channel.SendMessageAsync($"**Purge whitelist** ({Whitelist.Count} user(s)):\n{users}");
        }

        [Command("channel", RunMode = RunMode.Async)]
        public async Task PurgeChannel()
        {
            var userId = Context.Message.Author.Id;
            if (!IsWhitelisted(userId))
            {
                await Context.Channel.SendMessageAsync("You are not whitelisted to use the purge command.");
                return;
            }

            var channel = Context.Channel as SocketTextChannel;
            if (channel == null)
            {
                await Context.Channel.SendMessageAsync("This command can only be used in a server text channel.");
                return;
            }

            var statusMsg = await Context.Channel.SendMessageAsync($"Purging your messages in #{channel.Name}...");
            int deleted = await PurgeMessagesInChannel(channel, userId, statusMsg);
            await statusMsg.ModifyAsync(m => m.Content = $"Done! Deleted **{deleted}** message(s) from #{channel.Name}.");
        }

        [Command("all", RunMode = RunMode.Async)]
        public async Task PurgeAllChannels()
        {
            var userId = Context.Message.Author.Id;
            if (!IsWhitelisted(userId))
            {
                await Context.Channel.SendMessageAsync("You are not whitelisted to use the purge command.");
                return;
            }

            var guild = Context.Guild;
            var textChannels = guild.TextChannels.OrderBy(c => c.Position).ToList();

            // Unique button id per purge run so clicks only affect this invocation
            string skipButtonId = $"purge-skip-{Context.Message.Id}";
            var skipButton = new ComponentBuilder()
                .WithButton("Skip this channel", skipButtonId, ButtonStyle.Secondary)
                .Build();
            var noButton = new ComponentBuilder().Build();

            var statusMsg = await Context.Channel.SendMessageAsync("Starting full server purge across all text channels. This may take a while...");

            int totalDeleted = 0;
            int channelsProcessed = 0;
            int channelsSkipped = 0;

            // Listen for the skip button for the duration of this run
            Func<SocketMessageComponent, Task> onButton = async component =>
            {
                if (component.Data.CustomId != skipButtonId)
                    return;
                _skipRequested = true;
                await component.DeferAsync();
            };
            Context.Client.ButtonExecuted += onButton;

            try
            {
                for (int i = 0; i < textChannels.Count; i++)
                {
                    var channel = textChannels[i];
                    _skipRequested = false;

                    // channel.Name comes from the gateway cache, so the real name is shown
                    // even if the invoking user can't access the channel (a <#id> mention wouldn't render)
                    string channelLabel = $"#{channel.Name} (`{channel.Id}`)";

                    var botPerms = guild.CurrentUser.GetPermissions(channel);
                    if (!botPerms.ReadMessageHistory || !botPerms.ManageMessages)
                    {
                        channelsSkipped++;
                        await statusMsg.ModifyAsync(m =>
                        {
                            m.Content = $"Skipping {channelLabel} — no access | channel **{i + 1}/{textChannels.Count}**\n**{totalDeleted}** message(s) deleted so far";
                            m.Components = noButton;
                        });
                        continue;
                    }

                    await statusMsg.ModifyAsync(m =>
                    {
                        m.Content = $"Now purging {channelLabel} — channel **{i + 1}/{textChannels.Count}**\n**{totalDeleted}** message(s) deleted so far";
                        m.Components = skipButton;
                    });

                    int deleted = await PurgeMessagesInChannel(channel, userId);
                    totalDeleted += deleted;

                    if (_skipRequested)
                        channelsSkipped++;
                    else
                        channelsProcessed++;
                }
            }
            finally
            {
                Context.Client.ButtonExecuted -= onButton;
            }

            string skippedNote = channelsSkipped > 0 ? $" Skipped **{channelsSkipped}** channel(s)." : "";
            await statusMsg.ModifyAsync(m =>
            {
                m.Content = $"Purge complete! Deleted **{totalDeleted}** message(s) across **{channelsProcessed}** channel(s).{skippedNote}";
                m.Components = noButton;
            });
        }

        private async Task<int> PurgeMessagesInChannel(SocketTextChannel channel, ulong targetUserId, IUserMessage statusMessage = null)
        {
            int totalDeleted = 0;
            ulong? beforeId = null;

            while (true)
            {
                if (_skipRequested)
                    break;

                IEnumerable<IMessage> batch;
                try
                {
                    batch = beforeId == null
                        ? await channel.GetMessagesAsync(100).FlattenAsync()
                        : await channel.GetMessagesAsync(beforeId.Value, Direction.Before, 100).FlattenAsync();
                }
                catch (HttpException)
                {
                    break;
                }

                var messages = batch.ToList();
                if (messages.Count == 0)
                    break;

                beforeId = messages.Min(m => m.Id);

                var userMessages = messages.Where(m => m.Author.Id == targetUserId).ToList();
                if (userMessages.Count == 0)
                    continue;

                var cutoff = BulkDeleteCutoff;
                var recentMessages = userMessages.Where(m => m.CreatedAt >= cutoff).ToList();
                var oldMessages = userMessages.Where(m => m.CreatedAt < cutoff).ToList();

                // Bulk delete messages within 14 days (Discord requires 2+ messages for bulk delete)
                if (recentMessages.Count >= 2)
                {
                    try
                    {
                        await channel.DeleteMessagesAsync(recentMessages);
                        totalDeleted += recentMessages.Count;
                        await Task.Delay(500);
                    }
                    catch (HttpException)
                    {
                        // Fall back to individual deletes if bulk fails
                        foreach (var msg in recentMessages)
                        {
                            await TryDeleteMessage(msg);
                            totalDeleted++;
                            await Task.Delay(300);
                        }
                    }
                }
                else if (recentMessages.Count == 1)
                {
                    await TryDeleteMessage(recentMessages[0]);
                    totalDeleted++;
                    await Task.Delay(300);
                }

                // Delete messages older than 14 days one by one — bulk delete not allowed by Discord API
                foreach (var msg in oldMessages)
                {
                    if (_skipRequested)
                        break;
                    if (await TryDeleteMessage(msg))
                        totalDeleted++;
                    await Task.Delay(300); // ~3 deletes/sec to respect rate limits
                }

                if (statusMessage != null && totalDeleted > 0 && totalDeleted % 25 == 0)
                    await statusMessage.ModifyAsync(m => m.Content = $"Purging... ({totalDeleted} messages deleted so far)");
            }

            return totalDeleted;
        }

        private async Task<bool> TryDeleteMessage(IMessage message)
        {
            try
            {
                await message.DeleteAsync();
                return true;
            }
            catch (HttpException)
            {
                return false;
            }
        }
    }
}
