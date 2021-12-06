//using Discord;
//using Discord.Rest;
//using Discord.WebSocket;
//using ETHBot.DataLayer;
//using ETHBot.DataLayer.Data.Fun;
//using Microsoft.Data.Sqlite;
//using Microsoft.EntityFrameworkCore;
//using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Text;
//using System.Threading.Tasks;

//namespace ETHDINFKBot
//{
//    // NO EXCEPTIONHANDLING -> EVERY QUERY HAS TO MIGRATE OR RESTART
//    public class MigrateSQLiteToMariaDB
//    {
//        public int MigrateDiscordServers()
//        {
//            int count = 0;
//            string sqlQuery = $@"SELECT * FROM DiscordServers;";

//            using (var connection = new SqliteConnection(Program.ConnectionString))
//            {
//                using (var command = new SqliteCommand(sqlQuery, connection))
//                {
//                    command.CommandTimeout = 10;
//                    connection.Open();

//                    var reader = command.ExecuteReader();

//                    using (ETHBotDBContext context = new ETHBotDBContext())
//                    {
//                        while (reader.Read())
//                        {
//                            ulong DiscordServerId = Convert.ToUInt64(reader.GetString(0));
//                            string ServerName = reader.GetString(1);

//                            context.DiscordServers.Add(new ETHBot.DataLayer.Data.Discord.DiscordServer() { DiscordServerId = DiscordServerId, ServerName = ServerName });
//                            count++;
//                        }
//                        context.SaveChanges();
//                    }
//                }
//            }
//            return count;
//        }

//        public int MigrateDiscordChannels()
//        {
//            int count = 0;
//            string sqlQuery = $@"SELECT * FROM DiscordChannels;";

//            using (var connection = new SqliteConnection(Program.ConnectionString))
//            {
//                using (var command = new SqliteCommand(sqlQuery, connection))
//                {
//                    command.CommandTimeout = 10;
//                    connection.Open();

//                    var reader = command.ExecuteReader();

//                    using (ETHBotDBContext context = new ETHBotDBContext())
//                    {
//                        while (reader.Read())
//                        {
//                            ulong DiscordChannelId = Convert.ToUInt64(reader.GetString(0));
//                            string ChannelName = reader.GetString(1);
//                            ulong DiscordServerId = Convert.ToUInt64(reader.GetString(2));

//                            context.DiscordChannels.Add(new ETHBot.DataLayer.Data.Discord.DiscordChannel() { DiscordChannelId = DiscordChannelId, ChannelName = ChannelName, DiscordServerId = DiscordServerId });
//                            count++;
//                        }
//                        context.SaveChanges();
//                    }
//                }
//            }
//            return count;
//        }

//        public int MigrateDiscordUsers()
//        {
//            int count = 0;
//            string sqlQuery = $@"SELECT * FROM DiscordUsers;";

//            using (var connection = new SqliteConnection(Program.ConnectionString))
//            {
//                using (var command = new SqliteCommand(sqlQuery, connection))
//                {
//                    command.CommandTimeout = 10;
//                    connection.Open();

//                    var reader = command.ExecuteReader();

//                    using (ETHBotDBContext context = new ETHBotDBContext())
//                    {
//                        while (reader.Read())
//                        {
//                            ulong DiscordUserId = Convert.ToUInt64(reader.GetString(0));
//                            ushort DiscriminatorValue = Convert.ToUInt16(reader.GetString(1));
//                            bool IsBot = reader.GetBoolean(2);
//                            bool IsWebhook = reader.GetBoolean(3);
//                            string Username = reader.GetString(4);
//                            string AvatarUrl = reader.IsDBNull(5) ? null : reader.GetString(5);
//                            DateTimeOffset? JoinedAt = reader.IsDBNull(6) ? null : reader.GetDateTimeOffset(6);
//                            string Nickname = reader.IsDBNull(7) ? null : reader.GetString(7);
//                            int FirstDailyPostCount = reader.GetInt32(8);
//                            bool AllowedPlaceMultipixel = reader.GetBoolean(9);

//                            context.DiscordUsers.Add(new ETHBot.DataLayer.Data.Discord.DiscordUser()
//                            {
//                                DiscordUserId = DiscordUserId,
//                                DiscriminatorValue = DiscriminatorValue,
//                                IsBot = IsBot,
//                                IsWebhook = IsWebhook,
//                                Username = Username,
//                                AvatarUrl = AvatarUrl,
//                                JoinedAt = JoinedAt,
//                                Nickname = Nickname,
//                                FirstDailyPostCount = FirstDailyPostCount,
//                                AllowedPlaceMultipixel = AllowedPlaceMultipixel
//                            }); ;
//                            count++;
//                        }
//                        context.SaveChanges();
//                    }
//                }
//            }
//            return count;
//        }

//        public int MigrateDiscordMessages(SocketTextChannel textChannel)
//        {
//            int count = 0;
//            string sqlQuery = $@"SELECT * FROM DiscordMessages;";

//            using (var connection = new SqliteConnection(Program.ConnectionString))
//            {
//                using (var command = new SqliteCommand(sqlQuery, connection))
//                {
//                    command.CommandTimeout = 1000;
//                    connection.Open();

