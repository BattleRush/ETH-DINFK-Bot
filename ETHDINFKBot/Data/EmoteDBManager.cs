using Discord;
using Discord.WebSocket;
using ETHBot.DataLayer;
using ETHBot.DataLayer.Data.Discord;
using ETHBot.DataLayer.Data.Fun;
using Microsoft.Extensions.Logging;
using MySqlConnector;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace ETHDINFKBot.Data
{
    public class EmoteDBManager
    {
        private static EmoteDBManager _instance;
        private static object syncLock = new object();
        private readonly ILogger _logger = new Logger<EmoteDBManager>(Program.Logger);

        public static EmoteDBManager Instance()
        {
            lock (syncLock)
            {
                if (_instance == null)
                {
                    _instance = new EmoteDBManager();
                }
            }

            return _instance;
        }

        public DiscordEmote GetEmoteByName(string emoteName)
        {
            try
            {
                using (ETHBotDBContext context = new ETHBotDBContext())
                {
                    return context.DiscordEmotes.FirstOrDefault(i => i.EmoteName.ToLower() == emoteName.ToLower());
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message);
                return null;
            }
        }

        public bool SetEmoteBlockStatus(ulong emoteId, bool blockStatus)
        {
            try
            {
                using (ETHBotDBContext context = new ETHBotDBContext())
                {
                    var emote = context.DiscordEmotes.FirstOrDefault(i => i.DiscordEmoteId == emoteId);
                    if (emote != null)
                    {
                        emote.Blocked = blockStatus;

                        if (blockStatus)
                            emote.LocalPath = null;
                        // TODO in case of unblock reload the file

                        context.SaveChanges();
                        return true;
                    }
                    return false;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message);
                return false;
            }
        }

        public List<DiscordEmote> GetEmotes(string name = null, bool blocked = false)
        {
            try
            {
                using (ETHBotDBContext context = new ETHBotDBContext())
                {
                    // Select all
                    if (name == null)
                        return context.DiscordEmotes.AsQueryable().Where(i => i.Blocked == blocked).ToList();

                    // todo improve and better search
                    return context.DiscordEmotes.AsQueryable().Where(i => i.EmoteName.ToLower().Contains(name) && i.Blocked == blocked).ToList();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message);
                return null;
            }
        }

        public List<DiscordEmote> GetEmotesByDirectName(string name)
        {
            try
            {
                using (ETHBotDBContext context = new ETHBotDBContext())
                {
                    // todo improve and better search
                    return context.DiscordEmotes.AsQueryable().Where(i => i.EmoteName.ToLower() == name.ToLower() && !i.Blocked).ToList();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message);
                return null;
            }
        }

        public long TotalEmoteCount()
        {
            try
            {
                //using (ETHBotDBContext context = new ETHBotDBContext())
                //{
                //    return context.DiscordEmotes.Count();
                //}

                var sqlSelect = $@"SELECT COUNT(*) FROM DiscordEmotes";

                using (var connection = new MySqlConnection(Program.ApplicationSetting.ConnectionStringsSetting.ConnectionString_ReadOnly))
                {
                    using (var command = new MySqlCommand(sqlSelect, connection))
                    {
                        command.CommandTimeout = 5;
                        connection.Open();

                        return (long)command.ExecuteScalar();
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message);
            }

            return -1;
        }

        public DiscordEmote GetDiscordEmoteById(ulong id)
        {
            try
            {
                using (ETHBotDBContext context = new ETHBotDBContext())
                {
                    return context.DiscordEmotes.SingleOrDefault(i => i.DiscordEmoteId == id);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message);
                return null;
            }
        }

        public List<DiscordEmoteHistory> GetEmoteHistoryUsage(DateTime from, DateTime to)
        {
            try
            {
                using (ETHBotDBContext context = new ETHBotDBContext())
                {
                    return context.DiscordEmoteHistory.AsQueryable().Where(i => i.DateTimePosted > from && i.DateTimePosted < to).ToList();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message);
                return null;
            }
        }

        public List<FavouriteDiscordEmote> GetFavouriteEmotes(ulong discordUserId)
        {
            try
            {
                using (ETHBotDBContext context = new ETHBotDBContext())
                {
                    return context.FavouriteDiscordEmotes.AsQueryable().Where(i => i.DiscordUserId == discordUserId).ToList();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message);
                return null;
            }
        }
        public List<DiscordEmote> GetDiscordEmotes(List<ulong> discordEmoteIds)
        {
            try
            {
                using (ETHBotDBContext context = new ETHBotDBContext())
                {
                    return context.DiscordEmotes.AsQueryable().Where(i => discordEmoteIds.Contains(i.DiscordEmoteId)).ToList();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message);
                return null;
            }
        }

        public FavouriteDiscordEmote GetFavouriteEmote(ulong discordUserId, string name)
        {
            try
            {
                using (ETHBotDBContext context = new ETHBotDBContext())
                {
                    return context.FavouriteDiscordEmotes.AsQueryable().SingleOrDefault(i => i.DiscordUserId == discordUserId && i.Name.ToLower() == name.ToLower());
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message);
                return null;
            }
        }
        public FavouriteDiscordEmote GetFavouriteEmote(ulong discordUserId, ulong emoteId)
        {
            try
            {
                using (ETHBotDBContext context = new ETHBotDBContext())
                {
                    return context.FavouriteDiscordEmotes.AsQueryable().SingleOrDefault(i => i.DiscordUserId == discordUserId && i.DiscordEmoteId == emoteId);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message);
                return null;
            }
        }

        public FavouriteDiscordEmote AddFavouriteEmote(FavouriteDiscordEmote favouriteDiscordEmote)
        {
            try
            {
                using (ETHBotDBContext context = new ETHBotDBContext())
                {
                    context.FavouriteDiscordEmotes.Add(favouriteDiscordEmote);
                    context.SaveChanges();

                    // TODO return the original object with the new id instead
                    return GetFavouriteEmote(favouriteDiscordEmote.DiscordUserId, favouriteDiscordEmote.Name);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message);
                return null;
            }
        }

        public DiscordEmote AddDiscordEmote(DiscordEmote discordEmote)
        {
            try
            {
                using (ETHBotDBContext context = new ETHBotDBContext())
                {
                    var dbEmote = GetDiscordEmoteById(discordEmote.DiscordEmoteId);
                    if (dbEmote == null)
                    {
                        context.DiscordEmotes.Add(discordEmote);
                        context.SaveChanges();
                    }
                    else
                    {
                        return null;
                    }
                }

                return GetDiscordEmoteById(discordEmote.DiscordEmoteId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message);
                return null;
            }
        }


        private string MoveEmoteToDisk(DiscordEmote emote, byte[] imageData)
        {
            var emojiDate = SnowflakeUtils.FromSnowflake(emote.DiscordEmoteId);

            string additionalFolder = $"{emojiDate.Year}-{emojiDate.Month:00}";
            string path = Path.Combine(Program.ApplicationSetting.BasePath, "Emotes", additionalFolder);

            if (!Directory.Exists(path))
                Directory.CreateDirectory(path);

            int i = emote.Url.LastIndexOf(".");
            string fileEnding = emote.Url.Substring(i, emote.Url.Length - i);

            string filePath = Path.Combine(path, $"{emote.DiscordEmoteId}{fileEnding}");

            File.WriteAllBytes(filePath, imageData);

            return filePath;
        }


        // TODO change for new emote table
        public async Task<long> ProcessDiscordEmote(DiscordEmote emote, ulong? discordMessageId, int count, bool isReaction, SocketGuildUser user, bool isPreload)
        {
            Stopwatch watch = new Stopwatch();
            long elapsed = -1;
            try
            {
                using (ETHBotDBContext context = new ETHBotDBContext())
                {
                    var dbEmoji = GetDiscordEmoteById(emote.DiscordEmoteId);
                    if (dbEmoji == null)
                    {
                        watch.Start();
                        using (var webClient = new WebClient())
                        {
                            byte[] bytes = webClient.DownloadData(emote.Url);
                            string filePath = MoveEmoteToDisk(emote, bytes);
                            emote.LocalPath = filePath;
                            context.DiscordEmotes.Add(emote);
                        }
                        context.SaveChanges();
                        watch.Stop();
                        elapsed = watch.ElapsedMilliseconds;
                    }


                    var emojiStat = context.DiscordEmoteStatistics.SingleOrDefault(i => i.DiscordEmoteId == emote.DiscordEmoteId);
                    if (emojiStat == null)
                    {
                        context.DiscordEmoteStatistics.Add(new DiscordEmoteStatistic()
                        {
                            DiscordEmoteId = emote.DiscordEmoteId,
                            UsedAsReaction = !user.IsBot && isReaction ? count : 0,
                            UsedInText = !user.IsBot && !isReaction ? count : 0,
                            UsedInTextOnce = !user.IsBot && !isReaction ? 1 : 0,
                            UsedByBots = user.IsBot && !isReaction ? count : 0
                        });

                    }
                    else
                    {
                        emojiStat.UsedAsReaction += !user.IsBot && isReaction ? count : 0;
                        emojiStat.UsedInText += !user.IsBot && !isReaction ? count : 0;
                        emojiStat.UsedInTextOnce += !user.IsBot && !isReaction ? 1 : 0;
                        emojiStat.UsedByBots += user.IsBot && !isReaction ? count : 0;
                    }

                    var message = DatabaseManager.Instance().GetDiscordMessageById(discordMessageId);



                    if (message == null)
                        discordMessageId = null;

                    DateTime posted = DateTime.Now;

                    if (isPreload && discordMessageId.HasValue)
                    {
                        posted = SnowflakeUtils.FromSnowflake(discordMessageId.Value).UtcDateTime;
                    }

                    context.DiscordEmoteHistory.Add(new DiscordEmoteHistory()
                    {
                        DateTimePosted = posted,
                        Count = count,
                        IsReaction = isReaction,

                        DiscordUserId = user.Id,
                        DiscordEmoteId = emote.DiscordEmoteId,
                        DiscordMessageId = discordMessageId

                    });

                    context.SaveChanges();

                }


            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message);
                
            }

            return elapsed;

            //return GetEmojiStatisticById(emote.DiscordEmoteId);
        }

    }
}
