using Discord;
using Discord.Commands;
using ETHBot.DataLayer.Data.Discord;
using ETHDINFKBot.Data;
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
    public static class SQLInteractionHelper
    {
        private static SQLDBManager SQLDBManager = SQLDBManager.Instance();
        public static ComponentBuilder GetSavedSQLCommandParameters(int savedQueryId)
        {
            var savedQuery = SQLDBManager.GetSavedQueryById(savedQueryId);

            // edit the buttons

            var queryParameters = SQLDBManager.GetQueryParameters(savedQuery);

            // create for each parameter a button
            var messageBuilder = new ComponentBuilder();
            int count = 0;

            int row = 0;

            // this can have only up to 2 rows of params
            foreach (var parameter in queryParameters)
            {
                var style = ButtonStyle.Primary;
                switch (parameter.ParameterType)
                {
                    case "string":
                        style = ButtonStyle.Success;
                        break;
                    case "int":
                        style = ButtonStyle.Danger;
                        break;
                    case "datetime":
                        style = ButtonStyle.Secondary;
                        break;
                    case "bool":
                        style = ButtonStyle.Primary;
                        break;
                }
                row = count / 5;
                messageBuilder.WithButton(parameter.ParameterName, $"sql-change-datatype-{parameter.SavedQueryId}-{parameter.SavedQueryParameterId}", style, row: row);

                count++;
            }

            count += 5 - count % 5;

            // create button to change default value for each parameter
            foreach (var parameter in queryParameters)
            {
                row = count / 5;
                messageBuilder.WithButton(parameter.ParameterName, $"sql-change-defaultvalue-{parameter.SavedQueryId}-{parameter.SavedQueryParameterId}", ButtonStyle.Secondary, row: row);

                count++;
            }


            return messageBuilder;
        }

        public static (EmbedBuilder EmbedBuilder, ComponentBuilder ComponentBuilder) GetSavedInfoQueryById(SavedQuery savedQuery, bool sameUser)
        {
            // send message with embed of the query and buttons to execute, edit (only if the user is the same as the creator),
            // delete (only if the user is the same as the creator), change datatype, change default value
            var embedBuilder = new EmbedBuilder()
            {
                Title = $"{savedQuery.CommandName} ({savedQuery.SavedQueryId})",
                Description = $"```sql{Environment.NewLine}{savedQuery.Content}{Environment.NewLine}```",
                Color = Color.Blue
            };

            var queryParameters = SQLDBManager.Instance().GetQueryParameters(savedQuery);

            embedBuilder.AddField("Description", savedQuery.Description ?? "No description provided.");

            foreach (var parameter in queryParameters)
                embedBuilder.AddField(parameter.ParameterName, $"ID: {parameter.SavedQueryParameterId}{Environment.NewLine}Type: {parameter.ParameterType}{Environment.NewLine}Default value: {parameter.DefaultValue}");

            var messageBuilder = new ComponentBuilder();

            messageBuilder.WithButton("Execute command", $"sql-execute-command-{savedQuery.SavedQueryId}", ButtonStyle.Success, row: 0);
            messageBuilder.WithButton("Execute command (Draw)", $"sql-executedraw-cmd-{savedQuery.SavedQueryId}", ButtonStyle.Success, row: 0);
            messageBuilder.WithButton("Execute command (Chart)", $"sql-executechart-cmd-{savedQuery.SavedQueryId}", ButtonStyle.Success, row: 0);
            messageBuilder.WithButton("Create template", $"sql-template-command-{savedQuery.SavedQueryId}", ButtonStyle.Secondary, row: 0);
            messageBuilder.WithButton("Change parameter datatype/default value", $"sql-change-dt-cmd-{savedQuery.SavedQueryId}", ButtonStyle.Secondary, row: 0);
            //messageBuilder.WithButton("Change parameter default value", $"sql-change-defaultvalue-cmd-{savedQuery.SavedQueryId}", ButtonStyle.Secondary);

            if (sameUser)
            {
                messageBuilder.WithButton("Edit", $"sql-edit-cmd-{savedQuery.SavedQueryId}", ButtonStyle.Primary, row: 1);
                messageBuilder.WithButton("Delete", $"sql-delete-cmd-{savedQuery.SavedQueryId}", ButtonStyle.Danger, row: 1);
            }

            //await Context.Interaction.RespondAsync("", embed: embedBuilder.Build(), components: messageBuilder.Build());

            return (embedBuilder, messageBuilder);
        }

        public static EmbedBuilder GetCommandTemplate(string commandName)
        {
            var savedQuery = SQLDBManager.GetSavedQueryByCommandName(commandName);

            if (savedQuery == null)
                return null;

            return GetCommandTemplate(savedQuery.SavedQueryId);
        }
        public static EmbedBuilder GetCommandTemplate(int savedQueryId)
        {

            SQLDBManager sqlDBManager = SQLDBManager.Instance();

            var command = sqlDBManager.GetSavedQueryById(savedQueryId);

            var queryParameters = sqlDBManager.GetQueryParameters(command);

            string text = $"```sql{Environment.NewLine}{Program.CurrentPrefix}sql execute {command.CommandName}" + Environment.NewLine;

            foreach (var parameter in queryParameters)
            {
                string escapedParameterName = parameter.ParameterName.Replace("@", "!");
                text += $"{escapedParameterName}={parameter.DefaultValue} " + Environment.NewLine;
            }

            text += "```";

            text += $"{Environment.NewLine}For image response use 'executedraw' instead of 'execute' and for chart use 'executechart'.";

            EmbedBuilder embedBuilder = new EmbedBuilder();
            embedBuilder.WithTitle("SQL Command Template");
            embedBuilder.WithDescription(text);

            return embedBuilder;
        }

        // TODO culture invariant parsing
        public static MySqlParameter GetNumberParameter(string name, string value)
        {
            if (int.TryParse(value, out int intValue))
            {
                return new MySqlParameter(name, intValue);
            }
            else if (long.TryParse(value, out long longValue))
            {
                return new MySqlParameter(name, longValue);
            }
            else if (ulong.TryParse(value, out ulong ulongValue))
            {
                return new MySqlParameter(name, ulongValue);
            }
            else if (double.TryParse(value, out double doubleValue))
            {
                return new MySqlParameter(name, doubleValue);
            }
            else if (decimal.TryParse(value, out decimal decimalValue))
            {
                return new MySqlParameter(name, decimalValue);
            }
            else
            {
                throw new Exception("Could not parse number for field " + name + " with value " + value + ". Floats allow only a dot as decimal separator.");
            }
        }

        public static MySqlParameter GetDateTimeParameter(string name, string value)
        {
            if (DateTime.TryParse(value, out DateTime dateTimeValue))
            {
                return new MySqlParameter(name, dateTimeValue);
            }
            else
            {
                throw new Exception("Could not parse datetime for field " + name + " with value " + value + ". Please use the format yyyy-MM-dd HH:mm:ss");
            }
        }

        public static MySqlParameter GetStringParameter(string name, string value)
        {
            return new MySqlParameter(name, value);
        }

        public static MySqlParameter GetBoolParameter(string name, string value)
        {
            if (bool.TryParse(value, out bool boolValue))
            {
                return new MySqlParameter(name, boolValue);
            }
            else
            {
                throw new Exception("Could not parse bool for field " + name + " with value " + value + ". Please use true or false");
            }
        }
    }
}