using Discord.Commands;
using ETHBot.DataLayer;
using ETHBot.DataLayer.Data.Enums;
using ETHDINFKBot.Helpers;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Linq;
using Discord;
using ETHDINFKBot.Drawing;
using MySqlConnector;

namespace ETHDINFKBot.Modules
{
    [Group("sql")]
    public class SqlModule : ModuleBase<SocketCommandContext>
    {

        [Command("info")]
        public async Task TableInfo()
        {
            string prefix = Program.CurrentPrefix;

            Stopwatch watch = new Stopwatch();
            watch.Start();
            var queryResult = await SQLHelper.GetQueryResults(Context, @"
SELECT table_name FROM information_schema.tables
WHERE table_schema = 'ethbot_dev' 
ORDER BY table_name DESC;", true, 50);

            EmbedBuilder builder = new EmbedBuilder();

            builder.WithTitle($"{Program.Client.CurrentUser.Username} DB INFO");
            builder.WithDescription($@"SQL Tables 
DB Diagram: '{prefix}sql table info' 
DB Stats Help: '{prefix}sql stats help'");
            builder.WithColor(65, 17, 187);

            builder.WithThumbnailUrl(Program.Client.CurrentUser.GetAvatarUrl());
            builder.WithCurrentTimestamp();

            long totalRows = 0;

            List<string> largeTables = new List<string>()
            {
                "RedditPosts",
                "RedditImages",
                "PlaceBoardPixels",
                "PlaceBoardHistory",
                "DiscordEmoteHistory"
            };

            // TODO check if db name is needed

            long dbSizeInBytes = 0;

            string rowCountString = "";
            foreach (var row in queryResult.Data)
            {
                string tableName = row.ElementAt(0);

                string query = $"SELECT COUNT(*) FROM {tableName}";

                if (largeTables.Contains(tableName))
                    query = $@"SELECT AUTO_INCREMENT
FROM   information_schema.TABLES
WHERE  TABLE_SCHEMA = 'ethbot_dev' and TABLE_NAME = '{tableName}'";

                var rowCountInfo = await SQLHelper.GetQueryResults(Context, query, true, 1);

                string rowCountStr = rowCountInfo.Data.FirstOrDefault().FirstOrDefault(); // todo rework this first first thing

                // TODO could be done with one query
                string tableSizeQuery = $@"SELECT
    ROUND((DATA_LENGTH + INDEX_LENGTH)) AS `Size`
FROM
  information_schema.TABLES
WHERE
  TABLE_SCHEMA = 'ethbot_dev' and TABLE_NAME = '{tableName}'";

                var tableSize = await SQLHelper.GetQueryResults(Context, tableSizeQuery, true, 1);
                var sizeInBytesStr = tableSize.Data.FirstOrDefault()?.FirstOrDefault();

                if (long.TryParse(rowCountStr, out long rowCount) && long.TryParse(sizeInBytesStr, out long sizeInBytes))
                {
                    totalRows += rowCount;
                    rowCountString += $"{tableName} ({rowCount:N0}) {Math.Round(sizeInBytes / 1024d / 1024d, 2)} MB" + Environment.NewLine;
                    dbSizeInBytes += sizeInBytes;
                }
            }

            builder.AddField("Row count", rowCountString);

            //var dbSizeInfo = await GetQueryResults($"SELECT page_count *page_size / 1024 / 1024 as size FROM pragma_page_count(), pragma_page_size()", true, 1);
            //string dbSizeStr = dbSizeInfo.Data.FirstOrDefault().FirstOrDefault(); // todo rework this first first thing


            watch.Stop();
            builder.AddField("Total", $"Rows: {totalRows.ToString("N0")} {Environment.NewLine}" +
                $"DB Size: {Math.Round(dbSizeInBytes / 1024d / 1024d / 1024d, 2)} GB {Environment.NewLine}" +
                $"Query time: {watch.ElapsedMilliseconds.ToString("N0")}ms");

            Context.Channel.SendMessageAsync("", false, builder.Build());
        }


        [Group("stats")]
        public class SqlStats : ModuleBase<SocketCommandContext>
        {
            [Command("help")]
            public async Task SqlStatsHelp()
            {
                string prefix = ".";

#if DEBUG
                prefix = "dev.";
#endif

                EmbedBuilder builder = new EmbedBuilder();

                builder.WithTitle($"{Program.Client.CurrentUser.Username} SQL Stats Help");

                builder.WithColor(0, 0, 255);

                builder.WithThumbnailUrl(Program.Client.CurrentUser.GetAvatarUrl());
                builder.WithCurrentTimestamp();
                builder.AddField($"{prefix}sql stats help", "This message :)");
                builder.AddField($"{prefix}sql stats user", "User Stats");
                builder.AddField($"{prefix}sql stats index", "Index Stats");
                builder.AddField($"{prefix}sql stats table", "Table Stats");

                Context.Channel.SendMessageAsync("", false, builder.Build());
            }

            [Command("user")]
            public async Task SqlUserStats()
            {
                var queryResult = await SQLHelper.GetQueryResults(Context, @"SHOW USER_STATISTICS WHERE USER <> 'root'", true, 50);


                EmbedBuilder builder = new EmbedBuilder();

                builder.WithTitle($"{Program.Client.CurrentUser.Username} SQL Table Stats");
                builder.WithDescription(@"SQL Table Stats");
                builder.WithColor(65, 17, 187);

                builder.WithThumbnailUrl(Program.Client.CurrentUser.GetAvatarUrl());
                builder.WithCurrentTimestamp();

                foreach (var row in queryResult.Data)
                {
                    if (row[0] == Program.MariaDBFullUserName)
                        row[0] = "FULL USER";
                    if (row[0] == Program.MariaDBReadOnlyUserName)
                        row[0] = "READ-ONLY USER";
                }

                var resultString = SQLHelper.GetRowStringFromResult(queryResult.Header, queryResult.Data, new List<int>() { 0, 1, 6, 7, 9, 10, 11, 12, 13, 17, 18, 21, 24 }, true);

                Context.Channel.SendMessageAsync(resultString + Environment.NewLine + $"{queryResult.TotalResults.ToString("N0")} Row(s) affected Time: {queryResult.Time.ToString("N0")}ms");
            }

            [Command("index")]
            public async Task SqlIndexStats()
            {
                var queryResult = await SQLHelper.GetQueryResults(Context, @"SHOW INDEX_STATISTICS", true, 50);


                EmbedBuilder builder = new EmbedBuilder();

                builder.WithTitle($"{Program.Client.CurrentUser.Username} SQL Table Stats");
                builder.WithDescription(@"SQL Table Stats");
                builder.WithColor(65, 17, 187);

                builder.WithThumbnailUrl(Program.Client.CurrentUser.GetAvatarUrl());
                builder.WithCurrentTimestamp();


                var resultString = SQLHelper.GetRowStringFromResult(queryResult.Header, queryResult.Data, new List<int>() { 1, 2, 3 }, true);

                Context.Channel.SendMessageAsync(resultString + Environment.NewLine + $"{queryResult.TotalResults.ToString("N0")} Row(s) affected Time: {queryResult.Time.ToString("N0")}ms");
            }

            [Command("table")]
            public async Task SqlTableStats()
            {
                var queryResult = await SQLHelper.GetQueryResults(Context, @"SHOW TABLE_STATISTICS", true, 50);


                EmbedBuilder builder = new EmbedBuilder();

                builder.WithTitle($"{Program.Client.CurrentUser.Username} SQL Table Stats");
                builder.WithDescription(@"SQL Table Stats");
                builder.WithColor(65, 17, 187);

                builder.WithThumbnailUrl(Program.Client.CurrentUser.GetAvatarUrl());
                builder.WithCurrentTimestamp();


                var resultString = SQLHelper.GetRowStringFromResult(queryResult.Header, queryResult.Data, new List<int>() { 1, 2, 3, 4 }, true);

                Context.Channel.SendMessageAsync(resultString + Environment.NewLine + $"{queryResult.TotalResults.ToString("N0")} Row(s) affected Time: {queryResult.Time.ToString("N0")}ms");
            }
        }

        [Group("table")]
        public class SqlTableModule : ModuleBase<SocketCommandContext>
        {

            private readonly ILogger _logger = new Logger<DiscordModule>(Program.Logger);
            // help


            // list

            // into <table>

            // query <type> (select, insert, update)

            // draw image?

            [Command("help")]
            public async Task SqlTableHelp()
            {

            }





            private List<ForeignKeyInfo> GetForeignKeyInfo(DbCommand command, string tableName)
            {
                var list = new List<ForeignKeyInfo>();

                using (var reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        try
                        {
                            ForeignKeyInfo info = new ForeignKeyInfo()
                            {
                                FromTable = tableName, //reader.GetString(2),
                                FromTableFieldName = reader.GetString(3),
                                ToTable = reader.GetString(2),
                                ToTableFieldName = reader.GetString(4),


                            };
                            //0   0   DiscordServers DiscordServerId DiscordServerId NO ACTION CASCADE NONE

                            list.Add(info);

                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, ex.Message);
                        }

                    }
                }

                return list;
            }

            private DBTableInfo GetTableInfo(DbCommand command, string tableName)
            {
                DBTableInfo dbTableInfo = new DBTableInfo()
                {
                    TableName = tableName,
                    FieldInfos = new List<DBFieldInfo>()
                };

                using (var reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        try
                        {
                            DBFieldInfo field = new DBFieldInfo()
                            {
                                Id = reader.GetInt32(0),
                                Name = reader.GetString(1),
                                Type = reader.GetString(2),
                                Nullable = !reader.GetBoolean(3),
                                // df value needed?
                                IsPrimaryKey = reader.GetBoolean(5)

                            };


                            dbTableInfo.FieldInfos.Add(field);

                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, ex.Message);
                        }

                    }
                }

