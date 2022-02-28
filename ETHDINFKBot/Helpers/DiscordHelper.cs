using Discord;
using Discord.WebSocket;
using ETHBot.DataLayer.Data.Discord;
using ETHDINFKBot.Data;
using ETHDINFKBot.Drawing;
using SkiaSharp;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
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

        // TODO Handle  Unhandled exception. Discord.Net.HttpException: The server responded with error 50007: Cannot send messages to this user
        public static async Task<bool> SaveMessage(SocketTextChannel socketTextChannel, SocketGuildUser user, IMessage message, bool byCommand)
        {
            var dmManager = DatabaseManager.Instance();

            if (dmManager.IsSaveMessage(message.Id, user.Id))
            {
                // dont allow double saves
                return false;
            }

            string authorUsername = user.Username; // nickname?

            var link = $"https://discord.com/channels/{socketTextChannel.Guild.Id}/{socketTextChannel.Id}/{message.Id}";

            // TODO create common place for button ids
            var builderComponent = new ComponentBuilder().WithButton("Delete Message", "delete-saved-message-id", ButtonStyle.Danger);

            EmbedBuilder builder = new EmbedBuilder();

            builder.WithTitle($"Saved message from {authorUsername}");
            builder.WithColor(0, 128, 255);
            builder.WithDescription(message.Content);

            builder.AddField("Guild", socketTextChannel.Guild.Name, true);
            builder.AddField("Channel", socketTextChannel.Name, true);
            builder.AddField("User", message?.Author?.Username ?? "N/A", true);
            builder.AddField("DirectLink", $"[Link to the message]({link})");

            builder.WithAuthor(user);
            builder.WithCurrentTimestamp();


            var messageSend = await user.SendMessageAsync("", false, builder.Build(), null, null, builderComponent.Build(), message.Embeds.Select(i => i as Embed).ToArray());
            foreach (var item in message.Attachments)
            {
                await user.SendMessageAsync(item.Url, false, null, null, null, builderComponent.Build());
            }

            dmManager.SaveMessage(message.Id, message?.Author?.Id ?? user.Id, user.Id, link, message.Content, byCommand, message.Id);

            if (!byCommand)
            {
                // TODO give hint to use the new way
            }

            return true;
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
            catch (Exception ex)
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
            catch (Exception ex)
            {
                // do nothing -> usually a 404 error as the message is already removed
            }
        }



        public static async void DiscordUserBirthday(DiscordSocketClient client, ulong guildId, ulong channelId, bool reactions)
        {
            var spamChannel = client.GetGuild(guildId).GetTextChannel(channelId); // #spam

            try
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



                if (birthdayUsers.Count == 0)
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
                    builder.WithDescription($"Happy Discord Birthday <@{birthdayUser.DiscordUserId}> <:happe:816101506708799528> {(isFeb29Kid ? " (also for you Feb 29 xD)" : "")}"); // TODO maybe send it in the msg bug edited without a real ping

                    builder.AddField("Created at", userCreatedAt.ToString("F")); // TODO Check timezone stuff

                    var byUser = Program.Client.GetUser(birthdayUser.DiscordUserId);

                    if (byUser is not null)
                        builder.WithAuthor(byUser); // TODO Check User Download for offline users

                    builder.WithImageUrl(birthdayUser.AvatarUrl);
                    builder.WithTimestamp(SnowflakeUtils.FromSnowflake(birthdayUser.DiscordUserId)); // has to be in UTC

                    var message = await spamChannel.SendMessageAsync("", false, builder.Build());
                    await message.ModifyAsync(i => i.Content = $"<@{birthdayUser.DiscordUserId}>");

                    if (reactions)
                    {
                        // TODO Emote library
                        await message.AddReactionAsync(Emote.Parse($"<:yay:851469734545588234>"));
                        await message.AddReactionAsync(Emote.Parse($"<:yay:872093645212368967>"));
                        await message.AddReactionAsync(Emote.Parse($"<a:pepeD:818886775199629332>"));
                    }
                }
            }
            catch (Exception ex)
            {
                spamChannel.SendMessageAsync(ex.ToString()); // Send error message for today command
            }
        }


        public static (Dictionary<ulong, string> EmoteList, string textBlock, string Url, int TotalEmotesFound, int PageSize) SearchEmote(string search, ulong guildId, int page = 0, bool debug = false, int rows = 5, int columns = 10)
        {
            string emoteText = "";

            Dictionary<ulong, string> emoteList = new Dictionary<ulong, string>();

            Stopwatch watch = new Stopwatch();
            watch.Start();

            var emotes = EmoteDBManager.Instance().GetEmotes(search); // TODO dont dowload the emote data before its further filtered
            var guildEmotes = Program.Client.GetGuild(guildId).Emotes.ToList();

            int total = emotes.Count;
            int pageSize = rows * columns;


            Dictionary<string, int> dupes = new Dictionary<string, int>();

            string sep = "-";

            foreach (var emote in emotes)
            {
                int offset = 0;

                // we found a dupe
                if (dupes.ContainsKey(emote.EmoteName.ToLower()))
                    offset = dupes[emote.EmoteName.ToLower()] += 1;

                else
                    dupes.Add(emote.EmoteName.ToLower(), 1);

                if (debug)
                    emote.EmoteName = emote.DiscordEmoteId.ToString();
                else if (offset > 0)
                    emote.EmoteName += $"{sep}{offset}";
            }

            emotes = emotes.Skip(page * pageSize).Take(pageSize).ToList();


            string fileName = $"emote_{search}_{new Random().Next(int.MaxValue)}.png";

            var emoteDrawing = DrawPreviewImage(emotes, guildEmotes, rows, columns);

            DrawingHelper.SaveToDisk(Path.Combine(Program.ApplicationSetting.CDNPath, fileName), emoteDrawing.Bitmap);

            watch.Stop();

            //string text = "";

            int countEmotes = 0;
            int row = 0;

            emoteText = "```css" + Environment.NewLine + "[0] ";

            foreach (var emote in emotes)
            {
                string emoteString = $"{Program.CurrentPrefix}{emote.EmoteName} ";

                if (emote.Animated)
                    emoteString = $"[{emote.EmoteName}] ";

                if (guildEmotes.Any(i => i.Id == emote.DiscordEmoteId))
                    emoteString = $"({emote.EmoteName}) ";


                //text += emoteString;
                emoteText += emoteString;
                emoteList.Add(emote.DiscordEmoteId, emote.EmoteName);

                countEmotes++;

                if (countEmotes >= columns)
                {
                    //fields.Add($"[{row}]", "```css" + Environment.NewLine + text + "```");

                    row++;

                    if (row < rows)
                        emoteText += Environment.NewLine + $"[{row}] ";
                    else
                        break;

                    //text = "";

                    countEmotes = 0;
                }
            }
            emoteText = emoteText.Substring(0, Math.Min(emoteText.Length, 1990));
            emoteText += "```";

            return (emoteList, emoteText, $"https://cdn.battlerush.dev/{fileName}", total, pageSize);
        }

        // TODO alot of rework to do
        // TODO dynamic image sizes
        // TODO support 100+
        // TODO gifs -> video?
        public static (SKBitmap Bitmap, SKCanvas Canvas) DrawPreviewImage(List<DiscordEmote> emojis, List<GuildEmote> guildEmotes, int rows = 10, int columns = 10)
        {
            int padding = 50;
            int paddingY = 55;

            int imgSize = 48;
            int blockSize = imgSize + 35;

            int yOffsetFixForImage = 2;

            int width = Math.Min(emojis.Count, columns) * blockSize + padding;
            int height = (int)(Math.Ceiling(emojis.Count / (double)columns) * blockSize + paddingY - 5);

            width = Math.Max(width + 25, 350); // because of the title


            SKBitmap bitmap = new(width, height); // TODO insert into constructor
            SKCanvas canvas = new(bitmap);

            canvas.Clear(DrawingHelper.DiscordBackgroundColor);

            //Font drawFont = new Font("Arial", 10, FontStyle.Bold);
            //Font drawFontTitle = new Font("Arial", 12, FontStyle.Bold);
            //Font drawFontIndex = new Font("Arial", 16, FontStyle.Bold);

            //Brush brush = new SolidBrush(Color.White);
            //Brush brushNormal = new SolidBrush(Color.LightSkyBlue);
            //Brush brushGif = new SolidBrush(Color.Coral);
            //Brush brushEmote = new SolidBrush(Color.Gold);

            var normalEmotePaint = new SKPaint()
            {
                Color = new SKColor(255, 255, 255),
                Typeface = DrawingHelper.Typeface_Arial,
                TextSize = 13
            };

            var gifEmotePaint = new SKPaint()
            {
                Color = new SKColor(248, 131, 121),// Coral
                Typeface = DrawingHelper.Typeface_Arial,
                TextSize = 13
            };

            var serverEmotePaint = new SKPaint()
            {
                Color = new SKColor(255, 215, 0), // Gold
                Typeface = DrawingHelper.Typeface_Arial,
                TextSize = 13
            };

            canvas.DrawText($"Normal emote", new SKPoint(10, 15), normalEmotePaint);
            canvas.DrawText($"Gif emote", new SKPoint(125, 15), gifEmotePaint);
            canvas.DrawText($"Server emote", new SKPoint(210, 15), serverEmotePaint);

            //Pen p = new Pen(brush);

            // TODO make it more robust and cleaner
            for (int i = 0; i < rows; i++)
            {
                canvas.DrawText($"[{i}]", new SKPoint(10, i * blockSize + paddingY + 12), new SKPaint() { Color = new SKColor(255, 255, 255), Typeface = DrawingHelper.Typeface_Arial, TextSize = 20 });

                for (int j = 0; j < columns; j++)
                {
                    if (emojis.Count <= i * j)
                        break;

                    try
                    {
                        var emote = emojis[i * columns + j];

                        SKBitmap emoteBitmap;
                        using (var ms = new MemoryStream(File.ReadAllBytes(emote.LocalPath)))
                        {
                            emoteBitmap = SKBitmap.Decode(ms);
                        }

                        SKPaint paint = normalEmotePaint;

                        if (emote.Animated)
                            paint = gifEmotePaint;

                        // this server contains this emote
                        if (guildEmotes.Any(i => i.Id == emote.DiscordEmoteId))
                            paint = serverEmotePaint;

                        int x = j * blockSize + padding;
                        int y = i * blockSize + paddingY + yOffsetFixForImage;

                        canvas.DrawBitmap(emoteBitmap, new SKRect(x, y, x + imgSize, y + imgSize));
                        canvas.DrawText($"{emote.EmoteName}", new SKPoint(x - 1, i * blockSize + j % 2 * (imgSize + 15) + paddingY), paint);
                    }
                    catch (Exception ex)
                    {

                    }
                }

                canvas.DrawLine(new SKPoint(0, i * blockSize + paddingY - 15), new SKPoint(width, i * blockSize + paddingY - 15), DrawingHelper.DefaultDrawing);
            }

            //var stream = CommonHelper.GetStream(bitmap);

            //bitmap.Dispose();
            //canvas.Dispose();

            return (bitmap, canvas);
        }


    }
}