//                    var reader = command.ExecuteReader();

//                    using (ETHBotDBContext context = new ETHBotDBContext())
//                    {
//                        context.ChangeTracker.AutoDetectChangesEnabled = false;
//                        context.ChangeTracker.QueryTrackingBehavior = Microsoft.EntityFrameworkCore.QueryTrackingBehavior.NoTracking;

//                        while (reader.Read())
//                        {
//                            ulong MessageId = Convert.ToUInt64(reader.GetString(0));
//                            string Content = reader.GetString(1);
//                            ulong DiscordChannelId = Convert.ToUInt64(reader.GetString(2));
//                            ulong DiscordUserId = Convert.ToUInt64(reader.GetString(3));
//                            ulong? ReplyMessageId = reader.IsDBNull(4) ? null : Convert.ToUInt64(reader.GetString(4));
//                            bool Preloaded = reader.GetBoolean(5);

//                            context.DiscordMessages.Add(new ETHBot.DataLayer.Data.Discord.DiscordMessage()
//                            {
//                                DiscordMessageId = MessageId,
//                                Content = Content,
//                                DiscordChannelId = DiscordChannelId,
//                                DiscordUserId = DiscordUserId,
//                                ReplyMessageId = ReplyMessageId,
//                                Preloaded = Preloaded
//                            });
//                            count++;

//                            if (count % 100_000 == 0)
//                            {
//                                context.SaveChanges();
//                                textChannel.SendMessageAsync($"Step {count.ToString("N0")} in MigrateDiscordMessages");
//                            }
//                        }
//                        context.SaveChanges();
//                    }
//                }
//            }
//            return count;
//        }

//        public int MigrateDiscordEmotes(SocketTextChannel textChannel)
//        {
//            int count = 0;
//            string sqlQuery = $@"SELECT * FROM DiscordEmotes;";

//            using (var connection = new SqliteConnection(Program.ConnectionString))
//            {
//                using (var command = new SqliteCommand(sqlQuery, connection))
//                {
//                    command.CommandTimeout = 10;
//                    connection.Open();

//                    var reader = command.ExecuteReader();

//                    using (ETHBotDBContext context = new ETHBotDBContext())
//                    {
//                        while (reader.Read())
//                        {
//                            ulong DiscordEmoteId = Convert.ToUInt64(reader.GetString(0));
//                            string EmoteName = reader.GetString(1);
//                            bool Animated = reader.GetBoolean(2);
//                            string Url = reader.GetString(3);
//                            string LocalPath = reader.GetString(4);
//                            bool Blocked = reader.GetBoolean(5);
//                            DateTimeOffset CreatedAt = reader.GetDateTimeOffset(6);
//                            DateTimeOffset LastUpdatedAt = reader.GetDateTimeOffset(7);

//                            context.DiscordEmotes.Add(new ETHBot.DataLayer.Data.Discord.DiscordEmote()
//                            {
//                                DiscordEmoteId = DiscordEmoteId,
//                                EmoteName = EmoteName,
//                                Animated = Animated,
//                                Url = Url,
//                                LocalPath = LocalPath,
//                                Blocked = Blocked,
//                                CreatedAt = CreatedAt,
//                                LastUpdatedAt = LastUpdatedAt
//                            });
//                            count++;

//                            if (count % 10_000 == 0)
//                            {
//                                context.SaveChanges();
//                                textChannel.SendMessageAsync($"Step {count.ToString("N0")} in MigrateDiscordEmotes");
//                            }
//                        }
//                        context.SaveChanges();
//                    }
//                }
//            }
//            return count;
//        }

//        public int MigrateDiscordEmoteStatistics(SocketTextChannel textChannel)
//        {
//            int count = 0;
//            string sqlQuery = $@"SELECT * FROM DiscordEmoteStatistics;";

//            using (var connection = new SqliteConnection(Program.ConnectionString))
//            {
//                using (var command = new SqliteCommand(sqlQuery, connection))
//                {
//                    command.CommandTimeout = 10;
//                    connection.Open();

//                    var reader = command.ExecuteReader();

//                    using (ETHBotDBContext context = new ETHBotDBContext())
//                    {
//                        while (reader.Read())
//                        {
//                            ulong DiscordEmoteId = Convert.ToUInt64(reader.GetString(0));
//                            int UsedAsReaction = reader.GetInt32(1);
//                            int UsedInText = reader.GetInt32(2);
//                            int UsedInTextOnce = reader.GetInt32(3);
//                            int UsedByBots = reader.GetInt32(4);

//                            context.DiscordEmoteStatistics.Add(new ETHBot.DataLayer.Data.Discord.DiscordEmoteStatistic()
//                            {
//                                DiscordEmoteId = DiscordEmoteId,
//                                UsedAsReaction = UsedAsReaction,
//                                UsedInText = UsedInText,
//                                UsedInTextOnce = UsedInTextOnce,
//                                UsedByBots = UsedByBots
//                            });
//                            count++;

//                            if (count % 10_000 == 0)
//                            {
//                                context.SaveChanges();
//                                textChannel.SendMessageAsync($"Step {count.ToString("N0")} in MigrateDiscordEmoteStatistics");
//                            }
//                        }
//                        context.SaveChanges();
//                    }
//                }
//            }
//            return count;
//        }