                return dbTableInfo;
            }




            [Command("info")]
            public async Task TableInfoTables()
            {
                try
                {
                    var dbInfos = GetAllDBTableInfos();

                    // TODO dispose with using
                    DrawDbSchema drawDbSchema = new DrawDbSchema(dbInfos);
                    drawDbSchema.DrawAllTables();



                    var stream = CommonHelper.GetStream(drawDbSchema.Bitmap);
                    Context.Channel.SendFileAsync(stream, "test.png");

                    drawDbSchema.Dispose();
                    stream.Dispose();

                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, ex.Message);
                }
            }





            // todo maybe move to a seperate class
            private List<DBTableInfo> GetAllDBTableInfos()
            {
                List<string> tableList = new List<string>()
                {
                    "CommandTypes",
                    "CommandStatistics",
                    "DiscordMessages",
                    "DiscordChannels",
                    "DiscordServers",
                    "BannedLinks",

                    "PingStatistics",
                    "DiscordUsers",
                    "SavedMessages",
                    "RantMessages",
                    "RantTypes",
                    "BotChannelSettings",

                    "SubredditInfos",
                    "RedditPosts",
                    "RedditImages",

                    "DiscordEmoteHistory",
                    "DiscordEmotes",
                    "DiscordEmoteStatistics",

                    "PlaceBoardPixels",
                    "PlaceBoardHistory",
                    "PingHistory",
                    "DiscordRoles",
                    "BotSetting",

                    "PlacePerformanceInfos",
                    "BotStartUpTimes",
                    "__EFMigrationsHistory"
                };


                //;$"PRAGMA table_info('{item}')";
                //PRAGMA foreign_key_list('DiscordChannels');

                string text = "";
                string header = "";

                List<DBTableInfo> DbTableInfos = new List<DBTableInfo>();
                List<List<ForeignKeyInfo>> ForeignKeyInfos = new List<List<ForeignKeyInfo>>();

                using (var context = new ETHBotDBContext())
                {
                    foreach (var item in tableList)
                    {
                        using (var command = context.Database.GetDbConnection().CreateCommand())
                        {
                            command.CommandText = $"PRAGMA table_info('{item}')";
                            context.Database.OpenConnection();
                            if (item == "EmojiStatistics")
                            {
                                //TODO workaround until graphs drawing is done
                                //DbTableInfos.Add(new DBTableInfo());
                            }
                            DbTableInfos.Add(GetTableInfo(command, item));
                        }

                        using (var command = context.Database.GetDbConnection().CreateCommand())
                        {
                            command.CommandText = $"PRAGMA foreign_key_list('{item}')";
                            context.Database.OpenConnection();

                            ForeignKeyInfos.Add(GetForeignKeyInfo(command, item));
                        }
                    }
                }







                foreach (var item in DbTableInfos)
                {
                    text += "**" + item.TableName + "**" + Environment.NewLine;

                    if (item.FieldInfos == null)
                        item.FieldInfos = new List<DBFieldInfo>();

                    foreach (var item2 in item.FieldInfos)
                    {

                        // TODO cleanup
                        foreach (var ForeignKeyInfo in ForeignKeyInfos)
                        {
                            foreach (var fk in ForeignKeyInfo)
                            {
                                if (item2.Name == fk.FromTableFieldName && item.TableName == fk.FromTable)
                                {
                                    item2.IsForeignKey = true;
                                    item2.ForeignKeyInfo = fk;
                                }
                            }
                        }

                        string isPkFk = "";

                        if (item2.IsPrimaryKey)
                            isPkFk = "PK";
                        else if (item2.IsForeignKey)
                            isPkFk = $"FK to {item2.ForeignKeyInfo.ToTable}";



                        text += $"    {item2.Name} ({item2.Type}) {isPkFk} {(item2.Nullable ? "NULLABLE" : "NOT NULLABLE")}" + Environment.NewLine;
                    }

                    text += Environment.NewLine;

                    if (text.Length > 1500)
                    {
                        //Context.Channel.SendMessageAsync(text, false);
                        text = "";
                    }

                }
                return DbTableInfos;

            }
        }







        // TODO DUPLICATE REMOVE
        private bool AllowedToRun(BotPermissionType type)
        {
            var channelSettings = DatabaseManager.Instance().GetChannelSetting(Context.Message.Channel.Id);
            if (Context.Message.Author.Id != Program.Owner
                && !((BotPermissionType)channelSettings?.ChannelPermissionFlags).HasFlag(type))
            {
#if DEBUG
                Context.Channel.SendMessageAsync("blocked by perms", false);
#endif
                return true;
            }

            return false;
        }

        public async void WorkaroundForTimeoutNotWorking(CancellationTokenSource cts, string query, bool owner)
        {
            // normal 5 sec; owner 60 sec
            await Task.Delay(7000 * (owner ? 12 : 1));

            if (cts.IsCancellationRequested)
                return;

            await Context.Channel.SendMessageAsync("<:pepegun:747783377716904008>", false);
            await Context.Channel.SendMessageAsync($"<@!{Program.Owner}> someone tried to kill me with: {query.Substring(0, Math.Min(query.Length, 1500))}", false);
            if (query.Length > 1500)
                await Context.Channel.SendMessageAsync($"{query.Substring(1500, query.Length - 1500)}", false);
            //connect.Close();
            //command.Cancel();

            throw new TimeoutException("Time is over");
        }

        





        private bool ForbiddenQuery(string commandSql, ulong authorId)
        {
            if (CommonHelper.ContainsForbiddenQuery(commandSql) && authorId != ETHDINFKBot.Program.Owner)
            {
                Context.Channel.SendMessageAsync("Dont you dare to think you will be allowed to use this command https://tenor.com/view/you-shall-not-pass-lord-of-the-ring-gif-5234772", false);
                return true;
            }
            return false;
        }

        [Command("query")]
        public async Task Sql([Remainder] string commandSql)
        {
            if (AllowedToRun(BotPermissionType.EnableType2Commands))
                return;

            if (ForbiddenQuery(commandSql, Context.Message.Author.Id))
                return;

            try
            {
                /*
                CancellationTokenSource cts = new CancellationTokenSource();
                CancellationToken token = cts.Token;

                object commandResponse = null;
                Thread thread = new Thread(() => {
                    //Some work...
                    commandResponse = SqlCommand(commandSql);
                });
                thread.Start();
                thread.Join(3000);
                thread.int();

                */
                var commandResponse = await SQLHelper.SqlCommand(Context, commandSql);
                await Context.Channel.SendMessageAsync(commandResponse, false);

            }
            catch (Exception ex)
            {
                Context.Channel.SendMessageAsync("Is this all you got <:kekw:768912035928735775> " + ex.ToString(), false);
            }
        }

        [Command("queryd")] // better name xD
        public async Task SqlD([Remainder] string commandSql)
        {
            if (AllowedToRun(BotPermissionType.EnableType2Commands))
                return;

            if (ForbiddenQuery(commandSql, Context.Message.Author.Id))
                return;

            try
            {
                var queryResult = await SQLHelper.GetQueryResults(Context, commandSql, true, 50);
                string additionalString = $"Total row(s) affected: {queryResult.TotalResults.ToString("N0")} QueryTime: {queryResult.Time.ToString("N0")}ms";

                var drawTable = new DrawTable(queryResult.Header, queryResult.Data, additionalString);

                var stream = await drawTable.GetImage();
                if (stream == null)
                    return;// todo some message

                await Context.Channel.SendFileAsync(stream, "graph.png", "", false, null, null, false, null, new Discord.MessageReference(Context.Message.Id));
                stream.Dispose();
            }
            catch (Exception ex)
            {
                Context.Channel.SendMessageAsync("Is this all you got <:kekw:768912035928735775> " + ex.ToString(), false);
            }
        }
    }
}
