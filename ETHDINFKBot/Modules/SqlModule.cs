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
using Npgsql;

namespace ETHDINFKBot.Modules
{
    [Group("sql")]
    public class SqlModule : ModuleBase<SocketCommandContext>
    {
        [Group("dmdb")]
        public class DMDBModule : ModuleBase<SocketCommandContext>
        {
            [Command("help")]
            [Alias("info")]
            public async Task SqlStatsHelp()
            {
                EmbedBuilder builder = new EmbedBuilder();

                builder.WithTitle($"{Program.Client.CurrentUser.Username} DMDB Help");
                builder.WithDescription("This feature supports the 3 Databases provided by the DMDB Course: 'employee', 'zvv' and 'tpch'. " + Environment.NewLine +
                    "**IMPORTANT: Unlike on the main MariaDB you have full Admin permissions on the PostgreSQL.** It's intended that you may experiemnt with the Database without any need to install it locally." +
                    "Any Admin can restore the Database should it become unusable/corrupted. If you abuse it on purpose, then you will be banned for using this command.");
                builder.WithColor(0, 0, 255);

                builder.WithThumbnailUrl(Program.Client.CurrentUser.GetAvatarUrl());
                builder.WithCurrentTimestamp();
                builder.AddField($"{Program.CurrentPrefix}sql dmdb help", "This message :)");
                builder.AddField($"{Program.CurrentPrefix}sql dmdb restore", "Restore the Database (Any Admin/Mod can do this)");
                builder.AddField($"{Program.CurrentPrefix}sql dmdb info <database>", "Get the Table Graph. Available DBs: employee, zvv and tpch");
                builder.AddField($"{Program.CurrentPrefix}sql dmdb query <database> <query>", "Run the query on a specified Database (employee, zvv or tpch), restult will be in text form");
                builder.AddField($"{Program.CurrentPrefix}sql dmdb queryd <database> <query>", "Run the query on a specified Database (employee, zvv or tpch), but returns the restult as an Image ");

                await Context.Channel.SendMessageAsync("", false, builder.Build());
            }
            // TODO Duplicate from MariaDB
            private List<ForeignKeyInfo> GetForeignKeyInfo(List<List<string>> fkData, string tableName)
            {
                var list = new List<ForeignKeyInfo>();

                foreach (var row in fkData)
                {

                    try
                    {
                        ForeignKeyInfo info = new ForeignKeyInfo()
                        {
                            FromTable = tableName, //reader.GetString(2),
                            FromTableFieldName = row.ElementAt(3),
                            ToTable = row.ElementAt(5),
                            ToTableFieldName = row.ElementAt(6),

                        };
                        //0   0   DiscordServers DiscordServerId DiscordServerId NO ACTION CASCADE NONE

                        list.Add(info);

                    }
                    catch (Exception ex)
                    {
                        //_logger.LogError(ex, ex.Message);
                    }


                }

                return list;
            }

            private DBTableInfo GetTableInfo(List<List<string>> data, List<List<string>> pkData, string tableName)
            {
                DBTableInfo dbTableInfo = new DBTableInfo()
                {
                    TableName = tableName,
                    FieldInfos = new List<DBFieldInfo>()
                };


                foreach (var row in data)
                {


                    try
                    {

                        string genetalType = "null";
                        string type = row.ElementAt(0);

                        switch (type)
                        {
                            case "tinyint(1)":
                                genetalType = "bool";
                                break;
                            case string int_1 when int_1.StartsWith("tinyint"):
                            case string int_2 when int_2.StartsWith("int"):
                            case string int_3 when int_3.StartsWith("bigint"):
                            case string int_4 when int_4.StartsWith("numeric"):
                                genetalType = "int";
                                break;
                            case string string_1 when string_1.StartsWith("varchar"):
                            case string string_2 when string_2.StartsWith("longtext"):
                            case string string_3 when string_3.StartsWith("character"):
                            case string string_4 when string_4.StartsWith("character varying"):
                                genetalType = "string";
                                break;
                            case string dateTime_1 when dateTime_1.StartsWith("datetime"):
                            case string dateTime_2 when dateTime_2.StartsWith("date"):
                                genetalType = "datetime";
                                break;
                            default:
                                break;
                        }

                        bool isPk = false;

                        if (pkData.Any(i => i.ElementAt(0) == row.ElementAt(1)))
                            isPk = true;

                        DBFieldInfo field = new DBFieldInfo()
                        {
                            //Id = reader.GetInt32(0),
                            Name = row.ElementAt(1),
                            Type = row.ElementAt(0),
                            GeneralType = genetalType,
                            Nullable = row.ElementAt(2) == "YES",
                            // df value needed?
                            IsPrimaryKey = isPk

                        };


                        dbTableInfo.FieldInfos.Add(field);

                    }
                    catch (Exception ex)
                    {
                        // _logger.LogError(ex, ex.Message);
                    }


                }

                return dbTableInfo;
            }