//        public int MigrateDiscordEmoteHistory(SocketTextChannel textChannel)
//        {
//            int count = 0;
//            string sqlQuery = $@"SELECT * FROM DiscordEmoteHistory;";

//            using (var connection = new SqliteConnection(Program.ConnectionString))
//            {
//                using (var command = new SqliteCommand(sqlQuery, connection))
//                {
//                    command.CommandTimeout = 10;
//                    connection.Open();

//                    var reader = command.ExecuteReader();

//                    using (ETHBotDBContext context = new ETHBotDBContext())
//                    {
//                        context.ChangeTracker.AutoDetectChangesEnabled = false;
//                        context.ChangeTracker.QueryTrackingBehavior = Microsoft.EntityFrameworkCore.QueryTrackingBehavior.NoTracking;

//                        while (reader.Read())
//                        {
//                            int DiscordEmoteHistoryId = reader.GetInt32(0);
//                            bool IsReaction = reader.GetBoolean(1);
//                            int Count = reader.GetInt32(2);
//                            DateTime DateTimePosted = reader.GetDateTime(3);
//                            ulong DiscordEmoteId = Convert.ToUInt64(reader.GetString(4));
//                            ulong? DiscordUserId = reader.IsDBNull(5) ? null : Convert.ToUInt64(reader.GetString(5));
//                            ulong? DiscordMessageId = reader.IsDBNull(6) ? null : Convert.ToUInt64(reader.GetString(6));

//                            context.DiscordEmoteHistory.Add(new ETHBot.DataLayer.Data.Discord.DiscordEmoteHistory()
//                            {
//                                DiscordEmoteHistoryId = DiscordEmoteHistoryId,
//                                IsReaction = IsReaction,
//                                Count = Count,
//                                DateTimePosted = DateTimePosted,
//                                DiscordEmoteId = DiscordEmoteId,
//                                DiscordUserId = DiscordUserId,
//                                DiscordMessageId = DiscordMessageId
//                            });
//                            count++;

//                            if (count % 100_000 == 0)
//                            {
//                                context.SaveChanges();
//                                textChannel.SendMessageAsync($"Step {count.ToString("N0")} in MigrateDiscordEmoteStatistics");
//                            }
//                        }
//                        context.SaveChanges();
//                    }
//                }
//            }
//            return count;
//        }

//        public int MigrateBannedLinks()
//        {
//            int count = 0;
//            string sqlQuery = $@"SELECT * FROM BannedLinks;";

//            using (var connection = new SqliteConnection(Program.ConnectionString))
//            {
//                using (var command = new SqliteCommand(sqlQuery, connection))
//                {
//                    command.CommandTimeout = 10;
//                    connection.Open();

//                    var reader = command.ExecuteReader();

//                    using (ETHBotDBContext context = new ETHBotDBContext())
//                    {
//                        while (reader.Read())
//                        {
//                            int BannedLinkId = reader.GetInt32(0);
//                            string Link = reader.GetString(1);
//                            DateTimeOffset ReportTime = reader.GetDateTimeOffset(2);
//                            ulong AddedByDiscordUserId = Convert.ToUInt64(reader.GetString(3));

//                            context.BannedLinks.Add(new ETHBot.DataLayer.Data.Discord.BannedLink()
//                            {
//                                BannedLinkId = BannedLinkId,
//                                Link = Link,
//                                ReportTime = ReportTime,
//                                AddedByDiscordUserId = AddedByDiscordUserId
//                            });
//                            count++;
//                        }
//                        context.SaveChanges();
//                    }
//                }
//            }
//            return count;
//        }

//        public int MigrateCommandTypes()
//        {
//            int count = 0;
//            string sqlQuery = $@"SELECT * FROM CommandTypes;";

//            using (var connection = new SqliteConnection(Program.ConnectionString))
//            {
//                using (var command = new SqliteCommand(sqlQuery, connection))
//                {
//                    command.CommandTimeout = 10;
//                    connection.Open();

//                    var reader = command.ExecuteReader();

//                    using (ETHBotDBContext context = new ETHBotDBContext())
//                    {
//                        while (reader.Read())
//                        {
//                            int CommandTypeId = reader.GetInt32(0);
//                            string Name = reader.IsDBNull(1) ? null : reader.GetString(1);

//                            context.CommandTypes.Add(new ETHBot.DataLayer.Data.Discord.CommandType()
//                            {
//                                CommandTypeId = CommandTypeId,
//                                Name = Name
//                            });
//                            count++;
//                        }
//                        context.SaveChanges();
//                    }
//                }
//            }
//            return count;
//        }

//        public int MigrateCommandStatistics()
//        {
//            int count = 0;
//            string sqlQuery = $@"SELECT * FROM CommandStatistics;";

//            using (var connection = new SqliteConnection(Program.ConnectionString))
//            {
//                using (var command = new SqliteCommand(sqlQuery, connection))
//                {
//                    command.CommandTimeout = 10;
//                    connection.Open();

//                    var reader = command.ExecuteReader();

