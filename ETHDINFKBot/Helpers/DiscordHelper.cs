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
        // TODO add properties for the channels
        public static readonly Dictionary<string, ulong> DiscordChannels = new Dictionary<string, ulong>()
        {
            { "staff", 747754931905364000 },
            { "pullrequest", 816279194321420308 },
            { "serversuggestions", 816776685407043614 },
            { "memes", 747758757395562557 },
            { "ethmemes", 758293511514226718 },
            { "serotonin", 814440115392348171 }
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

            var general = client.GetGuild(guildId).GetTextChannel(channelId); // #spam

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

                builder.AddField("Created at", userCreatedAt.ToString()); // TODO Check timezone stuff

                var byUser = Program.Client.GetUser(birthdayUser.DiscordUserId);

                if (byUser is null)
                    continue;

                builder.WithImageUrl(birthdayUser.AvatarUrl);
                builder.WithAuthor(byUser);
                builder.WithTimestamp(userCreatedAt);

                //builder.WithCurrentTimestamp();

                var message = await general.SendMessageAsync("", false, builder.Build());

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