            // todo maybe move to a seperate class
            private async Task<List<DBTableInfo>> GetAllDBTableInfos(string database)
            {
                List<string> tableList = new List<string>()
                {

                };


#if DEBUG
                tableList = new List<string>(); // Clear because on windows the capitalization of tables is different and currently that breaks some SQL Queries
#endif

                var queryResult = await SQLHelper.GetQueryResultsPostgreSQL(Context, $@"SELECT table_name FROM information_schema.tables WHERE table_schema = 'public'", database, true, 50);

                // Add tables incase they arent in the list above for their correct order
                foreach (var item in queryResult.Data)
                {
                    tableList.Add(item.ElementAt(0));
                }

                //;$"PRAGMA table_info('{item}')";
                //PRAGMA foreign_key_list('DiscordChannels');

                string text = "";
                string header = "";

                List<DBTableInfo> DbTableInfos = new List<DBTableInfo>();
                List<List<ForeignKeyInfo>> ForeignKeyInfos = new List<List<ForeignKeyInfo>>();




                foreach (var table in tableList)
                {
                    string query = @$"SELECT data_type, column_name, is_nullable, is_identity
 FROM information_schema.columns
 WHERE table_schema = 'public' AND table_name = '{table}'";

                    var data = await SQLHelper.GetQueryResultsPostgreSQL(Context, query, database);


                    string getPrimaryKey = @$"SELECT c.column_name, c.data_type, constraint_type
FROM information_schema.table_constraints tc 
JOIN information_schema.constraint_column_usage AS ccu USING (constraint_schema, constraint_name) 
JOIN information_schema.columns AS c ON c.table_schema = tc.constraint_schema
  AND tc.table_name = c.table_name AND ccu.column_name = c.column_name
  WHERE tc.table_name = '{table}' AND constraint_type = 'PRIMARY KEY';";

                    var pkData = await SQLHelper.GetQueryResultsPostgreSQL(Context, getPrimaryKey, database, fullUser: true);

                    DbTableInfos.Add(GetTableInfo(data.Data, pkData.Data, table));

                    string getForeignKey = @$"SELECT
    tc.table_schema, 
    tc.constraint_name, 
    tc.table_name, 
    kcu.column_name, 
    ccu.table_schema AS foreign_table_schema,
    ccu.table_name AS foreign_table_name,
    ccu.column_name AS foreign_column_name 
FROM 
    information_schema.table_constraints AS tc 
    JOIN information_schema.key_column_usage AS kcu
      ON tc.constraint_name = kcu.constraint_name
      AND tc.table_schema = kcu.table_schema
    JOIN information_schema.constraint_column_usage AS ccu
      ON ccu.constraint_name = tc.constraint_name
      AND ccu.table_schema = tc.table_schema
WHERE tc.constraint_type = 'FOREIGN KEY' AND tc.table_name='{table}';";

                    var fkData = await SQLHelper.GetQueryResultsPostgreSQL(Context, getForeignKey, database, fullUser: true);

                    ForeignKeyInfos.Add(GetForeignKeyInfo(fkData.Data, table));

                }



                //              using (var command = context.Database.GetDbConnection().CreateCommand())
                //              {
                //                  command.CommandText = $@"select
                //  c.table_name,
                //  c.column_name,
                //  c.referenced_table_name,
                //  c.referenced_column_name
                //from information_schema.table_constraints fk
                //join information_schema.key_column_usage c
                //  on c.constraint_name = fk.constraint_name
                //where fk.constraint_type = 'FOREIGN KEY' AND c.TABLE_SCHEMA = '{Program.ApplicationSetting.MariaDBName ?? "ETHBot"}' AND c.table_name = '{item}'; ";
                //                  context.Database.OpenConnection();

                //                  //ForeignKeyInfos.Add(GetForeignKeyInfo(command, item));
                //              }









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


            [Command("info")]
            public async Task TableInfoTables(string database)
            {


                try
                {
                    var dbInfos = await GetAllDBTableInfos(database);

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
                    await Context.Channel.SendMessageAsync(ex.ToString());
                }
            }

            // TODO DUPLICATE MOVE TO HELPER CLASS
            private bool ForbiddenQuery(string commandSql, ulong authorId)
            {
                if (CommonHelper.ContainsForbiddenQuery(commandSql) && authorId != Program.ApplicationSetting.Owner)
                {
                    Context.Channel.SendMessageAsync("Dont you dare to think you will be allowed to use this command https://tenor.com/view/you-shall-not-pass-lord-of-the-ring-gif-5234772", false);
                    return true;
                }
                return false;
            }

            [Command("restore")]
            [RequireUserPermission(GuildPermission.ManageChannels)]
            public async Task RestoreDB()
            {
                Stopwatch stopwatch = Stopwatch.StartNew();
                var postgreSettings = Program.ApplicationSetting.PostgreSQLSetting;
                string restoreDBsScriptPath = Path.Combine(Program.ApplicationSetting.BasePath, "SQLScripts", "create.sql");
                string restoreDBsScript = string.Format(File.ReadAllText(restoreDBsScriptPath), postgreSettings.DMDBUserUsername, postgreSettings.DMDBUserPassword);

                var connString = $"Host={postgreSettings.Host};Port={postgreSettings.Port};Username={postgreSettings.OwnerUsername};Password={postgreSettings.OwnerPassword};Include Error Detail=True;Timeout=300;CommandTimeout=300;KeepAlive=300;";
                await using var connection = new NpgsqlConnection(connString);
                await connection.OpenAsync();

                try
                {
                    await using (var command = new NpgsqlCommand(restoreDBsScript, connection))
                    {
                        int rowsAffected = command.ExecuteNonQuery();
                        stopwatch.Stop();
                        await Context.Channel.SendMessageAsync($"Deleted and recreated Databases in {stopwatch.ElapsedMilliseconds}ms");
                    }
                }
                catch (Exception ex)
                {
                    await Context.Channel.SendMessageAsync(ex.Message);

                }



                List<string> dbNames = new List<string>()
                {
                    "employee",
                    "zvv",
                    "tpch"
                };



                foreach (var dbName in dbNames)
                {
                    Thread.Sleep(1000);
                    stopwatch.Restart();

                    var connStringDB = $"Host={postgreSettings.Host};Port={postgreSettings.Port};Username={postgreSettings.OwnerUsername};Password={postgreSettings.OwnerPassword};Database={dbName};";
                    string dbSchemaScriptAndInsertPath = Path.Combine(Program.ApplicationSetting.BasePath, "SQLScripts", $"{dbName}.sql");
                    string dbSchemaScriptAndInsert = string.Format(File.ReadAllText(dbSchemaScriptAndInsertPath), postgreSettings.DMDBUserUsername);

                    await using var connectionDB = new NpgsqlConnection(connStringDB);
                    await connectionDB.OpenAsync();
                    try
                    {
                        await using (var command = new NpgsqlCommand(dbSchemaScriptAndInsert, connectionDB))
                        {
                            int rowsAffected = command.ExecuteNonQuery();
                            await Context.Channel.SendMessageAsync($"Affected {rowsAffected} row(s) in {dbName} in {stopwatch.ElapsedMilliseconds}ms");
                        }
                    }
                    catch (Exception ex)
                    {
                        await Context.Channel.SendMessageAsync(ex.Message + " You may want to rerun the command.");

                    }

                }
                await Context.Channel.SendMessageAsync($"Done");

                // close all connections
                // reload the schema
                // populate default data
            }

            [Command("query")]
            public async Task SqlQuery(string database, [Remainder] string query)
            {
                var userId = Context.Message.Author.Id;

                if (!CommonHelper.AllowedToRun(BotPermissionType.EnableType2Commands, Context.Message.Channel.Id, userId))
                    return;

                if (ForbiddenQuery(query, Context.Message.Author.Id))
                    return;



                if (ActiveSQLCommands.ContainsKey(userId) && ActiveSQLCommands[userId].AddSeconds(10) > DateTime.Now)
                {
                    await Context.Channel.SendMessageAsync("Are you in such a hurry, that you cant wait out the last query you send out?", false);
                    return;
                }

                switch (database)
                {
                    case "employee":
                    case "zvv":
                    case "tvch":
                        break;
                    default:
                        await Context.Channel.SendMessageAsync("Invalid DB name. Available: employee, zvv, tvch");

                        return;
                }

                if (ActiveSQLCommands.ContainsKey(userId))
                    ActiveSQLCommands[userId] = DateTime.Now;
                else
                    ActiveSQLCommands.Add(userId, DateTime.Now);


                var commandResponse = await SQLHelper.SqlCommandPostgreSQL(Context, query, database);
                await Context.Channel.SendMessageAsync(commandResponse, false);

                // release the user again as the query finished
                ActiveSQLCommands[userId] = DateTime.MinValue;
            }

            [Command("queryd")]
            public async Task SqlQueryD(string database, [Remainder] string query)
            {
                var userId = Context.Message.Author.Id;

                if (!CommonHelper.AllowedToRun(BotPermissionType.EnableType2Commands, Context.Message.Channel.Id, userId))
                    return;

                if (ForbiddenQuery(query, Context.Message.Author.Id))
                    return;

                if (ActiveSQLCommands.ContainsKey(userId) && ActiveSQLCommands[userId].AddSeconds(10) > DateTime.Now)
                {
                    await Context.Channel.SendMessageAsync("Are you in such a hurry, that you cant wait out the last query you send out?", false);
                    return;
                }

                switch (database)
                {
                    case "employee":
                    case "zvv":
                    case "tvch":
                        break;
                    default:
                        await Context.Channel.SendMessageAsync("Invalid DB name. Available: employee, zvv, tvch");

                        return;
                }

                try
                {
                    if (ActiveSQLCommands.ContainsKey(userId))
                        ActiveSQLCommands[userId] = DateTime.Now;
                    else
                        ActiveSQLCommands.Add(userId, DateTime.Now);

                    var queryResult = await SQLHelper.GetQueryResultsPostgreSQL(Context, query, database, true, 100);
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


        [Command("info")]
        [Alias("help")]
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
DB Diagram: **{prefix}sql table info** 
DB Stats Help: **{prefix}sql stats help**
DMDB Help: **{prefix}sql dmdb help**");
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

                builder.AddField("Row count", rowCountString.Length > 0 ? rowCountString : "n/a");

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