//                    using (ETHBotDBContext context = new ETHBotDBContext())
//                    {
//                        while (reader.Read())
//                        {
//                            int CommandStatisticId = reader.GetInt32(0);
//                            int CommandTypeId = reader.GetInt32(1);
//                            ulong DiscordUserId = Convert.ToUInt64(reader.GetString(2));
//                            int Count = reader.GetInt32(3);

//                            context.CommandStatistics.Add(new ETHBot.DataLayer.Data.Discord.CommandStatistic()
//                            {
//                                CommandStatisticId = CommandStatisticId,
//                                CommandTypeId = CommandTypeId,
//                                DiscordUserId = DiscordUserId,
//                                Count = Count
//                            });
//                            count++;
//                        }
//                        context.SaveChanges();
//                    }
//                }
//            }
//            return count;
//        }

//        public int MigrateDiscordRoles()
//        {
//            int count = 0;
//            string sqlQuery = $@"SELECT * FROM DiscordRoles;";

//            using (var connection = new SqliteConnection(Program.ConnectionString))
//            {
//                using (var command = new SqliteCommand(sqlQuery, connection))
//                {
//                    command.CommandTimeout = 10;
//                    connection.Open();

//                    var reader = command.ExecuteReader();

//                    using (ETHBotDBContext context = new ETHBotDBContext())
//                    {
//                        while (reader.Read())
//                        {
//                            ulong DiscordRoleId = Convert.ToUInt64(reader.GetString(0));
//                            string ColorHex = reader.GetString(1);
//                            DateTimeOffset CreatedAt = reader.GetDateTimeOffset(2);
//                            bool IsHoisted = reader.GetBoolean(3);
//                            bool IsManaged = reader.GetBoolean(4);
//                            bool IsMentionable = reader.GetBoolean(5);
//                            string Name = reader.GetString(6);
//                            int Position = reader.GetInt32(7);
//                            ulong? DiscordServerId = reader.IsDBNull(8) ? null : Convert.ToUInt64(reader.GetString(8));

//                            context.DiscordRoles.Add(new ETHBot.DataLayer.Data.Discord.DiscordRole()
//                            {
//                                DiscordRoleId = DiscordRoleId,
//                                ColorHex = ColorHex,
//                                CreatedAt = CreatedAt,
//                                IsHoisted = IsHoisted,
//                                IsManaged = IsManaged,
//                                IsMentionable = IsMentionable,
//                                Name = Name,
//                                Position = Position,
//                                DiscordServerId = DiscordServerId
//                            });
//                            count++;
//                        }
//                        context.SaveChanges();
//                    }
//                }
//            }
//            return count;
//        }

//        public int MigratePingHistory(SocketTextChannel textChannel)
//        {
//            int count = 0;
//            string sqlQuery = $@"SELECT * FROM PingHistory;";

//            using (var connection = new SqliteConnection(Program.ConnectionString))
//            {
//                using (var command = new SqliteCommand(sqlQuery, connection))
//                {
//                    command.CommandTimeout = 10;
//                    connection.Open();

//                    var reader = command.ExecuteReader();

//                    using (ETHBotDBContext context = new ETHBotDBContext())
//                    {
//                        while (reader.Read())
//                        {
//                            int PingHistoryId = reader.GetInt32(0);
//                            ulong? DiscordRoleId = reader.IsDBNull(1) ? null : Convert.ToUInt64(reader.GetString(1));
//                            ulong? DiscordUserId = reader.IsDBNull(2) ? null : Convert.ToUInt64(reader.GetString(2));
//                            ulong? DiscordMessageId = reader.IsDBNull(3) ? null : Convert.ToUInt64(reader.GetString(3));
//                            ulong FromDiscordUserId = Convert.ToUInt64(reader.GetString(4));

//                            context.PingHistory.Add(new ETHBot.DataLayer.Data.Discord.PingHistory()
//                            {
//                                PingHistoryId = PingHistoryId,
//                                DiscordRoleId = DiscordRoleId,
//                                DiscordUserId = DiscordUserId,
//                                DiscordMessageId = DiscordMessageId,
//                                FromDiscordUserId = FromDiscordUserId
//                            });
//                            count++;

//                            if (count % 25_000 == 0)
//                            {
//                                context.SaveChanges();
//                                textChannel.SendMessageAsync($"Step {count.ToString("N0")} in MigratePingHistory");
//                            }
//                        }
//                        context.SaveChanges();
//                    }
//                }
//            }
//            return count;
//        }

//        public int MigratePingStatistics()
//        {
//            int count = 0;
//            string sqlQuery = $@"SELECT * FROM PingStatistics;";

//            using (var connection = new SqliteConnection(Program.ConnectionString))
//            {
//                using (var command = new SqliteCommand(sqlQuery, connection))
//                {
//                    command.CommandTimeout = 10;
//                    connection.Open();

//                    var reader = command.ExecuteReader();

//                    using (ETHBotDBContext context = new ETHBotDBContext())
//                    {
//                        while (reader.Read())
//                        {
//                            int PingInfoId = reader.GetInt32(0);
//                            int PingCount = reader.GetInt32(1);
//                            int PingCountOnce = reader.GetInt32(2);
//                            int PingCountBot = reader.GetInt32(3);
//                            ulong DiscordUserId = Convert.ToUInt64(reader.GetString(4));

