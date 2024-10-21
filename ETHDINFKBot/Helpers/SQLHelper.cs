using Discord;
using Discord.Commands;
using Discord.Interactions;
using MySqlConnector;
using Npgsql;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ETHDINFKBot.Helpers
{
    public static class SQLHelper
    {
        public static string GetRowStringFromResult(List<string> header, List<List<string>> data, List<int> selectColumns, bool formatNumbers = false)
        {
            if (data.Count == 0)
                return ""; // no response

            string result = "``";

            for (int i = 0; i < header.Count; i++)
            {
                if (selectColumns.Contains(i) || selectColumns.Count == 0)
                    result += header[i].Replace("`", "") + "\t"; // ensure it cant be escaped
            }

            result += "``" + Environment.NewLine;

            // If the header is really long then abort the message
            if (result.Length > 1000)
            {
                result = $"Reconsider how many columns you are selecting. Maybe use {Program.CurrentPrefix}sql queryd <query>";
                return result;
            }

            if (data.Count > 0)
            {
                result += "```";
                foreach (var row in data)
                {
                    string rowString = "";
                    for (int i = 0; i < row.Count; i++)
                    {
                        if (formatNumbers && long.TryParse(row[i], out long longValue))
                            row[i] = longValue.ToString("N0");

                        if (selectColumns.Contains(i) || selectColumns.Count == 0)
                            rowString += row[i] + "\t";
                    }

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

        public static async Task<string> SqlCommand(SocketCommandContext context, string commandSql, bool adminOverwrite = false)
        {
            var queryResult = await GetQueryResults(context, commandSql.ToString(), false, 2000, adminOverwrite);
            var resultString = GetRowStringFromResult(queryResult.Header, queryResult.Data, new List<int>());

            return resultString + Environment.NewLine + $"{queryResult.TotalResults.ToString("N0")} Row(s) affected Time: {queryResult.Time.ToString("N0")}ms";
        }


        public static async Task<string> SqlCommandRows(SocketCommandContext context, string commandSql, bool adminOverwrite = false, int rows = 10000)
        {
            var queryResult = await GetQueryResults(context, commandSql.ToString(), true, rows, adminOverwrite);
            var resultString = GetRowStringFromResult(queryResult.Header, queryResult.Data, new List<int>());

            return resultString + Environment.NewLine + $"{queryResult.TotalResults.ToString("N0")} Row(s) affected Time: {queryResult.Time.ToString("N0")}ms";
        }

        public static async Task<string> SqlCommandPostgreSQL(SocketCommandContext context, string commandSql, string database)
        {
            var author = context.Message.Author;

            var queryResult = await GetQueryResultsPostgreSQL(context, commandSql.ToString(), database, false, 2000);
            var resultString = GetRowStringFromResult(queryResult.Header, queryResult.Data, new List<int>());

            return resultString + Environment.NewLine + $"{queryResult.TotalResults.ToString("N0")} Row(s) affected Time: {queryResult.Time.ToString("N0")}ms";
        }

        public static async Task<(List<string> Header, List<List<string>> Data, int TotalResults, long Time)> GetQueryResultsInteraction(SocketInteractionContext context, string commandSql, bool limitRows = false, int limitLength = 2000, bool fullUser = false, bool disableCap = false, List<MySqlParameter> parameters = null)
        {
            ulong? authorId = context?.Interaction?.User?.Id;
            return await GetQueryResultsId(authorId, commandSql, limitRows, limitLength, fullUser, disableCap, null, context, parameters);
        }

        public static async Task<(List<string> Header, List<List<string>> Data, int TotalResults, long Time)> GetQueryResults(SocketCommandContext context, string commandSql, bool limitRows = false, int limitLength = 2000, bool fullUser = false, bool disableCap = false, List<MySqlParameter> parameters = null)
        {
            ulong? authorId = context?.Message?.Author?.Id;
            return await GetQueryResultsId(authorId, commandSql, limitRows, limitLength, fullUser, disableCap, context, null, parameters);
        }

        public static async Task<(List<string> Header, List<List<string>> Data, int TotalResults, long Time)> GetQueryResultsId(ulong? authorId, string commandSql, bool limitRows = false, int limitLength = 2000, bool fullUser = false, bool disableCap = false, SocketCommandContext context = null, SocketInteractionContext interactionContext = null, List<MySqlParameter> parameters = null)
        {
            // TODO Admin perms for daily jobs with no context object
            List<string> Header = new List<string>();
            List<List<string>> Data = new List<List<string>>();
            int TotalResults = 0;

            long Time = -1;

            int currentContentLength = 0;
            int currentRowCount = 0;


            //ancellationTokenSource cts = new CancellationTokenSource();

            try
            {
                Stopwatch watch = new Stopwatch();
                watch.Start();

                if (authorId == Program.ApplicationSetting.Owner)
                    fullUser = true;

                string connectionString = fullUser ? Program.ApplicationSetting.ConnectionStringsSetting.ConnectionString_Full : Program.ApplicationSetting.ConnectionStringsSetting.ConnectionString_ReadOnly;

                using (var connection = new MySqlConnection(connectionString))
                {
                    using (var command = new MySqlCommand(commandSql, connection))
                    {
                        command.CommandTimeout = fullUser || authorId == 223932775474921472 ? 300 : 60;

                        connection.Open();

                        if(parameters != null)
                            command.Parameters.AddRange(parameters.ToArray());

                        var reader = command.ExecuteReader();

                        while (reader.Read())
                        {
                            if (!disableCap)
                            {
                                // todo disable
                                // cap at 10k records to return in count (as temp fix if the query returns millions of rows)
                                if (TotalResults == 25_000)
                                {
                                    command.Cancel();
                                    break;
                                }

                                if (TotalResults > 250)
                                {
                                    TotalResults++;
                                    continue;
                                }
                            }


                            if (Header.Count == 0)
                            {
                                for (int i = 0; i < reader.FieldCount; i++)
                                {
                                    string fieldName = reader.GetName(i)?.ToString();
                                    if (currentContentLength + fieldName.Length > 1980)
                                        break;

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
                                            case "System.Int32":
                                                fieldString = reader.GetInt32(i).ToString();
                                                break;

                                            case "System.Int64":
                                                fieldString = reader.GetInt64(i).ToString();
                                                break;

                                            case "System.String":
                                                fieldString = reader.GetValue(i).ToString();
                                                break;

                                            case "System.Boolean":
                                                fieldString = reader.GetBoolean(i).ToString();
                                                break;

                                            case "System.DateTime":
                                                fieldString = reader.GetDateTime(i).ToString();
                                                break;

                                            case "System.Decimal":
                                                fieldString = reader.GetDecimal(i).ToString();
                                                break;

                                            case "System.Double":
                                                fieldString = reader.GetDecimal(i).ToString();
                                                break;

                                            default:
                                                //fieldString = $"{type} is unknown";
                                                fieldString = reader.GetValue(i).ToString();

                                                break;
                                        }

                                        // Prevent escaping from code blocks -> TODO in queryd allow it
                                        fieldString = fieldString?.Replace("`", "").Trim();

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
                        /*
                        int totalRow = 0;
                        reader.NextResult(); // 
                        if (reader.Read())
                        {
                            TotalResults = (int)reader[0];
                        }*/

                        // if 0 records maybe some got changed or inserted
                        if (TotalResults == 0)
                            TotalResults = reader.RecordsAffected;
                    }


                    connection.Close();
                }
                watch.Stop();
                Time = watch.ElapsedMilliseconds;

                //cts.Cancel();
            }
            catch (Exception ex)
            {
                if (context == null && interactionContext == null)
                    throw ex; // if no context is provided dont handle it


                EmbedBuilder builder = new EmbedBuilder();

                builder.WithTitle($"Error while executing SQL query");
                builder.WithColor(0, 128, 255);
                builder.WithDescription(ex.Message.Substring(0, Math.Min(ex.Message.Length, 2000)));

                builder.WithAuthor(context.Message.Author);
                builder.WithCurrentTimestamp();

                // TODO include maybe the sql query also in the embed
                if(context != null)
                    await context.Channel.SendMessageAsync("", false, builder.Build(), null, null, new MessageReference(context.Message.Id, context.Channel.Id));
                else
                    await interactionContext.Channel.SendMessageAsync("", false, builder.Build());
            }

            return (Header, Data, TotalResults, Time);
        }

        public static async Task<(List<string> Header, List<List<string>> Data, int TotalResults, long Time)> GetQueryResultsPostgreSQL(SocketCommandContext context, string commandSql, string database, bool limitRows = false, int limitLength = 2000, bool fullUser = false, bool disableCap = false)
        {
            // TODO Admin perms for daily jobs with no context object
            var author = context?.Message?.Author;

            List<string> Header = new List<string>();
            List<List<string>> Data = new List<List<string>>();
            int TotalResults = 0;

            long Time = -1;

            int currentContentLength = 0;
            int currentRowCount = 0;


            //ancellationTokenSource cts = new CancellationTokenSource();

            try
            {
                Stopwatch watch = new Stopwatch();
                watch.Start();
                var postgreSettings = Program.ApplicationSetting.PostgreSQLSetting;
                var connString = $"Host={postgreSettings.Host};Port={postgreSettings.Port};Command Timeout=10;Username={postgreSettings.DMDBUserUsername.ToLower()};Password={postgreSettings.DMDBUserPassword};Database={database}";
                if (fullUser)
                    connString = $"Host={postgreSettings.Host};Port={postgreSettings.Port};Command Timeout=10;Username={postgreSettings.OwnerUsername.ToLower()};Password={postgreSettings.OwnerPassword};Database={database}";

                await using var connection = new NpgsqlConnection(connString);
                await connection.OpenAsync();

                connection.ReloadTypes(); // This is needed for the employees table

                await using (var command = new NpgsqlCommand(commandSql, connection))
                await using (var reader = await command.ExecuteReaderAsync())
                {
                    while (await reader.ReadAsync())
                    {
                        if (!disableCap)
                        {
                            // todo disable
                            // cap at 10k records to return in count (as temp fix if the query returns millions of rows)
                            if (TotalResults == 10_000)
                            {
                                command.Cancel();
                                break;
                            }

                            if (TotalResults > 250)
                            {
                                TotalResults++;
                                continue;
                            }
                        }


                        if (Header.Count == 0)
                        {
                            for (int i = 0; i < reader.FieldCount; i++)
                            {
                                string fieldName = reader.GetName(i)?.ToString();
                                if (currentContentLength + fieldName.Length > 1980)
                                    break;

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
                                            //fieldString = $"{type} is unknown";
                                            fieldString = reader.GetValue(i).ToString()?.Replace("`", "");

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
                    /*
                    int totalRow = 0;
                    reader.NextResult(); // 
                    if (reader.Read())
                    {
                        TotalResults = (int)reader[0];
                    }*/

                    // if 0 records maybe some got changed or inserted
                    if (TotalResults == 0)
                        TotalResults = reader.RecordsAffected;
                }


                connection.Close();

                watch.Stop();
                Time = watch.ElapsedMilliseconds;

                //cts.Cancel();
            }
            catch (Exception ex)
            {
                if (context == null)
                    throw ex; // if no context is provided dont handle it


                EmbedBuilder builder = new EmbedBuilder();

                builder.WithTitle($"Error while executing SQL query");
                builder.WithColor(0, 128, 255);
                builder.WithDescription(ex.Message.Substring(0, Math.Min(ex.Message.Length, 2000)));

                builder.WithAuthor(context.Message.Author);
                builder.WithCurrentTimestamp();

                // TODO include maybe the sql query also in the embed
                await context.Channel.SendMessageAsync("", false, builder.Build(), null, null, new MessageReference(context.Message.Id, context.Channel.Id));
            }

            return (Header, Data, TotalResults, Time);
        }
    }
}
