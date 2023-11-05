using Discord;
using Discord.Commands;
using Discord.Interactions;
using Discord.Net;
using Discord.WebSocket;
using ETHBot.DataLayer;
using ETHBot.DataLayer.Data.Discord;
using ETHBot.DataLayer.Data.Enums;
using ETHBot.DataLayer.Data.ETH.Food;
using ETHDINFKBot.Data;
using ETHDINFKBot.Drawing;
using ETHDINFKBot.Helpers;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using MySqlConnector;
using RestSharp.Extensions;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Data;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace ETHDINFKBot.Interactions
{
    // Defines the modal that will be sent.
    public class CreateSQLCommandModal : IModal
    {
        public string Title => "Create new SQL command";

        // command name
        [InputLabel("Command Name")]
        [ModalTextInput("commandName", placeholder: "UserQuery", maxLength: 40)]
        public string CommandName { get; set; }

        // description
        [InputLabel("Description")]
        [ModalTextInput("description", placeholder: "Query to get user information", maxLength: 200, style: TextInputStyle.Paragraph)]
        public string Description { get; set; }

        // query
        [InputLabel("SQL Query")]
        [ModalTextInput("sqlQuery", placeholder: @"SELECT * 
        FROM DiscordUsers WHERE DiscordUserId = @Id", style: TextInputStyle.Paragraph)]
        public string SQLQuery { get; set; }
    }
    /*
        public class CreateSQLCommandParameterModal : IModal
        {
            public string Title => "Create new SQL command parameter";

            // command name
            [InputLabel("Command Name")]
            [ModalTextInput("commandName", placeholder: "UserQuery", maxLength: 40)]
            public string ParameterName { get; set; }

            // description
            [InputLabel("Description")]
            [ModalTextInput("description", placeholder: "Query to get user information", maxLength: 200, style: TextInputStyle.Paragraph)]
            public string ParameterType { get; set; }

            // query
            [InputLabel("Default value")]
            [ModalTextInput("defaultValue", placeholder: "1", style: TextInputStyle.Short)]
            public string SQLQuery { get; set; }
        }
    */
    public class SQLInteractions : InteractionModuleBase<SocketInteractionContext>
    {
        [ComponentInteraction("sql-create-command")]
        public async Task CreateSQLCommand()
        {
            //var t = Context.Interaction as SocketMessageComponent;

            //await Context.Interaction.DeferAsync();

            //int parameterCount = int.Parse(t.Data.Values.FirstOrDefault() ?? "0");

            //var i = 0;

            try
            {
                // create modal with one big field for query
                var builder = new ModalBuilder()
                {
                    Title = "Create SQL Command",
                    CustomId = "sql-create-command-modal"
                };

                builder.AddTextInput("Command Name", "CommandName", placeholder: "Command Name", required: true);
                builder.AddTextInput("Description", "Description", placeholder: "Description", required: true);
                builder.AddTextInput("SQL Query", "SQLQuery", placeholder: "SQL Query", style: TextInputStyle.Paragraph, required: true);


                //var modal = builder.Build();

                await Context.Interaction.RespondWithModalAsync<CreateSQLCommandModal>("sql-create-command-modal");
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
        }

        [ModalInteraction("sql-create-command-modal")]
        public async Task CreateCommandModal(CreateSQLCommandModal modal)
        {
            var t = Context.Interaction as SocketMessageComponent;
            var user = Context.Interaction.User;

            string variableRegex = @"^[a-zA-Z_][\w]*$";

            if (!System.Text.RegularExpressions.Regex.IsMatch(modal.CommandName, variableRegex))
            {
                await Context.Interaction.RespondAsync("Command name is not a valid variable name. Please use only letters, numbers and _");
                return;
            }

            try
            {
                // create the sql query in the database
                SQLDBManager sqlDBManager = SQLDBManager.Instance();

                // if command name longer than 40 chars abort
                if (modal.CommandName.Length > 40)
                {
                    await Context.Interaction.RespondAsync("Command name is longer than 40 characters. Please shorten it.");
                    return;
                }

                // regex find all sql parameters in the query
                var regexSQLVariables = new System.Text.RegularExpressions.Regex(@"@([a-zA-Z_][\w]*)");

                var parameters = regexSQLVariables.Matches(modal.SQLQuery).Select(x => x.Groups[0].Value).ToList();

                // if any variable is longer than 40 chars we need to abort
                foreach (var parameter in parameters)
                {
                    if (parameter.Length > 40)
                    {
                        EmbedBuilder embedBuilderParam = new EmbedBuilder()
                        {
                            Title = "Error",
                            Description = "Parameter " + parameter + " is longer than 40 characters. Please shorten it."
                        };

                        await Context.Interaction.RespondAsync("", embed: embedBuilderParam.Build());
                        return;
                    }
                }

                var savedQuery = sqlDBManager.CreateSQLCommand(new SavedQuery()
                {
                    CommandName = modal.CommandName,
                    Description = modal.Description,
                    Content = modal.SQLQuery,
                    DiscordUserId = Context.Interaction.User.Id
                }, user.Id);

                if (savedQuery == null)
                {
                    await Context.Interaction.RespondAsync("Error creating command. Likely you tried to edit someone elses command.");
                    return;
                }

                // create modal with one big field for query
                /*var builder = new ModalBuilder()
                {
                    Title = "Create SQL Command",
                    CustomId = "sql-create-commandparam-modal-" + savedQuery.SavedQueryId
                };*/

                //builder.AddTextInput("Parametername", "parametername", value: firstParameter, required: true);
                //builder.AddTextInput("DataType", "DataType", placeholder: "int or string", required: true);
                //builder.AddTextInput("Default value", "defaultvalue", placeholder: "1", required: false);

                //var modal2 = builder.Build();

                //await Context.Interaction.resp(modal2);

                foreach (var parameter in parameters)
                {
                    // create parameter
                    await sqlDBManager.CreateSavedQueryParameter(savedQuery.SavedQueryId, new SavedQueryParameter()
                    {
                        SavedQueryId = savedQuery.SavedQueryId,
                        ParameterName = parameter,
                        ParameterType = "string",
                        DefaultValue = ""
                    });
                }

                // respond with created command with command name
                //await Context.Interaction.RespondAsync($"Created command {modal.CommandName}");

                EmbedBuilder embedBuilder = new EmbedBuilder()
                {
                    Title = "Created command " + modal.CommandName,
                    Description = "Created command " + modal.CommandName
                };

                await Context.Interaction.RespondAsync("", embed: embedBuilder.Build());
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
        }

        // TODO make button stuff in a helper function for no duplicate code
        [ComponentInteraction("sql-change-datatype-*-*")]
        public async Task ChangeCommandParameterDatatype(int savedQueryId, int SavedQueryParameterId)
        {
            await Context.Interaction.DeferAsync();

            var user = Context.Interaction.User;
            var savedQuery = SQLDBManager.Instance().GetSavedQueryById(savedQueryId);

            if (savedQuery.DiscordUserId != user.Id)
            {
                await Context.Interaction.RespondAsync("You are not allowed to edit parameters for someone elses command.");
                return;
            }

            var savedParameter = SQLDBManager.Instance().GetSavedQueryParameterById(SavedQueryParameterId);

            string newType = "int";

            switch (savedParameter.ParameterType)
            {
                case "int":
                    newType = "string";
                    break;
                case "string":
                    newType = "datetime";
                    break;
                case "datetime":
                    newType = "bool";
                    break;
                case "bool":
                    newType = "int";
                    break;
            }

            savedParameter.ParameterType = newType;

            SQLDBManager.Instance().UpdateSavedQueryParameter(savedParameter);
            var messageBuilder = SQLInteractionHelper.GetSavedSQLCommandParameters(savedQueryId);

            await Context.Interaction.ModifyOriginalResponseAsync(x =>
            {
                x.Components = messageBuilder.Build();
            });
        }

        [ComponentInteraction("sql-view-command-*")]
        public async Task ViewSQLCommand(int savedQueryId)
        {
            try
            {
                var savedQuery = SQLDBManager.Instance().GetSavedQueryById(savedQueryId);
                if (savedQuery == null)
                {
                    await Context.Interaction.RespondAsync("Command not found.");
                    return;
                }

                var user = Context.Interaction.User;
                bool sameUser = savedQuery.DiscordUserId == user.Id;
                (EmbedBuilder embedBuilder, ComponentBuilder messageBuilder) = SQLInteractionHelper.GetSavedInfoQueryById(savedQuery, sameUser);
                await Context.Interaction.RespondAsync("", embed: embedBuilder.Build(), components: messageBuilder.Build());
            }
            catch (Exception e)
            {
                EmbedBuilder embedBuilder = new EmbedBuilder()
                {
                    Title = "Error",
                    Description = e.Message
                };

                await Context.Interaction.RespondAsync("", embed: embedBuilder.Build());
            }
        }

        [ComponentInteraction("sql-execute-command-*")]
        public async Task ExecuteCommand(int savedQueryId)
        {
            //await Context.Interaction.DeferAsync();
            var savedQuery = SQLDBManager.Instance().GetSavedQueryById(savedQueryId);

            var user = Context.Interaction.User;

            // TODO Check owner of interaction
            /*
            if (savedQuery.DiscordUserId != user.Id)
            {
                await Context.Interaction.RespondAsync("You are not allowed to execute someone elses command.");
                return;
            }*/

            var savedQueryParameters = SQLDBManager.Instance().GetQueryParameters(savedQuery);

            if (savedQueryParameters.Count == 0)
            {
                await Context.Interaction.DeferAsync();
                // we can run the command directly
                try
                {
                    var queryResult = await SQLHelper.GetQueryResultsInteraction(Context, savedQuery.Content, true, 100);
                    var resultString = SQLHelper.GetRowStringFromResult(queryResult.Header, queryResult.Data, new List<int>());

                    string additionalString = $"Total row(s) affected: {queryResult.TotalResults.ToString("N0")} QueryTime: {queryResult.Time.ToString("N0")}ms";

                    await Context.Channel.SendMessageAsync(resultString + additionalString);
                }
                catch (Exception e)
                {
                    await Context.Channel.SendMessageAsync(e.Message);
                }
                return;
            }
            else
            {
                // we need to spawn a modal to get the parameters

                if (savedQueryParameters.Count <= 5)
                {
                    // we can do it over modal
                    // todo provide the user maybe a way to also get a template

                    var builder = new ModalBuilder()
                    {
                        Title = "Execute SQL Command",
                        CustomId = "sql-execute-command-modal-" + savedQuery.SavedQueryId
                    };

                    int count = 1;
                    foreach (var parameter in savedQueryParameters)
                    {
                        builder.AddTextInput($"{parameter.ParameterName} ({parameter.ParameterType})",
                                     customId: "parameter" + count, placeholder: parameter.ParameterName,
                                     value: parameter.DefaultValue, required: false);

                        count++;
                    }

                    var modal = builder.Build();

                    await Context.Interaction.RespondWithModalAsync(modal);
                }
                else
                {
                    await Context.Interaction.DeferAsync();
                    // modals only support 5 fields we need to provide a template for the user to run

                    EmbedBuilder embedBuilder = SQLInteractionHelper.GetCommandTemplate(savedQuery.SavedQueryId);
                    await Context.Channel.SendMessageAsync("Modals support only 5 parameters. Execute query with template command", false, embedBuilder.Build());
                }
            }
        }

        [ComponentInteraction("sql-executedraw-cmd-*")]
        public async Task ExecuteDrawCommand(int savedQueryId)
        {
            //await Context.Interaction.DeferAsync();
            var savedQuery = SQLDBManager.Instance().GetSavedQueryById(savedQueryId);

            var user = Context.Interaction.User;

            // TODO Check owner of interaction
            /*
            if (savedQuery.DiscordUserId != user.Id)
            {
                await Context.Interaction.RespondAsync("You are not allowed to execute someone elses command.");
                return;
            }*/

            var savedQueryParameters = SQLDBManager.Instance().GetQueryParameters(savedQuery);

            if (savedQueryParameters.Count == 0)
            {
                await Context.Interaction.DeferAsync();
                // we can run the command directly
                try
                {
                    var queryResult = await SQLHelper.GetQueryResultsInteraction(Context, savedQuery.Content, true, 100);
                    string additionalString = $"Total row(s) affected: {queryResult.TotalResults.ToString("N0")} QueryTime: {queryResult.Time.ToString("N0")}ms";

                    var drawTable = new DrawTable(queryResult.Header, queryResult.Data, additionalString, null);

                    var stream = await drawTable.GetImage();
                    if (stream == null)
                        return;// todo some message

                    await Context.Channel.SendFileAsync(stream, "graph.png", "", false, null, null, false, null);
                    stream.Dispose();
                }
                catch (Exception e)
                {
                    await Context.Channel.SendMessageAsync(e.Message);
                }
                return;
            }
            else
            {
                // we need to spawn a modal to get the parameters

                if (savedQueryParameters.Count <= 5)
                {
                    // we can do it over modal
                    // todo provide the user maybe a way to also get a template

                    var builder = new ModalBuilder()
                    {
                        Title = "Execute SQL Command",
                        CustomId = "sql-executedraw-cmd-modal-" + savedQuery.SavedQueryId
                    };

                    int count = 1;
                    foreach (var parameter in savedQueryParameters)
                    {
                        builder.AddTextInput($"{parameter.ParameterName} ({parameter.ParameterType})",
                                     customId: "parameter" + count, placeholder: parameter.ParameterName,
                                     value: parameter.DefaultValue, required: false);

                        count++;
                    }

                    var modal = builder.Build();

                    await Context.Interaction.RespondWithModalAsync(modal);
                }
                else
                {
                    await Context.Interaction.DeferAsync();
                    // modals only support 5 fields we need to provide a template for the user to run

                    EmbedBuilder embedBuilder = SQLInteractionHelper.GetCommandTemplate(savedQuery.SavedQueryId);
                    await Context.Channel.SendMessageAsync("Modals support only 5 parameters. Execute query with template command", false, embedBuilder.Build());
                }
            }
        }


        [ComponentInteraction("sql-executechart-cmd-*")]
        public async Task ExecuteChartCommand(int savedQueryId)
        {
            //await Context.Interaction.DeferAsync();
            var savedQuery = SQLDBManager.Instance().GetSavedQueryById(savedQueryId);

            var user = Context.Interaction.User;

            // TODO Check owner of interaction
            /*
            if (savedQuery.DiscordUserId != user.Id)
            {
                await Context.Interaction.RespondAsync("You are not allowed to execute someone elses command.");
                return;
            }*/

            var savedQueryParameters = SQLDBManager.Instance().GetQueryParameters(savedQuery);

            if (savedQueryParameters.Count == 0)
            {
                await Context.Interaction.DeferAsync();
                // we can run the command directly
                try
                {
                    var queryResult = await SQLHelper.GetQueryResultsInteraction(Context, savedQuery.Content, true, 10000);
                    // TODO limit the number of rows better
                    //string additionalString = $"Total row(s) affected: {queryResult.TotalResults.ToString("N0")} QueryTime: {queryResult.Time.ToString("N0")}ms";

                    // TODO auto detect chart type

                    PieChart pieChart = new PieChart();

                    // TODO make it not reliant on int parse
                    pieChart.Data(queryResult.Data.Select(x => x.ElementAt(0)).ToList(), queryResult.Data.Select(x => int.Parse(x.ElementAt(1))).ToList());

                    var bitmap = pieChart.GetBitmap();

                    //var drawTable = new DrawTable(queryResult.Header, queryResult.Data, additionalString, null);

                    var stream = CommonHelper.GetStream(bitmap);

                    await Context.Channel.SendFileAsync(stream, "graph.png", "");
                    stream.Dispose();

                    pieChart.Dispose();
                }
                catch (Exception e)
                {
                    await Context.Channel.SendMessageAsync(e.Message);
                }
                return;
            }
            else
            {
                // we need to spawn a modal to get the parameters

                if (savedQueryParameters.Count <= 5)
                {
                    // we can do it over modal
                    // todo provide the user maybe a way to also get a template

                    var builder = new ModalBuilder()
                    {
                        Title = "Execute SQL Command",
                        CustomId = "sql-executechart-cmd-modal-" + savedQuery.SavedQueryId
                    };

                    int count = 1;
                    foreach (var parameter in savedQueryParameters)
                    {
                        builder.AddTextInput($"{parameter.ParameterName} ({parameter.ParameterType})",
                                     customId: "parameter" + count, placeholder: parameter.ParameterName,
                                     value: parameter.DefaultValue, required: false);

                        count++;
                    }

                    var modal = builder.Build();

                    await Context.Interaction.RespondWithModalAsync(modal);
                }
                else
                {
                    await Context.Interaction.DeferAsync();
                    // modals only support 5 fields we need to provide a template for the user to run

                    EmbedBuilder embedBuilder = SQLInteractionHelper.GetCommandTemplate(savedQuery.SavedQueryId);
                    await Context.Channel.SendMessageAsync("Modals support only 5 parameters. Execute query with template command", false, embedBuilder.Build());
                }
            }
        }

        [ComponentInteraction("sql-template-command-*")]
        public async Task GenerateTemplateForCommand(int savedQueryId)
        {
            var savedQuery = SQLDBManager.Instance().GetSavedQueryById(savedQueryId);

            var user = Context.Interaction.User;

            // TODO Check owner of interaction
            /*
            if (savedQuery.DiscordUserId != user.Id)
            {
                await Context.Interaction.RespondAsync("You are not allowed to execute someone elses command.");
                return;
            }*/

            EmbedBuilder embedBuilder = SQLInteractionHelper.GetCommandTemplate(savedQuery.SavedQueryId);

            await Context.Interaction.RespondAsync("", new Embed[] { embedBuilder.Build() });
        }

        [ModalInteraction("sql-execute-command-modal-*")]
        public async Task ExecuteCommandWithParamsModal(int savedQuery, GetParameterValuesModal modal)
        {
            var savedQueryObject = SQLDBManager.Instance().GetSavedQueryById(savedQuery);

            await Context.Interaction.DeferAsync();

            var user = Context.Interaction.User;

            // TODO Check owner of interaction
            /*
            if (savedQueryObject.DiscordUserId != user.Id)
            {
                await Context.Interaction.RespondAsync("You are not allowed to execute someone elses command.");
                return;
            }*/

            var savedQueryParameters = SQLDBManager.Instance().GetQueryParameters(savedQueryObject);

            var queryParameters = new List<MySqlParameter>();

            //var data = Context.Interaction.Data;

            //var json = Newtonsoft.Json.JsonConvert.SerializeObject(data);

            //var values = Newtonsoft.Json.JsonConvert.DeserializeObject<Dictionary<string, string>>(json);

            int count = 1;
            foreach (var parameter in savedQueryParameters)
            {
                string value = modal.GetValueByIndex(count);

                count++;

                if (value == null)
                    value = parameter.DefaultValue;

                if (value == "NULL")
                {
                    queryParameters.Add(new MySqlParameter(parameter.ParameterName, DBNull.Value));
                    continue;
                }

                switch (parameter.ParameterType)
                {
                    // todo double, long, ulong tests
                    case "int":
                        queryParameters.Add(SQLInteractionHelper.GetNumberParameter(parameter.ParameterName, value));
                        break;
                    case "string":
                        queryParameters.Add(SQLInteractionHelper.GetStringParameter(parameter.ParameterName, value));
                        break;
                    case "datetime":
                        queryParameters.Add(SQLInteractionHelper.GetDateTimeParameter(parameter.ParameterName, value));
                        break;
                    case "bool":
                        queryParameters.Add(SQLInteractionHelper.GetBoolParameter(parameter.ParameterName, value));
                        break;
                }
            }

            try
            {
                var queryResult = await SQLHelper.GetQueryResultsInteraction(Context, savedQueryObject.Content, true, 100, parameters: queryParameters);
                var resultString = SQLHelper.GetRowStringFromResult(queryResult.Header, queryResult.Data, new List<int>());

                string additionalString = $"Total row(s) affected: {queryResult.TotalResults.ToString("N0")} QueryTime: {queryResult.Time.ToString("N0")}ms";

                await Context.Interaction.Channel.SendMessageAsync(resultString + additionalString);
            }
            catch (Exception e)
            {
                await Context.Interaction.Channel.SendMessageAsync(e.Message);
            }
        }


        [ModalInteraction("sql-executedraw-cmd-modal-*")]
        public async Task ExecuteCommandWithParamsModalDraw(int savedQuery, GetParameterValuesModal modal)
        {
            var savedQueryObject = SQLDBManager.Instance().GetSavedQueryById(savedQuery);

            await Context.Interaction.DeferAsync();

            var user = Context.Interaction.User;

            // TODO Check owner of interaction
            /*
            if (savedQueryObject.DiscordUserId != user.Id)
            {
                await Context.Interaction.RespondAsync("You are not allowed to execute someone elses command.");
                return;
            }*/

            var savedQueryParameters = SQLDBManager.Instance().GetQueryParameters(savedQueryObject);

            var queryParameters = new List<MySqlParameter>();

            //var data = Context.Interaction.Data;

            //var json = Newtonsoft.Json.JsonConvert.SerializeObject(data);

            //var values = Newtonsoft.Json.JsonConvert.DeserializeObject<Dictionary<string, string>>(json);

            int count = 1;
            foreach (var parameter in savedQueryParameters)
            {
                string value = modal.GetValueByIndex(count);

                count++;

                if (value == null)
                    value = parameter.DefaultValue;

                if (value == "NULL")
                {
                    queryParameters.Add(new MySqlParameter(parameter.ParameterName, DBNull.Value));
                    continue;
                }

                switch (parameter.ParameterType)
                {
                    // todo double, long, ulong tests
                    case "int":
                        queryParameters.Add(SQLInteractionHelper.GetNumberParameter(parameter.ParameterName, value));
                        break;
                    case "string":
                        queryParameters.Add(SQLInteractionHelper.GetStringParameter(parameter.ParameterName, value));
                        break;
                    case "datetime":
                        queryParameters.Add(SQLInteractionHelper.GetDateTimeParameter(parameter.ParameterName, value));
                        break;
                    case "bool":
                        queryParameters.Add(SQLInteractionHelper.GetBoolParameter(parameter.ParameterName, value));
                        break;
                }
            }

            try
            {
                var queryResult = await SQLHelper.GetQueryResultsInteraction(Context, savedQueryObject.Content, true, 100, parameters: queryParameters);
                string additionalString = $"Total row(s) affected: {queryResult.TotalResults.ToString("N0")} QueryTime: {queryResult.Time.ToString("N0")}ms";

                var drawTable = new DrawTable(queryResult.Header, queryResult.Data, additionalString, null);

                var stream = await drawTable.GetImage();
                if (stream == null)
                    return;// todo some message

                await Context.Interaction.Channel.SendFileAsync(stream, "graph.png", "", false, null, null, false, null);
                stream.Dispose();
            }
            catch (Exception e)
            {
                await Context.Interaction.Channel.SendMessageAsync(e.Message);
            }
        }

        [ModalInteraction("sql-executechart-cmd-modal-*")]
        public async Task ExecuteCommandWithParamsModalChart(int savedQuery, GetParameterValuesModal modal)
        {
            var savedQueryObject = SQLDBManager.Instance().GetSavedQueryById(savedQuery);

            await Context.Interaction.DeferAsync();

            var user = Context.Interaction.User;

            // TODO Check owner of interaction
            /*
            if (savedQueryObject.DiscordUserId != user.Id)
            {
                await Context.Interaction.RespondAsync("You are not allowed to execute someone elses command.");
                return;
            }*/

            var savedQueryParameters = SQLDBManager.Instance().GetQueryParameters(savedQueryObject);

            var queryParameters = new List<MySqlParameter>();

            //var data = Context.Interaction.Data;

            //var json = Newtonsoft.Json.JsonConvert.SerializeObject(data);

            //var values = Newtonsoft.Json.JsonConvert.DeserializeObject<Dictionary<string, string>>(json);

            int count = 1;
            foreach (var parameter in savedQueryParameters)
            {
                string value = modal.GetValueByIndex(count);

                count++;

                if (value == null)
                    value = parameter.DefaultValue;

                if (value == "NULL")
                {
                    queryParameters.Add(new MySqlParameter(parameter.ParameterName, DBNull.Value));
                    continue;
                }

                switch (parameter.ParameterType)
                {
                    // todo double, long, ulong tests
                    case "int":
                        queryParameters.Add(SQLInteractionHelper.GetNumberParameter(parameter.ParameterName, value));
                        break;
                    case "string":
                        queryParameters.Add(SQLInteractionHelper.GetStringParameter(parameter.ParameterName, value));
                        break;
                    case "datetime":
                        queryParameters.Add(SQLInteractionHelper.GetDateTimeParameter(parameter.ParameterName, value));
                        break;
                    case "bool":
                        queryParameters.Add(SQLInteractionHelper.GetBoolParameter(parameter.ParameterName, value));
                        break;
                }
            }

            try
            {
                var queryResult = await SQLHelper.GetQueryResultsInteraction(Context, savedQueryObject.Content, true, 10000, parameters: queryParameters);
                // TODO limit the number of rows better
                //string additionalString = $"Total row(s) affected: {queryResult.TotalResults.ToString("N0")} QueryTime: {queryResult.Time.ToString("N0")}ms";

                // TODO auto detect chart type

                PieChart pieChart = new PieChart();

                // TODO make it not reliant on int parse
                pieChart.Data(queryResult.Data.Select(x => x.ElementAt(0)).ToList(), queryResult.Data.Select(x => int.Parse(x.ElementAt(1))).ToList());

                var bitmap = pieChart.GetBitmap();

                //var drawTable = new DrawTable(queryResult.Header, queryResult.Data, additionalString, null);

                var stream = CommonHelper.GetStream(bitmap);

                await Context.Channel.SendFileAsync(stream, "graph.png", "");
                stream.Dispose();

                pieChart.Dispose();
            }
            catch (Exception e)
            {
                await Context.Interaction.Channel.SendMessageAsync(e.Message);
            }
        }

        [ComponentInteraction("sql-change-defaultvalue-*-*")]
        public async Task ChangeCommandParameterDefaultValue(int savedQueryId, int SavedQueryParameterId)
        {
            //await Context.Interaction.DeferAsync();

            var user = Context.Interaction.User;

            var savedQuery = SQLDBManager.Instance().GetSavedQueryById(savedQueryId);

            if (savedQuery.DiscordUserId != user.Id)
            {
                await Context.Interaction.RespondAsync("You are not allowed to edit parameters for someone elses command.");
                return;
            }

            var savedParameter = SQLDBManager.Instance().GetSavedQueryParameterById(SavedQueryParameterId);

            // create modal with one big field for query
            // TODO limit char length for title around 40 else the whole thing just breaks into 1000000 pieces and cries on the floor
            var builder = new ModalBuilder()
            {
                Title = "Change default for " + savedParameter.ParameterName,
                CustomId = "sql-change-defaultvalue-modal-" + savedQuery.SavedQueryId + "-" + savedParameter.SavedQueryParameterId,
            };

            builder.AddTextInput("Default value", "default_value", value: savedParameter.DefaultValue, required: true);

            var modal2 = builder.Build();


            await Context.Interaction.RespondWithModalAsync(modal2);
        }

        [ModalInteraction("sql-change-defaultvalue-modal-*-*")]
        public async Task ChangeDefaultValueModel(int savedQueryId, int savedQueryParameterId, DefaultValueModal modal)
        {
            //await Context.Interaction.DeferAsync();
            var data = Context.Interaction.Data;

            var savedQuery = SQLDBManager.Instance().GetSavedQueryById(savedQueryId);

            if (savedQuery == null)
            {
                await Context.Interaction.RespondAsync("Command not found.");
                return;
            }

            if (savedQuery.DiscordUserId != Context.Interaction.User.Id)
            {
                await Context.Interaction.RespondAsync("You are not allowed to edit parameters for someone elses command.");
                return;
            }

            var savedParameter = SQLDBManager.Instance().GetSavedQueryParameterById(savedQueryParameterId);

            savedParameter.DefaultValue = modal.DefaultValue;

            SQLDBManager.Instance().UpdateSavedQueryParameter(savedParameter);

            EmbedBuilder embedBuilder = new EmbedBuilder()
            {
                Title = "Updated default value for parameter " + savedParameter.ParameterName,
                Description = "Updated default value for parameter " + savedParameter.ParameterName + " to " + savedParameter.DefaultValue
            };

            await Context.Interaction.RespondAsync("", embed: embedBuilder.Build());

            //await Context.Interaction.RespondAsync("Updated default value for parameter " + savedParameter.ParameterName + " to " + savedParameter.DefaultValue);
        }

        [ComponentInteraction("sql-edit-cmd-*")]
        public async Task EditSQLCommand(int savedQueryId)
        {
            try
            {
                var savedQuery = SQLDBManager.Instance().GetSavedQueryById(savedQueryId);

                var user = Context.Interaction.User;

                if (savedQuery.DiscordUserId != user.Id)
                {
                    await Context.Interaction.RespondAsync("You are not allowed to edit someone elses command.");
                    return;
                }

                var builder = new ModalBuilder()
                {
                    Title = "Create SQL Command",
                    CustomId = "sql-edit-cmd-modal-" + savedQuery.SavedQueryId
                };

                builder.AddTextInput("Command Name", "commandName", placeholder: "Command Name", value: savedQuery.CommandName, required: true);
                builder.AddTextInput("Description", "description", placeholder: "Description", value: savedQuery.Description, style: TextInputStyle.Paragraph, required: true);
                builder.AddTextInput("SQL Query", "sqlQuery", placeholder: "SQL Query", value: savedQuery.Content, style: TextInputStyle.Paragraph, required: true);

                var modal2 = builder.Build();

                await Context.Interaction.RespondWithModalAsync(modal2);
            }
            catch (Exception e)
            {
                await Context.Interaction.RespondAsync(e.Message);
                string fullError = e.ToString();

                // upload error as text file
                await Context.Interaction.Channel.SendFileAsync(new System.IO.MemoryStream(Encoding.UTF8.GetBytes(fullError)), "error.txt");

                if (e is HttpException httpException)
                {
                    foreach (var item in httpException.Errors)
                    {
                        await Context.Interaction.Channel.SendMessageAsync("Path: " + item.Path);
                    }
                }
            }
        }

        [ModalInteraction("sql-edit-cmd-modal-*")]
        public async Task EditSQLCommand(int savedQueryId, CreateSQLCommandModal modal)
        {
            var savedQuery = SQLDBManager.Instance().GetSavedQueryById(savedQueryId);
            var sqlDBManager = SQLDBManager.Instance();

            var user = Context.Interaction.User;

            if (savedQuery.DiscordUserId != user.Id)
            {
                await Context.Interaction.RespondAsync("You are not allowed to edit someone elses command.");
                return;
            }


            savedQuery.CommandName = modal.CommandName;
            savedQuery.Description = modal.Description;
            savedQuery.Content = modal.SQLQuery;


            // TODO Duplicate code from create command
            // regex find all sql parameters in the query
            var regexSQLVariables = new System.Text.RegularExpressions.Regex(@"@([a-zA-Z_][\w]*)");

            var parameters = regexSQLVariables.Matches(modal.SQLQuery).Select(x => x.Groups[0].Value).ToList();

            // if any variable is longer than 40 chars we need to abort
            foreach (var parameter in parameters)
            {
                if (parameter.Length > 40)
                {
                    EmbedBuilder embedBuilderParam = new EmbedBuilder()
                    {
                        Title = "Error",
                        Description = "Parameter " + parameter + " is longer than 40 characters. Please shorten it."
                    };

                    await Context.Interaction.RespondAsync("", embed: embedBuilderParam.Build());
                    return;
                }
            }

            foreach (var parameter in parameters)
            {
                // create parameter
                await sqlDBManager.CreateSavedQueryParameter(savedQuery.SavedQueryId, new SavedQueryParameter()
                {
                    SavedQueryId = savedQuery.SavedQueryId,
                    ParameterName = parameter,
                    ParameterType = "string",
                    DefaultValue = ""
                });
            }

            // find parameters that may need to be deleted because no longer used
            var savedQueryParameters = sqlDBManager.GetQueryParameters(savedQuery);

            List<string> deletedParameters = new List<string>();

            foreach (var savedQueryParameter in savedQueryParameters)
            {
                if (!parameters.Contains(savedQueryParameter.ParameterName))
                {
                    // delete parameter
                    sqlDBManager.DeleteSavedQueryParameter(savedQueryParameter.SavedQueryParameterId);
                    deletedParameters.Add(savedQueryParameter.ParameterName);
                }
            }


            await sqlDBManager.UpdateSQLCommand(savedQuery, user.Id);

            EmbedBuilder embedBuilder = new EmbedBuilder()
            {
                Title = "Updated command " + savedQuery.CommandName,
                Description = "Updated command " + savedQuery.CommandName
            };

            foreach (var deletedParameter in deletedParameters)
            {
                embedBuilder.AddField("Deleted parameter", deletedParameter);
            }

            await Context.Interaction.RespondAsync("", embed: embedBuilder.Build());

            //SQLDBManager.Instance().UpdateSQLCommand(savedQuery, user.Id);

            //await Context.Interaction.RespondAsync("Updated command name to " + savedQuery.CommandName);
        }

        [ComponentInteraction("sql-delete-cmd-*")]
        public async Task DeleteSQLCommand(int savedQueryId)
        {
            var savedQuery = SQLDBManager.Instance().GetSavedQueryById(savedQueryId);
            var user = Context.Interaction.User;

            if (savedQuery.DiscordUserId != user.Id)
            {
                await Context.Interaction.RespondAsync("You are not allowed to delete someone elses command.");
                return;
            }

            // spawn modal with command name to reconfirm deletion
            await Context.Interaction.RespondWithModalAsync<DeleteCommandModal>("sql-delete-cmd-modal-" + savedQuery.SavedQueryId);

            //bool success = await SQLDBManager.Instance().DeleteSavedQuery(savedQuery.SavedQueryId, user.Id);
            //await Context.Interaction.RespondAsync("Deleted command " + savedQuery.CommandName + " " + (success ? "successfully" : "unsuccessfully"));
        }

        [ModalInteraction("sql-delete-cmd-modal-*")]
        public async Task DeleteSQLCommandModal(int savedQueryId, DeleteCommandModal modal)
        {
            var savedQuery = SQLDBManager.Instance().GetSavedQueryById(savedQueryId);
            var user = Context.Interaction.User;

            if (savedQuery.DiscordUserId != user.Id)
            {
                await Context.Interaction.RespondAsync("You are not allowed to delete someone elses command.");
                return;
            }

            if (modal.CommandName != savedQuery.CommandName)
            {
                await Context.Interaction.RespondAsync("Command name does not match.");
                return;
            }

            bool success = await SQLDBManager.Instance().DeleteSavedQuery(savedQuery.SavedQueryId, user.Id);
            //await Context.Interaction.RespondAsync("Deleted command " + savedQuery.CommandName + " " + (success ? "successfully" : "unsuccessfully"));

            EmbedBuilder embedBuilder = new EmbedBuilder()
            {
                Title = "Deleted command " + savedQuery.CommandName,
                Description = "Deleted command " + savedQuery.CommandName + " " + (success ? "successfully" : "unsuccessfully")
            };

            await Context.Interaction.RespondAsync("", embed: embedBuilder.Build());
        }

        [ComponentInteraction("sql-change-dt-cmd-*")]
        public async Task ChangeSQLCommandParameterDatatype(int savedQueryId)
        {
            try
            {
                var savedQuery = SQLDBManager.Instance().GetSavedQueryById(savedQueryId);

                var user = Context.Interaction.User;

                if (savedQuery.DiscordUserId != user.Id)
                {
                    await Context.Interaction.RespondAsync("You are not allowed to edit someone elses command.");
                    return;
                }

                var messageBuilder = SQLInteractionHelper.GetSavedSQLCommandParameters(savedQueryId);

                EmbedBuilder embedBuilder = new EmbedBuilder();
                embedBuilder.WithTitle($"Command: {savedQuery.CommandName}");
                embedBuilder.WithDescription(@"Change datatype for each parameter, first row is for datatype. Second row for default value.
            Red: int
            Green: string
            Gray: datetime
            Blue: bool");

                embedBuilder.WithColor(255, 0, 0);
                embedBuilder.WithAuthor(Context.Interaction.User);

                await Context.Interaction.RespondAsync(embeds: new Embed[] { embedBuilder.Build() }, components: messageBuilder.Build());
            }
            catch (Exception e)
            {
                await Context.Interaction.RespondAsync(e.Message);
                string fullError = e.ToString();

                // upload error as text file
                await Context.Interaction.Channel.SendFileAsync(new System.IO.MemoryStream(Encoding.UTF8.GetBytes(fullError)), "error.txt");

                if (e is HttpException httpException)
                {
                    foreach (var item in httpException.Errors)
                    {
                        await Context.Interaction.Channel.SendMessageAsync("Path: " + item.Path);
                    }
                }
            }
        }

        [ComponentInteraction("sql-change-defaultvalue-cmd-*")]
        public async Task ChangeSQLCommandParameterDefaultValue(int savedQueryId)
        {
            var savedQuery = SQLDBManager.Instance().GetSavedQueryById(savedQueryId);

            var user = Context.Interaction.User;

            if (savedQuery.DiscordUserId != user.Id)
            {
                await Context.Interaction.RespondAsync("You are not allowed to edit someone elses command.");
                return;
            }

            var messageBuilder = SQLInteractionHelper.GetSavedSQLCommandParameters(savedQueryId);

            await Context.Interaction.RespondAsync(components: messageBuilder.Build());
        }
    }


    public class DefaultValueModal : IModal
    {
        public string Title => "Change default value for parameter";
        // Strings with the ModalTextInput attribute will automatically become components.
        [InputLabel("Default value")]
        [ModalTextInput("default_value", placeholder: "Enter default value", maxLength: 40)]
        public string DefaultValue { get; set; }
    }

    public class DeleteCommandModal : IModal
    {
        public string Title => "Command name";
        // Strings with the ModalTextInput attribute will automatically become components.
        [InputLabel("Default value")]
        [ModalTextInput("command_name", placeholder: "Enter command name to delete", maxLength: 40)]
        public string CommandName { get; set; }
    }

    public class GetParameterValuesModal : IModal
    {
        public string Title => "Command name";

        // parameter 1
        [InputLabel("Parameter 1")]
        [ModalTextInput("parameter1", placeholder: "Enter parameter 1", maxLength: 40)]
        public string Parameter1 { get; set; }

        // parameter 2
        [InputLabel("Parameter 2")]
        [ModalTextInput("parameter2", placeholder: "Enter parameter 2", maxLength: 40)]
        public string Parameter2 { get; set; }

        // parameter 3
        [InputLabel("Parameter 3")]
        [ModalTextInput("parameter3", placeholder: "Enter parameter 3", maxLength: 40)]
        public string Parameter3 { get; set; }

        // parameter 4
        [InputLabel("Parameter 4")]
        [ModalTextInput("parameter4", placeholder: "Enter parameter 4", maxLength: 40)]
        public string Parameter4 { get; set; }

        // parameter 5
        [InputLabel("Parameter 5")]
        [ModalTextInput("parameter5", placeholder: "Enter parameter 5", maxLength: 40)]
        public string Parameter5 { get; set; }


        public string GetValueByIndex(int index)
        {
            switch (index)
            {
                case 1:
                    return Parameter1;
                case 2:
                    return Parameter2;
                case 3:
                    return Parameter3;
                case 4:
                    return Parameter4;
                case 5:
                    return Parameter5;
                default:
                    throw new Exception("Index out of range");
            }
        }
    }
}