//                            context.PingStatistics.Add(new ETHBot.DataLayer.Data.Discord.PingStatistic()
//                            {
//                                PingInfoId = PingInfoId,
//                                PingCount = PingCount,
//                                PingCountOnce = PingCountOnce,
//                                PingCountBot = PingCountBot,
//                                DiscordUserId = DiscordUserId
//                            });
//                            count++;
//                        }
//                        context.SaveChanges();
//                    }
//                }
//            }
//            return count;
//        }

//        public int MigrateRantTypes()
//        {
//            int count = 0;
//            string sqlQuery = $@"SELECT * FROM RantTypes;";

//            using (var connection = new SqliteConnection(Program.ConnectionString))
//            {
//                using (var command = new SqliteCommand(sqlQuery, connection))
//                {
//                    command.CommandTimeout = 10;
//                    connection.Open();

//                    var reader = command.ExecuteReader();

//                    using (ETHBotDBContext context = new ETHBotDBContext())
//                    {
//                        while (reader.Read())
//                        {
//                            int RantTypeId = reader.GetInt32(0);
//                            string Name = reader.GetString(1);

//                            context.RantTypes.Add(new ETHBot.DataLayer.Data.Discord.RantType()
//                            {
//                                RantTypeId = RantTypeId,
//                                Name = Name
//                            });
//                            count++;
//                        }
//                        context.SaveChanges();
//                    }
//                }
//            }
//            return count;
//        }

//        public int MigrateRantMessages()
//        {
//            int count = 0;
//            string sqlQuery = $@"SELECT * FROM RantMessages;";

//            using (var connection = new SqliteConnection(Program.ConnectionString))
//            {
//                using (var command = new SqliteCommand(sqlQuery, connection))
//                {
//                    command.CommandTimeout = 10;
//                    connection.Open();

//                    var reader = command.ExecuteReader();

//                    using (ETHBotDBContext context = new ETHBotDBContext())
//                    {
//                        while (reader.Read())
//                        {
//                            int RantMessageId = reader.GetInt32(0);
//                            int RantTypeId = reader.GetInt32(1);
//                            string Content = reader.GetString(2);
//                            ulong DiscordChannelId = Convert.ToUInt64(reader.GetString(3));
//                            ulong DiscordUserId = Convert.ToUInt64(reader.GetString(4));
//                            ulong DiscordMessageId = Convert.ToUInt64(reader.GetString(5));

//                            context.RantMessages.Add(new ETHBot.DataLayer.Data.Discord.RantMessage()
//                            {
//                                RantMessageId = RantMessageId,
//                                RantTypeId = RantTypeId,
//                                Content = Content,
//                                DiscordChannelId = DiscordChannelId,
//                                DiscordUserId = DiscordUserId,
//                                DiscordMessageId = DiscordMessageId
//                            });
//                            count++;
//                        }
//                        context.SaveChanges();
//                    }
//                }
//            }

//            return count;
//        }

//        public int MigrateSavedMessages()
//        {
//            int count = 0;
//            string sqlQuery = $@"SELECT * FROM SavedMessages;";

//            using (var connection = new SqliteConnection(Program.ConnectionString))
//            {
//                using (var command = new SqliteCommand(sqlQuery, connection))
//                {
//                    command.CommandTimeout = 10;
//                    connection.Open();

//                    var reader = command.ExecuteReader();

//                    using (ETHBotDBContext context = new ETHBotDBContext())
//                    {
//                        while (reader.Read())
//                        {
//                            int SavedMessageId = reader.GetInt32(0);
//                            ulong DiscordMessageId = Convert.ToUInt64(reader.GetString(1));
//                            string DirectLink = reader.GetString(2);
//                            string Content = reader.GetString(3);
//                            bool SendInDM = reader.GetBoolean(4);
//                            ulong SavedByDiscordUserId = Convert.ToUInt64(reader.GetString(5));
//                            ulong ByDiscordUserId = Convert.ToUInt64(reader.GetString(6));

//                            context.SavedMessages.Add(new ETHBot.DataLayer.Data.Discord.SavedMessage()
//                            {
//                                SavedMessageId = SavedMessageId,
//                                DiscordMessageId = DiscordMessageId,
//                                DirectLink = DirectLink,
//                                Content = Content,
//                                SavedByDiscordUserId = SavedByDiscordUserId,
//                                ByDiscordUserId = ByDiscordUserId
//                            });
//                            count++;
//                        }
//                        context.SaveChanges();
//                    }
//                }
//            }

//            return count;
//        }

//        public int MigratePlaceBoardPerformanceInfos()
//        {
//            int count = 0;
//            string sqlQuery = $@"SELECT * FROM PlacePerformanceInfos;";

//            using (var connection = new SqliteConnection(Program.ConnectionString))
//            {
//                using (var command = new SqliteCommand(sqlQuery, connection))
//                {
//                    command.CommandTimeout = 10;
//                    connection.Open();

//                    var reader = command.ExecuteReader();

