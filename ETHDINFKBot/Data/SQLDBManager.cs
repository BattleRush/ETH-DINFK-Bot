using ETHBot.DataLayer;
using ETHBot.DataLayer.Data.Discord;
using ETHBot.DataLayer.Data.ETH.Food;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Formats.Asn1;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ETHDINFKBot.Data
{
    public class SQLDBManager
    {
        private static SQLDBManager _instance;
        private static object syncLock = new object();
        private readonly ILogger _logger = new Logger<SQLDBManager>(Program.Logger);

        public static SQLDBManager Instance()
        {
            lock (syncLock)
            {
                if (_instance == null)
                {
                    _instance = new SQLDBManager();
                }
            }

            return _instance;
        }



        public SavedQuery GetSavedQueryById(int id)
        {
            using (var db = new ETHBotDBContext())
            {
                return db.SavedQueries.FirstOrDefault(x => x.SavedQueryId == id);
            }
        }

        public SavedQueryParameter GetSavedQueryParameterById(int id)
        {
            using (var db = new ETHBotDBContext())
            {
                return db.SavedQueryParameters.FirstOrDefault(x => x.SavedQueryParameterId == id);
            }
        }

        // delete saved query with all parameters
        public async Task<bool> DeleteSavedQuery(int savedQueryId, ulong discordUserId)
        {
            using (var db = new ETHBotDBContext())
            {
                try
                {
                    var savedQuery = db.SavedQueries.FirstOrDefault(x => x.SavedQueryId == savedQueryId);
                    if (savedQuery == null)
                        return false;

                    // prevent delete of someone elses command
                    if (savedQuery.DiscordUserId != discordUserId)
                        return false;

                    var savedQueryParameters = db.SavedQueryParameters.Where(x => x.SavedQueryId == savedQueryId).ToList();

                    db.SavedQueryParameters.RemoveRange(savedQueryParameters);
                    db.SavedQueries.Remove(savedQuery);
                    db.SaveChanges();

                    return true;
                }
                catch (Exception e)
                {
                    _logger.LogError(e, "Error deleting saved query");
                    return false;
                }
            }
        }

        // create Update db entry
        public async Task<SavedQuery> UpdateSQLCommand(SavedQuery command, ulong discordUserId)
        {
            using (var db = new ETHBotDBContext())
            {
                try
                {
                    var savedQuery = db.SavedQueries.FirstOrDefault(x => x.SavedQueryId == command.SavedQueryId);
                    if (savedQuery == null)
                    {
                        CreateSQLCommand(command, discordUserId);
                        return GetSavedQueryByCommandName(command.CommandName);
                    }

                    // prevent edit of someone elses command
                    if (savedQuery.DiscordUserId != discordUserId)
                        return null;

                    savedQuery.CommandName = command.CommandName;
                    savedQuery.Content = command.Content;
                    savedQuery.Description = command.Description;

                    db.SaveChanges();

                    return GetSavedQueryByCommandName(command.CommandName);
                }
                catch (Exception e)
                {
                    _logger.LogError(e, "Error updating sql command");
                    return null;
                }
            }
        }

        public SavedQueryParameter UpdateSavedQueryParameter(SavedQueryParameter parameter)
        {
            using (var db = new ETHBotDBContext())
            {
                try
                {
                    var savedQueryParameter = db.SavedQueryParameters.FirstOrDefault(x => x.SavedQueryParameterId == parameter.SavedQueryParameterId);
                    if (savedQueryParameter == null)
                    {
                        CreateSavedQueryParameter(parameter.SavedQueryId, parameter);
                        return GetSavedQueryParameterById(parameter.SavedQueryParameterId);
                    }

                    savedQueryParameter.ParameterName = parameter.ParameterName;
                    savedQueryParameter.ParameterType = parameter.ParameterType;
                    savedQueryParameter.DefaultValue = parameter.DefaultValue;

                    db.SaveChanges();

                    return GetSavedQueryParameterById(parameter.SavedQueryParameterId);
                }
                catch (Exception e)
                {
                    _logger.LogError(e, "Error updating saved query parameter");
                    return null;
                }
            }
        }

        public async Task<List<SavedQuery>> GetAllSQLCommands()
        {
            using (var db = new ETHBotDBContext())
            {
                return db.SavedQueries.ToList();
            }
        }

        public async Task<List<SavedQuery>> GetAllSQLCommands(ulong userId)
        {
            using (var db = new ETHBotDBContext())
            {
                return db.SavedQueries.Where(x => x.DiscordUserId == userId).ToList();
            }
        }

        public async Task<List<SavedQuery>> GetAllSQLCommands(string search)
        {
            using (var db = new ETHBotDBContext())
            {
                return db.SavedQueries.Where(x => x.CommandName.ToLower().Contains(search.ToLower())).ToList();
            }
        }

        public SavedQuery GetSavedQueryByCommandName(string commandName)
        {
            using (var db = new ETHBotDBContext())
            {
                return db.SavedQueries.FirstOrDefault(x => x.CommandName == commandName);
            }
        }

        // create db entry
        public SavedQuery CreateSQLCommand(SavedQuery command, ulong callerId)
        {
            using (var db = new ETHBotDBContext())
            {
                try
                {
                    if (db.SavedQueries.Any(x => x.CommandName == command.CommandName))
                    {
                        // check if caller id is equal t othe creator id
                        if (db.SavedQueries.FirstOrDefault(x => x.CommandName == command.CommandName).DiscordUserId != callerId)
                        {
                            return null;
                        }
                        // update command
                        var oldCommand = db.SavedQueries.FirstOrDefault(x => x.CommandName == command.CommandName);
                        oldCommand.Content = command.Content;
                        oldCommand.Description = command.Description;

                        db.SavedQueries.Update(oldCommand);
                        db.SaveChanges();

                        return GetSavedQueryByCommandName(command.CommandName);
                    }

                    db.SavedQueries.Add(command);
                    db.SaveChanges();

                    return GetSavedQueryByCommandName(command.CommandName);
                }
                catch (Exception e)
                {
                    _logger.LogError(e, "Error creating sql command");
                    return null;
                }
            }
        }

        // get query parameters
        public List<SavedQueryParameter> GetQueryParameters(SavedQuery command)
        {
            using (var db = new ETHBotDBContext())
            {
                try
                {
                    return db.SavedQueryParameters.Where(x => x.SavedQueryId == command.SavedQueryId).ToList();
                }
                catch (Exception e)
                {
                    _logger.LogError(e, "Error getting query parameters");
                    return null;
                }
            }
        }

        // create saved query parameter
        public async Task<bool> CreateSavedQueryParameter(int savedQueryId, SavedQueryParameter parameter)
        {
            using (var db = new ETHBotDBContext())
            {
                try
                {
                    // check if for the query if a parameter with that name exists
                    if (db.SavedQueryParameters.Any(x => x.SavedQueryId == savedQueryId && x.ParameterName == parameter.ParameterName))
                        return false;

                    db.SavedQueryParameters.Add(parameter);
                    db.SaveChanges();

                    return true;
                }
                catch (Exception e)
                {
                    _logger.LogError(e, "Error creating saved query parameter");
                    return false;
                }
            }
        }

        public bool DeleteSavedQueryParameter(int savedQueryId)
        {
            using (var db = new ETHBotDBContext())
            {
                try
                {
                    var savedQueryParameter = db.SavedQueryParameters.FirstOrDefault(x => x.SavedQueryParameterId == savedQueryId);
                    if (savedQueryParameter == null)
                        return false;

                    db.SavedQueryParameters.Remove(savedQueryParameter);
                    db.SaveChanges();

                    return true;
                }
                catch (Exception e)
                {
                    _logger.LogError(e, "Error deleting saved query parameter");
                    return false;
                }
            }
        }

    }
}
