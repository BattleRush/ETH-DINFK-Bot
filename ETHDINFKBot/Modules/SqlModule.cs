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
            var queryResult = await GetQueryResults("SELECT name FROM sqlite_master WHERE type ='table' AND name NOT LIKE 'sqlite%';", true, 50);





            EmbedBuilder builder = new EmbedBuilder();

            builder.WithTitle("BattleRush's Helper Help");
            //builder.WithUrl("https://github.com/BattleRush/ETH-DINFK-Bot");
            builder.WithDescription(@"SQL Tables 
To get the diagram type: '.sql table info'");
            builder.WithColor(65, 17, 187);

            builder.WithThumbnailUrl("https://cdn.discordapp.com/avatars/774276700557148170/62279315dd469126ca4e5ab89a5e802a.png");
            //builder.WithFooter($"If you can read this then ping Mert | TroNiiXx | [13]");
            builder.WithCurrentTimestamp();
            //builder.WithAuthor(author);

            int totalRows = 0;

            foreach (var row in queryResult.Data)
            {
                string tableName = row.ElementAt(0);

                var rowCountInfo = await GetQueryResults($"SELECT COUNT(*) FROM {tableName}", true, 1);

                string rowCountStr = rowCountInfo.Data.FirstOrDefault().FirstOrDefault(); // todo rework this first first thing

                if (int.TryParse(rowCountStr, out int rowCount))
                {
                    totalRows += rowCount;
                    builder.AddField(tableName, $"Rows: {rowCount.ToString("N0")}", true);
                }

            }

            var dbSizeInfo = await GetQueryResults($"SELECT page_count *page_size / 1024 / 1024 as size FROM pragma_page_count(), pragma_page_size()", true, 1);

            string dbSizeStr = dbSizeInfo.Data.FirstOrDefault().FirstOrDefault(); // todo rework this first first thing


            if (int.TryParse(dbSizeStr, out int dbSize))
            {
                watch.Stop();
                builder.AddField("Total", $"Rows: {totalRows.ToString("N0")} {Environment.NewLine} Query time: {watch.ElapsedMilliseconds.ToString("N0")}ms");
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
                    "DiscordEmoteStatistics"
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
            await Task.Delay(5000 * (owner ? 12 : 1));

            if (cts.IsCancellationRequested)
                return;

            await Context.Channel.SendMessageAsync("<:pepegun:747783377716904008>", false);
            await Context.Channel.SendMessageAsync($"<@!{Program.Owner}> someone tried to kill me with: {query.Substring(0, Math.Min(query.Length, 1900))}", false);
            //connect.Close();
            //command.Cancel();

            throw new TimeoutException("Time is over");
        }

        private string GetRowStringFromResult(List<string> header, List<List<string>> data)
        {
            string result = "";

            result += $"**{string.Join("\t", header)}**" + Environment.NewLine;

            foreach (var row in data)
            {
                result += $"{string.Join("\t", row)}" + Environment.NewLine;
            }
            return result.Substring(0, Math.Min(result.Length, 1900));
        }

        private async Task<string> GetRowStringFromReader(SqliteDataReader reader, bool getHeader)
        {
            string resultString = "";

            if (getHeader)
            {
                resultString += "**";
                for (int i = 0; i < reader.FieldCount; i++)
                {
                    resultString += reader.GetName(i)?.ToString() + "\t";
                }
                resultString += "**";
                resultString += Environment.NewLine + "```";
            }

            // do something with result
            for (int i = 0; i < reader.FieldCount; i++)
            {
                try
                {
                    var type = reader.GetFieldType(i)?.FullName;
                    var fieldString = "null";

                    if (DBNull.Value.Equals(reader.GetValue(i)))
                    {
                        resultString += fieldString + "\t";
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

                    resultString += fieldString + "\t";
                }
                catch (Exception ex)
                {
                    throw ex;
                }

            }
            resultString += Environment.NewLine;

            return resultString;
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
                SqlCommand(commandSql);
            }
            catch (Exception ex)
            {
                Context.Channel.SendMessageAsync("Is this all you got <:kekw:768912035928735775> " + ex.ToString(), false);
            }
        }

        private int DrawRow(Graphics g, List<string> row, int padding, int currentHeight, Brush brush, Font font, List<int> widths)
        {
            float highestSize = 0;
            int currentWidthStart = padding;
            for (int i = 0; i < row.Count; i++)
            {
                int offsetX = currentWidthStart;
                int cellWidth = widths.ElementAt(i);

                string text = row.ElementAt(i);

                Rectangle headerDestRect = new Rectangle(offsetX, currentHeight, cellWidth, 500);

                var size = new SizeF();
                try
                {
                    size = g.MeasureString(text, font, new SizeF(cellWidth, 500), null);
                }
                catch (Exception ex)
                {
                    Context.Channel.SendMessageAsync("debug: " + text);
                    // todo log the text for future bugfix
                    text = Encoding.ASCII.GetString(Encoding.Convert(Encoding.UTF8, Encoding.GetEncoding(Encoding.ASCII.EncodingName, new EncoderReplacementFallback(string.Empty), new DecoderExceptionFallback()), Encoding.UTF8.GetBytes(text)));

                    Context.Channel.SendMessageAsync("debug2: " + text);
                    // recalculate the size again
                    size = g.MeasureString(text, font, new SizeF(cellWidth, 500), null);
                }

                if (size.Height > highestSize)
                    highestSize = size.Height;

                //g.DrawRectangle(Pens.Red, headerDestRect);
                using (StringFormat sf = new StringFormat())
                {
                    g.DrawString(text, font, brush, headerDestRect, sf);
                }
                currentWidthStart += cellWidth;
            }

            //currentHeight += (int)highestSize + padding / 5;

            currentWidthStart = padding;
            for (int i = 0; i < row.Count; i++)
            {
                int offsetX = currentWidthStart;
                int cellWidth = widths.ElementAt(i);
                Rectangle headerDestRect = new Rectangle(offsetX, padding, cellWidth, (int)highestSize + 1);
                //g.DrawRectangle(Pens.Red, headerDestRect);

                currentWidthStart += cellWidth;
            }

            return (int)highestSize;
        }

        private List<int> DefineTableCellWidths(List<string> header, List<List<string>> data, int normalCellWidth, Font font)
        {
            Graphics g;
            var b = new Bitmap(2000, 2000); // TODO insert into constructor
            g = Graphics.FromImage(b);
            g.SmoothingMode = SmoothingMode.AntiAlias;

            g.Clear(CommonHelper.DiscordBackgroundColor);

            // a cell can be max 1000 pixels wide
            var maxSize = new SizeF(1000, 1000);

            int[] maxWidthNeeded = new int[header.Count];

            // the minimum size is the header text size
            for (int i = 0; i < header.Count; i++)
            {
                var size = g.MeasureString(header.ElementAt(i), font, maxSize, null);
                maxWidthNeeded[i] = (int)size.Width + 10;
            }

            // find the max column size in the content
            for (int i = 0; i < maxWidthNeeded.Length; i++)
            {
                foreach (var row in data)
                {
                    var size = g.MeasureString(row.ElementAt(i), font, maxSize, null);

                    int currentWidth = (int)size.Width + 10;

                    if (maxWidthNeeded[i] < currentWidth)
                        maxWidthNeeded[i] = currentWidth;
                }
            }
            // find columns that need the flex property
            List<int> flexColumns = new List<int>();
            int freeRoom = 0;
            int flexContent = 0;
            for (int i = 0; i < maxWidthNeeded.Length; i++)
            {
                if (maxWidthNeeded[i] > normalCellWidth)
                {
                    flexColumns.Add(i);
                    flexContent += maxWidthNeeded[i] - normalCellWidth; // only the oversize
                }
                else
                {
                    freeRoom += normalCellWidth - maxWidthNeeded[i];
                }
            }


            if (flexColumns.Count == 0)
            {
                // no columns need flex so we distribute all even
                for (int i = 0; i < maxWidthNeeded.Length; i++)
                    maxWidthNeeded[i] = normalCellWidth;
            }
            else
            {
                // we need to distribute the free room over the flexContent by %
                foreach (var column in flexColumns)
                {
                    float percentNeeded = (maxWidthNeeded[column] - normalCellWidth) / flexContent;
                    float gettingFreeSpace = freeRoom * percentNeeded;
                    maxWidthNeeded[column] = normalCellWidth + (int)gettingFreeSpace;
                }
            }

            g.Dispose();
            b.Dispose();

            return maxWidthNeeded.ToList();
        }

        private async Task<Stream> GetQueryResultImage(List<string> Header, List<List<string>> Data, int totalRows, long Time)
        {
            Stopwatch watchDraw = new Stopwatch();
            watchDraw.Start();

            Brush brush = new SolidBrush(System.Drawing.Color.White);
            Pen whitePen = new Pen(brush);

            Font fontMain = new Font("Arial", 18);
            Font font = new Font("Arial", 16);


            // todo make dynamic 

            Bitmap Bitmap;
            Graphics Graphics;
            List<DBTableInfo> DBTableInfo;
            int width = 1920;
            int height = 8000;

            Bitmap = new Bitmap(width, height); // TODO insert into constructor
            Graphics = Graphics.FromImage(Bitmap);
            Graphics.SmoothingMode = SmoothingMode.AntiAlias;

            Graphics.Clear(CommonHelper.DiscordBackgroundColor);

            int padding = 20;

            int xSize = width - padding * 2;

            if (Header.Count == 0)
            {
                await Context.Channel.SendMessageAsync("No results", false, null, null, null, new Discord.MessageReference(Context.Message.Id));
                return null;
            }

            int cellWidth = xSize / Header.Count;

            int currentHeight = padding;

            List<int> widths = DefineTableCellWidths(Header, Data, cellWidth, font);

            string cellWithInfo = "normal" + cellWidth + " " + string.Join(", ", widths);

            //await Context.Channel.SendMessageAsync(cellWithInfo, false, null, null, null, new Discord.MessageReference(Context.Message.Id));


            currentHeight += DrawRow(Graphics, Header, padding, currentHeight, brush, font, widths);

            int failedDrawLineCount = 0;
            foreach (var row in Data)
            {
                try
                {
                    Graphics.DrawLine(whitePen, padding, currentHeight, Math.Max(width - padding, 0), currentHeight);
                }
                catch (Exception ex)
                {
                    failedDrawLineCount++;
                }
                try
                {
                    currentHeight += DrawRow(Graphics, row, padding, currentHeight, brush, font, widths);
                }
                catch(Exception ex)
                {
                    Context.Channel.SendMessageAsync(ex.ToString());
                    break;
                }
            }

            try
            {
                Graphics.DrawLine(whitePen, padding, currentHeight, Math.Max(width - padding, 0), currentHeight);
            }
            catch (Exception ex)
            {
                failedDrawLineCount++;
            }

            if(failedDrawLineCount > 0)
            {
                Context.Channel.SendMessageAsync($"Failed to draw {failedDrawLineCount} lines, widths: {string.Join(",", widths)}");
            }

            watchDraw.Stop();

            Graphics.DrawString($"Total row(s) affected: {totalRows.ToString("N0")} QueryTime: {Time.ToString("N0")}ms DrawTime: {watchDraw.ElapsedMilliseconds.ToString("N0")}ms", fontMain, new SolidBrush(System.Drawing.Color.Yellow), new Point(padding, currentHeight + padding));



            List<int> rowHeight = new List<int>();

            Rectangle DestinationRectangle = new Rectangle(10, 10, cellWidth, 500);


            //var size = Graphics.MeasureCharacterRanges("", drawFont2, DestinationRectangle, null);

            //Graphics.DrawString($"{(int)((maxValue / yNum) * i)}", drawFont2, b, new Point(40, 10 + ySize - (ySize / yNum) * i));


            Bitmap = cropImage(Bitmap, new Rectangle(0, 0, 1920, currentHeight + padding * 3));



            var stream = CommonHelper.GetStream(Bitmap);
            Bitmap.Dispose();
            Graphics.Dispose();
            return stream;
        }


        private static Bitmap cropImage(Bitmap bmpImage, Rectangle cropArea)
        {
            return bmpImage.Clone(cropArea, bmpImage.PixelFormat);
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

                var stream = await GetQueryResultImage(queryResult.Header, queryResult.Data, queryResult.TotalResults, queryResult.Time);
                if (stream == null)
                    return;

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
                    using (var command = new SqliteCommand(commandSql, connection))
                    {
                        command.CommandTimeout = 5;
                        connection.Open();

                        WorkaroundForTimeoutNotWorking(cts, commandSql, author.Id == ETHDINFKBot.Program.Owner);

                        var reader = await command.ExecuteReaderAsync();
                        while (reader.Read())
                        {
                            if (Header.Count == 0)
                            {
                                for (int i = 0; i < reader.FieldCount; i++)
                                {
                                    string fieldName = reader.GetName(i)?.ToString();
                                    Header.Add(fieldName);
                                    currentContentLength = fieldName.Length;
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
                                            currentContentLength = fieldString.Length;
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

                                        currentContentLength = fieldString.Length;
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

        private async void SqlCommand(string commandSql)
        {
            var author = Context.Message.Author;


            CancellationTokenSource cts = new CancellationTokenSource();
            WorkaroundForTimeoutNotWorking(cts, commandSql, author.Id == ETHDINFKBot.Program.Owner);

            var queryResult = await GetQueryResults(commandSql, false, 2000);
            var resultString = GetRowStringFromResult(queryResult.Header, queryResult.Data);

            cts.Cancel();



            try
            {
             /*   bool header = false;
                string resultString = "";
                int rowCount = 0;

                int maxRows = 25;

                Stopwatch watch = new Stopwatch();
                watch.Start();

                using (var connection = new SqliteConnection(Program.ConnectionString))
                {
                    using (var command = new SqliteCommand(commandSql, connection))
                    {
                        command.CommandTimeout = 4;
                        connection.Open();

                        WorkaroundForTimeoutNotWorking(cts, commandSql, author.Id == ETHDINFKBot.Program.Owner);

                        var reader = await command.ExecuteReaderAsync();
                        while (reader.Read())
                        {
                            if (rowCount < maxRows || resultString.Length < 2000)
                            {
                                string line = await GetRowStringFromReader(reader, !header);
                                resultString += line;
                                header = true;
                            }
                            rowCount++;
                        }
                    }
                }
                cts.Cancel();
                watch.Stop();
                if (resultString.Length > 1950)
                    resultString = resultString.Substring(0, 1950);

                if (rowCount != 0)
                    resultString += "```";*/

                await Context.Channel.SendMessageAsync(resultString + Environment.NewLine + $"{queryResult.TotalResults.ToString("N0")} Row(s) affected Time: {queryResult.Time.ToString("N0")}ms", false);
            }
            catch (Exception ex)
            {
                cts.Cancel();
                await Context.Channel.SendMessageAsync("Error: " + ex.Message, false);
            }
        }
    }

}