//                    using (ETHBotDBContext context = new ETHBotDBContext())
//                    {
//                        while (reader.Read())
//                        {
//                            int PlacePerformanceHistoryId = reader.GetInt32(0);
//                            DateTime DateTime = reader.GetDateTime(1);
//                            int SuccessCount = reader.GetInt32(2);
//                            //int FailedCount = reader.GetString(3);
//                            int AvgTimeInMs = reader.GetInt32(3);

//                            context.PlacePerformanceInfos.Add(new ETHBot.DataLayer.Data.Fun.PlacePerformanceInfo()
//                            {
//                                PlacePerformanceHistoryId = PlacePerformanceHistoryId,
//                                DateTime = DateTime,
//                                SuccessCount = SuccessCount,
//                                FailedCount = 0,
//                                AvgTimeInMs = AvgTimeInMs
//                            });
//                            count++;
//                        }
//                        context.SaveChanges();
//                    }
//                }
//            }

//            return count;
//        }

//        public int MigratePlaceBoardPixels(SocketTextChannel textChannel)
//        {
//            int count = 0;
//            string sqlQuery = $@"SELECT * FROM PlaceBoardPixels;";

//            using (var connection = new SqliteConnection(Program.ConnectionString))
//            {
//                using (var command = new SqliteCommand(sqlQuery, connection))
//                {
//                    command.CommandTimeout = 10;
//                    connection.Open();


//                    var reader = command.ExecuteReader();

//                    using (ETHBotDBContext context = new ETHBotDBContext())
//                    {
//                        context.ChangeTracker.AutoDetectChangesEnabled = false;
//                        context.ChangeTracker.QueryTrackingBehavior = Microsoft.EntityFrameworkCore.QueryTrackingBehavior.NoTracking;

//                        while (reader.Read())
//                        {
//                            short XPos = reader.GetInt16(0);
//                            short YPos = reader.GetInt16(1);
//                            byte R = reader.GetByte(2);
//                            byte G = reader.GetByte(3);
//                            byte B = reader.GetByte(4);

//                            context.PlaceBoardPixels.Add(new ETHBot.DataLayer.Data.Fun.PlaceBoardPixel()
//                            {
//                                XPos = XPos,
//                                YPos = YPos,
//                                R = R,
//                                G = G,
//                                B = B
//                            });
//                            count++;

//                            if (count % 200_000 == 0)
//                            {
//                                context.SaveChanges();
//                                textChannel.SendMessageAsync($"Step {count.ToString("N0")} in MigratePlaceBoardPixels");
//                            }
//                        }
//                        context.SaveChanges();
//                    }
//                }
//            }
//            return count;
//        }

//        // this is to reduce the id size from 8 bytes down to 1 byte
//        private Dictionary<ulong, byte> GetPlaceUserIds()
//        {
//            var returnVal = new Dictionary<ulong, byte>();

//            var sqlQuery = $@"
//SELECT DISTINCT DiscordUserId
//FROM PlaceBoardHistory 
//ORDER BY PlaceBoardHistoryId ASC";

//            using (var connection = new SqliteConnection(Program.ConnectionString))
//            {
//                using (var command = new SqliteCommand(sqlQuery, connection))
//                {
//                    command.CommandTimeout = 10;
//                    connection.Open();

//                    var reader = command.ExecuteReader();
//                    byte count = 1;

//                    while (reader.Read())
//                    {
//                        ulong userId = Convert.ToUInt64(reader.GetString(0));

//                        returnVal.Add(userId, count);
//                        count++;
//                    }
//                }
//            }

//            return returnVal;
//        }

//        public int MigratePlaceBoardDiscordUsers()
//        {
//            int count = 0;

//            using (ETHBotDBContext context = new ETHBotDBContext())
//            {
//                foreach (var user in GetPlaceUserIds())
//                {
//                    context.PlaceDiscordUsers.Add(new ETHBot.DataLayer.Data.Fun.PlaceDiscordUser()
//                    {
//                        PlaceDiscordUserId = user.Value,
//                        DiscordUserId = user.Key,
//                        TotalPixelsPlaced = 0
//                    });
//                    count++;
//                }
//                context.SaveChanges();

//            }
//            return count;
//        }

//        private long PlaceBoardCount()
//        {
//            int count = 0;
//            string sqlQuery = $@"SELECT COUNT(*) FROM PlaceBoardHistory;";

//            using (var connection = new SqliteConnection(Program.ConnectionString))
//            {
//                using (var command = new SqliteCommand(sqlQuery, connection))
//                {
//                    command.CommandTimeout = 10;
//                    connection.Open();

//                    return (long)command.ExecuteScalar();
//                }
//            }
//        }



//        public int MigratePlaceBoardPixelHistory(SocketTextChannel textChannel)
//        {
//            long totalCount = PlaceBoardCount();
//            int count = 0;

//            int pageSize = 100_000;

//            List<PlaceDiscordUser> users;
//            Dictionary<ulong, short> UserIds = new Dictionary<ulong, short>();
//            using (ETHBotDBContext context = new ETHBotDBContext())
//            {
//                users = context.PlaceDiscordUsers.ToList();

//                foreach (var item in users)
//                    UserIds.Add(item.DiscordUserId, item.PlaceDiscordUserId);
//            }


