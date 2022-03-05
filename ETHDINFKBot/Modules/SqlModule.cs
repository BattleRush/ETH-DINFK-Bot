using Discord.Commands;
using ETHBot.DataLayer;
using ETHBot.DataLayer.Data.Enums;
using ETHDINFKBot.Helpers;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Diagnostics;
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

            try
            {
                Stopwatch watch = new Stopwatch();
                watch.Start();

                var queryResult = await SQLHelper.GetQueryResults(Context, $@"
SELECT table_name FROM information_schema.tables
WHERE table_schema = '{Program.ApplicationSetting.MariaDBName}' 
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
WHERE  TABLE_SCHEMA = '{Program.ApplicationSetting.MariaDBName}' and TABLE_NAME = '{tableName}'";

                    var rowCountInfo = await SQLHelper.GetQueryResults(Context, query, true, 1);

                    string rowCountStr = rowCountInfo.Data.FirstOrDefault().FirstOrDefault(); // todo rework this first first thing

                    // TODO could be done with one query
                    string tableSizeQuery = $@"SELECT
    ROUND((DATA_LENGTH + INDEX_LENGTH)) AS `Size`
FROM
  information_schema.TABLES
WHERE
  TABLE_SCHEMA = '{Program.ApplicationSetting.MariaDBName}' and TABLE_NAME = '{tableName}'";

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

                await Context.Channel.SendMessageAsync("", false, builder.Build());
            }
            catch (Exception ex)
            {
                // TODO user Logger
                Console.WriteLine(ex.ToString());
            }
        }

        [Group("stats")]
        public class SqlStats : ModuleBase<SocketCommandContext>
        {
            [Command("help")]
            public async Task SqlStatsHelp()
            {
                EmbedBuilder builder = new EmbedBuilder();

                builder.WithTitle($"{Program.Client.CurrentUser.Username} SQL Stats Help");

                builder.WithColor(0, 0, 255);

                builder.WithThumbnailUrl(Program.Client.CurrentUser.GetAvatarUrl());
                builder.WithCurrentTimestamp();
                builder.AddField($"{Program.CurrentPrefix}sql stats help", "This message :)");
                builder.AddField($"{Program.CurrentPrefix}sql stats user", "User Stats");
                builder.AddField($"{Program.CurrentPrefix}sql stats index", "Index Stats");
                builder.AddField($"{Program.CurrentPrefix}sql stats table", "Table Stats");

                await Context.Channel.SendMessageAsync("", false, builder.Build());
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
                    if (row[0] == Program.ApplicationSetting.MariaDBFullUserName)
                        row[0] = "FULL USER";
                    if (row[0] == Program.ApplicationSetting.MariaDBReadOnlyUserName)
                        row[0] = "READ-ONLY USER";
                }

                var resultString = SQLHelper.GetRowStringFromResult(queryResult.Header, queryResult.Data, new List<int>() { 0, 1, 6, 7, 9, 10, 11, 12, 13, 17, 18, 21, 24 }, true);

                await Context.Channel.SendMessageAsync(resultString + Environment.NewLine + $"{queryResult.TotalResults.ToString("N0")} Row(s) affected Time: {queryResult.Time.ToString("N0")}ms");
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

                await Context.Channel.SendMessageAsync(resultString + Environment.NewLine + $"{queryResult.TotalResults.ToString("N0")} Row(s) affected Time: {queryResult.Time.ToString("N0")}ms");
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

                await Context.Channel.SendMessageAsync(resultString + Environment.NewLine + $"{queryResult.TotalResults.ToString("N0")} Row(s) affected Time: {queryResult.Time.ToString("N0")}ms");
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

           /* [Command("help")]
            public async Task SqlTableHelp()
            {

            }*/





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
                                FromTableFieldName = reader.GetString(1),
                                ToTable = reader.GetString(2),
                                ToTableFieldName = reader.GetString(3),


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

                            string genetalType = "null";
                            string type = reader.GetString(1).ToLower();

                            switch (type)
                            {
                                case "tinyint(1)":
                                    genetalType = "bool";
                                    break;
                                case string int_1 when int_1.StartsWith("tinyint"):
                                case string int_2 when int_2.StartsWith("int"):
                                case string int_3 when int_3.StartsWith("bigint"):
                                    genetalType = "int";
                                    break;
                                case string string_1 when string_1.StartsWith("varchar"):
                                case string string_2 when string_2.StartsWith("longtext"):
                                    genetalType = "string";
                                    break;
                                case string dateTime when dateTime.StartsWith("datetime"):
                                    genetalType = "datetime";
                                    break;
                                default:
                                    break;
                            }

                            DBFieldInfo field = new DBFieldInfo()
                            {
                                //Id = reader.GetInt32(0),
                                Name = reader.GetString(0),
                                Type = reader.GetString(1),
                                GeneralType = genetalType,
                                Nullable = reader.GetString(2) == "YES",
                                // df value needed?
                                IsPrimaryKey = reader.GetString(3) == "PRI"

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
                    var dbInfos = await GetAllDBTableInfos();

                    // TODO dispose with using
                    DrawDbSchema drawDbSchema = new DrawDbSchema(dbInfos);
                    drawDbSchema.DrawAllTables();



                    var stream = CommonHelper.GetStream(drawDbSchema.Bitmap);
                    await Context.Channel.SendFileAsync(stream, "test.png");

                    drawDbSchema.Dispose();
                    stream.Dispose();

                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, ex.Message);
                    await Context.Channel.SendMessageAsync(ex.ToString());
                }
            }





            // todo maybe move to a seperate class
            private async Task<List<DBTableInfo>> GetAllDBTableInfos()
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


