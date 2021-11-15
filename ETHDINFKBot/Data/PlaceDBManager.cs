using Discord;
using ETHBot.DataLayer;
using ETHBot.DataLayer.Data;
using ETHBot.DataLayer.Data.Fun;
using ETHDINFKBot.Enums;
using ETHDINFKBot.Helpers;
using ETHDINFKBot.Modules;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using MySqlConnector;
using SkiaSharp;
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

        public long GetUnchunkedPixels(long lastPixelIdChunked)
        {
            try
            {
                var sqlSelect = $@"SELECT COUNT(*) FROM PlaceBoardHistory WHERE PlaceBoardHistoryId > {lastPixelIdChunked}"; 

                using (var connection = new MySqlConnection(Program.MariaDBReadOnlyConnectionString))
                {
                    using (var command = new MySqlCommand(sqlSelect, connection))
                    {
                        command.CommandTimeout = 20;
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

        public long GetBoardHistoryCount(long lastPixelIdChunked, long chunkedPixels)
        {
            try
            {
                long unchunkedPixelIds = GetUnchunkedPixels(lastPixelIdChunked);
                return chunkedPixels + unchunkedPixelIds;
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

        public List<PlaceDiscordUser> GetPlaceDiscordUsers(bool onlyVerified = false)
        {
            try
            {
                using (ETHBotDBContext context = new ETHBotDBContext())
                {
                    if (onlyVerified)
                        return context.PlaceDiscordUsers.Include(i => i.DiscordUser).AsQueryable().Where(i => i.DiscordUser.AllowedPlaceMultipixel).ToList();

                    return context.PlaceDiscordUsers.ToList();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message);
                return null;
            }
        }

        public PlaceDiscordUser GetPlaceDiscordUserByDiscordUserId(ulong discordUserId)
        {
            try
            {
                using (ETHBotDBContext context = new ETHBotDBContext())
                {
                    return context.PlaceDiscordUsers.SingleOrDefault(i => i.DiscordUserId == discordUserId);
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
                    var placeUser = context.PlaceDiscordUsers.FirstOrDefault(i => i.DiscordUserId == discordUserId);
                    if (placeUser != null)
                        return true; // this user exists in the db

                    // get the max id and do + 1 as InnoDB increases the id in case of an failed insert
                    // this should work fine as there is rarely a new user
                    int maxId = 0;

                    // TODO DefaultIfEmpty()
                    try
                    {
                        maxId = context.PlaceDiscordUsers.Max(i => i.PlaceDiscordUserId);
                    }
                    catch { }

                    context.PlaceDiscordUsers.Add(new PlaceDiscordUser()
                    {
                        PlaceDiscordUserId = Convert.ToInt16(maxId + 1),
                        DiscordUserId = discordUserId,
                        TotalPixelsPlaced = 0
                    });
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

        public List<PlaceBoardHistory> GetBoardHistory(long fromPixelId, int amount = 100_000)
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
WHERE PlaceBoardHistoryId > {fromPixelId}
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
                            DateTime dateTimePlaced = reader.GetDateTime(7);
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
                                PlacedDateTime = dateTimePlaced,
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


                var sqlSelect = $@"
SELECT PlaceBoardHistoryId
FROM PlaceBoardHistory
WHERE DiscordUserId = {discordUserId} AND PlacedDateTime > {DateTime.UtcNow.AddMinutes(minutes)} AND XPos > {xStart} AND XPos < {xEnd} AND YPos > {yStart} AND YPos < {yEnd};

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
        public bool PlacePixel(short x, short y, SKColor color, short placeDiscordUserId)
        {
            // TODO REPLOAD THE BEFORE
            if (PlaceModule.PlaceDiscordUsers.Count == 0)
                PlaceModule.PlaceDiscordUsers = GetPlaceDiscordUsers();

            var placeUser = PlaceModule.PlaceDiscordUsers.SingleOrDefault(i => i.PlaceDiscordUserId == placeDiscordUserId);

            if (placeUser == null)
            {
                // If the user has been added and we cant fînd the id
                return false;
            }

            return PlacePixel(x, y, color, placeUser);
        }

        public bool PlacePixel(short x, short y, SKColor color, ulong discordUserId)
        {
            // TODO REPLOAD THE BEFORE
            if (PlaceModule.PlaceDiscordUsers.Count == 0)
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

            return PlacePixel(x, y, color, placeUser);
        }

        public bool PlacePixel(short x, short y, SKColor color, PlaceDiscordUser placeUser)
        {
            if (x < 0 || x >= 1000 || y < 0 || y >= 1000)
                return false; // reject these entries

            if (placeUser == null)
                return false;

            try
            {
                PlaceModule.CurrentPlaceBitmap?.SetPixel(x, y, color);

                var server = Program.PlaceServer;

                if (server != null)
                {
                    byte[] data = new byte[9];

                    byte[] xBytes = BitConverter.GetBytes(x);
                    byte[] yBytes = BitConverter.GetBytes(y);

                    data[0] = (byte)MessageEnum.LivePixel; // identifier

                    data[1] = xBytes[0];
                    data[2] = xBytes[1];
                    data[3] = yBytes[0];
                    data[4] = yBytes[1];

               

                    data[5] = color.Red;
                    data[6] = color.Green;
                    data[7] = color.Blue;

                    data[8] = Convert.ToByte(placeUser.PlaceDiscordUserId);

                    //Console.WriteLine($"Send: {x}/{y} paint R:{color.R}|G:{color.G}|B:{color.B}");

                    server.MulticastBinary(data, 0, 9);
                }
            }
            catch (Exception ex)
            {
                // ignore
                //Console.WriteLine($"Failed to draw on Bitmap: {x}/{y}");


                //_logger.LogError(ex, ex.Message);
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
#if DEBUG
            string sqlQuery = $@"
-- update the pixel on the live board
INSERT INTO PlaceBoardPixels
VALUES ({x}, {y}, {color.Red}, {color.Green}, {color.Blue})
ON DUPLICATE KEY UPDATE
R = {color.Red}, G = {color.Green}, B = {color.Blue};

-- insert a new entry into history
INSERT INTO PlaceBoardHistory (PlaceDiscordUserId, XPos, YPos, R, G, B, PlacedDateTime, Removed)
VALUES ({placeUser.PlaceDiscordUserId},{x},{y},{color.Red},{color.Green},{color.Blue},@placedDateTime, 0);";
#endif

#if !DEBUG
            string sqlQuery = $@"
-- update the pixel on the live board
UPDATE PlaceBoardPixels
SET R = {color.Red}, G = {color.Green}, B = {color.Blue}
WHERE XPos = {x} AND YPos = {y};

-- insert a new entry into history
INSERT INTO PlaceBoardHistory (PlaceDiscordUserId, XPos, YPos, R, G, B, PlacedDateTime, Removed)
VALUES ({placeUser.PlaceDiscordUserId},{x},{y},{color.Red},{color.Green},{color.Blue},@placedDateTime, 0)";
#endif

            using (var connection = new MySqlConnection(Program.FULL_MariaDBReadOnlyConnectionString))
            {
                using (var command = new MySqlCommand(sqlQuery, connection))
                {
                    try
                    {
                        command.CommandTimeout = 5;
                        connection.Open();

                        MySqlParameter parameter = command.Parameters.Add("@placedDateTime", System.Data.DbType.DateTime);
                        parameter.Value = DateTime.UtcNow;

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




        public List<PlaceMultipixelJob> GetMultipixelJobs(short placeDiscordUserId, bool onlyActive = true)
        {
            try
            {
                using (ETHBotDBContext context = new ETHBotDBContext())
                {
                    return context.PlaceMultipixelJobs
                        .AsQueryable()
                        .Where(i => i.PlaceDiscordUserId == placeDiscordUserId &&
                        (!onlyActive || (onlyActive && (i.Status == (int)MultipixelJobStatus.Importing || i.Status == (int)MultipixelJobStatus.Ready || i.Status == (int)MultipixelJobStatus.Active))))
                        .ToList(); // fine to do as not many jobs will be in the db
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message);
                return null;
            }
        }

        public PlaceMultipixelJob GetPlaceMultipixelJob(int placeMultipixelJobId)
        {
            try
            {
                using (ETHBotDBContext context = new ETHBotDBContext())
                {
                    return context.PlaceMultipixelJobs.SingleOrDefault(i => i.PlaceMultipixelJobId == placeMultipixelJobId);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message);
                return null;
            }
        }

        public PlaceMultipixelJob CreatePlaceMultipixelJob(short placeDiscordUserId, int totalPixels)
        {
            try
            {
                using (ETHBotDBContext context = new ETHBotDBContext())
                {
                    var newJob = new PlaceMultipixelJob()
                    {
                        PlaceDiscordUserId = placeDiscordUserId,
                        TotalPixels = totalPixels,
                        Status = (int)MultipixelJobStatus.None,
                        CreatedAt = DateTime.Now
                    };

                    context.PlaceMultipixelJobs.Add(newJob);
                    context.SaveChanges();

                    return newJob;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message);
                return null;
            }
        }


        // TODO maybe return the object?
        public bool UpdatePlaceMultipixelJobStatus(int placeMultipixelJobId, MultipixelJobStatus status)
        {
            try
            {
                using (ETHBotDBContext context = new ETHBotDBContext())
                {
                    var job = context.PlaceMultipixelJobs.SingleOrDefault(i => i.PlaceMultipixelJobId == placeMultipixelJobId);
                    if (job != null)
                    {
                        job.Status = (int)status;

                        if (status == MultipixelJobStatus.Done)
                            job.FinishedAt = DateTime.Now;

                        if (status == MultipixelJobStatus.Canceled)
                            job.CanceledAt = DateTime.Now;

                        context.SaveChanges();

                        return true;
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message);
            }

            return false;
        }

        public PlaceMultipixelPacket CreateMultipixelJobPacket(int placeMultipixelJobId, string instructions, int instructionCount)
        {
            try
            {
                using (ETHBotDBContext context = new ETHBotDBContext())
                {
                    var newPacket = new PlaceMultipixelPacket()
                    {
                        PlaceMultipixelJobId = placeMultipixelJobId,
                        InstructionCount = instructionCount,
                        Instructions = instructions
                    };

                    context.PlaceMultipixelPackets.Add(newPacket);
                    context.SaveChanges();

                    return newPacket;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message);
                return null;
            }
        }

        public PlaceMultipixelPacket GetNextFreeMultipixelJobPacket(int placeMultipixelJobId)
        {
            try
            {
                using (ETHBotDBContext context = new ETHBotDBContext())
                {
                    return context.PlaceMultipixelPackets.FirstOrDefault(i => i.PlaceMultipixelJobId == placeMultipixelJobId && i.Done == false);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message);
                return null;
            }
        }

        public int GetFinishedMultipixelJobPacketCount(int placeMultipixelJobId)
        {
            try
            {
                using (ETHBotDBContext context = new ETHBotDBContext())
                {
                    return context.PlaceMultipixelPackets.AsQueryable().Where(i => i.PlaceMultipixelJobId == placeMultipixelJobId && i.Done).Count();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message);
                return -1;
            }
        }

        public bool MarkMultipixelJobPacketAsDone(int placeMultipixelJobPacketId)
        {
            try
            {
                using (ETHBotDBContext context = new ETHBotDBContext())
                {
                    context.PlaceMultipixelPackets.Single(i => i.PlaceMultipixelPacketId == placeMultipixelJobPacketId).Done = true;
                    context.SaveChanges();
                    return true;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message);
            }

            return false;
        }
    }
}