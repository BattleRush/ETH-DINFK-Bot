using Discord;
using Discord.WebSocket;
using ETHBot.DataLayer.Data.Discord;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ETHDINFKBot.Helpers
{
    public static class DiscordHelper
    {
        // TODO add properties for the channels like server
        public static readonly Dictionary<string, ulong> DiscordChannels = new Dictionary<string, ulong>()
        {
            { "staff", 747754931905364000 },
            { "pullrequest", 816279194321420308 },
            { "serversuggestions", 816776685407043614 },
            { "memes", 747758757395562557 },
            { "ethmemes", 758293511514226718 },
            { "serotonin", 814440115392348171 },
            { "spam", 768600365602963496 }
        };

        public static readonly Dictionary<string, ulong> DiscordEmotes = new Dictionary<string, ulong>()
        {
            { "cavebob", 747783377146347590 },
            { "this", 747783377662378004 },
            { "that", 758262252699779073 },
            { "okay", 817420081775640616 },
            { "pikashrugA", 782676527648079894 },
            { "awww", 810266232061952040 },
            { "savethis", 780179874656419880 }
        };


        public static ulong GetRoleIdFromMention(SocketRole role)
        {
            ulong roleId = role.IsEveryone ? 1 : Convert.ToUInt64(role.Mention.Substring(3, role.Mention.Length - 4)); // exception handlting but should be fine i guess
            return roleId; 
        }

        public static List<PingHistory> GetTotalPingHistory(SocketGuildUser user, int limit = 30)
        {
            var dbManager = DatabaseManager.Instance();
            List<PingHistory> pingHistory = new();

            pingHistory.AddRange(dbManager.GetLastPingHistory(50, user.Id, null));

            foreach (var userRole in user.Roles)
            {
                ulong roleId = GetRoleIdFromMention(userRole);
                pingHistory.AddRange(dbManager.GetLastPingHistory(50, null, roleId));
            }

            // Add reply message pings
            pingHistory.AddRange(dbManager.GetLastReplyHistory(50, user.Id));

            pingHistory = pingHistory.OrderByDescending(i => i.DiscordMessageId).ToList(); // TODO Change to reply id
            pingHistory = pingHistory.Take(limit).ToList();

            return pingHistory;
        }

        public static EmbedBuilder GetEmbedForPingHistory(List<PingHistory> pingHistory, SocketGuildUser user)
        {
            var dbManager = DatabaseManager.Instance();

            string messageText = "";
            string currentBuilder = "";
            int count = 1;

            EmbedBuilder builder = new EmbedBuilder();
            builder.WithTitle($"{user.Nickname ?? user.Username} last 10 pings");

            foreach (var item in pingHistory)
            {
                //if (item.DiscordMessageId == null)
                //    continue;

                var dbMessage = dbManager.GetDiscordMessageById(item.DiscordMessageId);
                var dbChannel = dbManager.GetDiscordChannel(dbMessage?.DiscordChannelId);

                var dateTime = SnowflakeUtils.FromSnowflake(item.DiscordMessageId ?? 0); // TODO maybe track time in the ping history

                var dateTimeCET = dateTime.Add(Program.TimeZoneInfo.GetUtcOffset(DateTime.Now)); // CEST CONVERSION

                string link = null;

                if (dbChannel != null)
                    link = $"https://discord.com/channels/{dbChannel.DiscordServerId}/{dbMessage.DiscordChannelId}/{dbMessage.DiscordMessageId}";

                var channel = "unknown";
                if (dbMessage?.DiscordChannelId != null)
                    channel = $"<#{dbMessage?.DiscordChannelId}>";

                string line = "";

                // RoleIds smaller than 100 cant exist due to the Id size, so they are reserved for internal code
                if (item.DiscordRoleId.HasValue && item.DiscordRoleId.Value >= 100)
                    line += $"<@{item.FromDiscordUserId}> {(link == null ? "pinged" : $"[pinged]({link})")} <@&{item.DiscordRoleId}> at {dateTimeCET.ToString("dd.MM HH:mm")} in {channel} {Environment.NewLine}"; // todo check for everyone or here
                else if (item.DiscordRoleId.HasValue && item.DiscordRoleId.Value < 100)
                    line += $"<@{item.FromDiscordUserId}> {(link == null ? "replied" : $"[replied]({link})")} at {dateTimeCET.ToString("dd.MM HH:mm")} in {channel} {Environment.NewLine}"; // todo check for everyone or here
                else
                    line += $"<@{item.FromDiscordUserId}> {(link == null ? "pinged" : $"[pinged]({link})")} at {dateTimeCET.ToString("dd.MM HH:mm")} in {channel} {Environment.NewLine}";

                if (count <= 10)
                {
                    messageText += line;
                }
                else
                {
                    currentBuilder += line;

                    if (count % 5 == 0)
                    {
                        builder.AddField($"{(user.Id == 0 ? user.Username : "Your")} last {count} pings", currentBuilder, false);
                        currentBuilder = "";
                    }
                }

                count++;
            }

            messageText += Environment.NewLine;

            builder.WithDescription(messageText);
            builder.WithColor(128, 64, 128);

            builder.WithAuthor(user);
            builder.WithCurrentTimestamp();

            return builder;
        }

        public static async void ReloadRoles(SocketGuild guild)
        {
            try
            {
                var dbManager = DatabaseManager.Instance();
                var roles = guild.Roles;

                foreach (var role in roles)
                {
                    ulong roleId = GetRoleIdFromMention(role);
                    var dbRole = dbManager.GetDiscordRole(roleId);

                    if (dbRole == null)
                    {
                        // role doesnt exist

                        DiscordRole newRole = new DiscordRole()
                        {
                            DiscordRoleId = roleId,
                            DiscordServerId = roleId == 1 ? null : role.Guild.Id,
                            ColorHex = CommonHelper.HexConverter(role.Color),
                            IsHoisted = role.IsHoisted,
                            CreatedAt = role.CreatedAt,
                            IsManaged = role.IsManaged,
                            IsMentionable = role.IsMentionable,
                            Name = role.Name,
                            Position = role.Position
                        };

                        dbManager.CreateRole(newRole);
                    }
                    else
                    {
                        // TODO CHECK TO UPDATE ROLE
                    }
                }
            }
            catch(Exception ex)
            {

            }
        }

        public static async void DeleteMessage(IMessage message, TimeSpan timespan, string auditLogReason = null)
        {
            try
            {
                await Task.Delay(timespan);
                await message.DeleteAsync(new RequestOptions() { AuditLogReason = auditLogReason });
            }
            catch(Exception ex)
            {
                // do nothing -> usually a 404 error as the message is already removed
            }
        }



        public static async void DiscordUserBirthday(DiscordSocketClient client, ulong guildId, ulong channelId, bool reactions)
        {
            // TODO reschedule maybe for another time or add manual trigger

            // select all users from the DB
            // determine who has birthday today and send a message for each user
            var allUsers = DatabaseManager.Instance().GetDiscordUsers();
            var now = DateTime.UtcNow.AddHours(Program.TimeZoneInfo.IsDaylightSavingTime(DateTime.UtcNow) ? 2 : 1);

            List<DiscordUser> birthdayUsers = new List<DiscordUser>();

            foreach (var user in allUsers)
            {
                var userCreatedAt = SnowflakeUtils.FromSnowflake(user.DiscordUserId).AddHours(Program.TimeZoneInfo.IsDaylightSavingTime(DateTime.UtcNow) ? 2 : 1);

                if (userCreatedAt.Month == now.Month && userCreatedAt.Day == now.Day)
                {
                    // birthday kid
                    birthdayUsers.Add(user);
                }

                // Feb 29 kids (only in non leap years)
                if (now.Day == 28 && now.Month == 2
                    && userCreatedAt.Day == 29 && userCreatedAt.Month == 2
                    && !DateTime.IsLeapYear(now.Year))
                {
                    birthdayUsers.Add(user);
                }
            }

            var spamChannel = client.GetGuild(guildId).GetTextChannel(channelId); // #spam

            if(birthdayUsers.Count == 0)
                await spamChannel.SendMessageAsync("No birthdays today <:sadge:851469686578741298> maybe tomorrow...");

            foreach (var birthdayUser in birthdayUsers)
            {
                var userCreatedAt = SnowflakeUtils.FromSnowflake(birthdayUser.DiscordUserId).AddHours(Program.TimeZoneInfo.IsDaylightSavingTime(DateTime.UtcNow) ? 2 : 1);

                // - 1 because it starts the "next" year already
                int age = new DateTime((now.Date - userCreatedAt.Date).Ticks).Year - 1;

                // Include Feb 29 kids on non leap years
                bool isFeb29Kid = userCreatedAt.Date.Day == 29 && userCreatedAt.Date.Month == 2
                    && !DateTime.IsLeapYear(now.Year);

                EmbedBuilder builder = new EmbedBuilder();

                builder.WithTitle($"{birthdayUser.Nickname ?? birthdayUser.Username} is celebrating their {CommonHelper.DisplayWithSuffix(age)} Discord birthday today.");
                builder.WithColor(128, 64, 255); // TODO color for Feb 29?
                builder.WithDescription($"Happy Discord Birthday <:happe:816101506708799528> {(isFeb29Kid ? " (also for you Feb 29 xD)" : "")}");

                builder.AddField("Created at", userCreatedAt.ToString("F")); // TODO Check timezone stuff

                var byUser = Program.Client.GetUser(birthdayUser.DiscordUserId);

                if (byUser is null)
                    continue;

                builder.WithImageUrl(birthdayUser.AvatarUrl);
                builder.WithAuthor(byUser);
                builder.WithTimestamp(SnowflakeUtils.FromSnowflake(birthdayUser.DiscordUserId)); // has to be in UTC

                //builder.WithCurrentTimestamp();

                var message = await spamChannel.SendMessageAsync("", false, builder.Build());

                if (reactions)
                {
                    // TODO Emote library
                    await message.AddReactionAsync(Emote.Parse($"<:yay:851469734545588234>"));
                    await message.AddReactionAsync(Emote.Parse($"<:yay:872093645212368967>"));
                    await message.AddReactionAsync(Emote.Parse($"<a:pepeD:818886775199629332>"));
                }
            }
        }
    }
}