//            for (int i = 0; i < totalCount; i += pageSize)
//            {
//                using (ETHBotDBContext context = new ETHBotDBContext())
//                {
//                    context.ChangeTracker.AutoDetectChangesEnabled = false;
//                    context.ChangeTracker.QueryTrackingBehavior = Microsoft.EntityFrameworkCore.QueryTrackingBehavior.NoTracking;

//                    var data = new List<ETHBot.DataLayer.Data.Fun.PlaceBoardHistory>();

//                    string sqlQuery = $@"SELECT * FROM PlaceBoardHistory LIMIT {pageSize} OFFSET {i};";

//                    using (var connection = new SqliteConnection(Program.ConnectionString))
//                    {
//                        using (var command = new SqliteCommand(sqlQuery, connection))
//                        {
//                            command.CommandTimeout = 10;
//                            connection.Open();

//                            var reader = command.ExecuteReader();
//                            while (reader.Read())
//                            {
//                                int PlaceBoardHistoryId = reader.GetInt32(0);
//                                short XPos = reader.GetInt16(1);
//                                short YPos = reader.GetInt16(2);
//                                byte R = reader.GetByte(3);
//                                byte G = reader.GetByte(4);
//                                byte B = reader.GetByte(5);
//                                ulong userId = Convert.ToUInt64(reader.GetString(6));
//                                ulong SnowflakeTimePlaced = Convert.ToUInt64(reader.GetString(7));
//                                bool Removed = reader.GetBoolean(8);

//                                data.Add(new ETHBot.DataLayer.Data.Fun.PlaceBoardHistory()
//                                {
//                                    XPos = XPos,
//                                    YPos = YPos,
//                                    R = R,
//                                    G = G,
//                                    B = B,
//                                    PlaceDiscordUserId = UserIds[userId],
//                                    Removed = Removed,
//                                    PlacedDateTime = SnowflakeUtils.FromSnowflake(SnowflakeTimePlaced).UtcDateTime
//                                });

//                                count++;
//                            }

//                            context.PlaceBoardHistory.AddRange(data);

//                            textChannel.SendMessageAsync($"Step {count.ToString("N0")} in MigratePlaceBoardPixelHistory");
//                            context.SaveChanges();

//                            foreach (var e in context.ChangeTracker.Entries())
//                            {
//                                e.State = EntityState.Detached;
//                            }
//                        }
//                    }
//                }
//            }

//            return count;
//        }

//        public int MigrateSubredditInfos()
//        {
//            int count = 0;
//            string sqlQuery = $@"SELECT * FROM SubredditInfos;";

//            using (var connection = new SqliteConnection(Program.ConnectionString))
//            {
//                using (var command = new SqliteCommand(sqlQuery, connection))
//                {
//                    command.CommandTimeout = 10;
//                    connection.Open();

//                    var reader = command.ExecuteReader();

//                    using (ETHBotDBContext context = new ETHBotDBContext())
//                    {
//                        while (reader.Read())
//                        {
//                            int SubredditId = reader.GetInt32(0);
//                            string SubredditName = reader.GetString(1);
//                            string SubredditDescription = reader.GetString(2);
//                            bool IsManuallyBanned = reader.GetBoolean(3);
//                            bool IsNSFW = reader.GetBoolean(4);
//                            string NewestPost = reader.GetString(5);
//                            DateTime NewestPostDate = reader.GetDateTime(6);
//                            string OldestPost = reader.GetString(7);
//                            DateTime OldestPostDate = reader.GetDateTime(8);
//                            bool IsScraping = reader.GetBoolean(9);
//                            bool ReachedOldest = reader.GetBoolean(10);

//                            context.SubredditInfos.Add(new ETHBot.DataLayer.Data.Reddit.SubredditInfo()
//                            {
//                                SubredditId = SubredditId,
//                                SubredditName = SubredditName,
//                                SubredditDescription = SubredditDescription,
//                                IsManuallyBanned = IsManuallyBanned,
//                                IsNSFW = IsNSFW,
//                                NewestPost = NewestPost,
//                                NewestPostDate = NewestPostDate,
//                                OldestPost = OldestPost,
//                                OldestPostDate = OldestPostDate,
//                                IsScraping = IsScraping,
//                                ReachedOldest = ReachedOldest
//                            });
//                            count++;
//                        }
//                        context.SaveChanges();
//                    }
//                }
//            }
//            return count;
//        }

//        public int MigrateRedditPosts(SocketTextChannel textChannel)
//        {
//            int count = 0;
//            string sqlQuery = $@"SELECT * FROM RedditPosts;";

//            using (var connection = new SqliteConnection(Program.ConnectionString))
//            {
//                using (var command = new SqliteCommand(sqlQuery, connection))
//                {
//                    command.CommandTimeout = 10;
//                    connection.Open();

//                    var reader = command.ExecuteReader();

//                    using (ETHBotDBContext context = new ETHBotDBContext())
//                    {
//                        context.ChangeTracker.AutoDetectChangesEnabled = false;
//                        context.ChangeTracker.QueryTrackingBehavior = Microsoft.EntityFrameworkCore.QueryTrackingBehavior.NoTracking;

