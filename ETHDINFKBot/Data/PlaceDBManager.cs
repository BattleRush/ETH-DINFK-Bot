using Discord;
using ETHBot.DataLayer;
using ETHBot.DataLayer.Data;
using ETHBot.DataLayer.Data.Fun;
using ETHDINFKBot.Enums;
using ETHDINFKBot.Modules;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Logging;
using MySqlConnector;
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

        public List<PlaceBoardPixel> GetImageByYLines(int yFrom, int yUntil)
        {
            try
            {
                using (ETHBotDBContext context = new ETHBotDBContext())
                {
                    return context.PlaceBoardPixels.AsQueryable().Where(i => i.YPos >= yFrom && i.YPos < yUntil).ToList();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message);
                return null;
            }
        }

        public List<PlaceBoardHistory> GetBoardHistory(List<short> placeDiscordUserIds)
        {
            try
            {
                using (ETHBotDBContext context = new ETHBotDBContext())
                {
                    return context.PlaceBoardHistory.AsQueryable().Where(i => !i.Removed && (placeDiscordUserIds.Count == 0 || placeDiscordUserIds.Contains(i.PlaceDiscordUserId))).ToList();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message);
                return null;
            }
        }

        public long GetBoardHistoryCount()
        {
            try
            {
                /*using (ETHBotDBContext context = new ETHBotDBContext())
                {
                    return context.PlaceBoardHistory.Count();
                }*/


                //var sqlSelect = $@"SELECT MAX(_ROWID_) FROM ""PlaceBoardHistory"" LIMIT 1;"; // since no rows are deleted we can use this query to quickly find the row count
                var sqlSelect = $@"SELECT AUTO_INCREMENT
FROM   information_schema.TABLES
WHERE  TABLE_NAME = 'PlaceBoardHistory'"; // since no rows are deleted we can use this query to quickly find the row count

                using (var connection = new MySqlConnection(Program.MariaDBReadOnlyConnectionString))
                {
                    using (var command = new MySqlCommand(sqlSelect, connection))
                    {
                        command.CommandTimeout = 5;
                        connection.Open();

                        return Convert.ToInt64(command.ExecuteScalar()) - 1;
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message);
                return -1;
            }
        }

        public List<PlaceBoardHistory> GetBoardHistory(int x, int y, int size, List<short> discordUserIds)
        {
            try
            {
                using (ETHBotDBContext context = new ETHBotDBContext())
                {
                    return context.PlaceBoardHistory
                        .AsQueryable()
                        .Where(i => i.XPos >= x && i.XPos < x + size && i.YPos >= y && i.YPos < y + size && !i.Removed && (discordUserIds.Count == 0 || discordUserIds.Contains(i.PlaceDiscordUserId)))
                        .ToList();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message);
                return null;
            }
        }

        public List<PlaceDiscordUser> GetPlaceDiscordUsers()
        {
            try
            {
                using (ETHBotDBContext context = new ETHBotDBContext())
                {
                    return context.PlaceDiscordUsers.ToList();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message);
                return null;
            }
        }

        public bool AddPlaceDiscordUser(ulong discordUserId)
        {
            try
            {
                using (ETHBotDBContext context = new ETHBotDBContext())
                {
                    context.PlaceDiscordUsers.Add(new PlaceDiscordUser() { DiscordUserId = discordUserId });
                    context.SaveChanges();

                    return true;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message);
                return false;
            }
        }

        public bool AddPlacePerfRecord(PlacePerformanceInfo placePerfRecord)
        {
            try
            {
                using (ETHBotDBContext context = new ETHBotDBContext())
                {
                    context.PlacePerformanceInfos.Add(placePerfRecord);
                    context.SaveChanges();

                    return true;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message);
                return false;
            }
        }

        // 1440 = last day
        public List<PlacePerformanceInfo> GetPlacePerformanceInfo(int lastMinutes = 1440)
        {
            try
            {
                using (ETHBotDBContext context = new ETHBotDBContext())
                {
                    return context.PlacePerformanceInfos.AsQueryable().Where(i => i.DateTime > DateTime.UtcNow.AddMinutes(-lastMinutes)).ToList();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message);
                return null;
            }
        }

        // TODO maybe move to normal db manager
        public List<BotStartUpTime> GetBotStartUpTimes(DateTime from, DateTime until)
        {
            try
            {
                using (ETHBotDBContext context = new ETHBotDBContext())
                {
                    return context.BotStartUpTimes.AsQueryable().Where(i => i.StartUpTime >= from && i.StartUpTime <= until).ToList();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message);
                return null;
            }
        }

        public List<PlaceBoardHistory> GetBoardHistory(int from, int amount = 100_000)
        {
            /*try
            {
                using (ETHBotDBContext context = new ETHBotDBContext())
                {
                    // we might taking a hit by doing a tolist premature but sqlite doesnt like ulong ordering for whatever reason
                    // TODO maybe use Take last
                    return context.PlaceBoardHistory.AsQueryable().Skip(from).Take(amount).ToList();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message);
                return null;
            }*/

            var returnVal = new List<PlaceBoardHistory>();
            try
            {
                // TODO consider deleted records
                // TODO ensure order by is not needed
                var sqlQuery = $@"
SELECT *
FROM PlaceBoardHistory
WHERE PlaceBoardHistoryId > {from}
LIMIT {amount};";

                using (var connection = new MySqlConnection(Program.MariaDBReadOnlyConnectionString))
                {
                    using (var command = new MySqlCommand(sqlQuery, connection))
                    {
                        command.CommandTimeout = 10;
                        connection.Open();

                        var reader = command.ExecuteReader();

                        while (reader.Read())
                        {
                            int placeBoardHistoryId = reader.GetInt32(0);
                            short xPos = reader.GetInt16(1);
                            short yPos = reader.GetInt16(2);

                            byte r = reader.GetByte(3);
                            byte g = reader.GetByte(4);
                            byte b = reader.GetByte(5);

                            short placeDiscordUserId = reader.GetInt16(6);
                            ulong snowflakeTimePlaced = reader.GetUInt64(7);
                            bool removed = reader.GetBoolean(8);

                            returnVal.Add(new PlaceBoardHistory()
                            {
                                PlaceBoardHistoryId = placeBoardHistoryId,
                                XPos = xPos,
                                YPos = yPos,
                                R = r,
                                G = g,
                                B = b,
                                PlaceDiscordUserId = placeDiscordUserId,
                                PlacedDateTime = SnowflakeUtils.FromSnowflake(snowflakeTimePlaced).UtcDateTime,
                                Removed = removed
                            });
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message);
            }
            return returnVal;
        }

        // TO BE REMOVED SINCE NOW THERE IS AN AUX TABLE
        // this is to reduce the id size from 8 bytes down to 1 byte
        /*public Dictionary<ulong, byte> GetPlaceUserIds()
        {
            var returnVal = new Dictionary<ulong, byte>();
            try
            {
                var sqlQuery = $@"
SELECT DISTINCT DiscordUserId
FROM PlaceBoardHistory 
ORDER BY PlaceBoardHistoryId ASC";

                using (var connection = new SqliteConnection(Program.ConnectionString))
                {
                    using (var command = new SqliteCommand(sqlQuery, connection))
                    {
                        command.CommandTimeout = 10;
                        connection.Open();

                        var reader = command.ExecuteReader();
                        byte count = 1;

                        while (reader.Read())
                        {
                            ulong userId = Convert.ToUInt64(reader.GetString(0));

                            returnVal.Add(userId, count);
                            count++;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message);
            }
            return returnVal;
        }*/

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


                using (var connection = new MySqlConnection(Program.MariaDBReadOnlyConnectionString))
                {
                    using (var command = new MySqlCommand(sqlSelect, connection))
                    {
                        command.CommandTimeout = 5;
                        connection.Open();

                        oldestHistoryId = (long)command.ExecuteScalar();
                    }
                }

                if (oldestHistoryId < 0)
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


                using (var connection = new MySqlConnection(Program.FULL_MariaDBReadOnlyConnectionString))
                {
                    using (var command = new MySqlCommand(sqlSelect, connection))
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

        public bool PlacePixel(short x, short y, System.Drawing.Color color, ulong discordUserId)
        {
            if (x < 0 || x >= 1000 || y < 0 || y >= 1000)
                return false; // reject these entries

            // TODO REPLOAD THE BEFORE
            if(PlaceModule.PlaceDiscordUsers.Count == 0)
                PlaceModule.PlaceDiscordUsers = GetPlaceDiscordUsers();

            var placeUser = PlaceModule.PlaceDiscordUsers.SingleOrDefault(i => i.DiscordUserId == discordUserId);

            if (placeUser == null)
            {
                // If the user has been added then refresh the list
                if (AddPlaceDiscordUser(discordUserId))
                {
                    PlaceModule.PlaceDiscordUsers = GetPlaceDiscordUsers();
                    placeUser = PlaceModule.PlaceDiscordUsers.SingleOrDefault(i => i.DiscordUserId == discordUserId);
                }
            }
           
            if (placeUser == null)
            {
                return false;
            }

            try
            {
                PlaceModule.CurrentPlaceBitmap?.SetPixel(x, y, color);

                var sessions = Program.PlaceWebsocket.WebSocketServices["/place"].Sessions;

                if (sessions != null)
                {
                    byte[] data = new byte[9];

                    byte[] xBytes = BitConverter.GetBytes(x);
                    byte[] yBytes = BitConverter.GetBytes(y);

                    data[0] = (byte)MessageEnum.LivePixel; // identifier

                    data[1] = xBytes[0];
                    data[2] = xBytes[1];
                    data[3] = yBytes[0];
                    data[4] = yBytes[1];

                    data[5] = color.R;
                    data[6] = color.G;
                    data[7] = color.B;

                    data[8] = Convert.ToByte(placeUser.PlaceDiscordUserId);

                    //Console.WriteLine($"Send: {x}/{y} paint R:{color.R}|G:{color.G}|B:{color.B}");

                    sessions.Broadcast(data);
                }
            }
            catch (Exception ex)
            {
                // ignore
                Console.WriteLine($"Failed to draw on Bitmap: {x}/{y}");
            }

            // old query doing over entity framework

            // TODO create history automatically

            /*
            try
            {
                using (ETHBotDBContext context = new ETHBotDBContext())
                {
                    var currentPixel = context.PlaceBoardPixels.AsQueryable().SingleOrDefault(i => i.XPos == x && i.YPos == y);
                    if (currentPixel == null)
                    {
                        // create the pixel -> can soon be disabled
                        context.PlaceBoardPixels.Add(new PlaceBoardPixel()
                        {
                            XPos = x,
                            YPos = y,
                            R = color.R,
                            G = color.G,
                            B = color.B,
                        });
                    }
                    else
                    {
                        currentPixel.R = color.R;
                        currentPixel.G = color.G;
                        currentPixel.B = color.B;
                    }

                    context.PlaceBoardHistory.Add(new PlaceBoardHistory()
                    {
                        DiscordUserId = discordUserId,
                        XPos = x,
                        YPos = y,
                        R = color.R,
                        G = color.G,
                        B = color.B,
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
            */

            string sqlQuery = $@"
-- update the pixel on the live board
UPDATE PlaceBoardPixels
SET R = {color.R}, G = {color.G}, B = {color.B}
WHERE XPos = {x} AND YPos = {y};

-- insert a new entry into history
INSERT INTO PlaceBoardHistory (PlaceDiscordUserId, XPos, YPos, R, G, B, SnowflakeTimePlaced)
VALUES ({placeUser.PlaceDiscordUserId},{x},{y},{color.R},{color.G},{color.B},{SnowflakeUtils.ToSnowflake(DateTimeOffset.UtcNow)})";


            using (var connection = new MySqlConnection(Program.FULL_MariaDBReadOnlyConnectionString))
            {
                using (var command = new MySqlCommand(sqlQuery, connection))
                {
                    try
                    {
                        command.CommandTimeout = 5;
                        connection.Open();

                        int count = command.ExecuteNonQuery();

                        return count == 2; // update pixel and insert 1 record -> TODO track the failures
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
    }
}