#if DEBUG
                tableList = new List<string>(); // Clear because on windows the capitalization of tables is different and currently that breaks some SQL Queries
#endif


                var queryResult = await SQLHelper.GetQueryResults(Context, $@"
SELECT table_name FROM information_schema.tables
WHERE table_schema = '{Program.ApplicationSetting.MariaDBName ?? "ETHBot"}' 
ORDER BY table_name DESC;", true, 50);

                // Add tables incase they arent in the list above for their correct order
                foreach (var item in queryResult.Data)
                {
                    string tableName = item.ElementAt(0);

                    bool found = false;
                    foreach (var table in tableList)
                    {
                        if (table.ToLower() == tableName.ToLower())
                        {
                            found = true;
                            break;
                        }
                    }

                    if (found)
                        continue;

                    tableList.Add(tableName);
                }

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
                            command.CommandText = $"SHOW COLUMNS FROM {item} FROM {Program.ApplicationSetting.MariaDBName ?? "ETHBot"}";
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
                            command.CommandText = $@"select
    c.table_name,
    c.column_name,
    c.referenced_table_name,
    c.referenced_column_name
  from information_schema.table_constraints fk
  join information_schema.key_column_usage c
    on c.constraint_name = fk.constraint_name
  where fk.constraint_type = 'FOREIGN KEY' AND c.TABLE_SCHEMA = '{Program.ApplicationSetting.MariaDBName ?? "ETHBot"}' AND c.table_name = '{item}'; ";
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
                                if (item2.Name == fk.FromTableFieldName && item.TableName.ToLower() == fk.FromTable.ToLower())
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
            if (Context.Message.Author.Id != Program.ApplicationSetting.Owner
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
            await Context.Channel.SendMessageAsync($"<@!{Program.ApplicationSetting.Owner}> someone tried to kill me with: {query.Substring(0, Math.Min(query.Length, 1500))}", false);
            if (query.Length > 1500)
                await Context.Channel.SendMessageAsync($"{query.Substring(1500, query.Length - 1500)}", false);
            //connect.Close();
            //command.Cancel();

            throw new TimeoutException("Time is over");
        }







        private bool ForbiddenQuery(string commandSql, ulong authorId)
        {
            if (CommonHelper.ContainsForbiddenQuery(commandSql) && authorId != Program.ApplicationSetting.Owner)
            {
                Context.Channel.SendMessageAsync("Dont you dare to think you will be allowed to use this command https://tenor.com/view/you-shall-not-pass-lord-of-the-ring-gif-5234772", false);
                return true;
            }
            return false;
        }


        private static Dictionary<ulong, DateTime> ActiveSQLCommands = new Dictionary<ulong, DateTime>();

        [Command("query", RunMode = RunMode.Async)]
        public async Task Sql([Remainder] string commandSql)
        {
            var userId = Context.Message.Author.Id;
            if (AllowedToRun(BotPermissionType.EnableType2Commands))
                return;

            if (ForbiddenQuery(commandSql, userId))
                return;

            if (ActiveSQLCommands.ContainsKey(userId) && ActiveSQLCommands[userId].AddSeconds(15) > DateTime.Now)
            {
                await Context.Channel.SendMessageAsync("Are you in such a hurry, that you cant wait out the last query you send out?", false);
                return;
            }

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
                if (ActiveSQLCommands.ContainsKey(userId))
                    ActiveSQLCommands[userId] = DateTime.Now;
                else
                    ActiveSQLCommands.Add(userId, DateTime.Now);

                var commandResponse = await SQLHelper.SqlCommand(Context, commandSql);
                await Context.Channel.SendMessageAsync(commandResponse, false);

                // release the user again as the query finished
                ActiveSQLCommands[userId] = DateTime.MinValue;

            }
            catch (Exception ex)
            {
                Context.Channel.SendMessageAsync("Is this all you got <:kekw:768912035928735775> " + ex.ToString(), false);
            }
        }

        [Command("queryd", RunMode = RunMode.Async)] // better name xD
        public async Task SqlD([Remainder] string commandSql)
        {
            var userId = Context.Message.Author.Id;

            if (AllowedToRun(BotPermissionType.EnableType2Commands))
                return;

            if (ForbiddenQuery(commandSql, Context.Message.Author.Id))
                return;

            if (ActiveSQLCommands.ContainsKey(userId) && ActiveSQLCommands[userId].AddSeconds(15) > DateTime.Now)
            {
                await Context.Channel.SendMessageAsync("Are you in such a hurry, that you cant wait out the last query you send out?", false);
                return;
            }

            try
            {
                if (ActiveSQLCommands.ContainsKey(userId))
                    ActiveSQLCommands[userId] = DateTime.Now;
                else
                    ActiveSQLCommands.Add(userId, DateTime.Now);

                var queryResult = await SQLHelper.GetQueryResults(Context, commandSql, true, 100);
                string additionalString = $"Total row(s) affected: {queryResult.TotalResults.ToString("N0")} QueryTime: {queryResult.Time.ToString("N0")}ms";


                var drawTable = new DrawTable(queryResult.Header, queryResult.Data, additionalString, null);

                var stream = await drawTable.GetImage();
                if (stream == null)
                    return;// todo some message

                await Context.Channel.SendFileAsync(stream, "graph.png", "", false, null, null, false, null, new Discord.MessageReference(Context.Message.Id));
                stream.Dispose();

                // release the user again as the query finished
                ActiveSQLCommands[userId] = DateTime.MinValue;
            }
            catch (Exception ex)
            {
                await Context.Channel.SendMessageAsync("Is this all you got <:kekw:768912035928735775> " + ex.ToString(), false);
            }
        }
    }
}