//                        while (reader.Read())
//                        {
//                            int RedditPostId = reader.GetInt32(0);
//                            string PostTitle = reader.GetString(1);
//                            string PostId = reader.GetString(2);
//                            bool IsNSFW = reader.GetBoolean(3);
//                            DateTime PostedAt = reader.GetDateTime(4);
//                            string Author = reader.GetString(5);
//                            int UpvoteCount = reader.GetInt32(6);
//                            int DownvoteCount = reader.GetInt32(7);
//                            string Permalink = reader.GetString(8);
//                            string Url = reader.GetString(9);
//                            int SubredditInfoId = reader.GetInt32(10);
//                            string Content = reader.IsDBNull(11) ? null : reader.GetString(11);
//                            bool IsText = reader.GetBoolean(12);

//                            context.RedditPosts.Add(new ETHBot.DataLayer.Data.Reddit.RedditPost()
//                            {
//                                RedditPostId = RedditPostId,
//                                PostTitle = PostTitle,
//                                PostId = PostId,
//                                IsNSFW = IsNSFW,
//                                PostedAt = PostedAt,
//                                Author = Author,
//                                UpvoteCount = UpvoteCount,
//                                DownvoteCount = DownvoteCount,
//                                Permalink = Permalink,
//                                Url = Url.Substring(0, Math.Min(500, Url.Length)),
//                                IsText = IsText,
//                                Content = Content,
//                                SubredditInfoId = SubredditInfoId
//                            });
//                            count++;

//                            if (count % 100_000 == 0)
//                            {
//                                context.SaveChanges();
//                                textChannel.SendMessageAsync($"Step {count.ToString("N0")} in MigrateRedditPosts");
//                            }
//                        }
//                        context.SaveChanges();
//                    }
//                }
//            }
//            return count;
//        }

//        public int MigrateRedditImages(SocketTextChannel textChannel)
//        {
//            int count = 0;
//            string sqlQuery = $@"SELECT * FROM RedditImages;";

//            using (var connection = new SqliteConnection(Program.ConnectionString))
//            {
//                using (var command = new SqliteCommand(sqlQuery, connection))
//                {
//                    command.CommandTimeout = 10;
//                    connection.Open();

//                    var reader = command.ExecuteReader();

//                    using (ETHBotDBContext context = new ETHBotDBContext())
//                    {
//                        while (reader.Read())
//                        {
//                            int RedditImageId = reader.GetInt32(0);
//                            string Link = reader.GetString(1);
//                            string LocalPath = reader.IsDBNull(2) ? null : reader.GetString(2);
//                            bool Downloaded = reader.GetBoolean(3);
//                            bool IsNSFW = reader.GetBoolean(4);
//                            bool IsBlockedManually = reader.GetBoolean(5);
//                            int RedditPostId = reader.GetInt32(6);

//                            context.RedditImages.Add(new ETHBot.DataLayer.Data.Reddit.RedditImage()
//                            {
//                                RedditImageId = RedditImageId,
//                                Link = Link,
//                                LocalPath = LocalPath,
//                                Downloaded = Downloaded,
//                                IsNSFW = IsNSFW,
//                                IsBlockedManually = IsBlockedManually,
//                                RedditPostId = RedditPostId
//                            });
//                            count++;

//                            if (count % 100_000 == 0)
//                            {
//                                context.SaveChanges();
//                                textChannel.SendMessageAsync($"Step {count.ToString("N0")} in MigrateRedditPosts");
//                            }
//                        }
//                        context.SaveChanges();
//                    }
//                }
//            }
//            return count;
//        }

//        public int MigrateBotChannelSettings()
//        {
//            int count = 0;
//            string sqlQuery = $@"SELECT * FROM BotChannelSettings;";

//            using (var connection = new SqliteConnection(Program.ConnectionString))
//            {
//                using (var command = new SqliteCommand(sqlQuery, connection))
//                {
//                    command.CommandTimeout = 10;
//                    connection.Open();

//                    var reader = command.ExecuteReader();

//                    using (ETHBotDBContext context = new ETHBotDBContext())
//                    {
//                        while (reader.Read())
//                        {
//                            int BotChannelSettingId = reader.GetInt32(0);
//                            int ChannelPermissionFlags = reader.GetInt32(1);
//                            ulong DiscordChannelId = Convert.ToUInt64(reader.GetString(2));

//                            DateTimeOffset? OldestPostTimePreloaded = reader.IsDBNull(3) ? null : reader.GetDateTimeOffset(3);
//                            DateTimeOffset? NewestPostTimePreloaded = reader.IsDBNull(4) ? null : reader.GetDateTimeOffset(4);
//                            bool ReachedOldestPreload = reader.GetBoolean(5);


//                            context.BotChannelSettings.Add(new ETHBot.DataLayer.Data.BotChannelSetting()
//                            {
//                                BotChannelSettingId = BotChannelSettingId,
//                                ChannelPermissionFlags = ChannelPermissionFlags,
//                                DiscordChannelId = DiscordChannelId,
//                                OldestPostTimePreloaded = OldestPostTimePreloaded,
//                                NewestPostTimePreloaded = NewestPostTimePreloaded
//                            });
//                            count++;
//                        }
//                        context.SaveChanges();
//                    }
//                }
//            }
//            return count;
//        }
//    }
//}
