using Discord.Commands;
using MySqlConnector;
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

            string result = "";

            result += "**";

            for (int i = 0; i < header.Count; i++)
            {
                if (selectColumns.Contains(i) || selectColumns.Count == 0)
                    result += header[i] + "\t";
            }

            result += "**" + Environment.NewLine;

            

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

        public static async Task<string> SqlCommand(SocketCommandContext context, string commandSql)
        {
            var author = context.Message.Author;

            var queryResult = await GetQueryResults(context, commandSql.ToString(), false, 2000);
            var resultString = GetRowStringFromResult(queryResult.Header, queryResult.Data, new List<int>());

            return resultString + Environment.NewLine + $"{queryResult.TotalResults.ToString("N0")} Row(s) affected Time: {queryResult.Time.ToString("N0")}ms";
        }

        public static async Task<(List<string> Header, List<List<string>> Data, int TotalResults, long Time)> GetQueryResults(SocketCommandContext context, string commandSql, bool limitRows = false, int limitLength = 2000, bool fullUser = false)
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

                if (author?.Id == Program.Owner)
                    fullUser = true;

                string connectionString = fullUser ? Program.FULL_MariaDBReadOnlyConnectionString : Program.MariaDBReadOnlyConnectionString;

                using (var connection = new MySqlConnection(connectionString))
                {
                    using (var command = new MySqlCommand(commandSql, connection))
                    {
                        command.CommandTimeout = fullUser ? 240 : 15;

                        connection.Open();

                        var reader = command.ExecuteReader();
                        
                        while (reader.Read())
                        {
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
                }
                watch.Stop();
                Time = watch.ElapsedMilliseconds;

                //cts.Cancel();
            }
            catch (Exception ex)
            {
                if (context == null)
                    throw ex; // if no context is provided dont handle it

                //cts.Cancel();
                await context.Channel.SendMessageAsync("Error: " + ex.Message, false);
            }

            return (Header, Data, TotalResults, Time);
        }
    }
}
