using Discord;
using ETHBot.DataLayer;
using ETHBot.DataLayer.Data.Fun;
using ETHDINFKBot.Modules;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ETHDINFKBot.Data
{
    public class PlaceDBManager
    {
        private static PlaceDBManager _instance;
        private static object syncLock = new object();
        private readonly ILogger _logger = new Logger<PlaceDBManager>(Program.Logger);

        public static PlaceDBManager Instance()
        {
            lock (syncLock)
            {
                if (_instance == null)
                {
                    _instance = new PlaceDBManager();
                }
            }

            return _instance;
        }

        public bool GetBoardStatus()
        {
            try
            {
                using (ETHBotDBContext context = new ETHBotDBContext())
                {
                    return context.BotSetting.First().PlaceLocked;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message);
                return true;
            }
        }

        public bool SetBoardStatus(bool status)
        {
            try
            {
                using (ETHBotDBContext context = new ETHBotDBContext())
                {
                    context.BotSetting.First().PlaceLocked = status;
                    context.SaveChanges();

                    return status;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message);
                return true;
            }
        }

        public List<PlaceBoardPixel> GetCurrentImage()
        {
            try
            {
                using (ETHBotDBContext context = new ETHBotDBContext())
                {
                    return context.PlaceBoardPixels.ToList();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message);
                return null;
            }
        }

        public List<PlaceBoardHistory> GetBoardHistory(List<ulong> discordUserIds)
        {
            try
            {
                using (ETHBotDBContext context = new ETHBotDBContext())
                {
                    return context.PlaceBoardHistory.AsQueryable().Where(i => !i.Removed && (discordUserIds.Count == 0 || discordUserIds.Contains(i.DiscordUserId))).ToList();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message);
                return null;
            }
        }
        public int GetBoardHistoryCount()
        {
            try
            {
                using (ETHBotDBContext context = new ETHBotDBContext())
                {
                    return context.PlaceBoardHistory.Count();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message);
                return -1;
            }
        }

        public List<PlaceBoardHistory> GetBoardHistory(int x, int y, int size, List<ulong> discordUserIds)
        {
            try
            {
                using (ETHBotDBContext context = new ETHBotDBContext())
                {
                    return context.PlaceBoardHistory
                        .AsQueryable()
                        .Where(i => i.XPos >= x && i.XPos < x + size && i.YPos >= y && i.YPos < y + size && !i.Removed && (discordUserIds.Count == 0 || discordUserIds.Contains(i.DiscordUserId)))
                        .ToList();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message);
                return null;
            }
        }

        public long RemovePixels(ulong discordUserId, int minutes, int xStart, int xEnd, int yStart, int yEnd)
        {
            try
            {
                long oldestHistoryId = -1;

                /*using (ETHBotDBContext context = new ETHBotDBContext())
                {


                    oldestHistoryId = context.PlaceBoardHistory
                        .AsQueryable()
                        .Where(i => i.DiscordUserId == discordUserId && i.SnowflakeTimePlaced > fromSnowflake && i.XPos >= xStart && i.XPos < xEnd && i.YPos >= yStart && i.YPos < yEnd)
                        .OrderBy(i => i.PlaceBoardHistoryId)
                        .First().PlaceBoardHistoryId;
                }
                */

                if (minutes > 0)
                    minutes *= -1;// set it negative

                var fromTime = DateTimeOffset.UtcNow.AddMinutes(minutes);
                var fromSnowflake = SnowflakeUtils.ToSnowflake(fromTime);

                var sqlSelect = $@"
SELECT PlaceBoardHistoryId
FROM PlaceBoardHistory
WHERE DiscordUserId = {discordUserId} AND SnowflakeTimePlaced > {fromSnowflake} AND XPos > {xStart} AND XPos < {xEnd} AND YPos > {yStart} AND YPos < {yEnd};

";


                using (var connection = new SqliteConnection(Program.ConnectionString))
                {
                    using (var command = new SqliteCommand(sqlSelect, connection))
                    {
                        command.CommandTimeout = 5;
                        connection.Open();

                        oldestHistoryId = (long)command.ExecuteScalar();
                    }
                }

                if(oldestHistoryId < 0)
                {
                    return -1;
                }

                string sqlQuery = $@"
UPDATE PlaceBoardHistory
SET Removed = 1
WHERE DiscordUserId = {discordUserId} AND PlaceBoardHistoryId > {oldestHistoryId} AND XPos > {xStart} AND XPos < {xEnd} AND YPos > {yStart} AND YPos < {yEnd};

UPDATE PlaceBoardPixels
SET R = 255, G = 255, B = 255
WHERE XPos > {xStart} AND XPos < {xEnd} AND YPos > {yStart} AND YPos < {yEnd}";


                using (var connection = new SqliteConnection(Program.ConnectionString))
                {
                    using (var command = new SqliteCommand(sqlQuery, connection))
                    {
                        command.CommandTimeout = 5;
                        connection.Open();

                        return command.ExecuteNonQuery();
                    }
                }

            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message);
                return -1;
            }
        }

        // only select a selection of it
        public List<PlaceBoardPixel> GetCurrentImage(int xMin, int xMax, int yMin, int yMax)
        {
            try
            {
                using (ETHBotDBContext context = new ETHBotDBContext())
                {
                    return context.PlaceBoardPixels.AsQueryable().Where(i => i.XPos >= xMin && i.XPos < xMax && i.YPos >= yMin && i.YPos < yMax).ToList();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message);
                return null;
            }
        }

        public List<PlaceBoardHistory> GetPixelHistory(int x, int y, int amount = 25)
        {
            try
            {
                using (ETHBotDBContext context = new ETHBotDBContext())
                {
                    // we might taking a hit by doing a tolist premature but sqlite doesnt like ulong ordering for whatever reason
                    // TODO maybe use Take last
                    return context.PlaceBoardHistory.AsQueryable().Where(i => i.XPos == x && i.YPos == y && !i.Removed).OrderByDescending(i => i.PlaceBoardHistoryId).Take(amount).ToList();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message);
                return null;
            }
        }

        public List<PlaceBoardHistory> GetLastPixelHistory(int amount = 25)
        {
            try
            {
                using (ETHBotDBContext context = new ETHBotDBContext())
                {
                    // TODO maybe in raw sql
                    // we are taking a hit by doing a tolist premature but sqlite doesnt like ulong ordering for whatever reason
                    return context.PlaceBoardHistory.AsQueryable().Where(i => !i.Removed).OrderByDescending(i => i.PlaceBoardHistoryId).Take(amount).ToList();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message);
                return null;
            }
        }

        public bool PlacePixel(short x, short y, byte r, byte g, byte b, ulong discordUserId)
        {
            if (x < 0 || x >= 1000 || y < 0 || y >= 1000)
                return false; // reject these entries

            try
            {
                if (PlaceModule.LastRefresh.Add(TimeSpan.FromMinutes(10)) > DateTime.Now)
                {
                    var element = PlaceModule.PixelsCache.SingleOrDefault(i => i.XPos == x && i.YPos == y);
                    if (element == null)
                    {
                        PlaceModule.PixelsCache.Add(new PlaceBoardPixel()
                        {
                            XPos = x,
                            YPos = y,
                            R = r,
                            G = g,
                            B = b
                        });
                    }
                    else
                    {
                        element.R = r;
                        element.G = g;
                        element.B = b;
                    }
                }
            }
            catch (Exception ex)
            {
                // ignore
            }


            // TODO create history automatically
            try
            {
                using (ETHBotDBContext context = new ETHBotDBContext())
                {
                    var currentPixel = context.PlaceBoardPixels.AsQueryable().SingleOrDefault(i => i.XPos == x && i.YPos == y);
                    if (currentPixel == null)
                    {
                        // create the pixel
                        context.PlaceBoardPixels.Add(new PlaceBoardPixel()
                        {
                            XPos = x,
                            YPos = y,
                            R = r,
                            G = g,
                            B = b
                        });
                    }
                    else
                    {
                        currentPixel.R = r;
                        currentPixel.G = g;
                        currentPixel.B = b;
                    }


                    context.PlaceBoardHistory.Add(new PlaceBoardHistory()
                    {
                        DiscordUserId = discordUserId,
                        XPos = x,
                        YPos = y,
                        R = r,
                        G = g,
                        B = b,
                        SnowflakeTimePlaced = SnowflakeUtils.ToSnowflake(DateTimeOffset.UtcNow)
                    });

                    context.SaveChanges();

                    return true;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message);

                return false;
                //return null;
            }
        }
    }
}
