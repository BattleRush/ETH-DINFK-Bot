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

namespace ETHDINFKBot.Modules
{
    [Group("sql")]
    public class SqlModule : ModuleBase<SocketCommandContext>
    {

        [Command("info")]
        public async Task TableInfo()
        {
            Stopwatch watch = new Stopwatch();
            watch.Start();
            var queryResult = await GetQueryResults(@"
SELECT 
    name 
FROM sqlite_master 
WHERE type ='table' AND name NOT LIKE 'sqlite%' 
ORDER BY name DESC;", true, 50);

            EmbedBuilder builder = new EmbedBuilder();

            builder.WithTitle("BattleRush's Helper Help");
            builder.WithDescription(@"SQL Tables 
To get the diagram type: '.sql table info'");
            builder.WithColor(65, 17, 187);

            builder.WithThumbnailUrl("https://cdn.discordapp.com/avatars/774276700557148170/62279315dd469126ca4e5ab89a5e802a.png");
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

            string rowCountString = "";
            foreach (var row in queryResult.Data)
            {
                string tableName = row.ElementAt(0);

                string query = $"SELECT COUNT(*) FROM {tableName}";

                if (largeTables.Contains(tableName))
                    query = $@"SELECT MAX(_ROWID_) FROM ""{tableName}"" LIMIT 1;";

                var rowCountInfo = await GetQueryResults(query, true, 1);

                string rowCountStr = rowCountInfo.Data.FirstOrDefault().FirstOrDefault(); // todo rework this first first thing

                if (long.TryParse(rowCountStr, out long rowCount))
                {
                    totalRows += rowCount;
                    rowCountString += $"{tableName} ({rowCount:N0})" + Environment.NewLine;
                }
            }

            builder.AddField("Row count", rowCountString);

            var dbSizeInfo = await GetQueryResults($"SELECT page_count *page_size / 1024 / 1024 as size FROM pragma_page_count(), pragma_page_size()", true, 1);
            string dbSizeStr = dbSizeInfo.Data.FirstOrDefault().FirstOrDefault(); // todo rework this first first thing

            if (int.TryParse(dbSizeStr, out int dbSize))
            {
                watch.Stop();
                builder.AddField("Total", $"Rows: {totalRows.ToString("N0")} {Environment.NewLine}" +
                    $"DB Size: {Math.Round(dbSize/1024d, 2)} GB {Environment.NewLine}" + 
                    $"Query time: {watch.ElapsedMilliseconds.ToString("N0")}ms");
            }

            Context.Channel.SendMessageAsync("", false, builder.Build());
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

        private string GetRowStringFromResult(List<string> header, List<List<string>> data)
        {
            string result = "";

            result += $"**{string.Join("\t", header)}**" + Environment.NewLine;

            if (data.Count > 0)
            {
                result += "```";
                foreach (var row in data)
                {
                    string rowString = string.Join("\t", row);

                    // escape string
                    rowString = rowString.Replace("`", "");

                    result += rowString + Environment.NewLine;

                    if (result.Length > 2000)
                        break;
                }
                result = result.Substring(0, Math.Min(result.Length, 1900));
                result += "```";
            }
            else
            {
                result += "No row(s) returned";
            }

            return result;
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
                var commandResponse = await SqlCommand(commandSql);
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
                var queryResult = await GetQueryResults(commandSql, true, 50);
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

        private async Task<(List<string> Header, List<List<string>> Data, int TotalResults, long Time)> GetQueryResults(string commandSql, bool limitRows = false, int limitLength = 2000)
        {
            var author = Context.Message.Author;

            List<string> Header = new List<string>();
            List<List<string>> Data = new List<List<string>>();
            int TotalResults = 0;

            long Time = -1;

            int currentContentLength = 0;
            int currentRowCount = 0;


            CancellationTokenSource cts = new CancellationTokenSource();

            try
            {
                Stopwatch watch = new Stopwatch();
                watch.Start();

                using (var connection = new SqliteConnection(Program.ConnectionString))
                {
                    connection.DefaultTimeout = 1;
                    using (var command = new SqliteCommand(commandSql, connection))
                    {
                        command.CommandTimeout = 1;

                        connection.Open();

                        //WorkaroundForTimeoutNotWorking(cts, commandSql, author.Id == ETHDINFKBot.Program.Owner);

                        var reader = await command.ExecuteReaderAsync();
                        while (reader.Read())
                        {
                            if (Header.Count == 0)
                            {
                                for (int i = 0; i < reader.FieldCount; i++)
                                {
                                    string fieldName = reader.GetName(i)?.ToString();
                                    Header.Add(fieldName);
                                    currentContentLength += fieldName.Length;
                                }
                            }

                            if (limitRows && currentRowCount <= limitLength || !limitRows && currentContentLength <= limitLength)
                            {
                                List<string> row = new List<string>();

                                for (int i = 0; i < reader.FieldCount; i++)
                                {
                                    try
                                    {
                                        var type = reader.GetFieldType(i)?.FullName;
                                        var fieldString = "null";

                                        if (DBNull.Value.Equals(reader.GetValue(i)))
                                        {
                                            currentContentLength += fieldString.Length;
                                            row.Add(fieldString);
                                            continue;
                                        }

                                        switch (type)
                                        {
                                            case "System.Int64":
                                                fieldString = reader.GetInt64(i).ToString();
                                                break;

                                            case "System.String":
                                                fieldString = reader.GetValue(i).ToString()?.Replace("`", "");
                                                break;

                                            default:
                                                fieldString = $"{type} is unknown";
                                                break;
                                        }

                                        currentContentLength += fieldString.Length;
                                        row.Add(fieldString);
                                    }
                                    catch (Exception ex)
                                    {
                                        //currentContentLength = fieldName.Length;
                                        //row.Add(ex.ToString());
                                        throw ex;
                                    }

                                }

                                currentRowCount++;
                                Data.Add(row);
                            }
                            else
                            {
                                //break;// we dont need to look further _> we still need to count
                            }

                            TotalResults++;
                        }
                    }
                }

                cts.Cancel();
                watch.Stop();

                Time = watch.ElapsedMilliseconds;
            }
            catch (Exception ex)
            {
                cts.Cancel();
                await Context.Channel.SendMessageAsync("Error: " + ex.Message, false);
            }

            return (Header, Data, TotalResults, Time);
        }

        private async Task<string> SqlCommand(string commandSql)
        {
            var author = Context.Message.Author;

            var queryResult = await GetQueryResults(commandSql.ToString(), false, 2000);
            var resultString = GetRowStringFromResult(queryResult.Header, queryResult.Data);

            return resultString + Environment.NewLine + $"{queryResult.TotalResults.ToString("N0")} Row(s) affected Time: {queryResult.Time.ToString("N0")}ms";
        }
    }
}
