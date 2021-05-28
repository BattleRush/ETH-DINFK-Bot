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
    }
}
